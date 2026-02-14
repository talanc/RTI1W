using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using System.Numerics;
using System.Runtime.Intrinsics;
using static System.MathF;

namespace RTI1W.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}

[SimpleJob]
[DisassemblyDiagnoser]
public class IntersectRayBoxBenchmarks
{
    private readonly Ray ray1 = new(new Vector3(0, 0, 3), new Vector3(1, 1, 0));
    private readonly Box3 box1 = new(new Vector3(3, 3, 3), new Vector3(5, 5, 5));

    [Params(1)]
    public int N;

    private Ray ray;
    private Box3 box;

    private const float fMin = 0.001f;
    private const float fMax = float.PositiveInfinity;

    [GlobalSetup]
    public void Setup()
    {
        (ray, box) = N switch
        {
            1 => (ray1, box1),
            _ => throw new NotImplementedException(),
        };
    }

    [Benchmark(Baseline = true)]
    public bool Old() => IntersectRayBoxOld(ray, box, fMin, fMax);

    [Benchmark]
    public bool New() => IntersectRayBoxNew(ray, box, fMin, fMax);

    private static void Swap(ref float a, ref float b)
    {
        (b, a) = (a, b);
    }

    public static bool IntersectRayBoxOld(in Ray ray, Box3 box, float tMin, float tMax)
    {
        var invDir = ray.InvDirection;
        var t0 = (box.Min - ray.Origin) * invDir;
        var t1 = (box.Max - ray.Origin) * invDir;

        if (invDir.X < 0) (t0.X, t1.X) = (t1.X, t0.X);
        if (invDir.Y < 0) (t0.Y, t1.Y) = (t1.Y, t0.Y);
        if (invDir.Z < 0) (t0.Z, t1.Z) = (t1.Z, t0.Z);

        var tclose = Max(Max(t0.X, t0.Y), Max(t0.Z, tMin));
        var tfar = Min(Min(t1.X, t1.Y), Min(t1.Z, tMax));

        if (tfar <= tclose)
        {
            return false;
        }

        return true;
    }

    public static bool IntersectRayBoxNew(in Ray ray, Box3 box, float tMin, float tMax)
    {
        var invDir = ray.InvDirection;
        var t0 = (box.Min - ray.Origin) * invDir;
        var t1 = (box.Max - ray.Origin) * invDir;
        if (invDir.X < 0) (t0.X, t1.X) = (t1.X, t0.X);
        if (invDir.Y < 0) (t0.Y, t1.Y) = (t1.Y, t0.Y);
        if (invDir.Z < 0) (t0.Z, t1.Z) = (t1.Z, t0.Z);

        float tclose = t0.X;
        if (t0.Y > tclose) tclose = t0.Y;
        if (t0.Z > tclose) tclose = t0.Z;
        if (tMin > tclose) tclose = tMin;

        float tfar = t1.X;
        if (t1.Y < tfar) tfar = t1.Y;
        if (t1.Z < tfar) tfar = t1.Z;
        if (tMax < tfar) tfar = tMax;

        if (tfar <= tclose)
        {
            return false;
        }

        return true;
    }
}

[SimpleJob]
public class CalcPixelBenchmarks
{
    private static Vector3 color = new(0.3f, 1.0f, 0.3f);

    [Benchmark(Baseline = true)]
    public int Old() => CalcPixel_Old(color);

    [Benchmark]
    public int New() => CalcPixel_New(color);

    static CalcPixelBenchmarks()
    {
        Verify();
    }

    private static int samplesPerPixel = 100;
    private static float samplesPerPixelInv = 1f / samplesPerPixel;

    private static int CalcPixel_Old(Vector3 pixelColor)
    {
        var scale = 1.0f / samplesPerPixel;

        var cr = Sqrt(pixelColor.X * scale);
        var cg = Sqrt(pixelColor.Y * scale);
        var cb = Sqrt(pixelColor.Z * scale);

        var r = (int)(256 * Math.Clamp(cr, 0, 0.999));
        var g = (int)(256 * Math.Clamp(cg, 0, 0.999));
        var b = (int)(256 * Math.Clamp(cb, 0, 0.999));

        var d = (r << 16) | (g << 8) | b;
        return d;
    }

    private static int CalcPixel_New(Vector3 pixelColor)
    {
        var c = Vector3.SquareRoot(pixelColor * samplesPerPixelInv);

        var rgb_f32 = 256 * Vector3.ClampNative(c, Vector3.Zero, new Vector3(0.999f));
        var rgb_i32 = Vector128.ConvertToInt32Native(rgb_f32.AsVector128Unsafe());

        var r = rgb_i32.GetElement(0);
        var g = rgb_i32.GetElement(1);
        var b = rgb_i32.GetElement(2);

        var d = (r << 16) | (g << 8) | b;
        return d;
    }

    public static void Verify()
    {
        var result1 = CalcPixel_Old(color);
        var result2 = CalcPixel_New(color);
        if (result1 != result2)
        {
            throw new Exception($"Results do not match: {result1} != {result2}");
        }
    }
}