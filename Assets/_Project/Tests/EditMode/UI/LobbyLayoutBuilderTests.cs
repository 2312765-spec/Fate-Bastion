using System.Text.RegularExpressions;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using FateBastion.Editor.UI;

namespace FateBastion.Tests.EditMode.UI
{
    /// <summary>Runs Create Lobby Layout into a temporary folder: creates all prefabs once and never overwrites (C-12).</summary>
    public class LobbyLayoutBuilderTests
    {
        private const string TempFolder = "Assets/_LobbyLayoutBuilderTest";
        private const string SourceFontPath = "Assets/_Project/Art/Fonts/BeVietnamPro/BeVietnamPro-Regular.ttf";
        private const string MarkerName = "Marker";

        private TMP_FontAsset _font;

        [SetUp]
        public void SetUp()
        {
            AssetDatabase.DeleteAsset(TempFolder);
            // In-memory font asset so the test does not depend on the C-10 assets being created yet.
            var source = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
            Assume.That(source, Is.Not.Null, $"Thiếu {SourceFontPath}.");
            _font = TMP_FontAsset.CreateFontAsset(source);
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(TempFolder);
            if (_font != null) Object.DestroyImmediate(_font);
        }

        [Test]
        public void Create_EmptyFolder_CreatesAllPrefabs()
        {
            Assert.IsTrue(LobbyLayoutBuilder.Create(TempFolder, _font, _font));

            foreach (string prefabName in LobbyLayoutBuilder.AllPrefabNames)
                Assert.IsNotNull(Load(prefabName), $"Thiếu prefab {prefabName}.");
        }

        [Test]
        public void Create_BothCanvasesExist_ReturnsFalseAndKeepsEdits()
        {
            LobbyLayoutBuilder.Create(TempFolder, _font, _font);
            AddMarker(LobbyLayoutBuilder.LobbyCanvasName);

            LogAssert.Expect(LogType.Error, new Regex("already exist"));
            Assert.IsFalse(LobbyLayoutBuilder.Create(TempFolder, _font, _font));

            Assert.IsNotNull(Load(LobbyLayoutBuilder.LobbyCanvasName).transform.Find(MarkerName), "Lobby_Canvas có sẵn bị ghi đè.");
        }

        [Test]
        public void Create_OneCanvasMissing_RebuildsOnlyMissingAndKeepsOthers()
        {
            LobbyLayoutBuilder.Create(TempFolder, _font, _font);
            AddMarker(LobbyLayoutBuilder.LobbyCanvasName);
            AssetDatabase.DeleteAsset(UiBuildKit.GetPrefabPath(TempFolder, LobbyLayoutBuilder.MatchMenusCanvasName));

            Assert.IsTrue(LobbyLayoutBuilder.Create(TempFolder, _font, _font));

            Assert.IsNotNull(Load(LobbyLayoutBuilder.MatchMenusCanvasName));
            Assert.IsNotNull(Load(LobbyLayoutBuilder.LobbyCanvasName).transform.Find(MarkerName), "Lobby_Canvas có sẵn bị ghi đè.");
        }

        [Test]
        public void Create_HeroCardExists_ReusesItInsideRebuiltScreen()
        {
            LobbyLayoutBuilder.Create(TempFolder, _font, _font);
            AssetDatabase.DeleteAsset(UiBuildKit.GetPrefabPath(TempFolder, LobbyLayoutBuilder.LobbyCanvasName));
            AssetDatabase.DeleteAsset(UiBuildKit.GetPrefabPath(TempFolder, LobbyLayoutBuilder.CollectionScreenName));
            AddMarker(LobbyLayoutBuilder.HeroCardName);

            Assert.IsTrue(LobbyLayoutBuilder.Create(TempFolder, _font, _font));

            Transform firstCard = Load(LobbyLayoutBuilder.CollectionScreenName).transform.Find("DeckBar/DeckCard_1");
            Assert.IsNotNull(firstCard.Find(MarkerName), "Màn Túi dựng lại phải dùng HeroCard có sẵn, không dựng lại thẻ.");
        }

        private static GameObject Load(string prefabName) =>
            AssetDatabase.LoadAssetAtPath<GameObject>(UiBuildKit.GetPrefabPath(TempFolder, prefabName));

        private static void AddMarker(string prefabName)
        {
            string path = UiBuildKit.GetPrefabPath(TempFolder, prefabName);
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            var marker = new GameObject(MarkerName, typeof(RectTransform));
            SceneManager.MoveGameObjectToScene(marker, contents.scene);
            marker.transform.SetParent(contents.transform, false);
            PrefabUtility.SaveAsPrefabAsset(contents, path);
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }
}
