using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using FateBastion.Editor.Import;

namespace FateBastion.Editor.UI
{
    /// <summary>
    /// Shared helpers for the one-shot UI layout builders (Create HUD Layout C-11, Create Lobby Layout C-12).
    /// Call <see cref="Begin"/> before building and <see cref="End"/> afterwards; the builders are single-threaded menu tools.
    /// </summary>
    public static class UiBuildKit
    {
        public const string RegularFontPath = "Assets/_Project/Art/Fonts/BeVietnamPro/FA_BeVietnamPro_Regular.asset";
        public const string SemiBoldFontPath = "Assets/_Project/Art/Fonts/BeVietnamPro/FA_BeVietnamPro_SemiBold.asset";
        // Plain white sprite for Filled images: the rounded built-in sprite smears when it is filled instead of sliced.
        public const string WhiteSpritePath = "Assets/_Project/Art/UI/UI_White.png";

        // Layout values are a starting point at 1920x1080; C tweaks them in the prefabs afterwards. Not balance numbers.
        public static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
        public const float MatchWidthOrHeight = 0.5f;
        private const string BuiltinRoundedSprite = "UI/Skin/UISprite.psd";

        public static readonly Color PanelColor = new Color32(0xF4, 0xF4, 0xEF, 0xEB);
        public static readonly Color BorderColor = new Color32(0xCF, 0xCF, 0xC8, 0xFF);
        public static readonly Color TextColor = new Color32(0x22, 0x22, 0x22, 0xFF);
        public static readonly Color MutedTextColor = new Color32(0x77, 0x77, 0x77, 0xFF);
        public static readonly Color SelectedColor = new Color32(0x2F, 0x7B, 0xE0, 0xFF);
        public static readonly Color TrackColor = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
        public static readonly Color IconPlaceholderColor = new Color32(0xD8, 0xD8, 0xD2, 0xFF);

        public static TMP_FontAsset Regular { get; private set; }
        public static TMP_FontAsset SemiBold { get; private set; }
        public static Sprite Rounded { get; private set; }
        public static Sprite White { get; private set; }
        private static int _uiLayer;

        /// <summary>Loads the C-10 font assets for a menu command; logs an error naming the tool when they are missing.</summary>
        public static bool TryLoadFonts(string toolName, out TMP_FontAsset regular, out TMP_FontAsset semiBold)
        {
            regular = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RegularFontPath);
            semiBold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SemiBoldFontPath);
            if (regular != null && semiBold != null) return true;

            Debug.LogError($"[{toolName}] Missing font asset {RegularFontPath} or {SemiBoldFontPath}. Create them first (C-10).");
            return false;
        }

        public static void Begin(TMP_FontAsset regular, TMP_FontAsset semiBold)
        {
            Regular = regular;
            SemiBold = semiBold;
            Rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>(BuiltinRoundedSprite);
            White = LoadOrCreateWhiteSprite();
            _uiLayer = LayerMask.NameToLayer("UI");
        }

        public static void End()
        {
            Regular = null;
            SemiBold = null;
        }

        public static string GetPrefabPath(string folder, string prefabName) => folder + "/" + prefabName + ".prefab";

        /// <summary>Returns the prefab at folder/name, or builds and saves it when missing (existing prefabs keep B's scripts).</summary>
        public static GameObject LoadOrSave(Scene scene, string folder, string prefabName, System.Func<Scene, GameObject> build)
        {
            string path = GetPrefabPath(folder, prefabName);
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            return PrefabUtility.SaveAsPrefabAsset(build(scene), path);
        }

        // ---------- Composite helpers ----------

        /// <summary>Screen Space Overlay canvas scaled from 1920x1080, with a raycaster for buttons.</summary>
        public static Canvas AddScreenCanvas(RectTransform root, int sortingOrder)
        {
            var canvas = root.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = root.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = MatchWidthOrHeight;
            root.gameObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static void BuildBar(RectTransform bar, Color border, Color fill, float sampleFill, string textName, string sampleText)
        {
            AddImage(Stretch(Node("Frame", bar), 0f), border);
            AddImage(Stretch(Node("Track", bar), 3f), TrackColor);
            Image fillImage = AddImage(Stretch(Node("Fill", bar), 3f), fill);
            fillImage.sprite = White;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.fillAmount = sampleFill;
            AddText(Stretch(Node(textName, bar), 0f), sampleText, Regular, 20f, TextColor, TextAlignmentOptions.Center);
        }

        public static RectTransform BuildButton(RectTransform parent, string name, string label, float width, float height, float fontSize)
        {
            RectTransform button = Node(name, parent);
            button.sizeDelta = new Vector2(width, height);
            Preferred(button, width, height);
            BuildButtonVisual(button, label, fontSize);
            return button;
        }

        public static void BuildButtonVisual(RectTransform button, string label, float fontSize)
        {
            Image frame = AddImage(Stretch(Node("Frame", button), 0f), BorderColor);
            AddButton(button, frame);
            AddImage(Stretch(Node("Background", button), 2f), Color.white);
            AddText(Stretch(Node("Label", button), 6f), label, Regular, fontSize, TextColor, TextAlignmentOptions.Center);
        }

        // ---------- Primitive helpers ----------

        private static Sprite LoadOrCreateWhiteSprite()
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(WhiteSpritePath);
            if (sprite != null) return sprite;

            AssetFolderUtility.EnsureFolder(Path.GetDirectoryName(WhiteSpritePath).Replace('\\', '/'));
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var pixels = new Color32[16];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
            texture.SetPixels32(pixels);
            File.WriteAllBytes(WhiteSpritePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(WhiteSpritePath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(WhiteSpritePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(WhiteSpritePath);
        }

        public static RectTransform IgnoreLayout(RectTransform rect)
        {
            rect.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            return rect;
        }

        public static RectTransform Root(Scene scene, string name)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = _uiLayer };
            SceneManager.MoveGameObjectToScene(go, scene);
            return (RectTransform)go.transform;
        }

        public static RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = _uiLayer };
            SceneManager.MoveGameObjectToScene(go, parent.gameObject.scene);
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        public static RectTransform Instance(Scene scene, GameObject prefab, Transform parent, string name)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            go.name = name;
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>Anchors and pivots at the same corner/edge point, then offsets from it.</summary>
        public static RectTransform Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        /// <summary>Fills the parent, shrunk by <paramref name="inset"/> on every side (negative grows it).</summary>
        public static RectTransform Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
            return rect;
        }

        /// <summary>Full-width row hanging from the parent's top edge.</summary>
        public static RectTransform Row(RectTransform rect, float left, float right, float top, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(left, -top - height);
            rect.offsetMax = new Vector2(-right, -top);
            return rect;
        }

        public static Image AddImage(RectTransform rect, Color color)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = Rounded;
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = false; // Only button frames receive clicks (AddButton turns theirs back on).
            return image;
        }

        public static void AddButton(RectTransform rect, Image target)
        {
            target.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = target;
        }

        public static TextMeshProUGUI AddText(RectTransform rect, string text, TMP_FontAsset font, float size, Color color, TextAlignmentOptions alignment)
        {
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;
            return label;
        }

        public static LayoutElement Preferred(RectTransform rect, float width, float height)
        {
            var element = rect.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = width;
            element.preferredHeight = height;
            return element;
        }

        public static HorizontalLayoutGroup Horizontal(RectTransform rect, float spacing, TextAnchor alignment)
        {
            var layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            SetupLayout(layout, spacing, alignment);
            return layout;
        }

        public static VerticalLayoutGroup Vertical(RectTransform rect, float spacing, TextAnchor alignment)
        {
            var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            SetupLayout(layout, spacing, alignment);
            layout.childForceExpandWidth = true;
            return layout;
        }

        private static void SetupLayout(HorizontalOrVerticalLayoutGroup layout, float spacing, TextAnchor alignment)
        {
            layout.spacing = spacing;
            layout.childAlignment = alignment;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }
    }
}
