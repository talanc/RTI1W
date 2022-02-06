global using RTI1W;
global using static RTI1W.Helpers;
global using static System.Console;
global using static System.Math;

var timer = System.Diagnostics.Stopwatch.StartNew();

// Image

const double AspectRatio = 16.0 / 9.0;
const int ImageWidth = 400;
const int ImageHeight = (int)(ImageWidth / AspectRatio);
const int SamplesPerPixel = 100;
const int MaxDepth = 50;

// World 

var matGround = new Lambertian(C3(0.8, 0.8, 0.0));
var matCenter = new Lambertian(C3(0.1, 0.2, 0.5));
var matLeft = new Dielectric(1.5);
var matRight = new Metal(C3(0.8, 0.6, 0.2), 0.0);

var world = new HittableList()
{
    List = new()
    {
        new Sphere(P3(0, -100.5, -1), 100, matGround),
        new Sphere(P3(0, 0, -1), 0.5, matCenter),
        new Sphere(P3(-1, 0, -1), 0.5, matLeft),
        new Sphere(P3(-1, 0, -1), -0.4, matLeft),
        new Sphere(P3(1, 0, -1), 0.5, matRight)
    }
};

// Camera

var camera = new Camera();

// Render

WriteLine("P3");
WriteLine($"{ImageWidth} {ImageHeight}");
WriteLine("255");

for (var j = ImageHeight - 1; j >= 0; j--)
{
    Error.WriteLine($"Scanlines remaining: {j + 1}");
    for (var i = 0; i < ImageWidth; i++)
    {
        var pixelColor = C3(0, 0, 0);
        for (var s = 0; s < SamplesPerPixel; s++)
        {
            var u = (i + RandomDouble()) / (ImageWidth - 1);
            var v = (j + RandomDouble()) / (ImageHeight - 1);
            var r = camera.GetRay(u, v);
            pixelColor += RayColor(r, MaxDepth);
        }

        WriteColor(pixelColor, SamplesPerPixel);
    }
}

Error.WriteLine($"Done. ({timer.Elapsed.TotalSeconds:F2} secs)");

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
