global using RTI1W;
global using static RTI1W.Helpers;
global using static System.Console;
global using static System.Math;

// Image

const double AspectRatio = 16.0 / 9.0;
const int ImageWidth = 400;
const int ImageHeight = (int)(ImageWidth / AspectRatio);

// World 
var world = new HittableList()
{
    List = new()
    {
        new Sphere(V3(0, 0, -1), 0.5),
        new Sphere(V3(0, -100.5, -1), 100)
    }
};

// Camera

var viewportHeight = 2.0;
var viewportWidth = AspectRatio * viewportHeight;
var focalLength = 1.0;

var origin = P3(0, 0, 0);
var horizontal = V3(viewportWidth, 0, 0);
var vertical = V3(0, viewportHeight, 0);
var lowerLeftCorner = origin - horizontal / 2 - vertical / 2 - V3(0, 0, focalLength);

// Render

WriteLine("P3");
WriteLine($"{ImageWidth} {ImageHeight}");
WriteLine("255");

for (var j = ImageHeight - 1; j >= 0; j--)
{
    Error.WriteLine($"Scanlines remaining: {j + 1}");
    for (var i = 0; i < ImageWidth; i++)
    {
        var u = (double)i / (ImageWidth - 1);
        var v = (double)j / (ImageHeight - 1);
        var r = new Ray(origin, lowerLeftCorner + u * horizontal + v * vertical - origin);
        var pixelColor = RayColor(r);
        WriteColor(pixelColor);
    }
}

Error.WriteLine("Done.");

Vec3 RayColor(Ray r)
{
    var rec = world.Hit(r, 0, double.PositiveInfinity);
    if (rec.HasValue)
    {
        return 0.5 * (rec.Value.Normal + C3(1, 1, 1));
    }

    var unitDir = Vec3.UnitVector(r.Direction);
    var t = 0.5 * (unitDir.Y + 1);
    return (1 - t) * C3(1, 1, 1) + t * C3(0.5, 0.7, 1.0);
}
