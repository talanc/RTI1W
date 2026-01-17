namespace RTI1W;

public struct Ray
{
    public Vec3 Origin;
    public Vec3 Direction;
    public Vec3 InvDirection;

    public Ray(Vec3 origin, Vec3 direction)
    {
        Origin = origin;
        Direction = direction;
        InvDirection = 1 / Direction;
    }

    public Vec3 At(double t)
    {
        return Origin + t * Direction;
    }
}