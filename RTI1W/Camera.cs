namespace RTI1W;

public class Camera
{
    public Vector3 LookFrom, LookAt, VUp;
    public float VFov, AspectRatio, Aperture, FocusDist;

    public Vector3 Origin;
    public Vector3 LowerLeftCorner;
    public Vector3 Horizontal;
    public Vector3 Vertical;
    public Vector3 U;
    public Vector3 V;
    public Vector3 W;
    public float LensRadius;

    public void SetPosition(Vector3 lookFrom, Vector3 lookAt, Vector3 vUp, float vFov, float aspectRatio, float aperture, float focusDist)
    {
        LookFrom = lookFrom;
        LookAt = lookAt;
        VUp = vUp;
        VFov = vFov;
        AspectRatio = aspectRatio;
        Aperture = aperture;
        FocusDist = focusDist;

        var theta = DegreesToRadians(vFov);
        var h = Tan(theta / 2);
        var viewportHeight = 2.0f * h;
        var viewportWidth = aspectRatio * viewportHeight;

        W = UnitVector(lookFrom - lookAt);
        U = UnitVector(Cross(vUp, W));
        V = Cross(W, U);

        Origin = lookFrom;
        Horizontal = focusDist * viewportWidth * U;
        Vertical = focusDist * viewportHeight * V;
        LowerLeftCorner = Origin - Horizontal / 2 - Vertical / 2 - focusDist * W;

        LensRadius = aperture / 2;
    }

    public Ray GetRay(float s, float t)
    {
        var rd = LensRadius * RandomInUnitCircle();
        var offset = U * rd.X + V * rd.Y;

        var rayOrigin = Origin + offset;
        var rayDirection = LowerLeftCorner + s * Horizontal + t * Vertical - Origin - offset;

        return new Ray(rayOrigin, rayDirection);
    }
}
