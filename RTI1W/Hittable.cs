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

public class BvhNode
{
    public Box3 Bounds;
    public BvhNode? A, B;
    public Hittable? Item;
}

public class BvhHittable : Hittable
{
    private readonly BvhNode root;

    public BvhHittable(BvhNode root)
    {
        this.root = root;
    }

    public override Box3 GetBoundingBox()
    {
        return root.Bounds;
    }

    public override HitRecord? Hit(Ray r, double tMin, double tMax)
    {
        return NodeHit(root, r, tMin, tMax);
    }

    public HitRecord? NodeHit(BvhNode? node, Ray r, double tMin, double tMax)
    {
        if (node == null)
        {
            return null;
        }

        // Ignore if no intersection
        if (!IntersectRayBox(r, node.Bounds, tMin, tMax))
        {
            return null;
        }

        if (node.Item != null)
        {
            return node.Item.Hit(r, tMin, tMax);
        }

        var hitA = NodeHit(node.A, r, tMin, tMax);
        var closestSoFar = hitA?.T ?? tMax;
        var hitB = NodeHit(node.B, r, tMin, closestSoFar);

        if (hitA.HasValue && hitB.HasValue)
        {
            if (hitA.Value.T <= hitB.Value.T)
            {
                return hitA;
            }
            return hitB;
        }

        return hitA ?? hitB ?? null;
    }
}

public class BvhHelper
{
    public static BvhNode CreateBvh(List<Hittable> hittables)
    {
        if (hittables.Count == 0)
        {
            throw new InvalidOperationException();
        }

        var box = hittables[0].GetBoundingBox();
        for (var i = 1; i < hittables.Count; i++)
        {
            box = Box3.Union(box, hittables[i].GetBoundingBox());
        }

        if (hittables.Count == 1)
        {
            var item = hittables[0];
            return new BvhNode()
            {
                Bounds = box,
                Item = item
            };
        }

        if (hittables.Count == 2)
        {
            var item1 = hittables[0];
            var item2 = hittables[1];
            return new BvhNode()
            {
                Bounds = box,
                A = new BvhNode()
                {
                    Bounds = item1.GetBoundingBox(),
                    Item = item1
                },
                B = new BvhNode()
                {
                    Bounds = item2.GetBoundingBox(),
                    Item = item2
                }
            };
        }

        var size = box.GetSize();
        if (size.X > size.Y && size.X > size.Z)
        {
            hittables.Sort((a, b) => (int)((a.GetBoundingBox().GetMiddle().X - b.GetBoundingBox().GetMiddle().X) * 1000));
        }
        else if (size.Y > size.Z)
        {
            hittables.Sort((a, b) => (int)((a.GetBoundingBox().GetMiddle().Y - b.GetBoundingBox().GetMiddle().Y) * 1000));
        }
        else
        {
            hittables.Sort((a, b) => (int)((a.GetBoundingBox().GetMiddle().Z - b.GetBoundingBox().GetMiddle().Z) * 1000));
        }

        var size1 = hittables.Count / 2;
        var size2 = hittables.Count - size1;

        var partition1 = hittables.GetRange(0, size1);
        var partition2 = hittables.GetRange(size1, size2);
        return new BvhNode()
        {
            Bounds = box,
            A = CreateBvh(partition1),
            B = CreateBvh(partition2)
        };
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
