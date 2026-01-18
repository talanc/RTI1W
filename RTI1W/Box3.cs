namespace RTI1W;

public struct Box3 : IEquatable<Box3>
{
    public Vector3 Min;
    public Vector3 Max;

    public Box3()
    {
        var minVal = float.MinValue;
        var maxVal = float.MaxValue;
        Min = V3(maxVal, maxVal, maxVal);
        Max = V3(minVal, minVal, minVal);
    }

    public Box3(Vector3 p)
    {
        Min = Max = p;
    }

    public Box3(Vector3 p1, Vector3 p2)
    {
        Min = V3(Min(p1.X, p2.X), Min(p1.Y, p2.Y), Min(p1.Z, p2.Z));
        Max = V3(Max(p1.X, p2.X), Max(p1.Y, p2.Y), Max(p1.Z, p2.Z));
    }

    public bool Equals(Box3 other)
    {
        return Min == other.Min && Max == other.Max;
    }

    public override bool Equals(object? obj)
    {
        return obj is Box3 box && Equals(box);
    }

    public static bool operator ==(Box3 left, Box3 right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Box3 left, Box3 right)
    {
        return !(left == right);
    }

    public override int GetHashCode()
    {
        return Min.GetHashCode() ^ Max.GetHashCode();
    }

    public bool Contains(Vector3 p)
    {
        return Min.X <= p.X && Min.Y <= p.Y && Min.Z <= p.Z &&
            p.X <= Max.X && p.Y <= Max.Y && p.Z <= Max.Z;
    }

    public Vector3 GetSize()
    {
        return Max - Min;
    }

    public Vector3 GetMiddle()
    {
        return Min + GetSize() / 2;
    }

    public float GetVolume()
    {
        var size = GetSize();
        return size.X * size.Y * size.Z;
    }

    public Vector3 Lerp(Vector3 t)
    {
        return V3(
            Helpers.Lerp(t.X, Min.X, Max.X),
            Helpers.Lerp(t.Y, Min.Y, Max.Y),
            Helpers.Lerp(t.Z, Min.Z, Max.Z));
    }

    public static Box3 Union(Box3 b1, Box3 b2)
    {
        return new Box3(
            V3(Min(b1.Min.X, b2.Min.X), Min(b1.Min.Y, b2.Min.Y), Min(b1.Min.Z, b2.Min.Z)),
            V3(Max(b1.Max.X, b2.Max.X), Max(b1.Max.Y, b2.Max.Y), Max(b1.Max.Z, b2.Max.Z)));
    }

    public static Box3 Intersect(Box3 b1, Box3 b2)
    {
        return new Box3(
            V3(Max(b1.Min.X, b2.Min.X), Max(b1.Min.Y, b2.Min.Y), Max(b1.Min.Z, b2.Min.Z)),
            V3(Min(b1.Max.X, b2.Max.X), Min(b1.Max.Y, b2.Max.Y), Min(b1.Max.Z, b2.Max.Z)));
    }

    public static bool Overlaps(Box3 b1, Box3 b2)
    {
        return
            b1.Max.X >= b2.Min.X && b1.Min.X <= b2.Max.X &&
            b1.Max.Y >= b2.Min.Y && b1.Min.Y <= b2.Max.Y &&
            b1.Max.Z >= b2.Min.Z && b1.Min.Z <= b2.Max.Z;
    }
}
