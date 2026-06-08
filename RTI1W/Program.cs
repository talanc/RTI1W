using System.CommandLine;
using Raylib_cs;

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
var optInteractive = addOption("--interactive", "Render in a Raylib window with controls", false);
var optVerbosity = addOption("--verbosity", "Verbosity output mode", Verbosity.Normal);
var optQuiet = addOption("-q", "Quiet output (--verbosity Quiet)", false);
var optVerbose = addOption("-v", "Verbose output (--verbosity Verbose)", false);

// Don't use more than 1 verbosity option
rootCommand.Validators.Add(result =>
{
    var opts = new Option[] { optVerbosity, optVerbose, optQuiet };
    var num = opts.Count(curr => result.GetResult(curr) is { Implicit: false });

    if (num > 1)
    {
        var names = string.Join(", ", opts.Select(curr => curr.Name));
        result.AddError($"Only one of the following options can be used: {names}");
    }
});

// Ensure num threads is in the range of [1, ProcCount]
rootCommand.Validators.Add(result =>
{
    var numThreads = result.GetValue(optThreads);

    var minThreads = 1;
    var maxThreads = Environment.ProcessorCount;

    if (numThreads < minThreads || numThreads > maxThreads)
    {
        result.AddError($"{optThreads.Name} must be between {minThreads} and {maxThreads}");
    }
});

var parseResult = rootCommand.Parse(args);

if (parseResult.Errors.Count > 0 ||
    parseResult.Tokens.Any(curr => builtInOptions.Contains(curr.Value)))
{
    return parseResult.Invoke();
}

var outputPath = parseResult.GetRequiredValue(optOutput);
var samplesPerPixel = parseResult.GetValue(optSamples);
var maxDepth = parseResult.GetValue(optMaxDepth);
var numThreads = parseResult.GetValue(optThreads);
var bvh = parseResult.GetValue(optBvh);
var openOutput = parseResult.GetValue(optOpen);
var interactive = parseResult.GetValue(optInteractive);

var imageWidth = parseResult.GetValue(optWidth);
var imageHeight = parseResult.GetValue(optHeight);

// If only one of width or height is specified, calculate the other to maintain a 3:2 aspect ratio
var specifiedImageWidth = parseResult.GetResult(optWidth) is { Implicit: false };
var specifiedImageHeight = parseResult.GetResult(optHeight) is { Implicit: false };
if (specifiedImageWidth != specifiedImageHeight)
{
    const float defaultAspectRatio = 3f / 2f;

    if (!specifiedImageWidth) imageWidth = (int)Round(imageHeight * defaultAspectRatio);
    else imageHeight = (int)Round(imageWidth / defaultAspectRatio);
}

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
var camera = new Camera();
camera.SetPosition(lookFrom, lookAt, vUp, 20, aspectRatio, aperture, distToFocus);

//
// Image
//

var image = new int[imageWidth * imageHeight];

//
// Interactive
//

if (interactive)
{
    RunInteractive();
    return 0;
}

//
// Render
//

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
            pixelColor += RayTracer.RayColor(world, r, maxDepth);
        }

        RayTracer.SetPixel(imageWidth, image, dataX, dataY, pixelColor, samplesPerPixelInv);
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

void RunInteractive()
{
    var texturePixels = new int[imageWidth * imageHeight];

    var cameraPos = lookFrom;
    var forward = UnitVector(lookAt - lookFrom);
    var yaw = Atan2(forward.Z, forward.X);
    var pitch = Asin(forward.Y);

    const int targetTileCount = 64;
    var tilesX = Math.Max(1, (int)Ceiling(Sqrt(targetTileCount * ((float)imageWidth / imageHeight))));
    var tilesY = Math.Max(1, (int)Ceiling((float)targetTileCount / tilesX));
    var tileWidth = Math.Max(1, (imageWidth + tilesX - 1) / tilesX);
    var tileHeight = Math.Max(1, (imageHeight + tilesY - 1) / tilesY);
    var tileCount = tilesX * tilesY;

    var workItems = new (int sample, int tile)[tileCount];
    for (var i = 0; i < workItems.Length; i++)
    {
        workItems[i] = (sample: 1, tile: i);
    }

    var workerStartSignal = new ManualResetEventSlim(false);
    var workerGeneration = 0;
    var workerCompletedItems = 0;
    var workerNextItem = 0;

    var workerScratch = new Vector3[texturePixels.Length];

    var workerSample = 1;
    
    void UpdateDirectionAndCamera()
    {
        var dir = UnitVector(V3(
            x: Cos(pitch) * Cos(yaw),
            y: Sin(pitch),
            z: Cos(pitch) * Sin(yaw)
        ));

        camera.SetPosition(cameraPos, cameraPos + dir, vUp, 20, aspectRatio, aperture, distToFocus);
    }

    void Worker()
    {
    start:
        if (Volatile.Read(ref workerGeneration) == -1) return;
        workerStartSignal.Wait();

        var generation = Volatile.Read(ref workerGeneration);
        if (generation == -1) return;

    tile:
        if (generation != Volatile.Read(ref workerGeneration))
        {
            goto start;
        }

        var workItemIndex = Interlocked.Increment(ref workerNextItem) - 1;
        if (workItemIndex >= workItems.Length)
        {
            if (generation == Volatile.Read(ref workerGeneration)) workerStartSignal.Reset();
            goto start;
        }

        var (sample, tile) = workItems[workItemIndex];
        
        var (tileY, tileX) = Math.DivRem(tile, tilesX);

        var widthStart = tileX * tileWidth;
        var widthEnd = Math.Min(imageWidth, widthStart + tileWidth);
        var widthStep = 1;

        var heightStart = tileY * tileHeight;
        var heightEnd = Math.Min(imageHeight, heightStart + tileHeight);
        var heightStep = 1;

        var sInv = 1f / sample;

        for (var j = heightStart; j < heightEnd; j += heightStep)
        {
            var pixY = imageHeight - 1 - j;
            var dataY = j;

            for (var i = widthStart; i < widthEnd; i += widthStep)
            {
                var pixX = i;
                var dataX = i;

                var u = (pixX + RandomValue()) / (imageWidth - 1);
                var v = (pixY + RandomValue()) / (imageHeight - 1);

                var pixelIndex = RayTracer.GetIndex(imageWidth, dataX, dataY);
                var pixelColor = sample == 1
                    ? workerScratch[pixelIndex] = ColorBlack
                    : workerScratch[pixelIndex];

                var ray = camera.GetRay(u, v);
                var color = RayTracer.RayColor(world, ray, maxDepth);
                pixelColor += color;

                // update calculated color on texture
                RayTracer.GetPixel(pixelColor, sInv, out var r, out var g, out var b);
                texturePixels[pixelIndex] = r | (g << 8) | (b << 16) | (0xFF << 24); // R8G8B8A8

                // update accumulated color
                workerScratch[pixelIndex] = pixelColor;
            }
        }

        Interlocked.Increment(ref workerCompletedItems);
        goto tile;
    }

    void StartRender()
    {
        for (var i = 0; i < workItems.Length; i++)
        {
            workItems[i].sample = workerSample;
        }
        Random.Shared.Shuffle(workItems);
        Interlocked.Exchange(ref workerCompletedItems, 0);
        Interlocked.Exchange(ref workerNextItem, 0);
        if (workerGeneration > 1_000_000) Volatile.Write(ref workerGeneration, 0);
        else Interlocked.Increment(ref workerGeneration);
        workerStartSignal.Set();
    }

    UpdateDirectionAndCamera();

    var workerTasks = new Task[numThreads];
    for (var i = 0; i < workerTasks.Length; i++)
    {
        workerTasks[i] = Task.Run(() => Worker());
    }

    var windowSizeWidth = 900;
    var windowSizeHeight = (int)Round(windowSizeWidth / aspectRatio);
    var windowSizeIncrements = 150;

    Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
    Raylib.InitWindow(windowSizeWidth, windowSizeHeight, "RTI1W - Interactive");
    Raylib.SetTargetFPS(60);

    var blankImage = Raylib.GenImageColor(imageWidth, imageHeight, Color.Black);
    var frameTexture = Raylib.LoadTextureFromImage(blankImage);
    Raylib.UnloadImage(blankImage);

    StartRender();

    var uploadTimer = Stopwatch.StartNew();
    var uploadedGeneration = -1;
    var uploadedItems = -1;

    while (!Raylib.WindowShouldClose())
    {
        var rerender = false;

        if (Raylib.IsWindowResized())
        {
            rerender = true;
        }

        var dir = UnitVector(V3(
            Cos(pitch) * Cos(yaw),
            Sin(pitch),
            Cos(pitch) * Sin(yaw)
        ));
        var right = UnitVector(Cross(dir, vUp));

        var move = V3(0, 0, 0);
        if (Raylib.IsKeyDown(KeyboardKey.W)) { move += dir; }
        if (Raylib.IsKeyDown(KeyboardKey.S)) { move -= dir; }
        if (Raylib.IsKeyDown(KeyboardKey.A)) { move -= right; }
        if (Raylib.IsKeyDown(KeyboardKey.D)) { move += right; }
        if (Raylib.IsKeyDown(KeyboardKey.E)) { move -= vUp; }
        if (Raylib.IsKeyDown(KeyboardKey.Q)) { move += vUp; }

        if (move.LengthSquared() != 0)
        {
            var moveSpeed = 0.2f;

            cameraPos += move * moveSpeed;
            rerender = true;
        }

        var oldWindowSizeWidth = windowSizeWidth;

        if (Raylib.IsKeyPressed(KeyboardKey.Minus) || Raylib.IsKeyPressed(KeyboardKey.KpSubtract))
        {
            if (windowSizeWidth >= windowSizeIncrements * 2)
            {
                windowSizeWidth -= windowSizeIncrements;
            }
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Equal) || Raylib.IsKeyPressed(KeyboardKey.KpAdd))
        {
            windowSizeWidth += windowSizeIncrements;
        }

        if (windowSizeWidth != oldWindowSizeWidth)
        {
            var oldWindowSizeHeight = windowSizeHeight;

            windowSizeHeight = (int)Round(windowSizeWidth / aspectRatio);

            var windowPos = Raylib.GetWindowPosition();
            var newX = (int)windowPos.X + (oldWindowSizeWidth - windowSizeWidth) / 2;
            var newY = (int)windowPos.Y + (oldWindowSizeHeight - windowSizeHeight) / 2;
            Raylib.SetWindowPosition(newX, newY);
            Raylib.SetWindowSize(windowSizeWidth, windowSizeHeight);
        }

        if (Raylib.IsMouseButtonDown(MouseButton.Left) || Raylib.IsMouseButtonDown(MouseButton.Right))
        {
            var mouseDelta = Raylib.GetMouseDelta();
            var mouseSensitivity = 0.004f;

            if (mouseDelta.X != 0 || mouseDelta.Y != 0)
            {
                yaw += mouseDelta.X * mouseSensitivity;
                pitch -= mouseDelta.Y * mouseSensitivity;
                pitch = Math.Clamp(pitch, -1.4f, 1.4f);
                rerender = true;
            }
        }

        if (rerender)
        {
            workerSample = 1;
            UpdateDirectionAndCamera();
            StartRender();
        }

        var currentGeneration = Volatile.Read(ref workerGeneration);
        var currentItems = Volatile.Read(ref workerCompletedItems);
        var frameDone = currentItems >= workItems.Length;
        var shouldUpload = currentGeneration != uploadedGeneration || currentItems != uploadedItems;

        if (frameDone && workerSample < samplesPerPixel)
        {
            // and redo the frame with the next sample level
            frameDone = false;
            workerSample++;
            StartRender();
        }

        if (shouldUpload && (frameDone || uploadTimer.ElapsedMilliseconds >= 100))
        {
            Raylib.UpdateTexture(frameTexture, texturePixels);
            uploadedGeneration = currentGeneration;
            uploadedItems = currentItems;
            uploadTimer.Restart();
        }

        // hmm we probably dont need to redraw if the frame is complete

        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.Black);
        Raylib.DrawTexturePro(
            frameTexture,
            new Rectangle(0, 0, frameTexture.Width, frameTexture.Height),
            new Rectangle(0, 0, Raylib.GetScreenWidth(), Raylib.GetScreenHeight()),
            Vector2.Zero,
            0,
            Color.White);

        // hmm i think strings allocate, try with utf8/sbyte* instead
        Raylib.DrawText("WASD move, Q/E up-down, mouse left-click look", 10, 10, 18, Color.Black);

        var info = "Done";
        if (!frameDone)
        {
            // hmm this may allocate every frame
            info = $"{workerSample}/{samplesPerPixel}";
        }
        var infoMeasure = Raylib.MeasureText(info, 18);
        var infoX = windowSizeWidth - infoMeasure - 10;
        Raylib.DrawText(info, infoX, 10, 18, Color.Black);

        Raylib.EndDrawing();
    }

    Volatile.Write(ref workerGeneration, -1);
    workerStartSignal.Set();
    Task.WaitAll(workerTasks);
    workerStartSignal.Dispose();

    Raylib.UnloadTexture(frameTexture);
    Raylib.CloseWindow();
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