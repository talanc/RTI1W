namespace RTI1W;

public abstract class Hittable
{
    public abstract HitRecord? Hit(Ray r, double tMin, double tMax);
    public abstract Box3 GetBoundingBox();
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

public class BvhHittable : Hittable
{
    private readonly Box3 bounds;
    private readonly Hittable left;
    private readonly Hittable right;

    public BvhHittable(Box3 bounds, Hittable left, Hittable right)
    {
        this.bounds = bounds;
        this.left = left;
        this.right = right;
    }

    public override Box3 GetBoundingBox()
    {
        return bounds;
    }

    public override HitRecord? Hit(Ray r, double tMin, double tMax)
    {
        // Ignore if no intersection
        Metrics.EventRayBox();
        if (!IntersectRayBox(r, bounds, tMin, tMax))
        {
            return null;
        }

        var hitLeft = left.Hit(r, tMin, tMax);
        var hitRight = right.Hit(r, tMin, hitLeft?.T ?? tMax);

        return hitRight ?? hitLeft;
    }
}

public class HittableList : Hittable
{
    public List<Hittable> List = new();
    private Box3 BoundingBox;

    public void Add(Hittable hittable)
    {
        var hittableBox = hittable.GetBoundingBox();
        BoundingBox = Box3.Union(BoundingBox, hittableBox);

        List.Add(hittable);
    }

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

    public override Box3 GetBoundingBox()
    {
        return BoundingBox;
    }
}

public class Triangle : Hittable
{
    public readonly Vec3 P0, P1, P2;
    public readonly Material Material;

    private readonly Box3 BoundingBox;
    private readonly Vec3 N;

    public Triangle(Vec3 p0, Vec3 p1, Vec3 p2, Material material)
    {
        P0 = p0;
        P1 = p1;
        P2 = p2;
        Material = material;

        var minX = Min(Min(p0.X, p1.X), p2.X);
        var minY = Min(Min(p0.Y, p1.Y), p2.Y);
        var minZ = Min(Min(p0.Z, p1.Z), p2.Z);
        var maxX = Max(Max(p0.X, p1.X), p2.X);
        var maxY = Max(Max(p0.Y, p1.Y), p2.Y);
        var maxZ = Max(Max(p0.Z, p1.Z), p2.Z);
        BoundingBox = new Box3(V3(minX, minY, minZ), V3(maxX, maxY, maxZ));

        N = Cross(p1 - p0, p2 - p0);
    }

    public override Box3 GetBoundingBox()
    {
        return BoundingBox;
    }

    public override HitRecord? Hit(Ray r, double tMin, double tMax)
    {
        Metrics.EventRayTriangle();

        var d = -Dot(N, P0);

        var n_dot_v = Dot(N, r.Direction);
        if (IsNearZero(n_dot_v))
        {
            return null;
        }

        var nom = Dot(N, r.Origin) + d;

        var t = -(nom / n_dot_v);
        if (t < tMin || t > tMax)
        {
            return null;
        }

        var p = r.At(t);

        var e0 = P1 - P0;
        var e1 = P2 - P1;
        var e2 = P0 - P2;

        var test0 = Dot(N, Cross(e0, p - P0));
        var test1 = Dot(N, Cross(e1, p - P1));
        var test2 = Dot(N, Cross(e2, p - P2));
        if (test0 < 0 || test1 < 0 || test2 < 0)
        {
            return null;
        }

        var rec = new HitRecord()
        {
            P = p,
            T = t,
            Material = Material,
        };
        rec.SetFaceNormal(r, N);
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

    public override Box3 GetBoundingBox()
    {
        var half = V3(Radius, Radius, Radius);
        return new Box3(Center - half, Center + half);
    }

    public override HitRecord? Hit(Ray r, double tMin, double tMax)
    {
        Metrics.EventRaySphere();

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

public class MovingSphere : Hittable
{
    public Vec3 Center0, Center1;
    public double Time0, Time1;
    public double Radius;
    public Material Material;

    public MovingSphere(Vec3 center0, Vec3 center1, double time0, double time1, double radius, Material material)
    {
        Center0 = center0;
        Center1 = center1;
        Time0 = time0;
        Time1 = time1;
        Radius = radius;
        Material = material;
    }

    public Vec3 GetCenter(double time)
    {
        return Center0 + ((time - Time0) / (Time1 - Time0)) * (Center1 - Center0);
    }

    public override Box3 GetBoundingBox()
    {
        var half = V3(Radius, Radius, Radius);
        var box0 = new Box3(Center0 - half, Center0 + half);
        var box1 = new Box3(Center1 - half, Center1 + half);
        return Box3.Union(box0, box1);
    }

    public override HitRecord? Hit(Ray r, double tMin, double tMax)
    {
        Metrics.EventRaySphere();

        var center = GetCenter(r.Time);

        var oc = r.Origin - center;
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
        var outwardNormal = (p - center) / Radius;
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
