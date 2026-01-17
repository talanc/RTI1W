global using RTI1W;
global using static RTI1W.Helpers;
global using static System.Console;
global using static System.Math;
using System.CommandLine;

//
// Cmdline
//

var rootCommand = new RootCommand();
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
var optRunMode = addOption("--run-mode", "Run mode", RunMode.ParallelFor);
var optRunNum = addOption("--run-num", "Run parallel count", 4);
var optNoBvh = addOption("--no-bvh", "Do not use the BVH (makes it much slower)", false);
var optNoMetrics = addOption("--no-metrics", "Do not show progress and metrics", false);

var parseResult = rootCommand.Parse(args);

if (parseResult.Errors.Count > 0 ||
    parseResult.Tokens.Any(curr => curr.Value == "-h" || curr.Value == "--help"))
{
    return parseResult.Invoke();
}

var outputPath = parseResult.GetValue(optOutput)!;
var imageWidth = parseResult.GetValue(optWidth);
var imageHeight = parseResult.GetValue(optHeight);
var samplesPerPixel = parseResult.GetValue(optSamples);
var maxDepth = parseResult.GetValue(optMaxDepth);
var runMode = parseResult.GetValue(optRunMode);
var runNum = parseResult.GetValue(optRunNum);
var useBVH = !parseResult.GetValue(optNoBvh);
Metrics.IsActive = !parseResult.GetValue(optNoMetrics);

//
// World
//

var world = RandomScene();

//
// Camera
//

var aspectRatio = double.Round((double)imageWidth / imageHeight, 1);
var lookFrom = P3(13, 2, 3);
var lookAt = P3(0, 0, 0);
var vUp = P3(0, 1, 0);
var distToFocus = 10.0;
var aperture = 0.1;
var camera = new Camera(lookFrom, lookAt, vUp, 20, aspectRatio, aperture, distToFocus);

//
// Render
//

var image = new int[imageWidth * imageHeight];

var lineIds = new int[imageHeight];

var scanlinesRemaining = imageHeight;

Metrics.StartTimer("Render");


if (runMode == RunMode.Sequential)
{
    for (var i = 0; i < imageHeight; i += 1)
    {
        Scanline(i);
    }
}
else if (runMode == RunMode.ParallelFor)
{
    var parallelOpts = new ParallelOptions()
    {
        MaxDegreeOfParallelism = runNum,
    };
    Parallel.For(0, imageHeight, parallelOpts, Scanline);
}
else if (runMode == RunMode.Tasks)
{
    var tasks = new Task[runNum];
    for (var i = 0; i < runNum; i++)
    {
        var localIdx = i;
        tasks[localIdx] = Task.Run(() =>
        {
            for (var j = localIdx; j < imageHeight; j += runNum)
            {
                Scanline(j);
            }
        });
    }
    Task.WaitAll(tasks);
}
else
{
    throw new InvalidOperationException();
}

void Scanline(int j)
{
    if (Metrics.IsActive)
    {
        var id = lineIds[j] = Environment.CurrentManagedThreadId;
        var numLeft = Interlocked.Decrement(ref scanlinesRemaining);
        var errStr = $"Id={id,-4}Elem={j,-5}NumLeft={numLeft}";
        WriteLine(errStr);
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
            var u = (pixX + RandomDouble()) / (imageWidth - 1);
            var v = (pixY + RandomDouble()) / (imageHeight - 1);
            var r = camera.GetRay(u, v);
            pixelColor += RayColor(r, maxDepth);
        }

        SetPixel(image, dataX, dataY, pixelColor);
    }
};

Metrics.StopTimer();
Metrics.StartTimer("Save");

// Write PPM/P3 file
WriteLine("Creating image");
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

WriteLine($"Scanlines:");
foreach (var lineId in lineIds.GroupBy(curr => curr))
{
    WriteLine($"- {lineId.Key}: {lineId.Count()}");
}

return 0;

Vec3 RayColor(Ray r, int depth)
{
    if (depth <= 0)
    {
        return ColorBlack;
    }

    var recMaybe = world.Hit(r, 0.001, double.PositiveInfinity);
    if (recMaybe.HasValue)
    {
        var hitRec = recMaybe.Value;
        var matRecMaybe = hitRec.Material.Scatter(r, hitRec);
        if (matRecMaybe.HasValue)
        {
            var matRec = matRecMaybe.Value;
            return matRec.Attenuation * RayColor(matRec.Scattered, depth - 1);
        }
        return ColorBlack;
    }

    var unitDir = UnitVector(r.Direction);
    var t = 0.5 * (unitDir.Y + 1);
    return (1 - t) * ColorWhite + t * C3(0.5, 0.7, 1.0);
}

void SetPixel(int[] image, int x, int y, Vec3 pixelColor)
{
    var scale = 1.0 / samplesPerPixel;

    var cr = Sqrt(pixelColor.X * scale);
    var cg = Sqrt(pixelColor.Y * scale);
    var cb = Sqrt(pixelColor.Z * scale);

    var r = (int)(256 * Clamp(cr, 0, 0.999));
    var g = (int)(256 * Clamp(cg, 0, 0.999));
    var b = (int)(256 * Clamp(cb, 0, 0.999));

    var i = x + (y * imageWidth);
    var d = (r << 16) | (g << 8) | b;
    image[i] = d;
}

Hittable RandomScene()
{
    var world = new HittableList();

    var matGround = new Lambertian(C3(0.5, 0.5, 0.5));
    world.Add(new Sphere(P3(0, -1000, 0), 1000, matGround));

    for (var a = -11; a < 11; a++)
    {
        for (var b = -11; b < 11; b++)
        {
            var chooseMat = RandomDouble();
            var center = P3(a + 0.9 * RandomDouble(), 0.2, b + 0.9 * RandomDouble());

            if ((center - P3(4, 0.2, 0)).Length > 0.9)
            {
                if (chooseMat < 0.8)
                {
                    // Diffuse
                    var albedo = RandomVec3() * RandomVec3();
                    var mat = new Lambertian(albedo);

                    world.Add(new Sphere(center, 0.2, mat));
                }
                else if (chooseMat < 0.95)
                {
                    // Metal
                    var albedo = RandomVec3(0.5, 1);
                    var fuzz = RandomDouble(0, 0.5);
                    var mat = new Metal(albedo, fuzz);

                    world.Add(new Sphere(center, 0.2, mat));
                }
                else
                {
                    // Glass
                    var mat = new Dielectric(1.5);

                    world.Add(new Sphere(center, 0.2, mat));
                }
            }
        }
    }

    var material1 = new Dielectric(1.5);
    world.Add(new Sphere(P3(0, 1, 0), 1, material1));

    var material2 = new Lambertian(C3(0.4, 0.2, 0.1));
    world.Add(new Sphere(P3(-4, 1, 0), 1, material2));

    var material3 = new Metal(C3(0.7, 0.6, 0.5), 0.0);
    world.Add(new Sphere(P3(4, 1, 0), 1, material3));

    // A red triangle
    var t0 = P3(0, 1.4, 2.8);
    var t1 = P3(3, 1.9, 2.3);
    var t2 = P3(0, 2.2, 3);
    var material4 = new Lambertian(ColorRed);
    world.Add(new Triangle(t0, t1, t2, material4));

    if (useBVH)
    {
        var bvhHittable = BvhHelper.CreateBvh(world.List);
        return bvhHittable;
    }
    else
    {
        return world;
    }
}

enum RunMode
{
    Sequential,
    Tasks,
    ParallelFor,
}
