namespace RTI1W;

public class Sphere : Hittable
{
    public Vec3 Center;
    public double Radius;
    public Material Material;

    public Sphere(Vec3 center, double radius, Material material)
    {
        Center = center;
        Radius = radius;
        Material = material;
    }

    public override HitRecord? Hit(Ray r, double tMin, double tMax)
    {
        var oc = r.Origin - Center;
        var a = r.Direction.LengthSquared;
        var halfB = Dot(oc, r.Direction);
        var c = oc.LengthSquared - Radius * Radius;

        var discriminant = halfB * halfB - a * c;
        if (discriminant < 0)
        {
            return null;
        }
        var sqrtD = Sqrt(discriminant);

        var root = (-halfB - sqrtD) / a;

        if (root < tMin || tMax < root)
        {
            root = (-halfB + sqrtD) / a;
            if (root < tMin || tMax < root)
            {
                return null;
            }
        }

        var t = root;
        var p = r.At(t);
        var outwardNormal = (p - Center) / Radius;
        var rec = new HitRecord()
        {
            P = p,
            T = t,
            Material = Material,
        };
        rec.SetFaceNormal(r, outwardNormal);
        return rec;
    }
}
