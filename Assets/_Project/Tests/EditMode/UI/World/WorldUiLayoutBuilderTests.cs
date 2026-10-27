using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using FateBastion.Editor.UI;
using FateBastion.UI.World;

namespace FateBastion.Tests.EditMode.UI.World
{
    /// <summary>Runs Create World UI Layout into a temporary folder: prefab structure and wiring (C-14).</summary>
    public class WorldUiLayoutBuilderTests
    {
        private const string TempFolder = "Assets/_WorldUiLayoutBuilderTest";
        private const string SettingsPath = TempFolder + "/WorldUiSettings.asset";
        private const string MarkerName = "Marker";

        [SetUp]
        public void SetUp()
        {
            AssetDatabase.DeleteAsset(TempFolder);
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(TempFolder);
        }

        [Test]
        public void Create_EmptyFolder_CreatesCanvasHealthBarAndSettings()
        {
            Assert.IsTrue(WorldUiLayoutBuilder.Create(TempFolder, SettingsPath));

            Assert.IsNotNull(Load(WorldUiLayoutBuilder.CanvasName));
            Assert.IsNotNull(Load(WorldUiLayoutBuilder.HealthBarName));
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<WorldUiSettings>(SettingsPath));
        }

        [Test]
        public void Create_CanvasAlreadyExists_ReturnsFalseAndKeepsEdits()
        {
            WorldUiLayoutBuilder.Create(TempFolder, SettingsPath);
            AddMarker(WorldUiLayoutBuilder.CanvasName);

            LogAssert.Expect(LogType.Error, new Regex("already exists"));
            Assert.IsFalse(WorldUiLayoutBuilder.Create(TempFolder, SettingsPath));

            Assert.IsNotNull(Load(WorldUiLayoutBuilder.CanvasName).transform.Find(MarkerName), "WorldUI_Canvas có sẵn bị ghi đè.");
        }

        [Test]
        public void Create_SettingsAlreadyExist_ReusesThem()
        {
            AssetFolder();
            var existing = ScriptableObject.CreateInstance<WorldUiSettings>();
            AssetDatabase.CreateAsset(existing, SettingsPath);

            WorldUiLayoutBuilder.Create(TempFolder, SettingsPath);

            var root = Load(WorldUiLayoutBuilder.CanvasName).GetComponent<WorldUiRoot>();
            Assert.AreSame(existing, Field(root, "_settings"));
        }

        [Test]
        public void Canvas_IsOverlayWithoutRaycaster_SortsBelowHud()
        {
            WorldUiLayoutBuilder.Create(TempFolder, SettingsPath);
            GameObject canvasObject = Load(WorldUiLayoutBuilder.CanvasName);

            var canvas = canvasObject.GetComponent<Canvas>();
            Assert.IsNotNull(canvas);
            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, canvas.renderMode);
            Assert.Less(canvas.sortingOrder, 0, "Canvas động phải vẽ dưới HUD_Match (sortingOrder 0).");
            Assert.IsNull(canvasObject.GetComponent<GraphicRaycaster>(), "UI thế giới không được chặn click xuống thế giới.");
            foreach (Graphic graphic in canvasObject.GetComponentsInChildren<Graphic>(true))
                Assert.IsFalse(graphic.raycastTarget, $"{graphic.name} không được là raycast target.");
        }

        [Test]
        public void HealthBar_FillIsHorizontalFilledImage()
        {
            WorldUiLayoutBuilder.Create(TempFolder, SettingsPath);
            var bar = Load(WorldUiLayoutBuilder.HealthBarName).GetComponent<WorldHealthBar>();

            Assert.IsNotNull(bar);
            var fill = Field(bar, "_fill") as Image;
            Assert.IsNotNull(fill, "WorldHealthBar._fill chưa gắn.");
            Assert.AreEqual(Image.Type.Filled, fill.type);
            Assert.AreEqual(Image.FillMethod.Horizontal, fill.fillMethod);
            Assert.IsNotNull(fill.sprite, "Image Filled cần sprite trắng, không để trống.");
        }

        [Test]
        public void Canvas_RootAndWarningReferencesWired()
        {
            WorldUiLayoutBuilder.Create(TempFolder, SettingsPath);
            GameObject canvasObject = Load(WorldUiLayoutBuilder.CanvasName);
            var root = canvasObject.GetComponent<WorldUiRoot>();

            Assert.IsNotNull(root);
            Assert.IsNotNull(Field(root, "_settings"));
            Assert.AreSame(Load(WorldUiLayoutBuilder.HealthBarName).GetComponent<WorldHealthBar>(), Field(root, "_healthBarPrefab"));
            Assert.IsNotNull(Field(root, "_healthBarParent"));
            var warning = Field(root, "_warning") as OffscreenWarning;
            Assert.IsNotNull(warning);
            Assert.IsNotNull(Field(warning, "_arrow"));
            // Scene references are dragged in by hand (Hướng dẫn lắp), not baked into the prefab.
            Assert.IsNull(Field(root, "_enemyManager"));
        }

        private static GameObject Load(string prefabName) =>
            AssetDatabase.LoadAssetAtPath<GameObject>(WorldUiLayoutBuilder.GetPrefabPath(TempFolder, prefabName));

        private static Object Field(Object target, string name)
        {
            SerializedProperty property = new SerializedObject(target).FindProperty(name);
            Assert.IsNotNull(property, $"{target.GetType().Name} không có field {name}.");
            return property.objectReferenceValue;
        }

        private static void AssetFolder()
        {
            if (!AssetDatabase.IsValidFolder(TempFolder)) AssetDatabase.CreateFolder("Assets", TempFolder.Substring("Assets/".Length));
        }

        private static void AddMarker(string prefabName)
        {
            string path = WorldUiLayoutBuilder.GetPrefabPath(TempFolder, prefabName);
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            var marker = new GameObject(MarkerName, typeof(RectTransform));
            marker.transform.SetParent(contents.transform, false);
            PrefabUtility.SaveAsPrefabAsset(contents, path);
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }
}
