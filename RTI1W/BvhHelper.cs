namespace RTI1W;

public static class BvhHelper
{
    // See https://pbr-book.org/4ed/Primitives_and_Intersection_Acceleration/Bounding_Volume_Hierarchies

    public static BvhHittable CreateBvh(List<Hittable> hittables)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(hittables.Count, 2, nameof(hittables));

        var nodes = new NodeInfo[hittables.Count];
        for (var i = 0; i < hittables.Count; i++)
        {
            var h = hittables[i];
            nodes[i] = new NodeInfo(h, h.GetBoundingBox(), h.GetBoundingBox().Middle());
        }

        return (BvhHittable)CreateBvh(nodes, 0);
    }

    private record class NodeInfo(Hittable Hittable, Box3 Bounds, Vector3 Middle);

    private static Hittable CreateBvh(Span<NodeInfo> nodes, int depth)
    {
        if (nodes.Length == 1)
        {
            return nodes[0].Hittable;
        }

        if (nodes.Length <= 4 || depth > 32)
        {
            return CreateHittableList(nodes);
        }

        var bounds = nodes[0].Bounds;
        for (var i = 1; i < nodes.Length; i++)
        {
            bounds = Box3.Union(bounds, nodes[i].Bounds);
        }

        var noSplitCost = nodes.Length;
        var (cost, axis, pos) = FindBestSplit(nodes, bounds);
        if (cost >= noSplitCost)
        {
            return CreateHittableList(nodes);
        }

        // partition nodes by <pos, RHS starts at p
        var p = 0;
        var q = nodes.Length - 1;
        while (p <= q)
        {
            if (nodes[p].Middle[axis] < pos)
            {
                p++;
            }
            else
            {
                (nodes[q], nodes[p]) = (nodes[p], nodes[q]);
                q--;
            }
        }

        var leftNodes = nodes.Slice(0, p);
        var rightNodes = nodes.Slice(p);

        var left = CreateBvh(leftNodes, depth + 1);
        var right = CreateBvh(rightNodes, depth + 1);

        var sah = new BvhHittable(bounds, left, right, axis);

        return sah;
    }

    private static HittableList CreateHittableList(Span<NodeInfo> nodes)
    {
        var list = new HittableList();
        foreach (var node in nodes)
        {
            list.Add(node.Hittable);
        }
        return list;
    }

    private const float TraverseCost = 0.5f;
    private const float IntersectCost = 1.0f;

    private static (float Cost, int Axis, float Pos) FindBestSplit(Span<NodeInfo> nodes, Box3 bounds)
    {
        var bestCost = float.PositiveInfinity;
        var bestAxis = 0;
        var bestPos = 0.0f;

        Span<float> splitPositions = stackalloc float[16];

        for (var axis = 0; axis < 3; axis++)
        {
            if (!SetCandidatePositions(splitPositions, nodes, axis))
            {
                continue;
            }

            foreach (var pos in splitPositions)
            {
                var leftBounds = new Box3();
                var rightBounds = new Box3();
                var leftCount = 0;
                var rightCount = 0;

                foreach (var node in nodes)
                {
                    if (node.Middle[axis] < pos)
                    {
                        leftBounds = Box3.Union(leftBounds, node.Bounds);
                        leftCount++;
                    }
                    else
                    {
                        rightBounds = Box3.Union(rightBounds, node.Bounds);
                        rightCount++;
                    }
                }

                var cost = EvaluateSah(bounds, leftBounds, rightBounds, leftCount, rightCount);

                if (cost < bestCost)
                {
                    bestCost = cost;
                    bestAxis = axis;
                    bestPos = pos;
                }
            }
        }

        return (bestCost, bestAxis, bestPos);
    }

    private static float EvaluateSah(Box3 parentBounds, Box3 leftBounds, Box3 rightBounds, int leftCount, int rightCount)
    {
        var parentSA = CalculateSurfaceArea(parentBounds);
        var leftSA = CalculateSurfaceArea(leftBounds);
        var rightSA = CalculateSurfaceArea(rightBounds);

        var cost = TraverseCost +
            leftSA / parentSA * leftCount * IntersectCost +
            rightSA / parentSA * rightCount * IntersectCost;

        return cost;
    }

    private static float CalculateSurfaceArea(Box3 bounds)
    {
        var size = bounds.Size();
        return 2 * (size.X * size.Y + size.Y * size.Z + size.Z * size.X);
    }

    // Returns false if we cannot provide candidate positions
    private static bool SetCandidatePositions(Span<float> positions, Span<NodeInfo> nodes, int axis)
    {
        var min = float.MaxValue;
        var max = float.MinValue;
        foreach (var node in nodes)
        {
            min = Min(min, node.Middle[axis]);
            max = Max(max, node.Middle[axis]);
        }

        if (min > max || (max - min) < 1e-5f)
        {
            return false;
        }

        var inc = (max - min) / (positions.Length + 1);

        for (var i = 0; i < positions.Length; i++)
        {
            positions[i] = min + (i + 1) * inc;
        }

        return true;
    }
}

