namespace RTI1W;

public struct Ray
{
    public Vector3 Origin;
    public Vector3 Direction;
    public Vector3 InvDirection;

    public Ray(Vector3 origin, Vector3 direction)
    {
        Origin = origin;
        Direction = direction;
        InvDirection = new Vector3(1f / Direction.X, 1f / Direction.Y, 1f / Direction.Z);
    }

    public Vector3 At(float t)
    {
        return Origin + t * Direction;
    }
}