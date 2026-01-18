namespace RTI1W;

public struct Box3 : IEquatable<Box3>
{
    public Vector3 Min;
    public Vector3 Max;

    public Box3()
    {
        Min = new Vector3(float.MaxValue);
        Max = new Vector3(float.MinValue);
    }

    public Box3(Vector3 p)
    {
        Min = Max = p;
    }

    public Box3(Vector3 p1, Vector3 p2)
    {
        Min = Vector3.Min(p1, p2);
        Max = Vector3.Max(p1, p2);
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
        return Vector3.Lerp(Min, Max, t);
    }

    public static Box3 Union(Box3 b1, Box3 b2)
    {
        return new Box3()
        {
            Min = Vector3.Min(b1.Min, b2.Min),
            Max = Vector3.Max(b1.Max, b2.Max),
        };
    }

    public static Box3 Intersect(Box3 b1, Box3 b2)
    {
        return new Box3()
        {
            Min = Vector3.Max(b1.Min, b2.Min),
            Max = Vector3.Min(b1.Max, b2.Max),
        };
    }

    public static bool Overlaps(Box3 b1, Box3 b2)
    {
        return Vector3.GreaterThanOrEqualAll(b1.Max, b2.Min) &&
            Vector3.LessThanOrEqualAll(b1.Min, b2.Max);
    }
}
