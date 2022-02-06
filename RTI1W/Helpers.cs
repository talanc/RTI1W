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

    public static void WriteColor(Vec3 pixelColor, int samplesPerPixel)
    {
        var scale = 1.0 / samplesPerPixel;

        var r = Sqrt(pixelColor.X * scale);
        var g = Sqrt(pixelColor.Y * scale);
        var b = Sqrt(pixelColor.Z * scale);

        var ir = (int)(256 * Clamp(r, 0, 0.999));
        var ig = (int)(256 * Clamp(g, 0, 0.999));
        var ib = (int)(256 * Clamp(b, 0, 0.999));

        WriteLine($"{ir} {ig} {ib}");
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
            if (p.LengthSquared >= 1)
            {
                continue;
            }
            return p;
        }
    }

    public static Vec3 RandomUnitVector()
    {
        return Vec3.UnitVector(RandomInUnitSphere());
    }

    public static Vec3 RandomInHemisphere(Vec3 normal)
    {
        var inUnitSphere = RandomInUnitSphere();
        if (Vec3.Dot(inUnitSphere, normal) > 0)
        {
            return inUnitSphere;
        }
        return -inUnitSphere;
    }

    public static Vec3 Reflect(Vec3 v, Vec3 n)
    {
        return v - 2 * Vec3.Dot(v, n) * n;
    }

    public static Vec3 Refract(Vec3 uv, Vec3 n, double etaiOverEtat)
    {
        var cosTheta = Min(Vec3.Dot(-uv, n), 1.0);
        var rOutPerp = etaiOverEtat * (uv + cosTheta * n);
        var rOutParallel = -Sqrt(Abs(1 - rOutPerp.LengthSquared)) * n;
        return rOutPerp + rOutParallel;
    }

    public static Vec3 ColorBlack => V3(0, 0, 0);
    public static Vec3 ColorWhite => V3(1, 1, 1);
}
