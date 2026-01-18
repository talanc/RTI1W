namespace RTI1W;

public static class Helpers
{
    public static Vec3 C3(float x, float y, float z)
    {
        return new Vec3(x, y, z);
    }

    public static Vec3 P3(float x, float y, float z)
    {
        return new Vec3(x, y, z);
    }

    public static Vec3 V3(float x, float y, float z)
    {
        return new Vec3(x, y, z);
    }

    public static float Dot(Vec3 a, Vec3 b)
    {
        return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
    }

    public static Vec3 Cross(Vec3 a, Vec3 b)
    {
        return new Vec3(
            a.Y * b.Z - a.Z * b.Y,
            a.Z * b.X - a.X * b.Z,
            a.X * b.Y - a.Y * b.X);
    }

    public static Vec3 UnitVector(Vec3 a)
    {
        return a / a.Length;
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

    public static Vec3 RandomVec3()
    {
        return V3(RandomValue(), RandomValue(), RandomValue());
    }

    public static Vec3 RandomVec3(float min, float max)
    {
        return V3(RandomValue(min, max), RandomValue(min, max), RandomValue(min, max));
    }

    public static Vec3 RandomInUnitSphere()
    {
        var r = Pow(RandomValue(), 1.0f / 3.0f);
        var theta = RandomValue(0, Tau);
        var phi = Acos(2 * RandomValue() - 1);
        var sinPhi = Sin(phi);
        return V3(r * sinPhi * Cos(theta), r * sinPhi * Sin(theta), r * Cos(phi));
    }

    public static Vec3 RandomInUnitCircle()
    {
        var theta = RandomValue(0, Tau);
        var dist = Sqrt(RandomValue());
        return V3(dist * Cos(theta), dist * Sin(theta), 0);
    }

    public static Vec3 RandomUnitVector()
    {
        return UnitVector(RandomVec3());
    }

    public static Vec3 RandomInHemisphere(Vec3 normal)
    {
        var inUnitSphere = RandomInUnitSphere();
        if (Dot(inUnitSphere, normal) > 0)
        {
            return inUnitSphere;
        }
        return -inUnitSphere;
    }

    public static Vec3 Reflect(Vec3 v, Vec3 n)
    {
        return v - 2 * Dot(v, n) * n;
    }

    public static Vec3 Refract(Vec3 uv, Vec3 n, float etaiOverEtat)
    {
        var cosTheta = Min(Dot(-uv, n), 1.0f);
        var rOutPerp = etaiOverEtat * (uv + cosTheta * n);
        var rOutParallel = -Sqrt(Abs(1 - rOutPerp.LengthSquared)) * n;
        return rOutPerp + rOutParallel;
    }

    public static Vec3 ColorBlack => C3(0, 0, 0);
    public static Vec3 ColorWhite => C3(1, 1, 1);
    public static Vec3 ColorRed => C3(1, 0, 0);
    public static Vec3 ColorGreen => C3(0, 1, 0);
    public static Vec3 ColorBlue => C3(0, 0, 1);

    public static float Lerp(float t, float v1, float v2)
    {
        return (1 - t) * v1 + t * v2;
    }

    public static bool IsNearZero(float v)
    {
        const float S = 1e-8f;
        return Abs(v) < S;
    }

    public static bool IntersectRayBox(Ray ray, Box3 box)
    {
        return IntersectRayBox(ray, box, 0, float.PositiveInfinity);
    }

    public static bool IntersectRayBox(Ray ray, Box3 box, float tMin, float tMax)
    {
        var t0v = box.Min - ray.Origin;
        var t1v = box.Max - ray.Origin;

        var invDirX = ray.InvDirection.X;
        var t0x = t0v.X * invDirX;
        var t1x = t1v.X * invDirX;
        if (invDirX < 0) (t0x, t1x) = (t1x, t0x);
        tMin = t0x > tMin ? t0x : tMin;
        tMax = t1x < tMax ? t1x : tMax;
        if (tMax <= tMin)
        {
            return false;
        }

        var invDirY = ray.InvDirection.Y;
        var t0y = t0v.Y * invDirY;
        var t1y = t1v.Y * invDirY;
        if (invDirY < 0) (t0y, t1y) = (t1y, t0y);
        tMin = t0y > tMin ? t0y : tMin;
        tMax = t1y < tMax ? t1y : tMax;
        if (tMax <= tMin)
        {
            return false;
        }

        var invDirZ = ray.InvDirection.Z;
        var t0z = t0v.Z * invDirZ;
        var t1z = t1v.Z * invDirZ;
        if (invDirZ < 0) (t0z, t1z) = (t1z, t0z);
        tMin = t0z > tMin ? t0z : tMin;
        tMax = t1z < tMax ? t1z : tMax;
        if (tMax <= tMin)
        {
            return false;
        }

        return true;
    }
}
