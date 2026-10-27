using TMPro;
using UnityEditor;
using UnityEngine;

namespace FateBastion.Editor.UI
{
    /// <summary>
    /// Tools > Create Vietnamese Font Assets: makes the Dynamic Be Vietnam Pro TMP font assets (C-10).
    /// Skips any asset that already exists.
    /// </summary>
    public static class VietnameseFontAssetCreator
    {
        private const string SourceFolder = "Assets/_Project/Art/Fonts/BeVietnamPro";
        // TMP's own "Create > TextMeshPro > Font Asset > SDF" menu: Dynamic, 1024x1024, padding 9, SDFAA.
        private const string TmpCreateSdfMenu = "Assets/Create/TextMeshPro/Font Asset/SDF";

        private static readonly string[] SourceFonts = { "BeVietnamPro-Regular", "BeVietnamPro-SemiBold" };
        private static readonly string[] TargetPaths = { HudLayoutBuilder.RegularFontPath, HudLayoutBuilder.SemiBoldFontPath };

        [MenuItem("Tools/Create Vietnamese Font Assets")]
        public static void CreateAll()
        {
            for (int i = 0; i < SourceFonts.Length; i++) Create(SourceFolder + "/" + SourceFonts[i] + ".ttf", TargetPaths[i]);
            AssetDatabase.SaveAssets();
        }

        private static void Create(string sourcePath, string targetPath)
        {
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(targetPath) != null)
            {
                Debug.Log($"[Create Vietnamese Font Assets] {targetPath} already exists, skipped.");
                return;
            }

            var source = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
            if (source == null)
            {
                Debug.LogError($"[Create Vietnamese Font Assets] Missing {sourcePath}.");
                return;
            }

            // Reuse TMP's menu instead of copying its internals; it writes "<name> SDF.asset" next to the TTF.
            Selection.activeObject = source;
            EditorApplication.ExecuteMenuItem(TmpCreateSdfMenu);
            string createdPath = SourceFolder + "/" + source.name + " SDF.asset";
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(createdPath) == null)
            {
                Debug.LogError($"[Create Vietnamese Font Assets] TMP did not create {createdPath}. Use {TmpCreateSdfMenu} by hand.");
                return;
            }

            string moveError = AssetDatabase.MoveAsset(createdPath, targetPath);
            if (!string.IsNullOrEmpty(moveError))
            {
                Debug.LogError($"[Create Vietnamese Font Assets] {moveError}");
                return;
            }

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(targetPath);
            font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            // A second atlas page instead of failing when many characters are added at runtime.
            font.isMultiAtlasTexturesEnabled = true;
            EditorUtility.SetDirty(font);
            Debug.Log($"[Create Vietnamese Font Assets] Created {targetPath}.");
        }
    }
}
