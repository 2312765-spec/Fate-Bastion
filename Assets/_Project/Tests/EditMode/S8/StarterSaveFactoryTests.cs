using System;
using System.IO;
using NUnit.Framework;
using FateBastion.Meta;
using Object = UnityEngine.Object;

namespace FateBastion.Tests.EditMode.S8
{
    /// <summary>StarterSaveFactory (S8, B-11): a new player has 5 heroes, 1000 gems, ownedEver and can play at once.</summary>
    public class StarterSaveFactoryTests
    {
        [Test]
        public void CreateNew_SpecStarter_FiveHeroesThousandGemsOwnedEverAndValidDeck()
        {
            StarterSettings settings = MetaTestData.Starter();
            try
            {
                SaveData data = new StarterSaveFactory(settings).CreateNew();

                Assert.AreEqual(1000, data.gems);
                Assert.AreEqual(0, data.pityCounter);
                Assert.AreEqual(5, data.collection.Count);
                foreach (string id in MetaTestData.Starters)
                {
                    Assert.AreEqual(1, data.collection[id], id);
                    CollectionAssert.Contains(data.ownedEver, id);
                }

                Assert.IsTrue(new DeckService().CanStartMatch(data), "Vào trận được ngay.");
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void SaveServiceLoad_NoFile_CreatesStarterSave()
        {
            StarterSettings settings = MetaTestData.Starter();
            string folder = Path.Combine(Path.GetTempPath(), "FateBastion_S8_" + Guid.NewGuid().ToString("N"));
            try
            {
                var save = new SaveService(folder, new StarterSaveFactory(settings));
                SaveLoadResult result = save.Load();

                Assert.AreEqual(SaveLoadKind.CreatedNew, result.Kind);
                Assert.AreEqual(1000, result.Data.gems);
                Assert.AreEqual(5, result.Data.deck.Count);
            }
            finally
            {
                Object.DestroyImmediate(settings);
                if (Directory.Exists(folder)) Directory.Delete(folder, true);
            }
        }
    }
}
