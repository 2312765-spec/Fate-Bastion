using NUnit.Framework;
using FateBastion.Audio;

namespace FateBastion.Tests.EditMode.S11
{
    /// <summary>Minimum spacing between two shakes of the same kind (S11, C-18).</summary>
    public class ShakeGateTests
    {
        private const int CastleHit = (int)ShakeKind.CastleHit;
        private const int MeteorExplosion = (int)ShakeKind.MeteorExplosion;
        private const float Interval = 0.5f;

        private ShakeGate _gate;

        [SetUp]
        public void SetUp() => _gate = new ShakeGate(System.Enum.GetValues(typeof(ShakeKind)).Length);

        [Test]
        public void TryPass_FirstShake_Allowed() =>
            Assert.IsTrue(_gate.TryPass(CastleHit, now: 0f, Interval));

        [Test]
        public void TryPass_CastleHitTwiceWithinInterval_SecondBlocked()
        {
            _gate.TryPass(CastleHit, now: 10f, Interval);

            Assert.IsFalse(_gate.TryPass(CastleHit, now: 10.2f, Interval));
        }

        [Test]
        public void TryPass_AfterInterval_Allowed()
        {
            _gate.TryPass(CastleHit, now: 10f, Interval);

            Assert.IsTrue(_gate.TryPass(CastleHit, now: 10.5f, Interval));
        }

        [Test]
        public void TryPass_BlockedAttempt_DoesNotRestartInterval()
        {
            _gate.TryPass(CastleHit, now: 10f, Interval);
            _gate.TryPass(CastleHit, now: 10.4f, Interval);

            Assert.IsTrue(_gate.TryPass(CastleHit, now: 10.5f, Interval));
        }

        [Test]
        public void TryPass_DifferentKinds_IndependentIntervals()
        {
            _gate.TryPass(CastleHit, now: 10f, Interval);

            Assert.IsTrue(_gate.TryPass(MeteorExplosion, now: 10.1f, Interval));
        }

        [Test]
        public void TryPass_ZeroInterval_AlwaysAllowed()
        {
            _gate.TryPass(MeteorExplosion, now: 10f, 0f);

            Assert.IsTrue(_gate.TryPass(MeteorExplosion, now: 10f, 0f));
        }
    }
}
