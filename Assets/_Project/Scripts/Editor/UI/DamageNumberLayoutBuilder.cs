using TMPro;
using UnityEditor;
using UnityEngine;
using FateBastion.UI.World;

namespace FateBastion.Editor.UI
{
    /// <summary>
    /// Tools > Create Damage Number Layout (C-14 part 2): creates the DamageNumber prefab and adds a "DamageNumbers"
    /// node with DamageNumberSpawner to the existing WorldUI_Canvas. Safe to run again: nothing is added twice and an
    /// existing DamageNumber prefab is kept as tuned.
    /// </summary>
    public static class DamageNumberLayoutBuilder
    {
        public const string DamageNumberName = "DamageNumber";
        public const string NumbersNodeName = "DamageNumbers";
        private const string ToolName = "Create Damage Number Layout";

        // Starting size at 1920x1080; the text does not wrap, so the box only needs to fit the largest number.
        private static readonly Vector2 NumberSize = new Vector2(240f, 70f);

        [MenuItem("Tools/Create Damage Number Layout")]
        public static void CreateFromMenu()
        {
            if (Create(WorldUiLayoutBuilder.PrefabFolder, WorldUiLayoutBuilder.SettingsPath))
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(WorldUiLayoutBuilder.PrefabFolder, WorldUiLayoutBuilder.CanvasName)));
        }

        public static string PrefabPath(string folder, string prefabName) => folder + "/" + prefabName + ".prefab";

        /// <summary>Returns false when WorldUI_Canvas, the settings or the fonts are missing.</summary>
        public static bool Create(string folder, string settingsPath)
        {
            string canvasPath = PrefabPath(folder, WorldUiLayoutBuilder.CanvasName);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(canvasPath) == null)
            {
                Debug.LogError($"[{ToolName}] {canvasPath} is missing; run Tools > Create World UI Layout first.");
                return false;
            }

            var settings = AssetDatabase.LoadAssetAtPath<WorldUiSettings>(settingsPath);
            if (settings == null)
            {
                Debug.LogError($"[{ToolName}] {settingsPath} is missing; run Tools > Create World UI Layout first.");
                return false;
            }

            if (!UiBuildKit.TryLoadFonts(ToolName, out TMP_FontAsset _, out TMP_FontAsset semiBold)) return false;

            DamageNumber numberPrefab = LoadOrCreateNumber(folder, semiBold);
            bool added = AddSpawner(canvasPath, settings, numberPrefab);
            AssetDatabase.SaveAssets();
            Debug.Log(added
                ? $"[{ToolName}] Added {NumbersNodeName} with DamageNumberSpawner to {canvasPath}."
                : $"[{ToolName}] {canvasPath} already has a DamageNumberSpawner; nothing changed.");
            return true;
        }

        private static DamageNumber LoadOrCreateNumber(string folder, TMP_FontAsset font)
        {
            string path = PrefabPath(folder, DamageNumberName);
            var existing = AssetDatabase.LoadAssetAtPath<DamageNumber>(path);
            if (existing != null) return existing;

            var root = new GameObject(DamageNumberName, typeof(RectTransform));
            try
            {
                root.layer = LayerMask.NameToLayer("UI");
                var rect = (RectTransform)root.transform;
                rect.sizeDelta = NumberSize;
                rect.pivot = new Vector2(0.5f, 0f); // Bottom centre starts on the hit point.

                var text = root.AddComponent<TextMeshProUGUI>();
                text.font = font;
                text.text = "123";
                text.fontSize = 28f;
                text.alignment = TextAlignmentOptions.Bottom;
                text.textWrappingMode = TextWrappingModes.NoWrap;
                text.overflowMode = TextOverflowModes.Overflow;
                text.raycastTarget = false; // World UI never blocks clicks (S3).

                root.AddComponent<DamageNumber>().Configure(text);
                return PrefabUtility.SaveAsPrefabAsset(root, path).GetComponent<DamageNumber>();
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static bool AddSpawner(string canvasPath, WorldUiSettings settings, DamageNumber numberPrefab)
        {
            GameObject canvas = PrefabUtility.LoadPrefabContents(canvasPath);
            try
            {
                if (canvas.GetComponentInChildren<DamageNumberSpawner>(true) != null) return false;

                var node = new GameObject(NumbersNodeName, typeof(RectTransform));
                node.layer = LayerMask.NameToLayer("UI");
                var rect = (RectTransform)node.transform;
                rect.SetParent(canvas.transform, false);
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.SetAsLastSibling(); // Numbers draw over health bars.

                // Camera left empty: Camera.main at runtime, like WorldUiRoot.
                node.AddComponent<DamageNumberSpawner>().Configure(settings, numberPrefab, rect, null);
                PrefabUtility.SaveAsPrefabAsset(canvas, canvasPath);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(canvas);
            }
        }
    }
}
