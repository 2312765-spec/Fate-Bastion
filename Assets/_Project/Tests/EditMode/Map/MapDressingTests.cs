using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using FateBastion.Editor.UI;

namespace FateBastion.Tests.EditMode.Map
{
    /// <summary>Pure placement rules of Tools > Map > Dress Map_Level1 (C-17).</summary>
    public class MapDressingTests
    {
        // A 2 m wide, 10 m long road along +X as two triangles (corners on XZ).
        private static readonly List<Vector2> Road = new List<Vector2>
        {
            new Vector2(0f, -1f), new Vector2(10f, -1f), new Vector2(10f, 1f),
            new Vector2(0f, -1f), new Vector2(10f, 1f), new Vector2(0f, 1f)
        };

        [Test]
        public void DistanceToTriangles_PointOnRoad_IsZero()
        {
            Assert.AreEqual(0f, MapDressing.DistanceToTriangles(new Vector2(5f, 0.5f), Road), 1e-5f);
        }

        [Test]
        public void DistanceToTriangles_PointBesideRoad_MeasuresToEdge()
        {
            Assert.AreEqual(2f, MapDressing.DistanceToTriangles(new Vector2(5f, 3f), Road), 1e-5f);
            Assert.AreEqual(5f, MapDressing.DistanceToTriangles(new Vector2(-3f, 5f), Road), 1e-5f);
        }

        [Test]
        public void DistanceToTriangles_DegenerateTriangle_DoesNotSwallowPoints()
        {
            // A zero-area triangle (vertical road cap seen from above) must not report every point as on the road.
            var corners = new List<Vector2> { Vector2.zero, Vector2.zero, Vector2.zero };
            Assert.AreEqual(5f, MapDressing.DistanceToTriangles(new Vector2(3f, 4f), corners), 1e-5f);
        }

        [Test]
        public void Scatter_SameSeed_GivesSameLayout()
        {
            var area = new Rect(-10f, -10f, 20f, 20f);
            List<Vector2> a = MapDressing.Scatter(17, area, 300, 1.5f, null);
            List<Vector2> b = MapDressing.Scatter(17, area, 300, 1.5f, null);

            CollectionAssert.AreEqual(a, b);
            Assert.Greater(a.Count, 20);
        }

        [Test]
        public void Scatter_KeepsSpacingAndFilter()
        {
            var area = new Rect(-10f, -10f, 20f, 20f);
            List<Vector2> points = MapDressing.Scatter(3, area, 600, 1.5f, p => p.x > 0f);

            for (int i = 0; i < points.Count; i++)
            {
                Assert.Greater(points[i].x, 0f, "Filter rejected point was kept.");
                Assert.IsTrue(area.Contains(points[i]));
                for (int j = i + 1; j < points.Count; j++)
                    Assert.GreaterOrEqual((points[i] - points[j]).magnitude, 1.5f - 1e-4f, "Points closer than spacing.");
            }
        }

        [Test]
        public void InsideSquareAndRects_Boundaries()
        {
            Assert.IsTrue(MapDressing.InsideSquare(new Vector2(30f, -30f), 30f));
            Assert.IsFalse(MapDressing.InsideSquare(new Vector2(30.1f, 0f), 30f));
            var rects = new List<Rect> { new Rect(0f, 0f, 2f, 2f) };
            Assert.IsTrue(MapDressing.InsideAny(new Vector2(1f, 1f), rects));
            Assert.IsFalse(MapDressing.InsideAny(new Vector2(3f, 1f), rects));
        }
    }
}
