using System;
using System.Collections.Generic;
using System.Runtime.Intrinsics;
using System.Text;

namespace RTI1W;

public static class RayTracer
{
    public static Vector3 RayColor(Hittable world, Ray ray, int depth)
    {
        if (depth <= 0) return ColorBlack;

        if (world.Hit(ray, 0.001f, float.PositiveInfinity, out var hit))
        {
            var matRec = hit.Material.Scatter(ray, hit);
            return matRec.Attenuation * RayColor(world, matRec.Scattered, depth - 1);
        }

        var unitDir = UnitVector(ray.Direction);
        var t = 0.5f * (unitDir.Y + 1);
        return (1 - t) * ColorWhite + t * C3(0.5f, 0.7f, 1.0f);
    }

    public static void SetPixel(int imageWidth, int[] image, int x, int y, Vector3 pixelColor, float samplesPerPixelInv)
    {
        var c = Vector3.SquareRoot(pixelColor * samplesPerPixelInv);
        var rgb_f32 = 256 * Vector3.ClampNative(c, Vector3.Zero, new Vector3(0.999f));
        var rgb_i32 = Vector128.ConvertToInt32Native(rgb_f32.AsVector128Unsafe());

        var r = rgb_i32.GetElement(0);
        var g = rgb_i32.GetElement(1);
        var b = rgb_i32.GetElement(2);

        var d = (r << 16) | (g << 8) | b;
        var i = GetIndex(imageWidth, x, y);
        image[i] = d;
    }

    public static int GetIndex(int imageWidth, int x, int y)
    {
        return x + (y * imageWidth);
    }
}
