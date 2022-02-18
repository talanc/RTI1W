namespace RTI1W;

public static class Helpers
{
    public static Vec3 C3(double x, double y, double z)
    {
        return new Vec3(x, y, z);
    }

    public static Vec3 P3(double x, double y, double z)
    {
        return new Vec3(x, y, z);
    }

    public static Vec3 V3(double x, double y, double z)
    {
        return new Vec3(x, y, z);
    }

    public static double Dot(Vec3 a, Vec3 b)
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

    public static double DegreesToRadians(double deg)
    {
        return deg * PI / 180;
    }

    public static double RandomDouble()
    {
        return Random.Shared.NextDouble();
    }

    public static double RandomDouble(double min, double max)
    {
        return min + (max - min) * RandomDouble();
    }

    public static Vec3 RandomVec3()
    {
        return V3(RandomDouble(), RandomDouble(), RandomDouble());
    }

    public static Vec3 RandomVec3(double min, double max)
    {
        return V3(RandomDouble(min, max), RandomDouble(min, max), RandomDouble(min, max));
    }

    public static Vec3 RandomInUnitSphere()
    {
        while (true)
        {
            var p = RandomVec3(-1, 1);
            if (p.LengthSquared < 1)
            {
                return p;
            }
        }
    }

    public static Vec3 RandomInUnitDisk()
    {
        while (true)
        {
            var p = V3(RandomDouble(-1, 1), RandomDouble(-1, 1), 0);
            if (p.LengthSquared < 1)
            {
                return p;
            }
        }
    }

    public static Vec3 RandomUnitVector()
    {
        return UnitVector(RandomInUnitSphere());
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

    public static Vec3 Refract(Vec3 uv, Vec3 n, double etaiOverEtat)
    {
        var cosTheta = Min(Dot(-uv, n), 1.0);
        var rOutPerp = etaiOverEtat * (uv + cosTheta * n);
        var rOutParallel = -Sqrt(Abs(1 - rOutPerp.LengthSquared)) * n;
        return rOutPerp + rOutParallel;
    }

    public static Vec3 ColorBlack => V3(0, 0, 0);
    public static Vec3 ColorWhite => V3(1, 1, 1);
    public static Vec3 ColorRed => V3(1, 0, 0);
    public static Vec3 ColorBlue => V3(0, 0, 1);

    public static double Lerp(double t, double v1, double v2)
    {
        return (1 - t) * v1 + t * v2;
    }

    public static bool IsNearZero(double v)
    {
        const double S = 1e-8;
        return Abs(v) < S;
    }

    public static bool IntersectRayBox(Ray ray, Box3 box)
    {
        return IntersectRayBox(ray, box, 0, double.PositiveInfinity);
    }

    public static bool IntersectRayBox(Ray ray, Box3 box, double tMin, double tMax)
    {
        var invDirX = 1 / ray.Direction.X;
        var invDirY = 1 / ray.Direction.Y;
        var invDirZ = 1 / ray.Direction.Z;

        var t0x = (box.Min.X - ray.Origin.X) * invDirX;
        var t0y = (box.Min.Y - ray.Origin.Y) * invDirY;
        var t0z = (box.Min.Z - ray.Origin.Z) * invDirZ;

        var t1x = (box.Max.X - ray.Origin.X) * invDirX;
        var t1y = (box.Max.Y - ray.Origin.Y) * invDirY;
        var t1z = (box.Max.Z - ray.Origin.Z) * invDirZ;

        if (invDirX < 0) (t0x, t1x) = (t1x, t0x);
        if (invDirY < 0) (t0y, t1y) = (t1y, t0y);
        if (invDirZ < 0) (t0z, t1z) = (t1z, t0z);

        tMin = t0x > tMin ? t0x : tMin;
        tMax = t1x < tMax ? t1x : tMax;
        if (tMax <= tMin)
        {
            return false;
        }

        tMin = t0y > tMin ? t0y : tMin;
        tMax = t1y < tMax ? t1y : tMax;
        if (tMax <= tMin)
        {
            return false;
        }

        tMin = t0z > tMin ? t0z : tMin;
        tMax = t1z < tMax ? t1z : tMax;
        if (tMax <= tMin)
        {
            return false;
        }

        return true;
    }
}
