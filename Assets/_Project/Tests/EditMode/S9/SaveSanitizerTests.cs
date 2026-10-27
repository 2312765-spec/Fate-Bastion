using System.Collections.Generic;
using NUnit.Framework;
using FateBastion.Meta;

namespace FateBastion.Tests.EditMode.S9
{
    /// <summary>SaveSanitizer: drops hero ids that no longer exist and repairs the deck (S9 "Trường hợp đặc biệt").</summary>
    public class SaveSanitizerTests
    {
        private static SaveData Data()
        {
            return new SaveData
            {
                collection = new Dictionary<string, int>(),
                ownedEver = new List<string>(),
                deck = new List<string>()
            };
        }

        [Test]
        public void Sanitize_UnknownHeroId_RemovedFromEverywhere()
        {
            SaveData data = Data();
            data.collection["hero_a"] = 2;
            data.collection["ghost"] = 3;
            data.ownedEver.AddRange(new[] { "hero_a", "ghost" });
            data.deck.AddRange(new[] { "hero_a", "ghost" });

            List<string> warnings = SaveSanitizer.Sanitize(data, new[] { "hero_a", "hero_b" });

            CollectionAssert.AreEquivalent(new[] { "hero_a" }, data.collection.Keys);
            CollectionAssert.AreEquivalent(new[] { "hero_a" }, data.ownedEver);
            CollectionAssert.DoesNotContain(data.deck, "ghost");
            Assert.IsNotEmpty(warnings);
        }

        [Test]
        public void Sanitize_DeckHasDuplicateAndInvalid_FilledToFiveDistinct()
        {
            SaveData data = Data();
            foreach (string id in new[] { "hero_a", "hero_b", "hero_c", "hero_d", "hero_e", "hero_f" })
                data.collection[id] = 1;
            data.deck.AddRange(new[] { "hero_a", "hero_b", "hero_a" });

            SaveSanitizer.Sanitize(data,
                new[] { "hero_a", "hero_b", "hero_c", "hero_d", "hero_e", "hero_f" },
                new[] { "hero_c", "hero_d", "hero_e", "hero_f" });

            Assert.AreEqual(SaveSanitizer.DeckSize, data.deck.Count);
            CollectionAssert.AllItemsAreUnique(data.deck);
            CollectionAssert.AreEquivalent(new[] { "hero_a", "hero_b", "hero_c", "hero_d", "hero_e" }, data.deck);
        }

        [Test]
        public void Sanitize_ZeroOrNegativeCopies_Removed()
        {
            SaveData data = Data();
            data.collection["hero_a"] = 0;
            data.collection["hero_b"] = -2;
            data.collection["hero_c"] = 4;

            List<string> warnings = SaveSanitizer.Sanitize(data, new[] { "hero_a", "hero_b", "hero_c" });

            CollectionAssert.AreEquivalent(new[] { "hero_c" }, data.collection.Keys);
            Assert.IsNotEmpty(warnings);
        }

        [Test]
        public void Sanitize_NotEnoughOwnedHeroes_KeepsDeckShortAndWarns()
        {
            SaveData data = Data();
            data.collection["hero_a"] = 1;
            data.deck.Add("hero_a");

            List<string> warnings = SaveSanitizer.Sanitize(data, new[] { "hero_a", "hero_b" });

            Assert.AreEqual(1, data.deck.Count);
            Assert.IsNotEmpty(warnings, "phải cảnh báo deck thiếu tướng");
        }

        [Test]
        public void Sanitize_NoRoster_DoesNothing()
        {
            SaveData data = Data();
            data.collection["ghost"] = 1;
            data.deck.Add("ghost");

            List<string> warnings = SaveSanitizer.Sanitize(data, new string[0]);

            Assert.AreEqual(1, data.collection.Count);
            Assert.AreEqual(1, data.deck.Count);
            Assert.IsEmpty(warnings);
        }
    }
}
