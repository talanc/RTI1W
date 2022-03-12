global using RTI1W;
global using static RTI1W.Helpers;
global using static System.Console;
global using static System.Math;

Metrics.Activate();

// Image

const double AspectRatio = 3.0 / 2.0;
const int ImageWidth = 200;
const int ImageHeight = (int)(ImageWidth / AspectRatio);
const int SamplesPerPixel = 100;
const int MaxDepth = 30;

// World

var world = RandomScene();

// Camera

var lookFrom = P3(13, 2, 3);
var lookAt = P3(0, 0, 0);
var vUp = P3(0, 1, 0);
var distToFocus = 10.0;
var aperture = 0.1;
var camera = new Camera(lookFrom, lookAt, vUp, 20, AspectRatio, aperture, distToFocus, 0, 1);

// Render

var image = new int[ImageWidth * ImageHeight];

Metrics.StartTimer("Render");

var scanlinesRemaining = ImageHeight;

var parallelOpts = new ParallelOptions();
Parallel.For(0, ImageHeight, parallelOpts, j =>
{
    var writeScanlines = Interlocked.Decrement(ref scanlinesRemaining) + 1;
    Error.WriteLine($"Scanlines remaining: {writeScanlines}");
    
    var pixY = ImageHeight - 1 - j;
    var dataY = j;

    for (var i = 0; i < ImageWidth; i++)
    {
        var pixX = i;
        var dataX = i;

        var pixelColor = C3(0, 0, 0);
        for (var s = 0; s < SamplesPerPixel; s++)
        {
            var u = (pixX + RandomDouble()) / (ImageWidth - 1);
            var v = (pixY + RandomDouble()) / (ImageHeight - 1);
            var r = camera.GetRay(u, v);
            pixelColor += RayColor(r, MaxDepth);
        }

        SetPixel(image, dataX, dataY, pixelColor);
    }
});

Metrics.StopTimer();
Metrics.StartTimer("Save");

// Write PPM/P3 file
WriteLine("P3");
Write(ImageWidth); Write(' '); WriteLine(ImageHeight);
WriteLine("255");
for (var i = 0; i < image.Length; i++)
{
    var d = image[i];
    var r = (d >> 16) & 0xFF;
    var g = (d >> 8) & 0xFF;
    var b = (d >> 0) & 0xFF;
    Write(r); Write(' '); Write(g); Write(' '); WriteLine(b);
}

Metrics.StopTimer();

Metrics.Display();

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
    var scale = 1.0 / SamplesPerPixel;

    var cr = Sqrt(pixelColor.X * scale);
    var cg = Sqrt(pixelColor.Y * scale);
    var cb = Sqrt(pixelColor.Z * scale);

    var r = (int)(256 * Clamp(cr, 0, 0.999));
    var g = (int)(256 * Clamp(cg, 0, 0.999));
    var b = (int)(256 * Clamp(cb, 0, 0.999));

    var i = x + (y * ImageWidth);
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

                    var center2 = center + V3(0, RandomDouble(0, 0.5), 0);

                    world.Add(new MovingSphere(center, center2, 0, 1, 0.2, mat));
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

#if false
    return world;
#else

    var bvhHittable = BvhHelper.CreateBvh(world.List);

    return bvhHittable;
#endif
}