namespace RTI1W;

public readonly struct Ray
{
    public readonly Vector3 Origin;
    public readonly Vector3 Direction;
    public readonly Vector3 InvDirection;

    public Ray(Vector3 origin, Vector3 direction)
    {
        Origin = origin;
        Direction = direction;
        InvDirection = Vector3.One / Direction;
    }

    public Vector3 At(float t)
    {
        return Origin + t * Direction;
    }
}