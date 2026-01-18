using System.Diagnostics.CodeAnalysis;

namespace RTI1W;

public struct Vec3 : IEquatable<Vec3>
{
    public float X;
    public float Y;
    public float Z;

    public Vec3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public float LengthSquared => X * X + Y * Y + Z * Z;

    public float Length => Sqrt(LengthSquared);

    public bool IsNearZero()
    {
        const float S = 1e-8f;
        return Abs(X) < S && Abs(Y) < S && Abs(Z) < S;
    }

    public static Vec3 operator +(Vec3 a)
    {
        return new Vec3(+a.X, +a.Y, +a.Z);
    }

    public static Vec3 operator +(Vec3 a, Vec3 b)
    {
        return new Vec3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    }

    public static Vec3 operator -(Vec3 a)
    {
        return new Vec3(-a.X, -a.Y, -a.Z);
    }

    public static Vec3 operator -(Vec3 a, Vec3 b)
    {
        return new Vec3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    }

    public static Vec3 operator *(Vec3 a, Vec3 b)
    {
        return new Vec3(a.X * b.X, a.Y * b.Y, a.Z * b.Z);
    }

    public static Vec3 operator *(Vec3 a, float b)
    {
        return new Vec3(a.X * b, a.Y * b, a.Z * b);
    }

    public static Vec3 operator *(float a, Vec3 b)
    {
        return new Vec3(a * b.X, a * b.Y, a * b.Z);
    }

    public static Vec3 operator /(Vec3 a, Vec3 b)
    {
        return new Vec3(a.X / b.X, a.Y / b.Y, a.Z / b.Z);
    }

    public static Vec3 operator /(Vec3 a, float b)
    {
        return new Vec3(a.X / b, a.Y / b, a.Z / b);
    }

    public static Vec3 operator /(float a, Vec3 b)
    {
        return new Vec3(a / b.X, a / b.Y, a / b.Z);
    }

    public override string ToString()
    {
        return $"{X} {Y} {Z}";
    }

    public bool Equals(Vec3 other)
    {
        return X == other.X && Y == other.Y && Z == other.Z;
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if (obj is Vec3 v)
        {
            return Equals(v);
        }
        return base.Equals(obj);
    }

    public override int GetHashCode()
    {
        return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
    }

    public static bool operator ==(Vec3 left, Vec3 right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Vec3 left, Vec3 right)
    {
        return !(left == right);
    }
}
