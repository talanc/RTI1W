using System.Diagnostics.CodeAnalysis;

namespace RTI1W;

public struct Vec3 : IEquatable<Vec3>
{
    public double X;
    public double Y;
    public double Z;

    public Vec3(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public double LengthSquared => X * X + Y * Y + Z * Z;

    public double Length => Sqrt(LengthSquared);

    public bool IsNearZero()
    {
        const double S = 1e-8;
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

    public static Vec3 operator *(Vec3 a, double b)
    {
        return new Vec3(a.X * b, a.Y * b, a.Z * b);
    }

    public static Vec3 operator *(double a, Vec3 b)
    {
        return new Vec3(a * b.X, a * b.Y, a * b.Z);
    }

    public static Vec3 operator /(Vec3 a, Vec3 b)
    {
        return new Vec3(a.X / b.X, a.Y / b.Y, a.Z / b.Z);
    }

    public static Vec3 operator /(Vec3 a, double b)
    {
        return new Vec3(a.X / b, a.Y / b, a.Z / b);
    }

    public static Vec3 operator /(double a, Vec3 b)
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
