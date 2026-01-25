using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using System.Numerics;
using System.Security.Cryptography;
using static System.MathF;

namespace RTI1W.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<IntersectRayBoxBenchmarks>();
    }
}

[SimpleJob]
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

    public static bool IntersectRayBoxOld(Ray ray, Box3 box, float tMin, float tMax)
    {
        // TODO i think this can use SIMD

        var invDir = ray.InvDirection;
        var t0 = (box.Min - ray.Origin) * invDir;
        var t1 = (box.Max - ray.Origin) * invDir;

        if (invDir.X < 0) Swap(ref t0.X, ref t1.X);
        if (invDir.Y < 0) Swap(ref t0.Y, ref t1.Y);
        if (invDir.Z < 0) Swap(ref t0.Z, ref t1.Z);

        tMin = Max(tMin, t0.X);
        tMax = Min(tMax, t1.X);
        if (tMax <= tMin)
        {
            return false;
        }

        tMin = Max(tMin, t0.Y);
        tMax = Min(tMax, t1.Y);
        if (tMax <= tMin)
        {
            return false;
        }

        tMin = Max(tMin, t0.Z);
        tMax = Min(tMax, t1.Z);
        if (tMax <= tMin)
        {
            return false;
        }

        return true;
    }

    public static bool IntersectRayBoxNew(Ray ray, Box3 box, float tMin, float tMax)
    {
        var invDir = ray.InvDirection;
        var t0 = (box.Min - ray.Origin) * invDir;
        var t1 = (box.Max - ray.Origin) * invDir;

        if (invDir.X < 0) Swap(ref t0.X, ref t1.X);
        if (invDir.Y < 0) Swap(ref t0.Y, ref t1.Y);
        if (invDir.Z < 0) Swap(ref t0.Z, ref t1.Z);

        var tclose = Max(Max(t0.X, t0.Y), Max(t0.Z, tMin));
        var tfar = Min(Min(t1.X, t1.Y), Min(t1.Z, tMax));

        if (tfar <= tclose)
        {
            return false;
        }

        return true;
    }
}