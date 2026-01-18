global using static RTI1W.Helpers;
global using static System.Math;
using System;

[assembly: Parallelize]

namespace RTI1W.Tests;

[TestClass]
public class HelpersTests
{
    [TestMethod]
    [DataRow(true, "0,0,3", "1,1,0")]
    [DataRow(true, "0,0,3.1", "1,1,0")]
    [DataRow(true, "0,0,4", "1,1,0")]
    [DataRow(true, "3,0,3", "0,1,0")]
    [DataRow(true, "3,6,3", "0,-1,0")]
    public void TestIntersectRayBox(bool expectedIntersects, string ps, string ds)
    {
        // Arrange
        var p = ParseVec3(ps);
        var d = ParseVec3(ds);
        var ray = new Ray(p,d);
        var box = new Box3(V3(3, 3, 3), V3(5, 5, 5));

        // Act
        var intersects = IntersectRayBox(ray, box);

        // Assert
        Assert.AreEqual(expectedIntersects, intersects);
    }

    private static Vec3 ParseVec3(string s)
    {
        var span = s.AsSpan();
        Span<Range> ranges = stackalloc Range[3];
        span.Split(ranges, ",", StringSplitOptions.TrimEntries);
        var x = float.Parse(span[ranges[0]]);
        var y = float.Parse(span[ranges[1]]);
        var z = float.Parse(span[ranges[2]]);
        return V3(x, y, z);
    }

    [TestMethod]
    public void TestRandomInUnitCircle()
    {
        for (var i = 0; i < 1_000_000; i++)
        {
            // Arrange

            // Act
            var item = RandomInUnitCircle();

            // Assert
            Assert.IsLessThan(1, item.LengthSquared);
        }
    }

    [TestMethod]
    public void TestRandomInUnitCircleDistribution()
    {
        const int N = 1_000_000;
        float sumX = 0, sumY = 0, sumX2 = 0, sumY2 = 0;
        for (int i = 0; i < N; i++)
        {
            var p = RandomInUnitCircle();
            sumX += p.X;
            sumY += p.Y;
            sumX2 += p.X * p.X;
            sumY2 += p.Y * p.Y;
        }
        float meanX = sumX / N;
        float meanY = sumY / N;
        float varX = sumX2 / N - meanX * meanX;
        float varY = sumY2 / N - meanY * meanY;

        // Check means are close to 0
        Assert.IsLessThan(0.01, Abs(meanX));
        Assert.IsLessThan(0.01, Abs(meanY));

        // Check variances are close to 1/4
        Assert.IsLessThan(0.01, Abs(varX - 0.25));
        Assert.IsLessThan(0.01, Abs(varY - 0.25));
    }

    [TestMethod]
    public void TestRandomInUnitSphere()
    {
        for (var i = 0; i < 1_000_000; i++)
        {
            // Arrange

            // Act
            var item = RandomInUnitSphere();

            // Assert
            Assert.IsLessThan(1, item.LengthSquared);
        }
    }

    [TestMethod]
    public void TestRandomInUnitSphereDistribution()
    {
        const int N = 1_000_000;
        float sumX = 0, sumY = 0, sumZ = 0, sumX2 = 0, sumY2 = 0, sumZ2 = 0;
        for (int i = 0; i < N; i++)
        {
            var p = RandomInUnitSphere();
            sumX += p.X;
            sumY += p.Y;
            sumZ += p.Z;
            sumX2 += p.X * p.X;
            sumY2 += p.Y * p.Y;
            sumZ2 += p.Z * p.Z;
        }
        float meanX = sumX / N;
        float meanY = sumY / N;
        float meanZ = sumZ / N;
        float varX = sumX2 / N - meanX * meanX;
        float varY = sumY2 / N - meanY * meanY;
        float varZ = sumZ2 / N - meanZ * meanZ;

        // Check means are close to 0
        Assert.IsLessThan(0.01, Abs(meanX));
        Assert.IsLessThan(0.01, Abs(meanY));
        Assert.IsLessThan(0.01, Abs(meanZ));

        // Check variances are close to 1/5
        Assert.IsLessThan(0.01, Abs(varX - 0.2));
        Assert.IsLessThan(0.01, Abs(varY - 0.2));
        Assert.IsLessThan(0.01, Abs(varZ - 0.2));
    }
}