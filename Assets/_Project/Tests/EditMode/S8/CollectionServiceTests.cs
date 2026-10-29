using System;
using NUnit.Framework;
using FateBastion.Core;
using FateBastion.Meta;
using Object = UnityEngine.Object;

namespace FateBastion.Tests.EditMode.S8
{
    /// <summary>CollectionService (S8, B-10): spare copies, merge rules, auto-select, never down to 0 copies.</summary>
    public class CollectionServiceTests
    {
        private GachaTable _table;
        private CollectionService _bag;

        [SetUp]
        public void SetUp()
        {
            _table = MetaTestData.Table();
            _bag = new CollectionService(_table, new Random(7));
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_table);

        [Test]
        public void SpareCopies_SeveralHeroesSameRarity_Summed()
        {
            SaveData data = MetaTestData.NewPlayer();
            data.collection["hero_cung_thu"] = 3; // 2 spare
            data.collection["hero_tu_si"] = 2;    // 1 spare
            data.collection["hero_nguoi_tuyet"] = 5; // Rare, not counted

            Assert.AreEqual(3, _bag.SpareCopies(data, Rarity.Common));
            Assert.AreEqual(0, _bag.MissingFor(data, Rarity.Common));
            Assert.IsTrue(_bag.CanMerge(data, Rarity.Common));
        }

        [Test]
        public void Merge_TwoSparesOnly_RefusedWithOneMissing()
        {
            SaveData data = MetaTestData.NewPlayer();
            data.collection["hero_cung_thu"] = 3;

            MergeResult result = _bag.MergeAuto(data, Rarity.Common);

            Assert.AreEqual(MergeError.NotEnoughSpare, result.Error);
            Assert.AreEqual(1, result.Missing);
            Assert.AreEqual(3, data.collection["hero_cung_thu"], "Nothing spent.");
        }

        [Test]
        public void Merge_ThreeCommonSpares_GivesOneRareAndKeepsOneCopy()
        {
            SaveData data = MetaTestData.NewPlayer();
            data.collection["hero_cung_thu"] = 3;
            data.collection["hero_tu_si"] = 2;

            MergeResult result = _bag.Merge(data, Rarity.Common, new[] { "hero_cung_thu", "hero_cung_thu", "hero_tu_si" });

            Assert.IsTrue(result.Success, result.Error.ToString());
            Assert.AreEqual(Rarity.Rare, result.Rarity);
            Assert.AreEqual(1, data.collection["hero_cung_thu"]);
            Assert.AreEqual(1, data.collection["hero_tu_si"]);
            Assert.Greater(CollectionService.CopiesOf(data, result.HeroId), 0);
        }

        [Test]
        public void Merge_SelectingLastCopy_Refused()
        {
            SaveData data = MetaTestData.NewPlayer();
            data.collection["hero_cung_thu"] = 2; // 1 spare
            data.collection["hero_tu_si"] = 3;    // 2 spare

            MergeResult result = _bag.Merge(data, Rarity.Common, new[] { "hero_cung_thu", "hero_cung_thu", "hero_tu_si" });

            Assert.AreEqual(MergeError.HeroHasNoSpare, result.Error);
            Assert.AreEqual(2, data.collection["hero_cung_thu"]);
            Assert.AreEqual(3, data.collection["hero_tu_si"]);
        }

        [Test]
        public void Merge_MixedRarities_Refused()
        {
            SaveData data = MetaTestData.NewPlayer();
            data.collection["hero_cung_thu"] = 4;
            data.collection["hero_nguoi_tuyet"] = 4;

            MergeResult result = _bag.Merge(data, Rarity.Common, new[] { "hero_cung_thu", "hero_cung_thu", "hero_nguoi_tuyet" });

            Assert.AreEqual(MergeError.WrongRarity, result.Error);
        }

        [Test]
        public void Merge_WrongSelectionSize_Refused()
        {
            SaveData data = MetaTestData.NewPlayer();
            data.collection["hero_cung_thu"] = 5;

            MergeResult result = _bag.Merge(data, Rarity.Common, new[] { "hero_cung_thu", "hero_cung_thu" });

            Assert.AreEqual(MergeError.WrongSelectionSize, result.Error);
        }

        [Test]
        public void Merge_Legendary_Refused()
        {
            SaveData data = MetaTestData.NewPlayer();
            data.collection["hero_long_vuong"] = 10;

            Assert.IsFalse(_bag.CanMerge(data, Rarity.Legendary));
            Assert.AreEqual(MergeError.LegendaryCannotMerge, _bag.MergeAuto(data, Rarity.Legendary).Error);
            Assert.AreEqual(10, data.collection["hero_long_vuong"]);
        }

        [Test]
        public void Merge_EpicSpares_AlwaysGivesTheLegendary()
        {
            SaveData data = MetaTestData.NewPlayer();
            data.collection["hero_thanh_nu"] = 4;
            data.ownedEver.Add("hero_thanh_nu");

            MergeResult result = _bag.MergeAuto(data, Rarity.Epic);

            Assert.IsTrue(result.Success);
            Assert.AreEqual("hero_long_vuong", result.HeroId);
            Assert.IsTrue(result.IsNew);
            Assert.AreEqual(1050, data.gems, "First-own +50.");
            CollectionAssert.Contains(data.ownedEver, "hero_long_vuong");
        }

        [Test]
        public void AutoSelect_TakesFromHeroWithMostSparesFirst()
        {
            SaveData data = MetaTestData.NewPlayer();
            data.collection["hero_cung_thu"] = 2;     // 1 spare
            data.collection["hero_linh_bang"] = 4;    // 3 spares

            CollectionAssert.AreEqual(new[] { "hero_linh_bang", "hero_linh_bang", "hero_linh_bang" },
                _bag.AutoSelect(data, Rarity.Common));
        }

        [Test]
        public void MergeAuto_RepeatedUntilImpossible_NoHeroEverAtZero()
        {
            var rng = new Random(99);
            for (int run = 0; run < 50; run++)
            {
                SaveData data = MetaTestData.NewPlayer();
                foreach (string id in MetaTestData.Commons) data.collection[id] = 1 + rng.Next(6);

                for (int rarity = 0; rarity < (int)Rarity.Legendary; rarity++)
                {
                    while (_bag.CanMerge(data, (Rarity)rarity))
                    {
                        Assert.IsTrue(_bag.MergeAuto(data, (Rarity)rarity).Success);
                    }
                }

                foreach (int copies in data.collection.Values) Assert.GreaterOrEqual(copies, 1);
                foreach (string id in data.deck) Assert.GreaterOrEqual(data.collection[id], 1, "Deck untouched.");
            }
        }
    }
}
