using System;
using System.IO;
using NUnit.Framework;
using FateBastion.Core;
using FateBastion.Meta;
using Object = UnityEngine.Object;

namespace FateBastion.Tests.EditMode.S8
{
    /// <summary>GachaService (S8, B-08/B-09): rates, pity, x10 guarantee, prices and first-own gems.</summary>
    public class GachaServiceTests
    {
        private GachaTable _table;

        [TearDown]
        public void TearDown()
        {
            if (_table != null) Object.DestroyImmediate(_table);
        }

        private GachaService Service(int seed = 12345) => new GachaService(_table, new Random(seed));

        [Test]
        public void PullOne_100000PullsPityOff_RatesWithinHalfPoint()
        {
            _table = MetaTestData.Table(pity: 0);
            GachaService gacha = Service(2026);
            SaveData data = MetaTestData.Rich(int.MaxValue);
            var counts = new int[GachaTable.RarityCount];
            const int pulls = 100_000;

            for (int i = 0; i < pulls; i++)
            {
                data.gems = int.MaxValue; // Keep paying; first-own gems would overflow otherwise.
                counts[(int)gacha.PullOne(data).Rolls[0].Rarity]++;
            }

            double[] expected = { 65d, 25d, 8.5d, 1.5d };
            for (int i = 0; i < counts.Length; i++)
            {
                double percent = counts[i] * 100d / pulls;
                Assert.AreEqual(expected[i], percent, 0.5d, $"{(Rarity)i}: {percent:0.00}%");
            }
        }

        [Test]
        public void PullOne_NoLegendaryWeight_25thPullIsLegendaryByPity()
        {
            _table = MetaTestData.Table(legendary: 0f);
            GachaService gacha = Service();
            SaveData data = MetaTestData.Rich();

            for (int i = 1; i <= 24; i++)
            {
                GachaRoll roll = gacha.PullOne(data).Rolls[0];
                Assert.AreNotEqual(Rarity.Legendary, roll.Rarity, $"pull {i}");
                Assert.AreEqual(i, data.pityCounter);
            }

            GachaRoll pity = gacha.PullOne(data).Rolls[0];
            Assert.AreEqual(Rarity.Legendary, pity.Rarity);
            Assert.IsTrue(pity.FromPity);
            Assert.AreEqual(0, data.pityCounter, "Legendary resets the counter.");
        }

        [Test]
        public void PullOne_PityCounterSavedAndLoaded_NextPullIsLegendary()
        {
            _table = MetaTestData.Table(legendary: 0f);
            string folder = Path.Combine(Path.GetTempPath(), "FateBastion_S8_" + Guid.NewGuid().ToString("N"));
            try
            {
                SaveData data = MetaTestData.Rich();
                data.pityCounter = 24;
                new SaveService(folder).Save(data);

                var reloaded = new SaveService(folder);
                reloaded.Load();
                Assert.AreEqual(24, reloaded.Data.pityCounter);

                GachaRoll roll = Service().PullOne(reloaded.Data).Rolls[0];
                Assert.AreEqual(Rarity.Legendary, roll.Rarity);
            }
            finally
            {
                if (Directory.Exists(folder)) Directory.Delete(folder, true);
            }
        }

        [Test]
        public void PullTen_ManySeeds_AlwaysHasEpicOrBetter()
        {
            _table = MetaTestData.Table();
            for (int seed = 0; seed < 2000; seed++)
            {
                SaveData data = MetaTestData.Rich();
                GachaPullResult result = new GachaService(_table, new Random(seed)).PullTen(data);

                Assert.AreEqual(GachaService.TenPullCount, result.Rolls.Count);
                bool epicOrBetter = false;
                foreach (GachaRoll roll in result.Rolls) epicOrBetter |= roll.IsEpicOrBetter;
                Assert.IsTrue(epicOrBetter, $"seed {seed}");
            }
        }

        [Test]
        public void PullTen_AllCommonsRolled_LastReplacedByEpicAndPityCountsIt()
        {
            // Epic is so unlikely that the first nine are commons and the guarantee must kick in.
            _table = MetaTestData.Table(common: 100f, rare: 0f, epic: 0.0001f, legendary: 0f);
            SaveData data = MetaTestData.Rich();

            GachaPullResult result = Service().PullTen(data);

            GachaRoll last = result.Rolls[9];
            Assert.IsTrue(last.Guaranteed);
            Assert.AreEqual(Rarity.Epic, last.Rarity);
            for (int i = 0; i < 9; i++) Assert.AreEqual(Rarity.Common, result.Rolls[i].Rarity);
            Assert.AreEqual(10, data.pityCounter, "9 commons + 1 Epic, no Legendary: counter is 10.");
        }

        [Test]
        public void PullTen_Applied_GemsCopiesAndOwnedEverConsistent()
        {
            _table = MetaTestData.Table();
            for (int seed = 0; seed < 300; seed++)
            {
                SaveData data = MetaTestData.NewPlayer();
                data.gems = 900;
                int copiesBefore = Total(data);
                int ownedBefore = data.ownedEver.Count;

                GachaPullResult result = new GachaService(_table, new Random(seed)).PullTen(data);

                int newHeroes = data.ownedEver.Count - ownedBefore;
                Assert.AreEqual(copiesBefore + 10, Total(data), $"seed {seed}");
                Assert.AreEqual(newHeroes * 50, result.GemsAwarded, $"seed {seed}");
                Assert.AreEqual(newHeroes * 50, data.gems, $"seed {seed}: 900 - 900 + bonus");
                foreach (string id in data.ownedEver) Assert.Greater(CollectionService.CopiesOf(data, id), 0);
            }
        }

        [Test]
        public void PullTen_NotEnoughGems_RefusedAndNothingChanges()
        {
            _table = MetaTestData.Table();
            SaveData data = MetaTestData.NewPlayer();
            data.gems = 899;
            data.pityCounter = 7;

            GachaPullResult result = Service().PullTen(data);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(GachaError.NotEnoughGems, result.Error);
            Assert.AreEqual(899, data.gems);
            Assert.AreEqual(7, data.pityCounter);
            Assert.AreEqual(5, Total(data));
        }

        [Test]
        public void PullOne_Prices_100And900()
        {
            _table = MetaTestData.Table();
            GachaService gacha = Service();
            Assert.AreEqual(100, gacha.CostOf(1));
            Assert.AreEqual(900, gacha.CostOf(GachaService.TenPullCount));
        }

        [Test]
        public void PullOne_StarterHero_NoFirstOwnGems()
        {
            // Only the starter commons can come out: none of them is new.
            _table = MetaTestData.Table(common: 100f, rare: 0f, epic: 0f, legendary: 0f, pity: 0);
            var table = _table;
            table.SetHeroes(new[]
            {
                new GachaTable.HeroEntry("hero_cung_thu", Rarity.Common),
                new GachaTable.HeroEntry("hero_nguoi_tuyet", Rarity.Rare),
                new GachaTable.HeroEntry("hero_thanh_nu", Rarity.Epic),
                new GachaTable.HeroEntry("hero_long_vuong", Rarity.Legendary)
            });
            SaveData data = MetaTestData.NewPlayer();

            GachaPullResult result = Service().PullOne(data);

            Assert.IsFalse(result.Rolls[0].IsNew);
            Assert.AreEqual(0, result.GemsAwarded);
            Assert.AreEqual(900, data.gems);
            Assert.AreEqual(2, CollectionService.CopiesOf(data, "hero_cung_thu"));
        }

        [Test]
        public void Constructor_RarityWithoutHeroes_Throws()
        {
            _table = MetaTestData.Table();
            _table.SetHeroes(new[] { new GachaTable.HeroEntry("hero_cung_thu", Rarity.Common) });
            Assert.Throws<ArgumentException>(() => Service());
        }

        private static int Total(SaveData data)
        {
            int total = 0;
            foreach (int copies in data.collection.Values) total += copies;
            return total;
        }
    }
}
