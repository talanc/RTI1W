global using static RTI1W.Helpers;
global using static System.Math;

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
        var d = s.Split(',');
        var x = double.Parse(d[0].Trim());
        var y = double.Parse(d[1].Trim());
        var z = double.Parse(d[2].Trim());
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
        const int n = 1000000;
        double sumX = 0, sumY = 0, sumX2 = 0, sumY2 = 0;
        for (int i = 0; i < n; i++)
        {
            var p = RandomInUnitCircle();
            sumX += p.X;
            sumY += p.Y;
            sumX2 += p.X * p.X;
            sumY2 += p.Y * p.Y;
        }
        double meanX = sumX / n;
        double meanY = sumY / n;
        double varX = sumX2 / n - meanX * meanX;
        double varY = sumY2 / n - meanY * meanY;

        // Check means are close to 0
        Assert.IsLessThan(0.01, Abs(meanX));
        Assert.IsLessThan(0.01, Abs(meanY));

        // Check variances are close to 1/4
        Assert.IsLessThan(0.01, Abs(varX - 0.25));
        Assert.IsLessThan(0.01, Abs(varY - 0.25));
    }
}