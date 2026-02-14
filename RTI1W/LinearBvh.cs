namespace RTI1W;

public struct LinearBvhNode
{
    public Box3 Bounds;
    public int PrimOffsetOrRightOffset;
    public ushort NumPrims;
    public byte Axis;
}

public static class LinearBvhHelper
{
    public static LinearBvhHittable CreateLinearBvh(List<Hittable> hittables)
    {
        var root = BvhHelper.CreateBvh(hittables);
        var linearNodes = new List<LinearBvhNode>();
        var primitives = new List<Hittable>();
        var offset = 0;
        Flatten(linearNodes, primitives, root, ref offset);

        return new LinearBvhHittable(linearNodes.ToArray(), primitives.ToArray())
        {
            Bounds = root.Bounds
        };
    }

    public static int Flatten(List<LinearBvhNode> linearNodes, List<Hittable> primitives, Hittable hittable, ref int offset)
    {
        var myOffset = offset;
        offset++;

        var nodeIdx = linearNodes.Count;
        var node = new LinearBvhNode();
        linearNodes.Add(node);

        if (hittable is Sphere or Triangle)
        {
            node.Bounds = hittable.Bounds;
            node.PrimOffsetOrRightOffset = primitives.Count;
            node.NumPrims = 1;
            primitives.Add(hittable);
        }
        else if (hittable is HittableList hittableList)
        {
            node.Bounds = hittableList.Bounds;
            node.PrimOffsetOrRightOffset = primitives.Count;
            node.NumPrims = (ushort)hittableList.List.Count;
            primitives.AddRange(hittableList.List);
        }
        else if (hittable is BvhHittable bvhHittable)
        {
            node.Bounds = bvhHittable.Bounds;
            node.Axis = (byte)bvhHittable.Axis;

            Flatten(linearNodes, primitives, bvhHittable.Left, ref offset);

            var offset2 = Flatten(linearNodes, primitives, bvhHittable.Right, ref offset);
            node.PrimOffsetOrRightOffset = offset2;
        }
        else
        {
            throw new NotImplementedException();
        }

        linearNodes[nodeIdx] = node;

        return myOffset;
    }
}

public class LinearBvhHittable : Hittable
{
    public LinearBvhNode[] LinearNodes;
    public Hittable[] Primitives;

    public LinearBvhHittable(LinearBvhNode[] linearNodes, Hittable[] primitives)
    {
        LinearNodes = linearNodes;
        Primitives = primitives;
    }

    public override bool Hit(in Ray r, float tMin, float tMax, out HitRecord hit)
    {
        var hasHit = false;
        hit = default;

        Span<int> nodeStack = stackalloc int[128];
        nodeStack[0] = 0;
        var nodeStackCount = 1;

        Span<bool> dirIsNeg = [r.InvDirection[0] < 0, r.InvDirection[1] < 0, r.InvDirection[2] < 0];

        while (true)
        {
            var nodeIndex = nodeStack[nodeStackCount - 1];
            nodeStackCount--;

            var node = LinearNodes[nodeIndex];

            var enter = false;
            if (node.NumPrims == 1)
            {
                enter = true;
            }
            else
            {
                Metrics.EventRayBvh();
                enter = IntersectRayBox(r, node.Bounds, tMin, tMax);
            }

            if (enter)
            {
                if (node.NumPrims > 0)
                {
                    for (var i = 0; i < node.NumPrims; i++)
                    {
                        if (Primitives[node.PrimOffsetOrRightOffset + i].Hit(r, tMin, tMax, out var hit2))
                        {
                            hit = hit2;
                            tMax = hit2.T;
                            hasHit = true;
                        }
                    }
                }
                else
                {
                    var a = nodeIndex + 1;
                    var b = node.PrimOffsetOrRightOffset;
                    nodeStack[nodeStackCount + 0] = dirIsNeg[node.Axis] ? a : b;
                    nodeStack[nodeStackCount + 1] = dirIsNeg[node.Axis] ? b : a;
                    nodeStackCount += 2;
                }
            }

            if (nodeStackCount == 0)
            {
                break;
            }
        }

        return hasHit;
    }
}