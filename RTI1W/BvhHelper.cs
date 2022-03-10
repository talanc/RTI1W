namespace RTI1W;

public class BvhHelper
{
    public static BvhHittable CreateBvh(List<Hittable> hittables)
    {
        var hittableBoxArr = new NodeInfo[hittables.Count];
        for (var i = 0; i < hittableBoxArr.Length; i++)
        {
            var hittable = hittables[i];
            var bounds = hittable.GetBoundingBox();
            hittableBoxArr[i] = new NodeInfo(hittable, bounds, bounds.GetMiddle());
        }

        var span = new Span<NodeInfo>(hittableBoxArr);
        return CreateBvh(span);
    }

    private record class NodeInfo(Hittable Hittable, Box3 Bounds, Vec3 Middle);

    private static BvhHittable CreateBvh(Span<NodeInfo> span)
    {
        if (span.Length == 0)
        {
            throw new ArgumentException("span is empty", nameof(span));
        }

        var bounds = span[0].Bounds;
        for (var i = 1; i < span.Length; i++)
        {
            bounds = Box3.Union(bounds, span[i].Bounds);
        }

        if (span.Length == 1)
        {
            return new BvhHittable(bounds, span[0].Hittable, span[0].Hittable);
        }

        if (span.Length == 2)
        {
            return new BvhHittable(bounds, span[0].Hittable, span[1].Hittable);
        }

        var size = bounds.GetSize();
        if (size.X > size.Y && size.X > size.Z)
        {
            span.Sort((a, b) => (int)((a.Middle.X - b.Middle.X) * 1000));
        }
        else if (size.Y > size.Z)
        {
            span.Sort((a, b) => (int)((a.Middle.Y - b.Middle.Y) * 1000));
        }
        else
        {
            span.Sort((a, b) => (int)((a.Middle.Z - b.Middle.Z) * 1000));
        }

        var size1 = span.Length / 2;
        var size2 = span.Length - size1;

        var partition1 = span.Slice(0, size1);
        var partition2 = span.Slice(size1, size2);
        return new BvhHittable(bounds, CreateBvh(partition1), CreateBvh(partition2));
    }
}
