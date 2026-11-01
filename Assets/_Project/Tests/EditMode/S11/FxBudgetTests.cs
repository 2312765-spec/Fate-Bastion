using NUnit.Framework;
using FateBastion.Audio;

namespace FateBastion.Tests.EditMode.S11
{
    /// <summary>Cap and minimum spacing of pooled effects per kind (C-09).</summary>
    public class FxBudgetTests
    {
        private const int HitFire = (int)FxKind.HitFire;
        private const int HitIce = (int)FxKind.HitIce;

        private FxBudget _budget;

        [SetUp]
        public void SetUp() => _budget = new FxBudget(System.Enum.GetValues(typeof(FxKind)).Length);

        [Test]
        public void TryStart_FirstEffect_AllowedAndCounted()
        {
            Assert.IsTrue(_budget.TryStart(HitFire, now: 0f, maxConcurrent: 3, minInterval: 0f));
            Assert.AreEqual(1, _budget.CountPlaying(HitFire));
        }

        [Test]
        public void TryStart_AtCap_Refused()
        {
            for (int i = 0; i < 3; i++) _budget.TryStart(HitFire, now: 0f, maxConcurrent: 3, minInterval: 0f);

            Assert.IsFalse(_budget.TryStart(HitFire, now: 0f, maxConcurrent: 3, minInterval: 0f));
            Assert.AreEqual(3, _budget.CountPlaying(HitFire));
        }

        [Test]
        public void Finish_AtCap_FreesOneSlot()
        {
            for (int i = 0; i < 3; i++) _budget.TryStart(HitFire, now: 0f, maxConcurrent: 3, minInterval: 0f);

            _budget.Finish(HitFire);

            Assert.IsTrue(_budget.TryStart(HitFire, now: 0f, maxConcurrent: 3, minInterval: 0f));
        }

        [Test]
        public void TryStart_WithinInterval_Refused()
        {
            _budget.TryStart(HitFire, now: 10f, maxConcurrent: 8, minInterval: 0.1f);

            Assert.IsFalse(_budget.TryStart(HitFire, now: 10.05f, maxConcurrent: 8, minInterval: 0.1f));
        }

        [Test]
        public void TryStart_AfterInterval_Allowed()
        {
            _budget.TryStart(HitFire, now: 10f, maxConcurrent: 8, minInterval: 0.1f);

            Assert.IsTrue(_budget.TryStart(HitFire, now: 10.1f, maxConcurrent: 8, minInterval: 0.1f));
        }

        [Test]
        public void TryStart_RefusedAttempt_DoesNotRestartInterval()
        {
            _budget.TryStart(HitFire, now: 10f, maxConcurrent: 8, minInterval: 0.1f);
            _budget.TryStart(HitFire, now: 10.06f, maxConcurrent: 8, minInterval: 0.1f);

            Assert.IsTrue(_budget.TryStart(HitFire, now: 10.1f, maxConcurrent: 8, minInterval: 0.1f));
        }

        [Test]
        public void TryStart_OtherKindFull_StillAllowed()
        {
            _budget.TryStart(HitFire, now: 0f, maxConcurrent: 1, minInterval: 1f);

            Assert.IsTrue(_budget.TryStart(HitIce, now: 0f, maxConcurrent: 1, minInterval: 1f));
        }

        [Test]
        public void Finish_NothingPlaying_StaysAtZero()
        {
            _budget.Finish(HitFire);

            Assert.AreEqual(0, _budget.CountPlaying(HitFire));
        }

        [Test]
        public void Reset_ClearsCountsAndIntervals()
        {
            _budget.TryStart(HitFire, now: 10f, maxConcurrent: 1, minInterval: 5f);

            _budget.Reset();

            Assert.AreEqual(0, _budget.CountPlaying(HitFire));
            Assert.IsTrue(_budget.TryStart(HitFire, now: 10f, maxConcurrent: 1, minInterval: 5f));
        }
    }
}
