namespace RTI1W;

public abstract class Hittable
{
    public abstract bool Hit(Ray r, float tMin, float tMax, out HitRecord hit);
    public abstract Box3 GetBoundingBox();
}

public struct HitRecord
{
    public Vector3 P;
    public Vector3 Normal;
    public Material Material;
    public float T;
    public bool FrontFace;

    public void SetFaceNormal(Ray r, Vector3 outwardNormal)
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
    private readonly int axis;

    public BvhHittable(Box3 bounds, Hittable left, Hittable right, int axis)
    {
        this.bounds = bounds;
        this.left = left;
        this.right = right;
        this.axis = axis;
    }

    public override Box3 GetBoundingBox()
    {
        return bounds;
    }

    public override bool Hit(Ray r, float tMin, float tMax, out HitRecord hit)
    {
        Metrics.EventRayBvh();

        // Ignore if no intersection
        if (!IntersectRayBox(r, bounds, tMin, tMax))
        {
            hit = default;
            return false;
        }

        var (a, b) = r.Direction[axis] < 0 ? (right, left) : (left, right);

        if (a.Hit(r, tMin, tMax, out hit))
        {
            if (b.Hit(r, tMin, hit.T, out var hit2) && hit2.T < hit.T)
            {
                hit = hit2;
            }
            return true;
        }
        else if (b.Hit(r, tMin, tMax, out hit))
        {
            return true;
        }

        return false;
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

    public override bool Hit(Ray r, float tMin, float tMax, out HitRecord hit)
    {
        hit = default;

        var hasHit = false;
        var closestSoFar = tMax;

        foreach (var obj in List)
        {
            if (obj.Hit(r, tMin, closestSoFar, out var objHit))
            {
                hasHit = true;
                hit = objHit;
                closestSoFar = objHit.T;
            }
        }

        return hasHit;
    }

    public override Box3 GetBoundingBox()
    {
        return BoundingBox;
    }
}

public class Triangle : Hittable
{
    public readonly Vector3 P0, P1, P2;
    public readonly Material Material;

    private readonly Box3 BoundingBox;
    private readonly Vector3 N;

    public Triangle(Vector3 p0, Vector3 p1, Vector3 p2, Material material)
    {
        P0 = p0;
        P1 = p1;
        P2 = p2;
        Material = material;

        var min = Vector3.Min(Vector3.Min(p0, p1), p2);
        var max = Vector3.Max(Vector3.Max(p0, p1), p2);
        BoundingBox = new Box3(min, max);

        N = Cross(p1 - p0, p2 - p0);
    }

    public override Box3 GetBoundingBox()
    {
        return BoundingBox;
    }

    public override bool Hit(Ray r, float tMin, float tMax, out HitRecord hit)
    {
        Metrics.EventRayTriangle();

        hit = default;

        var d = -Dot(N, P0);

        var n_dot_v = Dot(N, r.Direction);
        if (IsNearZero(n_dot_v))
        {
            return false;
        }

        var nom = Dot(N, r.Origin) + d;

        var t = -(nom / n_dot_v);
        if (t < tMin || t > tMax)
        {
            return false;
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
            return false;
        }

        hit = new HitRecord()
        {
            P = p,
            T = t,
            Material = Material,
        };
        hit.SetFaceNormal(r, N);
        return true;
    }
}

public class Sphere : Hittable
{
    public Vector3 Center;
    public float Radius;
    public Material Material;

    public Sphere(Vector3 center, float radius, Material material)
    {
        Center = center;
        Radius = radius;
        Material = material;
    }

    public override Box3 GetBoundingBox()
    {
        var half = new Vector3(Radius);
        return new Box3(Center - half, Center + half);
    }

    public override bool Hit(Ray r, float tMin, float tMax, out HitRecord hit)
    {
        Metrics.EventRaySphere();

        hit = default;

        var oc = r.Origin - Center;
        var a = r.Direction.LengthSquared();
        var halfB = Dot(oc, r.Direction);
        var c = oc.LengthSquared() - Radius * Radius;

        var discriminant = halfB * halfB - a * c;
        if (discriminant < 0)
        {
            return false;
        }
        var sqrtD = Sqrt(discriminant);

        var root = (-halfB - sqrtD) / a;

        if (root < tMin || tMax < root)
        {
            root = (-halfB + sqrtD) / a;
            if (root < tMin || tMax < root)
            {
                return false;
            }
        }

        var t = root;
        var p = r.At(t);
        var outwardNormal = (p - Center) / Radius;

        hit = new HitRecord()
        {
            P = p,
            T = t,
            Material = Material,
        };
        hit.SetFaceNormal(r, outwardNormal);
        return true;
    }
}
