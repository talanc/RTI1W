namespace RTI1W;

public class Camera
{
    public Vec3 Origin;
    public Vec3 LowerLeftCorner;
    public Vec3 Horizontal;
    public Vec3 Vertical;

    public Camera()
    {
        const double AspectRatio = 16.0 / 9.0;

        var viewportHeight = 2.0;
        var viewportWidth = AspectRatio * viewportHeight;
        var focalLength = 1.0;

        Origin = P3(0, 0, 0);
        Horizontal = V3(viewportWidth, 0, 0);
        Vertical = V3(0, viewportHeight, 0);
        LowerLeftCorner = Origin - Horizontal / 2 - Vertical / 2 - V3(0, 0, focalLength);
    }

    public Ray GetRay(double u, double v)
    {
        return new Ray(Origin, LowerLeftCorner + u * Horizontal + v * Vertical - Origin);
    }
}
