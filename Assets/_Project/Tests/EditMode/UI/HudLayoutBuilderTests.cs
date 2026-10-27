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
    /// <summary>Runs Create HUD Layout into a temporary folder: creates all prefabs once and never overwrites (C-11).</summary>
    public class HudLayoutBuilderTests
    {
        private const string TempFolder = "Assets/_HudLayoutBuilderTest";
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
        public void Create_EmptyFolder_CreatesHudAndBothSlotPrefabs()
        {
            Assert.IsTrue(HudLayoutBuilder.Create(TempFolder, _font, _font));

            Assert.IsNotNull(Load(HudLayoutBuilder.HudName));
            Assert.IsNotNull(Load(HudLayoutBuilder.DeckSlotName));
            Assert.IsNotNull(Load(HudLayoutBuilder.CooldownSlotName));
        }

        [Test]
        public void Create_HudAlreadyExists_ReturnsFalseAndKeepsEdits()
        {
            HudLayoutBuilder.Create(TempFolder, _font, _font);
            AddMarker(HudLayoutBuilder.HudName);

            LogAssert.Expect(LogType.Error, new Regex("already exists"));
            Assert.IsFalse(HudLayoutBuilder.Create(TempFolder, _font, _font));

            Assert.IsNotNull(Load(HudLayoutBuilder.HudName).transform.Find(MarkerName), "HUD_Match có sẵn bị ghi đè.");
        }

        [Test]
        public void Create_SlotPrefabExists_ReusesItInsideHud()
        {
            HudLayoutBuilder.Create(TempFolder, _font, _font);
            AssetDatabase.DeleteAsset(HudLayoutBuilder.GetPrefabPath(TempFolder, HudLayoutBuilder.HudName));
            AddMarker(HudLayoutBuilder.DeckSlotName);

            Assert.IsTrue(HudLayoutBuilder.Create(TempFolder, _font, _font));

            Transform firstSlot = Load(HudLayoutBuilder.HudName).transform.Find("Bottom/DeckBar/DeckSlot_1");
            Assert.IsNotNull(firstSlot.Find(MarkerName), "HUD mới phải dùng DeckSlot có sẵn, không dựng lại.");
        }

        private static GameObject Load(string prefabName) =>
            AssetDatabase.LoadAssetAtPath<GameObject>(HudLayoutBuilder.GetPrefabPath(TempFolder, prefabName));

        private static void AddMarker(string prefabName)
        {
            string path = HudLayoutBuilder.GetPrefabPath(TempFolder, prefabName);
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            var marker = new GameObject(MarkerName, typeof(RectTransform));
            SceneManager.MoveGameObjectToScene(marker, contents.scene);
            marker.transform.SetParent(contents.transform, false);
            PrefabUtility.SaveAsPrefabAsset(contents, path);
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }
}
