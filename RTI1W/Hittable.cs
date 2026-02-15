namespace RTI1W;

public abstract class Hittable
{
    public Box3 Bounds;
    public abstract bool Hit(in Ray r, float tMin, float tMax, out HitRecord hit);
}

public struct HitRecord
{
    public Vector3 P;
    public Vector3 Normal;
    public Material Material;
    public float T;
    public bool FrontFace;

    public void SetFaceNormal(in Ray r, Vector3 outwardNormal)
    {
        FrontFace = Dot(r.Direction, outwardNormal) < 0;
        Normal = FrontFace ? outwardNormal : -outwardNormal;
    }
}

public class BvhHittable : Hittable
{
    public readonly Hittable Left;
    public readonly Hittable Right;
    public readonly int Axis;

    public BvhHittable(Box3 bounds, Hittable left, Hittable right, int axis)
    {
        Bounds = bounds;
        Left = left;
        Right = right;
        Axis = axis;
    }

    public override bool Hit(in Ray r, float tMin, float tMax, out HitRecord hit)
    {
        Metrics.EventRayBvh();

        // Ignore if no intersection
        if (!IntersectRayBox(r, Bounds, tMin, tMax))
        {
            hit = default;
            return false;
        }

        var (a, b) = r.Direction[Axis] < 0 ? (Right, Left) : (Left, Right);

        if (a.Hit(r, tMin, tMax, out hit))
        {
            if (b.Hit(r, tMin, hit.T, out var hit2))
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
    public List<Hittable> List;

    public HittableList(List<Hittable> list)
    {
        List = list;

        Bounds = list[0].Bounds;
        for (int i = 1; i < list.Count; i++)
        {
            Bounds = Box3.Union(list[i].Bounds, Bounds);
        }
    }

    public override bool Hit(in Ray r, float tMin, float tMax, out HitRecord hit)
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
}

public class Triangle : Hittable
{
    public readonly Vector3 P0, P1, P2;
    public readonly Material Material;

    private readonly Vector3 N;

    public Triangle(Vector3 p0, Vector3 p1, Vector3 p2, Material material)
    {
        P0 = p0;
        P1 = p1;
        P2 = p2;
        Material = material;

        var min = Vector3.Min(Vector3.Min(p0, p1), p2);
        var max = Vector3.Max(Vector3.Max(p0, p1), p2);
        Bounds = new Box3(min, max);

        N = Cross(p1 - p0, p2 - p0);
    }

    public override bool Hit(in Ray r, float tMin, float tMax, out HitRecord hit)
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

        var half = new Vector3(radius);
        Bounds.Min = Center - half;
        Bounds.Max = Center + half;
    }

    public override bool Hit(in Ray r, float tMin, float tMax, out HitRecord hit)
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
