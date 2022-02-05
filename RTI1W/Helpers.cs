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

    public static void WriteColor(Vec3 pixelColor)
    {
        var ir = (int)(255.999 * pixelColor.X);
        var ig = (int)(255.999 * pixelColor.Y);
        var ib = (int)(255.999 * pixelColor.Z);

        WriteLine($"{ir} {ig} {ib}");
    }

    public static double DegreesToRadians(double deg)
    {
        return deg * PI / 180;
    }

    private static Random random = new Random();

    public static double RandomDouble()
    {
        return random.NextDouble();
    }

    public static double RandomDouble(double min, double max)
    {
        return min + (max - min) * RandomDouble();
    }
}
