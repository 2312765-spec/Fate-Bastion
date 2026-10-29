using NUnit.Framework;
using FateBastion.Heroes;

namespace FateBastion.Tests.EditMode.S6
{
    /// <summary>RageMeter adds bounded rage and only allows consuming a full meter.</summary>
    public class RageMeterTests
    {
        private RageMeter _meter;

        [SetUp]
        public void SetUp()
        {
            _meter = new RageMeter(100f);
        }

        [Test]
        public void Add_PerAttackAmount_AccumulatesRage()
        {
            _meter.Add(5f);
            _meter.Add(5f);

            Assert.AreEqual(10f, _meter.Current);
        }

        [Test]
        public void Add_ExceedsMaximum_ClampsToMaximum()
        {
            for (int i = 0; i < 21; i++)
                _meter.Add(5f);

            Assert.AreEqual(100f, _meter.Current);
            Assert.IsTrue(_meter.IsFull);
        }

        [Test]
        public void TryConsumeFull_BelowMaximum_PreservesRage()
        {
            _meter.Add(95f);

            Assert.IsFalse(_meter.TryConsumeFull());
            Assert.AreEqual(95f, _meter.Current);
        }

        [Test]
        public void TryConsumeFull_AtMaximum_ResetsRage()
        {
            _meter.Add(100f);

            Assert.IsTrue(_meter.TryConsumeFull());
            Assert.AreEqual(0f, _meter.Current);
        }

        [Test]
        public void Reset_WithExistingRage_ClearsAndReportsChange()
        {
            _meter.Add(5f);

            Assert.IsTrue(_meter.Reset());
            Assert.AreEqual(0f, _meter.Current);
            Assert.IsFalse(_meter.Reset());
        }
    }
}
