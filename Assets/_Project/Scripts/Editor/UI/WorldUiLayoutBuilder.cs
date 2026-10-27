using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using FateBastion.Editor.Import;
using FateBastion.UI.World;

namespace FateBastion.Editor.UI
{
    /// <summary>
    /// Tools > Create World UI Layout: builds the dynamic world UI canvas (health bars, off-screen arrow) and the
    /// health bar prefab (C-14). One-shot like Create HUD Layout: never overwrites an existing prefab.
    /// </summary>
    public static class WorldUiLayoutBuilder
    {
        public const string PrefabFolder = "Assets/_Project/Prefabs/UI/World";
        public const string SettingsPath = "Assets/_Project/Data/Settings/WorldUiSettings.asset";

        public const string CanvasName = "WorldUI_Canvas";
        public const string HealthBarName = "WorldHealthBar";

        /// <summary>Below HUD_Match (sorting order 0), so the HUD always draws on top of bars and arrows.</summary>
        public const int CanvasSortingOrder = -10;

        // Layout values are a starting point at 1920x1080 (same scaler as HUD_Match); C tweaks them in the prefab.
        private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
        private const float MatchWidthOrHeight = 0.5f;
        private static readonly Vector2 HealthBarSize = new Vector2(90f, 12f);
        private const float HealthBarBorder = 2f;
        private static readonly Vector2 ArrowSize = new Vector2(56f, 56f);
        private const string BuiltinRoundedSprite = "UI/Skin/UISprite.psd";
        // Built-in arrow points down; the icon is turned +90° so the arrow root points right at rotation 0.
        private const string BuiltinArrowSprite = "UI/Skin/DropdownArrow.psd";
        private const float ArrowIconRotation = 90f;

        private static readonly Color BarTrackColor = new Color(0f, 0f, 0f, 0.6f);
        private static readonly Color BarFillColor = new Color32(0xE0, 0x45, 0x3A, 0xFF);
        private static readonly Color ArrowColor = new Color32(0xE0, 0x2A, 0x2A, 0xFF);

        private static Sprite _white;
        private static int _uiLayer;

        [MenuItem("Tools/Create World UI Layout")]
        public static void CreateFromMenu()
        {
            if (Create(PrefabFolder, SettingsPath))
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(GetPrefabPath(PrefabFolder, CanvasName)));
        }

        public static string GetPrefabPath(string folder, string prefabName) => folder + "/" + prefabName + ".prefab";

        /// <summary>
        /// Creates WorldHealthBar (only if missing), WorldUiSettings at <paramref name="settingsPath"/> (only if
        /// missing) and WorldUI_Canvas. Returns false without touching anything when WorldUI_Canvas already exists.
        /// </summary>
        public static bool Create(string folder, string settingsPath)
        {
            string canvasPath = GetPrefabPath(folder, CanvasName);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(canvasPath) != null)
            {
                Debug.LogError($"[Create World UI Layout] {canvasPath} already exists; not overwritten. Delete it first to rebuild.");
                return false;
            }

            AssetFolderUtility.EnsureFolder(folder);
            WorldUiSettings settings = LoadOrCreateSettings(settingsPath);
            _white = AssetDatabase.LoadAssetAtPath<Sprite>(HudLayoutBuilder.WhiteSpritePath);
            if (_white == null) _white = AssetDatabase.GetBuiltinExtraResource<Sprite>(BuiltinRoundedSprite);
            _uiLayer = LayerMask.NameToLayer("UI");

            // Build in a preview scene so the open scene is never touched or dirtied.
            Scene scene = EditorSceneManager.NewPreviewScene();
            try
            {
                GameObject healthBar = LoadOrSave(scene, folder, HealthBarName, BuildHealthBar);
                GameObject canvas = BuildCanvas(scene, settings, healthBar.GetComponent<WorldHealthBar>());
                PrefabUtility.SaveAsPrefabAsset(canvas, canvasPath);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
                _white = null;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Create World UI Layout] Created {canvasPath}. Drag it into the match scene and assign EnemyManager and Camera.");
            return true;
        }

        private static WorldUiSettings LoadOrCreateSettings(string path)
        {
            var settings = AssetDatabase.LoadAssetAtPath<WorldUiSettings>(path);
            if (settings != null) return settings;

            AssetFolderUtility.EnsureFolder(Path.GetDirectoryName(path)?.Replace('\\', '/'));
            settings = ScriptableObject.CreateInstance<WorldUiSettings>();
            AssetDatabase.CreateAsset(settings, path);
            return settings;
        }

        private static GameObject LoadOrSave(Scene scene, string folder, string prefabName, System.Func<Scene, GameObject> build)
        {
            string path = GetPrefabPath(folder, prefabName);
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            return PrefabUtility.SaveAsPrefabAsset(build(scene), path);
        }

        private static GameObject BuildHealthBar(Scene scene)
        {
            RectTransform root = Root(scene, HealthBarName);
            root.sizeDelta = HealthBarSize;
            root.pivot = new Vector2(0.5f, 0f); // Bottom centre sits on the enemy's HealthBarAnchor.

            AddImage(Stretch(Node("Track", root), 0f), BarTrackColor);
            Image fill = AddImage(Stretch(Node("Fill", root), HealthBarBorder), BarFillColor);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 1f;

            root.gameObject.AddComponent<WorldHealthBar>().Configure(fill);
            return root.gameObject;
        }

        private static GameObject BuildCanvas(Scene scene, WorldUiSettings settings, WorldHealthBar healthBarPrefab)
        {
            RectTransform root = Root(scene, CanvasName);
            var canvas = root.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = CanvasSortingOrder;
            var scaler = root.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = MatchWidthOrHeight;
            // No GraphicRaycaster: world UI must never block clicks to the world (S3).

            RectTransform bars = Stretch(Node("HealthBars", root), 0f);

            RectTransform arrow = Node("OffscreenArrow", root);
            arrow.sizeDelta = ArrowSize;
            RectTransform icon = Stretch(Node("Icon", arrow), 0f);
            icon.localRotation = Quaternion.Euler(0f, 0f, ArrowIconRotation);
            Image iconImage = AddImage(icon, ArrowColor);
            iconImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(BuiltinArrowSprite);
            iconImage.preserveAspect = true;
            var warning = arrow.gameObject.AddComponent<OffscreenWarning>();
            warning.Configure(arrow);
            arrow.gameObject.SetActive(false); // Shown by OffscreenWarning only while needed.

            root.gameObject.AddComponent<WorldUiRoot>().Configure(null, null, settings, healthBarPrefab, bars, warning);
            return root.gameObject;
        }

        // ---------- Helpers (same conventions as HudLayoutBuilder) ----------

        private static RectTransform Root(Scene scene, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = _uiLayer;
            SceneManager.MoveGameObjectToScene(go, scene);
            return (RectTransform)go.transform;
        }

        private static RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = _uiLayer;
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static RectTransform Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
            return rect;
        }

        private static Image AddImage(RectTransform rect, Color color)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = _white;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }
    }
}
