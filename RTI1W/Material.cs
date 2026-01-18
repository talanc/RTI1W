namespace RTI1W;

public abstract class Material
{
    public abstract ScatterRecord? Scatter(Ray rayIn, HitRecord rec);
}

public struct ScatterRecord
{
    public Vector3 Attenuation;
    public Ray Scattered;
}

public class Lambertian : Material
{
    public Vector3 Albedo;

    public Lambertian(Vector3 albedo)
    {
        Albedo = albedo;
    }

    public override ScatterRecord? Scatter(Ray rayIn, HitRecord rec)
    {
        var scatterDirection = rec.Normal + RandomUnitVector();

        if (IsNearZero(scatterDirection))
        {
            scatterDirection = rec.Normal;
        }

        return new ScatterRecord()
        {
            Scattered = new Ray(rec.P, scatterDirection),
            Attenuation = Albedo
        };
    }
}

public class Metal : Material
{
    public Vector3 Albedo;
    public float Fuzz;

    public Metal(Vector3 albedo, float fuzz)
    {
        Albedo = albedo;
        Fuzz = fuzz < 1 ? fuzz : 1;
    }

    public override ScatterRecord? Scatter(Ray rayIn, HitRecord rec)
    {
        var reflected = Reflect(UnitVector(rayIn.Direction), rec.Normal);
        return new ScatterRecord()
        {
            Scattered = new Ray(rec.P, reflected + Fuzz * RandomInUnitSphere()),
            Attenuation = Albedo
        };
    }
}

public class Dielectric : Material
{
    public float Ir;

    public Dielectric(float ir)
    {
        Ir = ir;
    }

    public override ScatterRecord? Scatter(Ray rayIn, HitRecord rec)
    {
        var reflectionRatio = rec.FrontFace ? 1 / Ir : Ir;

        var unitDirection = UnitVector(rayIn.Direction);
        var cosTheta = Min(Dot(-unitDirection, rec.Normal), 1);
        var sinTheta = Sqrt(1.0f - cosTheta * cosTheta);

        var cannotRefract = reflectionRatio * sinTheta > 1;
        Vector3 direction;
        if (cannotRefract || Reflectance(cosTheta, reflectionRatio) > RandomValue())
        {
            direction = Reflect(unitDirection, rec.Normal);
        }
        else
        {
            direction = Refract(unitDirection, rec.Normal, reflectionRatio);
        }

        return new ScatterRecord()
        {
            Attenuation = ColorWhite,
            Scattered = new Ray(rec.P, direction)
        };
    }

    private float Reflectance(float cosine, float refIdx)
    {
        var r0 = ((1 - refIdx) / (1 + refIdx));
        r0 = r0 * r0;
        return r0 + (1 - r0) * Pow(1 - cosine, 5);
    }
}