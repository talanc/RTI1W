global using RTI1W;
global using System.Numerics;
global using static RTI1W.Helpers;
global using static System.Console;
global using static System.MathF;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.Runtime.Intrinsics;

//
// Cmdline
//

var rootCommand = new RootCommand();

// gather existing options before we add our own...
var builtInOptions = rootCommand.Options
    .SelectMany(opt => Enumerable.Empty<string>()
        .Append(opt.Name)
        .Concat(opt.Aliases))
    .ToList();

Option<T> addOption<T>(string name, string description, T? defaultValue = default)
{
    var option = new Option<T>(name)
    {
        Description = description,
    };
    if (!EqualityComparer<T>.Default.Equals(default, defaultValue))
    {
        option.DefaultValueFactory = ar => defaultValue!;
    }
    rootCommand.Add(option);
    return option;
}

var optOutput = addOption("--output", "Output filename (P3 .ppm file)", "image.ppm");
var optWidth = addOption("--width", "Output image width", 192);
var optHeight = addOption("--height", "Output image height", 128);
var optSamples = addOption("--samples", "Samples per pixel", 100);
var optMaxDepth = addOption("--max-depth", "Max depth / bounces per ray", 30);
var optThreads = addOption("--threads", "Number of parallel threads", 4);
var optBvh = addOption("--bvh", "Bounding volume hierarchy structure", BvhMode.Tree);
var optSeed = addOption("--seed", "Random seed (advanced use for testing)", 0);
var optOpen = addOption("--open", "Open output image at end", false);
var optVerbosity = addOption("--verbosity", "Verbosity output mode", Verbosity.Normal);
var optQuiet = addOption("-q", "Quiet output (--verbosity Quiet)", false);
var optVerbose = addOption("-v", "Verbose output (--verbosity Verbose)", false);

// Ensure only one of the verbosity options are used
rootCommand.Validators.Add(result =>
{
    var opts = new Option[] { optVerbosity, optVerbose, optQuiet };

    var num = result.Children
        .OfType<OptionResult>()
        .Count(curr => opts.Contains(curr.Option));

    if (num > 1)
    {
        var names = string.Join(", ", opts.Select(curr => curr.Name));
        result.AddError($"Only one of the following options can be used: {names}");
    }
});

var parseResult = rootCommand.Parse(args);

if (parseResult.Errors.Count > 0 ||
    parseResult.Tokens.Any(curr => builtInOptions.Contains(curr.Value)))
{
    return parseResult.Invoke();
}

var outputPath = parseResult.GetRequiredValue(optOutput);
var imageWidth = parseResult.GetValue(optWidth);
var imageHeight = parseResult.GetValue(optHeight);
var samplesPerPixel = parseResult.GetValue(optSamples);
var maxDepth = parseResult.GetValue(optMaxDepth);
var numThreads = parseResult.GetValue(optThreads);
var bvh = parseResult.GetValue(optBvh);
var openOutput = parseResult.GetValue(optOpen);

Verbosity verbosity;
if (parseResult.GetValue(optQuiet)) verbosity = Verbosity.Quiet;
else if (parseResult.GetValue(optVerbose)) verbosity = Verbosity.Diagnostic;
else verbosity = parseResult.GetValue(optVerbosity);

if (verbosity >= Verbosity.Diagnostic) Metrics.ActivateEvents();
RandomSeed = parseResult.GetValue(optSeed);

var samplesPerPixelInv = 1.0f / samplesPerPixel;

//
// World
//

Metrics.StartTimer("CreateScene");
var world = RandomScene();
Metrics.StopTimer();

//
// Camera
//

var aspectRatio = Round((float)imageWidth / imageHeight, 1);
var lookFrom = P3(13, 2, 3);
var lookAt = P3(0, 0, 0);
var vUp = P3(0, 1, 0);
var distToFocus = 10.0f;
var aperture = 0.1f;
var camera = new Camera(lookFrom, lookAt, vUp, 20, aspectRatio, aperture, distToFocus);

//
// Render
//

var image = new int[imageWidth * imageHeight];

int[] scanlinesCompleted = [];
if (verbosity >= Verbosity.Normal)
{
    scanlinesCompleted = new int[numThreads];
}

var scanlinesRemaining = imageHeight;

Metrics.StartTimer("Render");


if (numThreads == 1)
{
    for (var i = 0; i < imageHeight; i += 1)
    {
        Scanline(0, i);
    }
}
else
{
    var tasks = new Task[numThreads];
    for (var i = 0; i < numThreads; i++)
    {
        var localIdx = i;
        tasks[localIdx] = Task.Run(() =>
        {
            for (var j = localIdx; j < imageHeight; j += numThreads)
            {
                Scanline(localIdx, j);
            }
        });
    }
    Task.WaitAll(tasks);
}

void Scanline(int threadId, int j)
{
    if (verbosity >= Verbosity.Normal)
    {
        scanlinesCompleted[threadId]++;

        var numLeft = Interlocked.Decrement(ref scanlinesRemaining);

        // This is the same as:
        //   WriteLine($"Id={id,-4}Elem={j,-5}NumLeft={numLeft}")
        // But its allocation free!
        Span<char> buf = stackalloc char[128];
        var pos = 0;
        Append(buf, ref pos, "Id", threadId, 4);
        Append(buf, ref pos, "Elem", j, 5);
        Append(buf, ref pos, "NumLeft", numLeft, 0);
        WriteLine(buf[..pos]);

        static void Append(Span<char> buf, ref int pos, string name, int value, int valuePadding)
        {
            // Name
            name.AsSpan().CopyTo(buf[pos..]);
            pos += name.Length;

            // =
            buf[pos] = '=';
            pos++;

            // Value
            value.TryFormat(buf[pos..], out var w);
            pos += w;

            // Padding
            if (w < valuePadding)
            {
                for (var p = 0; p < valuePadding - w; p++)
                {
                    buf[pos] = ' ';
                    pos++;
                }
            }
        }
    }

    var pixY = imageHeight - 1 - j;
    var dataY = j;

    for (var i = 0; i < imageWidth; i++)
    {
        var pixX = i;
        var dataX = i;

        var pixelColor = C3(0, 0, 0);
        for (var s = 0; s < samplesPerPixel; s++)
        {
            var u = (pixX + RandomValue()) / (imageWidth - 1);
            var v = (pixY + RandomValue()) / (imageHeight - 1);
            var r = camera.GetRay(u, v);
            pixelColor += RayColor(r, maxDepth);
        }

        SetPixel(image, dataX, dataY, pixelColor);
    }
};

Metrics.StopTimer();
Metrics.StartTimer("Save");

// Write PPM/P3 file
if (verbosity >= Verbosity.Normal) WriteLine("Creating image");
using (var sw = new StreamWriter(outputPath))
{
    sw.WriteLine("P3");
    sw.Write(imageWidth);
    sw.Write(' ');
    sw.WriteLine(imageHeight);
    sw.WriteLine("255");
    for (var i = 0; i < image.Length; i++)
    {
        var d = image[i];
        var r = (d >> 16) & 0xFF;
        var g = (d >> 8) & 0xFF;
        var b = (d >> 0) & 0xFF;
        sw.Write(r);
        sw.Write(' ');
        sw.Write(g);
        sw.Write(' ');
        sw.WriteLine(b);
    }
}

Metrics.StopTimer();

Metrics.Display();

if (verbosity >= Verbosity.Normal)
{
    WriteLine($"Scanlines:");
    for (var i = 0; i < scanlinesCompleted.Length; i++)
    {
        WriteLine($"- Thread {i}: {scanlinesCompleted[i]}");
    }
}

if (openOutput)
{
    Process.Start(new ProcessStartInfo()
    {
        FileName = outputPath,
        UseShellExecute = true,
    });
}

return 0;

Vector3 RayColor(Ray r, int depth)
{
    if (depth <= 0)
    {
        return ColorBlack;
    }

    if (world.Hit(r, 0.001f, float.PositiveInfinity, out var hit))
    {
        var matRecMaybe = hit.Material.Scatter(r, hit);
        if (matRecMaybe.HasValue)
        {
            var matRec = matRecMaybe.Value;
            return matRec.Attenuation * RayColor(matRec.Scattered, depth - 1);
        }
        return ColorBlack;
    }

    var unitDir = UnitVector(r.Direction);
    var t = 0.5f * (unitDir.Y + 1);
    return (1 - t) * ColorWhite + t * C3(0.5f, 0.7f, 1.0f);
}

void SetPixel(int[] image, int x, int y, Vector3 pixelColor)
{
    var c = Vector3.SquareRoot(pixelColor * samplesPerPixelInv);

    var rgb_f32 = 256 * Vector3.ClampNative(c, Vector3.Zero, new Vector3(0.999f));
    var rgb_i32 = Vector128.ConvertToInt32Native(rgb_f32.AsVector128Unsafe());

    var r = rgb_i32.GetElement(0);
    var g = rgb_i32.GetElement(1);
    var b = rgb_i32.GetElement(2);

    var d = (r << 16) | (g << 8) | b;

    var i = x + (y * imageWidth);

    image[i] = d;
}

Hittable RandomScene()
{
    var world = new List<Hittable>();

    var matGround = new Lambertian(C3(0.5f, 0.5f, 0.5f));
    world.Add(new Sphere(P3(0, -1000, 0), 1000, matGround));

    for (var a = -11; a < 11; a++)
    {
        for (var b = -11; b < 11; b++)
        {
            var chooseMat = RandomValue();
            var center = P3(a + 0.9f * RandomValue(), 0.2f, b + 0.9f * RandomValue());

            if ((center - P3(4, 0.2f, 0)).Length() > 0.9)
            {
                if (chooseMat < 0.8)
                {
                    // Diffuse
                    var albedo = RandomVector3() * RandomVector3();
                    var mat = new Lambertian(albedo);

                    world.Add(new Sphere(center, 0.2f, mat));
                }
                else if (chooseMat < 0.95)
                {
                    // Metal
                    var albedo = RandomVector3(0.5f, 1);
                    var fuzz = RandomValue(0, 0.5f);
                    var mat = new Metal(albedo, fuzz);

                    world.Add(new Sphere(center, 0.2f, mat));
                }
                else
                {
                    // Glass
                    var mat = new Dielectric(1.5f);

                    world.Add(new Sphere(center, 0.2f, mat));
                }
            }
        }
    }

    var material1 = new Dielectric(1.5f);
    world.Add(new Sphere(P3(0, 1, 0), 1, material1));

    var material2 = new Lambertian(C3(0.4f, 0.2f, 0.1f));
    world.Add(new Sphere(P3(-4, 1, 0), 1, material2));

    var material3 = new Metal(C3(0.7f, 0.6f, 0.5f), 0);
    world.Add(new Sphere(P3(4, 1, 0), 1, material3));

    // A red triangle
    var t0 = P3(0, 1.4f, 2.8f);
    var t1 = P3(3, 1.9f, 2.3f);
    var t2 = P3(0, 2.2f, 3);
    var material4 = new Lambertian(ColorRed);
    world.Add(new Triangle(t0, t1, t2, material4));

    switch (bvh)
    {
        case BvhMode.Tree:
            var bvhHittable = BvhHelper.CreateBvh(world);
            return bvhHittable;
        
        case BvhMode.Linear:
            var linearBvhHittable = LinearBvhHelper.CreateLinearBvh(world);
            return linearBvhHittable;

        case BvhMode.None:
            return new HittableList(world);

        default:
            throw new NotImplementedException();
    }
}

enum Verbosity
{
    Normal = 0,
    Diagnostic = 1,
    Quiet = -1,
}

enum BvhMode
{
    Tree = 0,
    Linear = 1,
    None = -1
}