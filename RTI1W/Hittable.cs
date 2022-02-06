namespace RTI1W;

public abstract class Hittable
{
    public abstract HitRecord? Hit(Ray r, double tMin, double tMax);
}

public struct HitRecord
{
    public Vec3 P;
    public Vec3 Normal;
    public Material Material;
    public double T;
    public bool FrontFace;

    public void SetFaceNormal(Ray r, Vec3 outwardNormal)
    {
        FrontFace = Dot(r.Direction, outwardNormal) < 0;
        Normal = FrontFace ? outwardNormal : -outwardNormal;
    }
}

public class HittableList : Hittable
{
    public List<Hittable> List = new List<Hittable>();

    public override HitRecord? Hit(Ray r, double tMin, double tMax)
    {
        HitRecord? rec = null;
        var closestSoFar = tMax;

        foreach (var obj in List)
        {
            var objRec = obj.Hit(r, tMin, closestSoFar);
            if (objRec.HasValue)
            {
                rec = objRec;
                closestSoFar = objRec.Value.T;
            }
        }

        return rec;
    }
}

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
