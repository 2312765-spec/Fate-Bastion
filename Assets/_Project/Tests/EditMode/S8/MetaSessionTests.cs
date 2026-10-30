using System;
using System.IO;
using NUnit.Framework;
using FateBastion.Core;
using FateBastion.Meta;
using Object = UnityEngine.Object;

namespace FateBastion.Tests.EditMode.S8
{
    /// <summary>MetaSession (S8): a successful pull, merge or deck change is saved at once; a refused one is not.</summary>
    public class MetaSessionTests
    {
        private string _folder;
        private GachaTable _table;
        private StarterSettings _starter;
        private SaveService _save;
        private MetaSession _session;

        [SetUp]
        public void SetUp()
        {
            _folder = Path.Combine(Path.GetTempPath(), "FateBastion_S8_" + Guid.NewGuid().ToString("N"));
            _table = MetaTestData.Table();
            _starter = MetaTestData.Starter();
            _save = new SaveService(_folder, new StarterSaveFactory(_starter));
            _save.Load();
            var rng = new Random(5);
            _session = new MetaSession(_save, new GachaService(_table, rng), new CollectionService(_table, rng), new DeckService());
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_table);
            Object.DestroyImmediate(_starter);
            if (Directory.Exists(_folder)) Directory.Delete(_folder, true);
        }

        private SaveData Reload()
        {
            var other = new SaveService(_folder);
            other.Load();
            return other.Data;
        }

        [Test]
        public void PullTen_Success_SavedToDisk()
        {
            int changed = 0;
            _session.Changed += _ => changed++;

            GachaPullResult result = _session.PullTen();

            Assert.IsTrue(result.Success);
            Assert.AreEqual(1, changed);
            SaveData disk = Reload();
            Assert.AreEqual(_session.Data.gems, disk.gems);
            Assert.AreEqual(_session.Data.pityCounter, disk.pityCounter);
        }

        [Test]
        public void PullTen_NotEnoughGems_NotSaved()
        {
            _session.Data.gems = 50;
            int changed = 0;
            _session.Changed += _ => changed++;

            Assert.IsFalse(_session.PullTen().Success);
            Assert.AreEqual(0, changed);
            Assert.IsFalse(File.Exists(_save.SaveFilePath), "A new player's file is only written on the first change.");
        }

        [Test]
        public void SetDeckSlot_Success_SavedToDisk()
        {
            string third = _session.Data.deck[2];
            Assert.AreEqual(DeckError.None, _session.SetDeckSlot(0, third));
            Assert.AreEqual(third, Reload().deck[0]);
        }

        [Test]
        public void MergeAuto_Success_SavedToDisk()
        {
            _session.Data.collection["hero_cung_thu"] = 4;

            MergeResult result = _session.MergeAuto(Rarity.Common);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(1, Reload().collection["hero_cung_thu"]);
        }
    }
}
