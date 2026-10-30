using NUnit.Framework;
using FateBastion.UI.World;

namespace FateBastion.Tests.EditMode.UI.World
{
    /// <summary>At most N damage numbers; when full, smaller numbers are dropped first (S11).</summary>
    public class DamageNumberBudgetTests
    {
        private const int Capacity = 3;

        private DamageNumberBudget _budget;

        [SetUp]
        public void SetUp() => _budget = new DamageNumberBudget(Capacity);

        private void Fill(params int[] amounts)
        {
            foreach (int amount in amounts) _budget.TryReserve(amount);
        }

        [Test]
        public void TryReserve_UnderCap_Accepted()
        {
            Assert.GreaterOrEqual(_budget.TryReserve(10), 0);
            Assert.AreEqual(1, _budget.ActiveCount);
        }

        [Test]
        public void TryReserve_FullAndSmaller_Dropped()
        {
            Fill(10, 20, 30);

            Assert.AreEqual(-1, _budget.TryReserve(5));
            Assert.AreEqual(Capacity, _budget.ActiveCount);
        }

        [Test]
        public void TryReserve_FullAndEqualToSmallest_Dropped()
        {
            Fill(10, 20, 30);

            Assert.AreEqual(-1, _budget.TryReserve(10));
        }

        [Test]
        public void TryReserve_FullAndLarger_ReplacesSmallest()
        {
            _budget.TryReserve(20);
            int smallestSlot = _budget.TryReserve(10);
            _budget.TryReserve(30);

            Assert.AreEqual(smallestSlot, _budget.TryReserve(50));
            Assert.AreEqual(Capacity, _budget.ActiveCount);
        }

        [Test]
        public void TryReserve_ReplacedSlot_IsNoLongerTheSmallest()
        {
            Fill(10, 20, 30);
            _budget.TryReserve(50); // Replaces 10; smallest is now 20.

            Assert.AreEqual(-1, _budget.TryReserve(15));
        }

        [Test]
        public void Release_FreesSlotForAnyNumber()
        {
            Fill(10, 20, 30);
            _budget.Release(0);

            Assert.GreaterOrEqual(_budget.TryReserve(1), 0);
        }

        [Test]
        public void Release_Twice_CountsOnce()
        {
            Fill(10);
            _budget.Release(0);
            _budget.Release(0);

            Assert.AreEqual(0, _budget.ActiveCount);
        }

        [Test]
        public void TryReserve_HundredHitsAtCap60_Stays60()
        {
            var budget = new DamageNumberBudget(60);
            for (int i = 0; i < 100; i++) budget.TryReserve(i + 1);

            Assert.AreEqual(60, budget.ActiveCount);
        }
    }
}
