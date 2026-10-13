using NUnit.Framework;
using UnityEngine;
using FateBastion.Player;

namespace FateBastion.Tests.EditMode.S3
{
    /// <summary>Camera yaw / pitch / zoom limits and smoothing (S3).</summary>
    public class CameraOrbitTests
    {
        private const float MinDistance = 4f;
        private const float MaxDistance = 25f;
        private const float MinPitch = 10f;
        private const float MaxPitch = 60f;
        private const float Tolerance = 1e-3f;

        private CameraOrbit _orbit;

        [SetUp]
        public void SetUp()
        {
            _orbit = new CameraOrbit(MinDistance, MaxDistance, MinPitch, MaxPitch,
                initialYaw: 0f, initialPitch: 30f, initialDistance: 12f);
        }

        [Test]
        public void ApplyLook_MouseRight_IncreasesYaw()
        {
            _orbit.ApplyLook(new Vector2(100f, 0f), yawSpeed: 0.1f, pitchSpeed: 0.1f);

            Assert.AreEqual(10f, _orbit.Yaw, Tolerance);
        }

        [Test]
        public void ApplyLook_FullTurns_YawStaysIn0To360()
        {
            _orbit.ApplyLook(new Vector2(-4000f, 0f), yawSpeed: 0.1f, pitchSpeed: 0.1f);

            Assert.GreaterOrEqual(_orbit.Yaw, 0f);
            Assert.Less(_orbit.Yaw, 360f);
            Assert.AreEqual(320f, _orbit.Yaw, Tolerance);
        }

        [Test]
        public void ApplyLook_MouseUp_LowersPitch()
        {
            _orbit.ApplyLook(new Vector2(0f, 100f), yawSpeed: 0.1f, pitchSpeed: 0.1f);

            Assert.AreEqual(20f, _orbit.Pitch, Tolerance);
        }

        [Test]
        public void ApplyLook_FarDown_ClampsPitchAtMax()
        {
            _orbit.ApplyLook(new Vector2(0f, -10000f), yawSpeed: 0.1f, pitchSpeed: 0.1f);

            Assert.AreEqual(MaxPitch, _orbit.Pitch, Tolerance);
        }

        [Test]
        public void ApplyLook_FarUp_ClampsPitchAtMin()
        {
            _orbit.ApplyLook(new Vector2(0f, 10000f), yawSpeed: 0.1f, pitchSpeed: 0.1f);

            Assert.AreEqual(MinPitch, _orbit.Pitch, Tolerance);
        }

        [Test]
        public void ApplyZoom_ScrollUp_MovesTargetCloserByOneStep()
        {
            _orbit.ApplyZoom(1f, zoomStep: 2f);

            Assert.AreEqual(10f, _orbit.TargetDistance, Tolerance);
        }

        [Test]
        public void ApplyZoom_ScrollSizeIgnored_OnlySignCounts()
        {
            // Windows may report 120 per notch, newer Input System 1 per notch: both are one step.
            _orbit.ApplyZoom(-120f, zoomStep: 2f);

            Assert.AreEqual(14f, _orbit.TargetDistance, Tolerance);
        }

        [Test]
        public void ApplyZoom_ManySteps_ClampsBetween4And25()
        {
            for (int i = 0; i < 50; i++) _orbit.ApplyZoom(1f, zoomStep: 2f);
            Assert.AreEqual(MinDistance, _orbit.TargetDistance, Tolerance);

            for (int i = 0; i < 50; i++) _orbit.ApplyZoom(-1f, zoomStep: 2f);
            Assert.AreEqual(MaxDistance, _orbit.TargetDistance, Tolerance);
        }

        [Test]
        public void Tick_LongEnough_DistanceReachesTarget()
        {
            _orbit.ApplyZoom(1f, zoomStep: 2f);

            for (int i = 0; i < 120; i++) _orbit.Tick(1f / 60f, smoothTime: 0.1f);

            Assert.AreEqual(10f, _orbit.Distance, 0.01f);
        }

        [Test]
        public void Tick_ZeroDt_DistanceUnchanged()
        {
            _orbit.ApplyZoom(1f, zoomStep: 2f);

            _orbit.Tick(0f, smoothTime: 0.1f);

            Assert.AreEqual(12f, _orbit.Distance, Tolerance);
        }

        [Test]
        public void Tick_ZeroSmoothTime_SnapsToTarget()
        {
            _orbit.ApplyZoom(1f, zoomStep: 2f);

            _orbit.Tick(1f / 60f, smoothTime: 0f);

            Assert.AreEqual(10f, _orbit.Distance, Tolerance);
        }

        [Test]
        public void Constructor_InitialValuesOutOfRange_AreClamped()
        {
            var orbit = new CameraOrbit(MinDistance, MaxDistance, MinPitch, MaxPitch,
                initialYaw: 0f, initialPitch: 90f, initialDistance: 100f);

            Assert.AreEqual(MaxPitch, orbit.Pitch, Tolerance);
            Assert.AreEqual(MaxDistance, orbit.Distance, Tolerance);
            Assert.AreEqual(MaxDistance, orbit.TargetDistance, Tolerance);
        }
    }
}
