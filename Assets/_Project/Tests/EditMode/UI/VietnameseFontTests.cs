using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;
using FateBastion.Editor.UI;

namespace FateBastion.Tests.EditMode.UI
{
    /// <summary>Checks the Be Vietnam Pro TextMeshPro font assets (C-10): Dynamic, and every Vietnamese letter present.</summary>
    public class VietnameseFontTests
    {
        private static readonly string[] FontAssetPaths = { HudLayoutBuilder.RegularFontPath, HudLayoutBuilder.SemiBoldFontPath };

        // 12 vowels x 6 tones (none, huyền, sắc, hỏi, ngã, nặng) + đ = 73, upper and lower case = 146 characters.
        private const string Vowels = "aăâeêioôơuưy";
        private static readonly string[] ToneMarks = { "", "̀", "́", "̉", "̃", "̣" };
        // Non-letter symbols the HUD uses ("Wave 8/15 · 0:18", "Tốc ×2").
        private const string HudSymbols = "·×";
        private const int VietnameseLetterCount = 146;

        [Test]
        public void VietnameseLetters_Generated_Are146PrecomposedCharacters()
        {
            string letters = VietnameseLetters();
            Assert.AreEqual(VietnameseLetterCount, letters.Length);
            Assert.AreEqual(VietnameseLetterCount, new HashSet<char>(letters).Count, "Không được trùng ký tự.");
        }

        [TestCaseSource(nameof(FontAssetPaths))]
        public void FontAsset_Settings_DynamicWithSourceFont(string path)
        {
            TMP_FontAsset font = LoadFont(path);
            Assert.AreEqual(AtlasPopulationMode.Dynamic, font.atlasPopulationMode, $"{path}: Atlas Population Mode phải là Dynamic.");
            Assert.IsNotNull(font.sourceFontFile, $"{path}: thiếu Source Font File – Dynamic cần file TTF khi chạy build.");
            Assert.IsTrue(font.isMultiAtlasTexturesEnabled, $"{path}: bật Multi Atlas Textures để không hết chỗ khi thêm ký tự.");
        }

        [TestCaseSource(nameof(FontAssetPaths))]
        public void FontAsset_SourceFont_HasEveryVietnameseLetter(string path)
        {
            TMP_FontAsset font = LoadFont(path);
            Assume.That(font.sourceFontFile, Is.Not.Null);

            // Ask the font file directly instead of TMP_FontAsset.HasCharacters(tryAddCharacter: true),
            // which would write glyphs into the committed atlas.
            FontEngine.InitializeFontEngine();
            Assert.AreEqual(FontEngineError.Success, FontEngine.LoadFontFace(font.sourceFontFile), $"{path}: không đọc được file font.");

            var missing = new StringBuilder();
            string required = VietnameseLetters() + HudSymbols;
            for (int i = 0; i < required.Length; i++)
            {
                if (!FontEngine.TryGetGlyphWithUnicodeValue(required[i], GlyphLoadFlags.LOAD_NO_BITMAP, out Glyph _))
                    missing.Append(required[i]);
            }

            Assert.AreEqual(string.Empty, missing.ToString(), $"{path}: font thiếu ký tự.");
        }

        [Test]
        public void TmpSettings_DefaultFont_IsBeVietnamProRegular()
        {
            Assert.AreSame(LoadFont(HudLayoutBuilder.RegularFontPath), TMP_Settings.defaultFontAsset,
                "Project Settings → TextMesh Pro → Default Font Asset phải là FA_BeVietnamPro_Regular.");
        }

        private static TMP_FontAsset LoadFont(string path)
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (font == null) Assert.Fail($"Chưa có font asset {path} – tạo theo hướng dẫn C-10.");
            return font;
        }

        private static string VietnameseLetters()
        {
            var lower = new StringBuilder();
            for (int v = 0; v < Vowels.Length; v++)
            {
                for (int t = 0; t < ToneMarks.Length; t++)
                    lower.Append((Vowels[v] + ToneMarks[t]).Normalize(NormalizationForm.FormC));
            }

            lower.Append('đ');
            string letters = lower.ToString();
            return letters + letters.ToUpperInvariant();
        }
    }
}
