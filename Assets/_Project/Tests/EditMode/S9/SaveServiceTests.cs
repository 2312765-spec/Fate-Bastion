using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using FateBastion.Meta;

namespace FateBastion.Tests.EditMode.S9
{
    /// <summary>SaveService: safe write (tmp -> Replace/Move -> bak), read fallbacks and round-trip (S9, A-14/A-15).</summary>
    public class SaveServiceTests
    {
        private string _dir;
        private SaveService _service;

        private sealed class TestStarterFactory : IStarterSaveFactory
        {
            public int Created;
            public SaveData CreateNew()
            {
                Created++;
                return new SaveData
                {
                    gems = 1000,
                    ownedEver = new List<string> { "hero_cung_thu", "hero_linh_bang" },
                    deck = new List<string> { "hero_cung_thu", "hero_linh_bang" }
                };
            }
        }

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "FateBastion_S9_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
            _service = new SaveService(_dir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        private static SaveData SampleData()
        {
            return new SaveData
            {
                gems = 1200,
                pityCounter = 17,
                collection = new Dictionary<string, int> { { "hero_cung_thu", 3 }, { "hero_long_vuong", 1 } },
                deck = new List<string> { "hero_cung_thu", "hero_phu_thuy_lua", "hero_linh_bang", "hero_thanh_nu", "hero_long_vuong" },
                ownedEver = new List<string> { "hero_cung_thu", "hero_long_vuong" },
                levelProgress = new Dictionary<string, int> { { "level_1", 3 }, { "level_2", 2 } },
                endlessBest = 23,
                tutorialDone = true,
                settings = new SaveSettings { master = 0.4f, music = 0.2f, sfx = 0.9f, cameraShake = false, skipConfirm = false }
            };
        }

        private static void AssertSameData(SaveData expected, SaveData actual)
        {
            Assert.AreEqual(expected.version, actual.version);
            Assert.AreEqual(expected.gems, actual.gems);
            Assert.AreEqual(expected.pityCounter, actual.pityCounter);
            Assert.AreEqual(expected.endlessBest, actual.endlessBest);
            Assert.AreEqual(expected.tutorialDone, actual.tutorialDone);
            CollectionAssert.AreEqual(expected.deck, actual.deck);
            CollectionAssert.AreEqual(expected.ownedEver, actual.ownedEver);
            CollectionAssert.AreEquivalent(expected.collection, actual.collection);
            CollectionAssert.AreEquivalent(expected.levelProgress, actual.levelProgress);
            Assert.AreEqual(expected.settings.master, actual.settings.master, 1e-4f);
            Assert.AreEqual(expected.settings.music, actual.settings.music, 1e-4f);
            Assert.AreEqual(expected.settings.sfx, actual.settings.sfx, 1e-4f);
            Assert.AreEqual(expected.settings.cameraShake, actual.settings.cameraShake);
            Assert.AreEqual(expected.settings.skipConfirm, actual.settings.skipConfirm);
        }

        [Test]
        public void Save_ThenLoad_RoundTripsAllFields()
        {
            _service.Save(SampleData());

            var reloaded = new SaveService(_dir);
            SaveLoadResult result = reloaded.Load();

            Assert.AreEqual(SaveLoadKind.Loaded, result.Kind);
            AssertSameData(SampleData(), result.Data);
        }

        [Test]
        public void Save_FirstTime_NoExistingFile_CreatesFileWithoutError()
        {
            Assert.IsFalse(_service.Exists);
            _service.Save(SampleData());

            Assert.IsTrue(_service.Exists);
            Assert.IsTrue(File.Exists(_service.SaveFilePath));
            Assert.IsFalse(File.Exists(_service.TempFilePath), "save.tmp phải được dọn sau khi ghi");
        }

        [Test]
        public void Save_SecondTime_WritesBackupAndKeepsNoTemp()
        {
            SaveData first = SampleData();
            _service.Save(first);

            SaveData second = SampleData();
            second.gems = 5;
            _service.Save(second);

            Assert.IsTrue(File.Exists(_service.BackupFilePath), "lần lưu thứ hai phải tạo save.bak");
            Assert.IsFalse(File.Exists(_service.TempFilePath));

            // save.json holds the new data; save.bak holds the previous save.
            Assert.AreEqual(5, _service.Load().Data.gems);
            SaveData backup = Newtonsoft.Json.JsonConvert.DeserializeObject<SaveData>(
                File.ReadAllText(_service.BackupFilePath));
            Assert.AreEqual(first.gems, backup.gems);
        }

        [Test]
        public void Load_NoFiles_CreatesNewFromStarterFactory()
        {
            var factory = new TestStarterFactory();
            var service = new SaveService(_dir, factory);

            SaveLoadResult result = service.Load();

            Assert.AreEqual(SaveLoadKind.CreatedNew, result.Kind);
            Assert.AreEqual(1, factory.Created);
            Assert.AreEqual(1000, result.Data.gems);
            Assert.AreEqual(SaveData.CurrentVersion, result.Data.version);
        }

        [Test]
        public void Load_CorruptMainFile_UsesBackup()
        {
            _service.Save(SampleData());   // json = A
            SaveData second = SampleData();
            second.gems = 77;
            _service.Save(second);         // json = B, bak = A
            File.WriteAllText(_service.SaveFilePath, "{ this is not json ]]");

            SaveLoadResult result = new SaveService(_dir).Load();

            Assert.AreEqual(SaveLoadKind.RecoveredFromBackup, result.Kind);
            Assert.IsNotNull(result.Error);
            Assert.AreEqual(SampleData().gems, result.Data.gems, "dùng dữ liệu từ save.bak");
        }

        [Test]
        public void Load_BothFilesCorrupt_CreatesNewWithoutCrash()
        {
            _service.Save(SampleData());
            SaveData second = SampleData();
            second.gems = 77;
            _service.Save(second);
            File.WriteAllText(_service.SaveFilePath, "garbage");
            File.WriteAllText(_service.BackupFilePath, "garbage");

            var factory = new TestStarterFactory();
            SaveLoadResult result = new SaveService(_dir, factory).Load();

            Assert.AreEqual(SaveLoadKind.CreatedNew, result.Kind);
            Assert.AreEqual(1000, result.Data.gems);
            Assert.IsNotNull(result.Error);
        }

        [Test]
        public void Load_EmptyFile_TreatedAsCorrupt()
        {
            _service.Save(SampleData());
            File.WriteAllText(_service.SaveFilePath, "   ");

            SaveLoadResult result = new SaveService(_dir).Load();
            Assert.AreNotEqual(SaveLoadKind.Loaded, result.Kind);
        }

        [Test]
        public void Delete_RemovesSaveBackupAndTemp()
        {
            _service.Save(SampleData());
            _service.Save(SampleData());

            _service.Delete();

            Assert.IsFalse(File.Exists(_service.SaveFilePath));
            Assert.IsFalse(File.Exists(_service.BackupFilePath));
            Assert.IsFalse(File.Exists(_service.TempFilePath));
            Assert.IsNull(_service.Data);
        }
    }
}
