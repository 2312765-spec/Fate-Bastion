using NUnit.Framework;
using FateBastion.Core;
using FateBastion.Heroes;
using Object = UnityEngine.Object;

namespace FateBastion.Tests.EditMode.S4
{
    /// <summary>HeroEconomy + HeroLevelTable (S4, B-04 data, B-05): upgrade prices, level stats and sell refunds.</summary>
    public class HeroEconomyTests
    {
        private PlacementRules _rules;
        private HeroLevelTable _levels;

        [SetUp]
        public void SetUp()
        {
            _rules = PlacementTestData.Rules();
            _levels = PlacementTestData.Levels();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_rules);
            Object.DestroyImmediate(_levels);
        }

        [TestCase(Rarity.Common, 75, 150)]
        [TestCase(Rarity.Rare, 150, 300)]
        [TestCase(Rarity.Epic, 262, 525)]
        [TestCase(Rarity.Legendary, 450, 900)]
        public void UpgradeCost_Levels2And3_FloorOfPlaceCostTimesPercent(Rarity rarity, int toLevel2, int toLevel3)
        {
            Assert.AreEqual(toLevel2, HeroEconomy.UpgradeCost(_rules, _levels, rarity, 1));
            Assert.AreEqual(toLevel3, HeroEconomy.UpgradeCost(_rules, _levels, rarity, 2));
        }

        [Test]
        public void Sell_EpicLevel2_Refunds306()
        {
            int spent = HeroEconomy.GoldSpentAt(_rules, _levels, Rarity.Epic, 2);
            Assert.AreEqual(612, spent);
            Assert.AreEqual(306, HeroEconomy.SellRefund(_rules, spent));
        }

        [TestCase(Rarity.Common, 1, 50)]
        [TestCase(Rarity.Common, 3, 162)]   // (100 + 75 + 150) × 0.5 = 162.5
        [TestCase(Rarity.Legendary, 3, 975)] // (600 + 450 + 900) × 0.5
        public void SellRefund_RoundedDown(Rarity rarity, int level, int refund)
        {
            Assert.AreEqual(refund, HeroEconomy.SellRefund(_rules, HeroEconomy.GoldSpentAt(_rules, _levels, rarity, level)));
        }

        [Test]
        public void UpgradeCost_AtMaxLevel_Throws()
        {
            Assert.IsFalse(HeroEconomy.CanUpgrade(_levels, 3));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => HeroEconomy.UpgradeCost(_rules, _levels, Rarity.Common, 3));
        }

        [Test]
        public void MultipliersOf_Levels1To3_MatchTable()
        {
            LevelMultipliers one = _levels.MultipliersOf(1);
            LevelMultipliers two = _levels.MultipliersOf(2);
            LevelMultipliers three = _levels.MultipliersOf(3);

            Assert.AreEqual(1f, one.Damage);
            Assert.AreEqual(1.4f, two.Damage, 1e-5f);
            Assert.AreEqual(1.1f, two.AttackSpeed, 1e-5f);
            Assert.AreEqual(1.1f, two.Range, 1e-5f);
            Assert.AreEqual(2f, three.Damage, 1e-5f);
            Assert.AreEqual(1.2f, three.AttackSpeed, 1e-5f);
            Assert.AreEqual(1.15f, three.Range, 1e-5f);
            Assert.AreEqual(3, _levels.MaxLevel);
        }
    }
}
