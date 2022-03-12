namespace RTI1W;

public struct Ray
{
    public Vec3 Origin;
    public Vec3 Direction;
    public double Time;

    public Ray(Vec3 origin, Vec3 direction, double time = 0)
    {
        Origin = origin;
        Direction = direction;
        Time = time;
    }

    public Vec3 At(double t)
    {
        return Origin + t * Direction;
    }
}