﻿namespace RTI1W;

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
