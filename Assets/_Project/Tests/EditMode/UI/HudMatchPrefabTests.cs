using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using FateBastion.Editor.UI;

namespace FateBastion.Tests.EditMode.UI
{
    /// <summary>Checks the HUD_Match prefab layout (C-11) that B attaches view scripts to.</summary>
    public class HudMatchPrefabTests
    {
        private static readonly string HudPath = HudLayoutBuilder.GetPrefabPath(HudLayoutBuilder.PrefabFolder, HudLayoutBuilder.HudName);
        private static readonly string DeckSlotPath = HudLayoutBuilder.GetPrefabPath(HudLayoutBuilder.PrefabFolder, HudLayoutBuilder.DeckSlotName);
        private static readonly string CooldownSlotPath = HudLayoutBuilder.GetPrefabPath(HudLayoutBuilder.PrefabFolder, HudLayoutBuilder.CooldownSlotName);

        // Hierarchy contract with B (handover C-11). Renaming one of these breaks B's references: change both sides.
        private const string DeckBar = "Bottom/DeckBar";
        private const string MeteorSlot = "Bottom/MeteorSlot";
        private const string HeroInfoPanel = "HeroInfoPanel";
        private const string SkipWaveButton = "Top/SkipWaveButton";
        private const string UltimateGroup = "HeroInfoPanel/Content/UltimateGroup";
        private const string StatsPath = "HeroInfoPanel/Content/Stats/";

        private static readonly string[] RequiredTexts =
        {
            "Top/WavePanel/WaveText",
            "Top/WavePanel/CastleBar/CastleText",
            "HeroInfoPanel/Content/Header/NameLevelText",
            "HeroInfoPanel/Content/Header/TagsText",
            StatsPath + "DamageRow/Value", StatsPath + "DamageRow/Next",
            StatsPath + "AttackSpeedRow/Value", StatsPath + "AttackSpeedRow/Next",
            StatsPath + "RangeRow/Value", StatsPath + "RangeRow/Next",
            StatsPath + "DpsRow/Value", StatsPath + "DpsRow/Next",
            UltimateGroup + "/RageBar/RageText",
            "Bottom/Economy/GoldText",
            "Bottom/Economy/UnitCountText",
            MeteorSlot + "/TitleText", MeteorSlot + "/KeyText", MeteorSlot + "/TimerText",
        };

        private static readonly string[] RequiredButtons =
        {
            SkipWaveButton,
            "TopRight/SpeedButton",
            "TopRight/SettingsButton",
            UltimateGroup + "/UltimateButton",
            "HeroInfoPanel/Content/Actions/UpgradeButton",
            "HeroInfoPanel/Content/Actions/SellButton",
            MeteorSlot,
        };

        private static readonly string[] FilledBars = { "Top/WavePanel/CastleBar/Fill", UltimateGroup + "/RageBar/Fill" };

        private static readonly string[] HiddenByDefault =
        {
            HeroInfoPanel, SkipWaveButton, UltimateGroup,
            StatsPath + "DamageRow/Next", StatsPath + "AttackSpeedRow/Next", StatsPath + "RangeRow/Next", StatsPath + "DpsRow/Next",
        };

        private static readonly string[] DeckSlotChildren = { "RarityFrame", "SelectedFrame", "Icon", "KeyText", "NameText", "PriceText", "CountText" };

        private GameObject _root;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(HudPath) == null) return;
            _root = PrefabUtility.LoadPrefabContents(HudPath);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            if (_root != null) PrefabUtility.UnloadPrefabContents(_root);
            _root = null;
        }

        [Test]
        public void Root_Canvas_IsOverlayScaledFrom1920x1080()
        {
            RequirePrefab();
            var canvas = _root.GetComponent<Canvas>();
            var scaler = _root.GetComponent<CanvasScaler>();
            Assert.IsNotNull(canvas, "Gốc HUD_Match phải có Canvas.");
            Assert.IsNotNull(scaler, "Gốc HUD_Match phải có CanvasScaler.");
            Assert.IsNotNull(_root.GetComponent<GraphicRaycaster>(), "Gốc HUD_Match phải có GraphicRaycaster để bấm nút.");
            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, canvas.renderMode);
            Assert.AreEqual(CanvasScaler.ScaleMode.ScaleWithScreenSize, scaler.uiScaleMode);
            Assert.AreEqual(new Vector2(1920f, 1080f), scaler.referenceResolution);
        }

        [TestCaseSource(nameof(RequiredTexts))]
        public void RequiredText_Exists(string path)
        {
            RequirePrefab();
            Find<TextMeshProUGUI>(path);
        }

        [TestCaseSource(nameof(RequiredButtons))]
        public void RequiredButton_Exists(string path)
        {
            RequirePrefab();
            Assert.IsNotNull(Find<Button>(path).targetGraphic, $"{path}: Button thiếu Target Graphic.");
        }

        [TestCaseSource(nameof(FilledBars))]
        public void Bar_Fill_IsHorizontalFilledImage(string path)
        {
            RequirePrefab();
            Image fill = Find<Image>(path);
            Assert.AreEqual(Image.Type.Filled, fill.type, $"{path}: phải là Image Filled để B đặt fillAmount.");
            Assert.AreEqual(Image.FillMethod.Horizontal, fill.fillMethod);
            Assert.IsNotNull(fill.sprite, $"{path}: Image Filled cần sprite, không có sprite thì fillAmount không có tác dụng.");
        }

        [TestCaseSource(nameof(HiddenByDefault))]
        public void OptionalNode_IsInactiveByDefault(string path)
        {
            RequirePrefab();
            Assert.IsFalse(FindTransform(path).gameObject.activeSelf, $"{path} chỉ hiện khi cần, mặc định phải tắt.");
        }

        [Test]
        public void DeckBar_HasFiveDeckSlotInstancesInOrder()
        {
            RequirePrefab();
            Transform bar = FindTransform(DeckBar);
            var deckSlot = AssetDatabase.LoadAssetAtPath<GameObject>(DeckSlotPath);
            Assert.AreEqual(HudLayoutBuilder.DeckSlotCount, bar.childCount, "DeckBar phải có đúng 5 ô.");
            for (int i = 0; i < bar.childCount; i++)
            {
                Transform slot = bar.GetChild(i);
                Assert.AreEqual("DeckSlot_" + (i + 1), slot.name, "Ô deck phải xếp theo phím 1–5.");
                Assert.AreSame(deckSlot, PrefabUtility.GetCorrespondingObjectFromOriginalSource(slot.gameObject),
                    $"{slot.name} phải là instance của {DeckSlotPath} để sửa một chỗ là đủ 5 ô.");
            }
        }

        [Test]
        public void DeckSlot_HasRequiredChildrenAndCanvasGroup()
        {
            RequirePrefab();
            Transform slot = FindTransform(DeckBar).GetChild(0);
            Assert.IsNotNull(slot.GetComponent<CanvasGroup>(), "Ô deck cần CanvasGroup ở gốc để làm mờ khi thiếu Vàng.");
            Assert.IsNotNull(slot.GetComponent<Button>(), "Ô deck phải bấm được.");
            for (int i = 0; i < DeckSlotChildren.Length; i++)
                Assert.IsNotNull(slot.Find(DeckSlotChildren[i]), $"DeckSlot thiếu {DeckSlotChildren[i]}.");
            Assert.IsFalse(slot.Find("SelectedFrame").gameObject.activeSelf, "SelectedFrame chỉ bật khi ô đang được chọn.");
        }

        [Test]
        public void MeteorSlot_IsCooldownSlotWithReadyRadialOverlay()
        {
            RequirePrefab();
            Transform slot = FindTransform(MeteorSlot);
            Assert.AreSame(AssetDatabase.LoadAssetAtPath<GameObject>(CooldownSlotPath), PrefabUtility.GetCorrespondingObjectFromOriginalSource(slot.gameObject),
                $"MeteorSlot phải là instance của {CooldownSlotPath}.");

            Image overlay = Find<Image>(MeteorSlot + "/Overlay");
            Assert.AreEqual(Image.Type.Filled, overlay.type);
            Assert.AreEqual(Image.FillMethod.Radial360, overlay.fillMethod);
            Assert.AreEqual(0f, overlay.fillAmount, "Mặc định Thiên thạch sẵn sàng: lớp phủ hồi chiêu = 0.");
            Assert.IsFalse(overlay.raycastTarget, "Lớp phủ không được chặn click vào ô.");
        }

        [Test]
        public void AllTexts_UseBeVietnamProFontAssets()
        {
            RequirePrefab();
            var regular = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(HudLayoutBuilder.RegularFontPath);
            var semiBold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(HudLayoutBuilder.SemiBoldFontPath);
            TMP_Text[] texts = _root.GetComponentsInChildren<TMP_Text>(true);
            Assert.IsNotEmpty(texts);
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_FontAsset font = texts[i].font;
                Assert.IsTrue(font != null && (font == regular || font == semiBold),
                    $"{PathOf(texts[i].transform)} dùng font {(font != null ? font.name : "null")}, phải là Be Vietnam Pro (C-10).");
            }
        }

        [Test]
        public void RaycastTargets_AreOnlyButtonGraphics()
        {
            RequirePrefab();
            var buttonGraphics = new HashSet<Graphic>();
            Button[] buttons = _root.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++) buttonGraphics.Add(buttons[i].targetGraphic);

            Graphic[] graphics = _root.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i].raycastTarget)
                    Assert.IsTrue(buttonGraphics.Contains(graphics[i]),
                        $"{PathOf(graphics[i].transform)} không phải nút nhưng bật Raycast Target (tốn raycast, có thể chặn click).");
            }
        }

        private void RequirePrefab()
        {
            if (_root == null) Assert.Fail($"Chưa có prefab {HudPath} – chạy Tools → Create HUD Layout (C-11).");
        }

        private Transform FindTransform(string path)
        {
            Transform child = _root.transform.Find(path);
            Assert.IsNotNull(child, $"HUD_Match thiếu object {path}.");
            return child;
        }

        private T Find<T>(string path) where T : Component
        {
            var component = FindTransform(path).GetComponent<T>();
            Assert.IsNotNull(component, $"{path} thiếu component {typeof(T).Name}.");
            return component;
        }

        private string PathOf(Transform t)
        {
            string path = t.name;
            for (Transform p = t.parent; p != null && p != _root.transform; p = p.parent) path = p.name + "/" + path;
            return path;
        }
    }
}
