global using static RTI1W.Helpers;

namespace RTI1W.Tests;

[TestClass]
public class HelpersTests
{
    [DataTestMethod]
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
}