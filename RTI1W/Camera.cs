﻿namespace RTI1W;

public class Camera
{
    public Vec3 Origin;
    public Vec3 LowerLeftCorner;
    public Vec3 Horizontal;
    public Vec3 Vertical;
    public Vec3 U;
    public Vec3 V;
    public Vec3 W;
    public double LensRadius;

    public Camera(Vec3 lookFrom, Vec3 lookAt, Vec3 vUp, double vFov, double aspectRatio, double aperture, double focusDist)
    {
        var theta = DegreesToRadians(vFov);
        var h = Tan(theta / 2);
        var viewportHeight = 2.0 * h;
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

    public Ray GetRay(double s, double t)
    {
        var rd = LensRadius * RandomInUnitDisk();
        var offset = U * rd.X + V * rd.Y;

        var rayOrigin = Origin + offset;
        var rayDirection = LowerLeftCorner + s * Horizontal + t * Vertical - Origin - offset;

        return new Ray(rayOrigin, rayDirection);
    }
}
