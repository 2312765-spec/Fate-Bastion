using NUnit.Framework;
using UnityEngine;
using FateBastion.UI.World;

namespace FateBastion.Tests.EditMode.UI.World
{
    /// <summary>Screen projection and edge clamping for the off-screen warning arrow (C-14, S10).</summary>
    public class OffscreenMathTests
    {
        private static readonly Vector2 Screen = new Vector2(1920f, 1080f);
        private const float Margin = 50f;
        private const float Tolerance = 0.01f;

        [Test]
        public void IsOnScreen_PointInside_ReturnsTrue()
        {
            Assert.IsTrue(OffscreenMath.IsOnScreen(new Vector3(960f, 540f, 10f), Screen));
        }

        [Test]
        public void IsOnScreen_PointRightOfScreen_ReturnsFalse()
        {
            Assert.IsFalse(OffscreenMath.IsOnScreen(new Vector3(2000f, 540f, 10f), Screen));
        }

        [Test]
        public void IsOnScreen_PointBelowScreen_ReturnsFalse()
        {
            Assert.IsFalse(OffscreenMath.IsOnScreen(new Vector3(960f, -1f, 10f), Screen));
        }

        [Test]
        public void IsOnScreen_PointBehindCamera_ReturnsFalse()
        {
            // Inside the rectangle but z < 0: WorldToScreenPoint mirrors points behind the camera.
            Assert.IsFalse(OffscreenMath.IsOnScreen(new Vector3(960f, 540f, -10f), Screen));
        }

        [Test]
        public void GetEdgePosition_PointRightOfScreen_ClampsToRightEdgeWithMargin()
        {
            Vector2 position = OffscreenMath.GetEdgePosition(new Vector3(3000f, 540f, 10f), Screen, Margin, out float angle);

            Assert.AreEqual(1870f, position.x, Tolerance);
            Assert.AreEqual(540f, position.y, Tolerance);
            Assert.AreEqual(0f, angle, Tolerance);
        }

        [Test]
        public void GetEdgePosition_PointAboveScreen_ClampsToTopEdgeAndAngleIs90()
        {
            Vector2 position = OffscreenMath.GetEdgePosition(new Vector3(960f, 2000f, 10f), Screen, Margin, out float angle);

            Assert.AreEqual(960f, position.x, Tolerance);
            Assert.AreEqual(1030f, position.y, Tolerance);
            Assert.AreEqual(90f, angle, Tolerance);
        }

        [Test]
        public void GetEdgePosition_FarCorner_StaysOnMarginRectangle()
        {
            Vector2 position = OffscreenMath.GetEdgePosition(new Vector3(9000f, 9000f, 10f), Screen, Margin, out _);

            Assert.LessOrEqual(position.x, 1870f + Tolerance);
            Assert.LessOrEqual(position.y, 1030f + Tolerance);
            bool onEdge = Mathf.Abs(position.x - 1870f) < Tolerance || Mathf.Abs(position.y - 1030f) < Tolerance;
            Assert.IsTrue(onEdge, $"Mũi tên phải nằm trên rìa, đang ở {position}.");
        }

        [Test]
        public void GetEdgePosition_PointBehindCamera_FlipsToOppositeEdge()
        {
            // Projects to the right half, but is behind the camera: the real direction is to the left.
            Vector2 position = OffscreenMath.GetEdgePosition(new Vector3(1500f, 540f, -10f), Screen, Margin, out float angle);

            Assert.AreEqual(Margin, position.x, Tolerance);
            Assert.AreEqual(540f, position.y, Tolerance);
            Assert.AreEqual(180f, Mathf.Abs(angle), Tolerance);
        }

        [Test]
        public void GetEdgePosition_BehindCameraAtCenter_PointsDown()
        {
            Vector2 position = OffscreenMath.GetEdgePosition(new Vector3(960f, 540f, -5f), Screen, Margin, out float angle);

            Assert.AreEqual(960f, position.x, Tolerance);
            Assert.AreEqual(Margin, position.y, Tolerance);
            Assert.AreEqual(-90f, angle, Tolerance);
        }

        [Test]
        public void GetEdgePosition_MarginLargerThanScreen_StaysAtCenter()
        {
            Vector2 position = OffscreenMath.GetEdgePosition(new Vector3(3000f, 540f, 10f), new Vector2(40f, 40f), Margin, out _);

            Assert.AreEqual(20f, position.x, 1f);
            Assert.AreEqual(20f, position.y, 1f);
        }
    }
}
