using NUnit.Framework;
using UnityEngine;
using FateBastion.Combat;

namespace FateBastion.Tests.EditMode.S1
{
    /// <summary>Tests that a projectile whose target died flies to the last known point and deals no damage (S1, S6).</summary>
    public class ProjectileFlightTests
    {
        private const float Speed = 10f;

        private ProjectileFlight _flight;

        [SetUp]
        public void SetUp()
        {
            _flight = new ProjectileFlight();
            _flight.Launch(Vector3.zero, new Vector3(10f, 0f, 0f), Speed);
        }

        [Test]
        public void Step_TargetAlive_FlyingUntilReached()
        {
            Assert.AreEqual(ProjectileStepResult.Flying, _flight.Step(0.5f, true, new Vector3(10f, 0f, 0f)));
            Assert.AreEqual(5f, _flight.Position.x, 1e-4f);
        }

        [Test]
        public void Step_TargetAlive_HitsWhenReached()
        {
            _flight.Step(0.5f, true, new Vector3(10f, 0f, 0f));
            Assert.AreEqual(ProjectileStepResult.HitTarget, _flight.Step(0.5f, true, new Vector3(10f, 0f, 0f)));
        }

        [Test]
        public void Step_TargetMoves_Follows()
        {
            _flight.Step(0.5f, true, new Vector3(5f, 0f, 5f));
            Assert.AreEqual(new Vector3(5f, 0f, 5f), _flight.TargetPoint);
            Assert.Greater(_flight.Position.z, 0f);
        }

        [Test]
        public void Step_TargetDiesMidFlight_FliesToLastPositionWithoutHit()
        {
            _flight.Step(0.5f, true, new Vector3(10f, 0f, 0f));

            // Target died: the position passed in is ignored from now on.
            Assert.AreEqual(ProjectileStepResult.Flying, _flight.Step(0.2f, false, new Vector3(99f, 0f, 99f)));
            Assert.IsTrue(_flight.TargetLost);
            Assert.AreEqual(ProjectileStepResult.ArrivedNoTarget, _flight.Step(0.5f, false, Vector3.zero));
            Assert.AreEqual(new Vector3(10f, 0f, 0f), _flight.Position);
        }

        [Test]
        public void Step_TargetLost_StaysLostWhenAliveAgain()
        {
            // A pooled enemy can be reused as a new enemy while the projectile is still flying.
            _flight.Step(0.1f, false, new Vector3(10f, 0f, 0f));
            _flight.Step(0.1f, true, new Vector3(50f, 0f, 0f));
            Assert.IsTrue(_flight.TargetLost);
            Assert.AreEqual(new Vector3(10f, 0f, 0f), _flight.TargetPoint);
        }

        [Test]
        public void Step_LargeDeltaTime_DoesNotOvershoot()
        {
            Assert.AreEqual(ProjectileStepResult.HitTarget, _flight.Step(5f, true, new Vector3(10f, 0f, 0f)));
            Assert.AreEqual(new Vector3(10f, 0f, 0f), _flight.Position);
        }

        [Test]
        public void Step_FlyingTargetUp2m_TravelsIn3D()
        {
            _flight.Launch(Vector3.zero, new Vector3(0f, 2f, 0f), Speed);
            Assert.AreEqual(ProjectileStepResult.HitTarget, _flight.Step(0.25f, true, new Vector3(0f, 2f, 0f)));
        }

        [Test]
        public void Step_AfterFinished_ReportsFinishedState()
        {
            _flight.Step(5f, true, new Vector3(10f, 0f, 0f));
            Assert.IsFalse(_flight.IsFlying);
            Assert.AreEqual(ProjectileStepResult.Idle, _flight.Step(0.1f, true, new Vector3(10f, 0f, 0f)));
        }

        [Test]
        public void Stop_MidFlight_EndsWithoutResult()
        {
            _flight.Step(0.1f, true, new Vector3(10f, 0f, 0f));
            _flight.Stop();
            Assert.IsFalse(_flight.IsFlying);
            Assert.AreEqual(ProjectileStepResult.Idle, _flight.Step(5f, true, new Vector3(10f, 0f, 0f)));
        }

        [Test]
        public void Launch_AfterFinished_ResetsLostFlag()
        {
            _flight.Step(0.1f, false, Vector3.zero);
            _flight.Launch(Vector3.zero, new Vector3(1f, 0f, 0f), Speed);
            Assert.IsFalse(_flight.TargetLost);
            Assert.IsTrue(_flight.IsFlying);
        }
    }
}
