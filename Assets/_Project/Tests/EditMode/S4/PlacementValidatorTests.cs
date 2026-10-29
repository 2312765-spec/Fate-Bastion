using NUnit.Framework;
using FateBastion.Core;
using FateBastion.Heroes;
using Object = UnityEngine.Object;

namespace FateBastion.Tests.EditMode.S4
{
    /// <summary>PlacementValidator (S4, B-01): the 6 checks in order, per-rarity caps, army 15, gold.</summary>
    public class PlacementValidatorTests
    {
        private PlacementRules _rules;

        [SetUp]
        public void SetUp() => _rules = PlacementTestData.Rules();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_rules);

        private static PlacementCheck Check(int gold = 1000, Rarity rarity = Rarity.Common, int placed = 0, int total = 0,
            bool placeable = true, bool free = true, bool meteor = false) =>
            new PlacementCheck(gold, rarity, placed, total, placeable, free, meteor);

        [Test]
        public void Validate_AllGood_None() => Assert.AreEqual(PlacementError.None, PlacementValidator.Validate(_rules, Check()));

        [TestCase(Rarity.Common, 99)]
        [TestCase(Rarity.Rare, 199)]
        [TestCase(Rarity.Epic, 349)]
        [TestCase(Rarity.Legendary, 599)]
        public void Validate_OneGoldShort_NotEnoughGold(Rarity rarity, int gold)
        {
            Assert.AreEqual(PlacementError.NotEnoughGold, PlacementValidator.Validate(_rules, Check(gold, rarity)));
            Assert.AreEqual(PlacementError.None, PlacementValidator.Validate(_rules, Check(gold + 1, rarity)));
        }

        [TestCase(Rarity.Common, 6)]
        [TestCase(Rarity.Rare, 5)]
        [TestCase(Rarity.Epic, 4)]
        [TestCase(Rarity.Legendary, 2)]
        public void Validate_PerHeroCapReached_HeroLimitReached(Rarity rarity, int cap)
        {
            Assert.AreEqual(PlacementError.None, PlacementValidator.Validate(_rules, Check(rarity: rarity, placed: cap - 1)));
            Assert.AreEqual(PlacementError.HeroLimitReached, PlacementValidator.Validate(_rules, Check(rarity: rarity, placed: cap)));
        }

        [Test]
        public void Validate_FifteenHeroesOnField_ArmyFull()
        {
            Assert.AreEqual(PlacementError.None, PlacementValidator.Validate(_rules, Check(total: 14)));
            Assert.AreEqual(PlacementError.ArmyFull, PlacementValidator.Validate(_rules, Check(total: 15)));
        }

        [Test]
        public void Validate_PhysicsAndMeteor_ReportedInOrder()
        {
            Assert.AreEqual(PlacementError.NotPlaceableGround, PlacementValidator.Validate(_rules, Check(placeable: false, free: false, meteor: true)));
            Assert.AreEqual(PlacementError.AreaBlocked, PlacementValidator.Validate(_rules, Check(free: false, meteor: true)));
            Assert.AreEqual(PlacementError.AimingMeteor, PlacementValidator.Validate(_rules, Check(meteor: true)));
        }

        [Test]
        public void Validate_SeveralFailures_FirstOneInSpecOrderWins()
        {
            PlacementCheck everythingWrong = Check(gold: 0, placed: 6, total: 15, placeable: false, free: false, meteor: true);
            Assert.AreEqual(PlacementError.NotEnoughGold, PlacementValidator.Validate(_rules, everythingWrong));
            Assert.AreEqual(PlacementError.HeroLimitReached, PlacementValidator.Validate(_rules, Check(placed: 6, total: 15, placeable: false)));
        }

        [Test]
        public void CanPlaceAnother_GoldForOneMore_StaysInPreview()
        {
            Assert.IsTrue(PlacementValidator.CanPlaceAnother(_rules, 100, Rarity.Common, 1, 1));
            Assert.IsFalse(PlacementValidator.CanPlaceAnother(_rules, 99, Rarity.Common, 1, 1), "Out of gold: preview exits.");
            Assert.IsFalse(PlacementValidator.CanPlaceAnother(_rules, 1000, Rarity.Legendary, 2, 2), "Cap reached: preview exits.");
        }
    }
}
