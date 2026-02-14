namespace RTI1W;

public static class Helpers
{
    public static Vector3 C3(float x, float y, float z)
    {
        return new Vector3(x, y, z);
    }

    public static Vector3 P3(float x, float y, float z)
    {
        return new Vector3(x, y, z);
    }

    public static Vector3 V3(float x, float y, float z)
    {
        return new Vector3(x, y, z);
    }

    public static float Dot(Vector3 a, Vector3 b)
    {
        return Vector3.Dot(a, b);
    }

    public static Vector3 Cross(Vector3 a, Vector3 b)
    {
        return Vector3.Cross(a, b);
    }

    public static Vector3 UnitVector(Vector3 a)
    {
        return Vector3.Normalize(a);
    }

    public static float DegreesToRadians(float deg)
    {
        return deg * PI / 180;
    }

    // Useful for deterministic testing (0 means its not used)
    public static int RandomSeed { get; set; } = 0;

    private static readonly ThreadLocal<Random> randomLocal = new(() => new Random(RandomSeed));

    /// <summary>
    /// Returns a value between [0.0, 1.0)
    /// </summary>
    public static float RandomValue()
    {
        if (RandomSeed != 0)
        {
            return randomLocal.Value!.NextSingle();
        }
        return Random.Shared.NextSingle();
    }

    /// <summary>
    /// Returns a value between [min, max)
    /// </summary>
    public static float RandomValue(float min, float max)
    {
        return min + (max - min) * RandomValue();
    }

    public static Vector3 RandomVector3()
    {
        return V3(RandomValue(), RandomValue(), RandomValue());
    }

    public static Vector3 RandomVector3(float min, float max)
    {
        return V3(RandomValue(min, max), RandomValue(min, max), RandomValue(min, max));
    }

    public static Vector3 RandomInUnitSphere()
    {
        var r = Pow(RandomValue(), 1.0f / 3.0f);
        var theta = RandomValue(0, Tau);
        var phi = Acos(2 * RandomValue() - 1);
        var sinPhi = Sin(phi);
        return V3(r * sinPhi * Cos(theta), r * sinPhi * Sin(theta), r * Cos(phi));
    }

    public static Vector3 RandomInUnitCircle()
    {
        var theta = RandomValue(0, Tau);
        var dist = Sqrt(RandomValue());
        return V3(dist * Cos(theta), dist * Sin(theta), 0);
    }

    public static Vector3 RandomUnitVector()
    {
        return UnitVector(RandomVector3());
    }

    public static Vector3 RandomInHemisphere(Vector3 normal)
    {
        var inUnitSphere = RandomInUnitSphere();
        if (Dot(inUnitSphere, normal) > 0)
        {
            return inUnitSphere;
        }
        return -inUnitSphere;
    }

    public static Vector3 Reflect(Vector3 v, Vector3 n)
    {
        return v - 2 * Dot(v, n) * n;
    }

    public static Vector3 Refract(Vector3 uv, Vector3 n, float etaiOverEtat)
    {
        var cosTheta = Min(Dot(-uv, n), 1.0f);
        var rOutPerp = etaiOverEtat * (uv + cosTheta * n);
        var rOutParallel = -Sqrt(Abs(1 - rOutPerp.LengthSquared())) * n;
        return rOutPerp + rOutParallel;
    }

    public static Vector3 ColorBlack => C3(0, 0, 0);
    public static Vector3 ColorWhite => C3(1, 1, 1);
    public static Vector3 ColorRed => C3(1, 0, 0);
    public static Vector3 ColorGreen => C3(0, 1, 0);
    public static Vector3 ColorBlue => C3(0, 0, 1);

    public static float Lerp(float t, float v1, float v2)
    {
        return (1 - t) * v1 + t * v2;
    }

    public static bool IsNearZero(float v)
    {
        const float S = 1e-8f;
        return Abs(v) < S;
    }

    public static bool IsNearZero(Vector3 v)
    {
        const float S = 1e-8f;
        return Abs(v.X) < S && Abs(v.Y) < S && Abs(v.Z) < S;
    }

    public static bool IntersectRayBox(in Ray ray, Box3 box)
    {
        return IntersectRayBox(ray, box, 0, float.PositiveInfinity);
    }

    private static void Swap(ref float a, ref float b)
    {
        (b, a) = (a, b);
    }

    public static bool IntersectRayBox(in Ray ray, Box3 box, float tMin, float tMax)
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
