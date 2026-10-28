using NUnit.Framework;
using FateBastion.Audio;

namespace FateBastion.Tests.EditMode.S11
{
    /// <summary>At most N copies of one sound at once (S11: 100 deaths → 4 sounds).</summary>
    public class VoiceLimiterTests
    {
        private const int Max = 4;

        private VoiceLimiter<string> _limiter;

        [SetUp]
        public void SetUp() => _limiter = new VoiceLimiter<string>(capacity: 8);

        [Test]
        public void TryAcquire_FifthInstance_Rejected()
        {
            for (int i = 0; i < Max; i++) Assert.IsTrue(_limiter.TryAcquire("death", Max));

            Assert.IsFalse(_limiter.TryAcquire("death", Max));
            Assert.AreEqual(Max, _limiter.CountPlaying("death"));
        }

        [Test]
        public void TryAcquire_HundredRequests_CountStaysAtMax()
        {
            int accepted = 0;
            for (int i = 0; i < 100; i++)
            {
                if (_limiter.TryAcquire("death", Max)) accepted++;
            }

            Assert.AreEqual(Max, accepted);
        }

        [Test]
        public void Release_AfterLimit_AcceptsAgain()
        {
            for (int i = 0; i < Max; i++) _limiter.TryAcquire("death", Max);

            _limiter.Release("death");

            Assert.IsTrue(_limiter.TryAcquire("death", Max));
        }

        [Test]
        public void TryAcquire_DifferentSounds_CountedSeparately()
        {
            for (int i = 0; i < Max; i++) _limiter.TryAcquire("death", Max);

            Assert.IsTrue(_limiter.TryAcquire("hit", Max));
        }

        [Test]
        public void Release_UnknownSound_StaysAtZero()
        {
            _limiter.Release("never");

            Assert.AreEqual(0, _limiter.CountPlaying("never"));
        }
    }
}
