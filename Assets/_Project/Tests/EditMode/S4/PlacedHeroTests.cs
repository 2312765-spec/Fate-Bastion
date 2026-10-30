using NUnit.Framework;
using FateBastion.Core;
using FateBastion.Heroes;

namespace FateBastion.Tests.EditMode.S4
{
    /// <summary>PlacedHero (S4, B-04): upgrade price and level 1–3, gold spent, sell refund with rounding.</summary>
    public class PlacedHeroTests
    {
        private PlacementRules _rules;
        private HeroLevelTable _levels;

        [SetUp]
        public void SetUp()
        {
            _rules = PlacementTestData.Rules();
            _levels = PlacementTestData.Levels();
        }

        [Test]
        public void Begin_Level1WithPlaceCost()
        {
            var hero = new PlacedHero();
            hero.Begin("hero_x", Rarity.Epic, 2, 350);

            Assert.IsTrue(hero.IsPlaced);
            Assert.AreEqual(1, hero.Level);
            Assert.AreEqual(350, hero.GoldSpent);
            Assert.AreEqual(2, hero.DeckSlot);
            Assert.AreEqual(262, hero.NextUpgradeCost(_rules, _levels));
        }

        [Test]
        public void TryUpgrade_EpicToLevel2_Pays262_SellsFor306()
        {
            var hero = new PlacedHero();
            hero.Begin("hero_x", Rarity.Epic, 0, 350);
            var wallet = new Wallet(1000);

            UpgradeError error = hero.TryUpgrade(_rules, _levels, wallet, out int cost);

            Assert.AreEqual(UpgradeError.None, error);
            Assert.AreEqual(262, cost);
            Assert.AreEqual(2, hero.Level);
            Assert.AreEqual(612, hero.GoldSpent);
            Assert.AreEqual(738, wallet.Gold);
            Assert.AreEqual(306, hero.SellRefund(_rules));
        }

        [TestCase(Rarity.Common, 100, 75, 150)]
        [TestCase(Rarity.Rare, 200, 150, 300)]
        [TestCase(Rarity.Epic, 350, 262, 525)]
        [TestCase(Rarity.Legendary, 600, 450, 900)]
        public void TryUpgrade_ToLevel3_PricesMatchTable(Rarity rarity, int place, int toLevel2, int toLevel3)
        {
            var hero = new PlacedHero();
            hero.Begin("hero_x", rarity, 0, place);
            var wallet = new Wallet(10000);

            hero.TryUpgrade(_rules, _levels, wallet, out int cost2);
            hero.TryUpgrade(_rules, _levels, wallet, out int cost3);

            Assert.AreEqual(toLevel2, cost2);
            Assert.AreEqual(toLevel3, cost3);
            Assert.AreEqual(3, hero.Level);
            Assert.AreEqual(place + toLevel2 + toLevel3, hero.GoldSpent);
            Assert.AreEqual(-1, hero.NextUpgradeCost(_rules, _levels));
        }

        [Test]
        public void TryUpgrade_AtMaxLevel_MaxLevelEvenWithoutGold()
        {
            var hero = new PlacedHero();
            hero.Begin("hero_x", Rarity.Common, 0, 100);
            var wallet = new Wallet(225);
            hero.TryUpgrade(_rules, _levels, wallet, out _);
            hero.TryUpgrade(_rules, _levels, wallet, out _);

            Assert.AreEqual(0, wallet.Gold);
            Assert.AreEqual(UpgradeError.MaxLevel, hero.TryUpgrade(_rules, _levels, wallet, out _));
            Assert.AreEqual(3, hero.Level);
        }

        [Test]
        public void TryUpgrade_NotEnoughGold_NothingChanges()
        {
            var hero = new PlacedHero();
            hero.Begin("hero_x", Rarity.Legendary, 0, 600);
            var wallet = new Wallet(449);

            Assert.AreEqual(UpgradeError.NotEnoughGold, hero.TryUpgrade(_rules, _levels, wallet, out int cost));
            Assert.AreEqual(450, cost);
            Assert.AreEqual(1, hero.Level);
            Assert.AreEqual(600, hero.GoldSpent);
            Assert.AreEqual(449, wallet.Gold);
        }

        [Test]
        public void NotPlaced_CannotUpgradeOrRefund()
        {
            var hero = new PlacedHero();
            var wallet = new Wallet(1000);

            Assert.AreEqual(UpgradeError.NotPlaced, hero.TryUpgrade(_rules, _levels, wallet, out _));
            Assert.AreEqual(0, hero.SellRefund(_rules));
        }

        [Test]
        public void Clear_ResetsEverything()
        {
            var hero = new PlacedHero();
            hero.Begin("hero_x", Rarity.Rare, 4, 200);
            hero.Clear();

            Assert.IsFalse(hero.IsPlaced);
            Assert.AreEqual(0, hero.Level);
            Assert.AreEqual(0, hero.GoldSpent);
            Assert.AreEqual(-1, hero.DeckSlot);
        }
    }
}
