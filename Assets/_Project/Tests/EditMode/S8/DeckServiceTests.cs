using NUnit.Framework;
using FateBastion.Meta;

namespace FateBastion.Tests.EditMode.S8
{
    /// <summary>DeckService (S8, B-12): exactly 5 different owned heroes.</summary>
    public class DeckServiceTests
    {
        private readonly DeckService _deck = new DeckService();

        [Test]
        public void Validate_NewPlayer_CanStartMatch()
        {
            SaveData data = MetaTestData.NewPlayer();
            Assert.AreEqual(DeckError.None, _deck.Validate(data));
            Assert.IsTrue(_deck.CanStartMatch(data));
        }

        [Test]
        public void TrySetDeck_FourHeroes_WrongSizeAndUnchanged()
        {
            SaveData data = MetaTestData.NewPlayer();
            var before = data.deck.ToArray();

            Assert.AreEqual(DeckError.WrongSize, _deck.TrySetDeck(data, new[] { "hero_cung_thu", "hero_tu_si", "hero_linh_bang", "hero_nguoi_tuyet" }));
            CollectionAssert.AreEqual(before, data.deck);
        }

        [Test]
        public void TrySetDeck_Duplicate_Refused()
        {
            SaveData data = MetaTestData.NewPlayer();
            Assert.AreEqual(DeckError.Duplicate, _deck.TrySetDeck(data,
                new[] { "hero_cung_thu", "hero_cung_thu", "hero_tu_si", "hero_linh_bang", "hero_nguoi_tuyet" }));
        }

        [Test]
        public void TrySetDeck_HeroNotOwned_Refused()
        {
            SaveData data = MetaTestData.NewPlayer();
            Assert.AreEqual(DeckError.NotOwned, _deck.TrySetDeck(data,
                new[] { "hero_long_vuong", "hero_cung_thu", "hero_tu_si", "hero_linh_bang", "hero_nguoi_tuyet" }));
        }

        [Test]
        public void TrySetSlot_HeroAlreadyInDeck_SwapsSlots()
        {
            SaveData data = MetaTestData.NewPlayer();
            string first = data.deck[0];
            string third = data.deck[2];

            Assert.AreEqual(DeckError.None, _deck.TrySetSlot(data, 0, third));

            Assert.AreEqual(third, data.deck[0]);
            Assert.AreEqual(first, data.deck[2]);
            Assert.AreEqual(DeckError.None, _deck.Validate(data));
        }

        [Test]
        public void TrySetSlot_NewOwnedHero_ReplacesSlot()
        {
            SaveData data = MetaTestData.NewPlayer();
            data.collection["hero_thanh_nu"] = 1;

            Assert.AreEqual(DeckError.None, _deck.TrySetSlot(data, 4, "hero_thanh_nu"));
            Assert.AreEqual("hero_thanh_nu", data.deck[4]);
            Assert.AreEqual(DeckError.None, _deck.Validate(data));
        }

        [Test]
        public void TrySetSlot_OutOfRange_Refused()
        {
            SaveData data = MetaTestData.NewPlayer();
            Assert.AreEqual(DeckError.SlotOutOfRange, _deck.TrySetSlot(data, 5, "hero_cung_thu"));
        }
    }
}
