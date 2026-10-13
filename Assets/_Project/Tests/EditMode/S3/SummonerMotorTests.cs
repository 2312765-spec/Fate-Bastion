using NUnit.Framework;
using UnityEngine;
using FateBastion.Player;

namespace FateBastion.Tests.EditMode.S3
{
    /// <summary>Walk / sprint speed, camera-relative direction and gravity of the Summoner (S3).</summary>
    public class SummonerMotorTests
    {
        private const float WalkSpeed = 6f;
        private const float SprintSpeed = 9f;
        private const float Gravity = -20f;
        private const float Dt = 1f / 60f;
        private const float Tolerance = 1e-4f;

        private SummonerMotor _motor;

        [SetUp]
        public void SetUp()
        {
            _motor = new SummonerMotor(WalkSpeed, SprintSpeed, Gravity);
        }

        private static Vector3 Planar(Vector3 v) => new Vector3(v.x, 0f, v.z);

        [Test]
        public void ComputeVelocity_ForwardYaw0Walk_Returns6AlongZ()
        {
            Vector3 v = _motor.ComputeVelocity(Vector2.up, 0f, sprint: false, grounded: true, Dt);

            Assert.AreEqual(0f, v.x, Tolerance);
            Assert.AreEqual(WalkSpeed, v.z, Tolerance);
        }

        [Test]
        public void ComputeVelocity_ForwardSprint_Returns9()
        {
            Vector3 v = _motor.ComputeVelocity(Vector2.up, 0f, sprint: true, grounded: true, Dt);

            Assert.AreEqual(SprintSpeed, Planar(v).magnitude, Tolerance);
        }

        [Test]
        public void ComputeVelocity_ForwardYaw90_MovesAlongPositiveX()
        {
            Vector3 v = _motor.ComputeVelocity(Vector2.up, 90f, sprint: false, grounded: true, Dt);

            Assert.AreEqual(WalkSpeed, v.x, Tolerance);
            Assert.AreEqual(0f, v.z, Tolerance);
        }

        [Test]
        public void ComputeVelocity_RightYaw0_MovesAlongPositiveX()
        {
            Vector3 v = _motor.ComputeVelocity(Vector2.right, 0f, sprint: false, grounded: true, Dt);

            Assert.AreEqual(WalkSpeed, v.x, Tolerance);
            Assert.AreEqual(0f, v.z, Tolerance);
        }

        [Test]
        public void ComputeVelocity_Diagonal_IsNotFasterThanStraight()
        {
            Vector3 v = _motor.ComputeVelocity(new Vector2(1f, 1f), 0f, sprint: false, grounded: true, Dt);

            Assert.AreEqual(WalkSpeed, Planar(v).magnitude, Tolerance);
        }

        [Test]
        public void ComputeVelocity_HalfInput_HalfSpeed()
        {
            Vector3 v = _motor.ComputeVelocity(new Vector2(0f, 0.5f), 0f, sprint: false, grounded: true, Dt);

            Assert.AreEqual(WalkSpeed * 0.5f, Planar(v).magnitude, Tolerance);
        }

        [Test]
        public void ComputeVelocity_NoInput_NoPlanarMovement()
        {
            Vector3 v = _motor.ComputeVelocity(Vector2.zero, 45f, sprint: true, grounded: true, Dt);

            Assert.AreEqual(0f, Planar(v).magnitude, Tolerance);
        }

        [Test]
        public void ComputeVelocity_Airborne_AccumulatesGravity()
        {
            _motor.ComputeVelocity(Vector2.zero, 0f, false, grounded: false, Dt);
            Vector3 v = _motor.ComputeVelocity(Vector2.zero, 0f, false, grounded: false, Dt);

            Assert.AreEqual(Gravity * Dt * 2f, v.y, Tolerance);
        }

        [Test]
        public void ComputeVelocity_GroundedAfterFall_DoesNotKeepFallSpeed()
        {
            for (int i = 0; i < 60; i++)
            {
                _motor.ComputeVelocity(Vector2.zero, 0f, false, grounded: false, Dt);
            }

            Vector3 v = _motor.ComputeVelocity(Vector2.zero, 0f, false, grounded: true, Dt);

            Assert.AreEqual(SummonerMotor.GroundedVerticalVelocity, v.y, Tolerance);
        }

        [Test]
        public void ComputeVelocity_ZeroDt_NoGravityChange()
        {
            Vector3 v = _motor.ComputeVelocity(Vector2.zero, 0f, false, grounded: false, 0f);

            Assert.AreEqual(0f, v.y, Tolerance);
            Assert.IsFalse(float.IsNaN(v.y));
        }

        [Test]
        public void ResetVertical_AfterFall_ClearsVerticalVelocity()
        {
            _motor.ComputeVelocity(Vector2.zero, 0f, false, grounded: false, 1f);

            _motor.ResetVertical();

            Assert.AreEqual(0f, _motor.VerticalVelocity, Tolerance);
        }
    }
}
