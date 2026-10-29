using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using FateBastion.Core;
using FateBastion.Heroes;

namespace FateBastion.Tests.EditMode.S4
{
    /// <summary>MatchDeck (S4, B-02): per-hero cap by rarity, army cap 15, gold check, counts on place and sell.</summary>
    public class MatchDeckTests
    {
        private readonly List<Object> _created = new List<Object>();
        private PlacementRules _rules;

        [SetUp]
        public void SetUp() => _rules = PlacementTestData.Rules();

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _created) Object.DestroyImmediate(o);
            _created.Clear();
            Object.DestroyImmediate(_rules);
        }

        private HeroData Hero(string id, Rarity rarity)
        {
            var data = ScriptableObject.CreateInstance<HeroData>();
            data.id = id;
            data.rarity = rarity;
            _created.Add(data);
            return data;
        }

        private MatchDeck FullDeck() => new MatchDeck(new[]
        {
            Hero("c", Rarity.Common), Hero("r", Rarity.Rare), Hero("e", Rarity.Epic), Hero("l", Rarity.Legendary), Hero("c2", Rarity.Common)
        });

        [TestCase(0, 6)]
        [TestCase(1, 5)]
        [TestCase(2, 4)]
        [TestCase(3, 2)]
        public void PerHeroCap_ByRarity(int slot, int cap)
        {
            MatchDeck deck = FullDeck();
            for (int i = 0; i < cap; i++)
            {
                Assert.AreEqual(PlacementError.None, deck.CheckEconomy(_rules, slot, 10000), $"copy {i + 1}");
                deck.RecordPlaced(slot);
            }

            Assert.AreEqual(PlacementError.HeroLimitReached, deck.CheckEconomy(_rules, slot, 10000));
        }

        [Test]
        public void ArmyCap_15()
        {
            MatchDeck deck = FullDeck();
            int[] fill = { 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 4, 4, 4, 4 };
            foreach (int slot in fill) deck.RecordPlaced(slot);

            Assert.AreEqual(15, deck.TotalPlaced);
            Assert.AreEqual(PlacementError.ArmyFull, deck.CheckEconomy(_rules, 2, 10000));
        }

        [Test]
        public void NotEnoughGold_CheckedFirst()
        {
            MatchDeck deck = FullDeck();
            Assert.AreEqual(PlacementError.NotEnoughGold, deck.CheckEconomy(_rules, 3, 599));
            Assert.AreEqual(PlacementError.None, deck.CheckEconomy(_rules, 3, 600));
        }

        [Test]
        public void RecordRemoved_FreesTheCopyAndRaisesEvents()
        {
            MatchDeck deck = FullDeck();
            int slotEvents = 0;
            int lastTotal = -1;
            deck.SlotChanged += _ => slotEvents++;
            deck.TotalChanged += total => lastTotal = total;

            deck.RecordPlaced(3);
            deck.RecordPlaced(3);
            Assert.AreEqual(PlacementError.HeroLimitReached, deck.CheckEconomy(_rules, 3, 10000));

            deck.RecordRemoved(3);

            Assert.AreEqual(1, deck[3].PlacedCount);
            Assert.AreEqual(1, deck.TotalPlaced);
            Assert.AreEqual(1, lastTotal);
            Assert.AreEqual(3, slotEvents);
            Assert.AreEqual(PlacementError.None, deck.CheckEconomy(_rules, 3, 10000));
        }

        [Test]
        public void EmptySlots_AreInvalid_DistinctSkipsThem()
        {
            HeroData a = Hero("a", Rarity.Common);
            var deck = new MatchDeck(new[] { a, null, a });

            Assert.IsTrue(deck.IsValidSlot(0));
            Assert.IsFalse(deck.IsValidSlot(1));
            Assert.IsFalse(deck.IsValidSlot(4));
            Assert.IsFalse(deck.IsValidSlot(-1));
            CollectionAssert.AreEqual(new[] { a }, deck.DistinctHeroes());
        }

        [Test]
        public void Reset_ZeroesCounts()
        {
            MatchDeck deck = FullDeck();
            deck.RecordPlaced(0);
            deck.RecordPlaced(2);
            deck.Reset();

            Assert.AreEqual(0, deck.TotalPlaced);
            Assert.AreEqual(0, deck[0].PlacedCount);
            Assert.AreEqual(0, deck[2].PlacedCount);
        }
    }
}
