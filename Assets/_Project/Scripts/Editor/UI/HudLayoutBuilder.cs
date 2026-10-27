using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using FateBastion.Editor.Import;
using static FateBastion.Editor.UI.UiBuildKit;

namespace FateBastion.Editor.UI
{
    /// <summary>
    /// Tools > Create HUD Layout: builds the in-match HUD prefabs (C-11) from the GDD section 9 mockup.
    /// One-shot: never overwrites an existing prefab, so view scripts B attached later are not lost.
    /// </summary>
    public static class HudLayoutBuilder
    {
        public const string PrefabFolder = "Assets/_Project/Prefabs/UI";
        public const string RegularFontPath = UiBuildKit.RegularFontPath;
        public const string SemiBoldFontPath = UiBuildKit.SemiBoldFontPath;
        public const string WhiteSpritePath = UiBuildKit.WhiteSpritePath;

        public const string HudName = "HUD_Match";
        public const string DeckSlotName = "DeckSlot";
        public const string CooldownSlotName = "CooldownSlot";
        public const int DeckSlotCount = 5;

        // Layout values are a starting point taken from the GDD mockup at 1920x1080; C tweaks them in the prefab
        // afterwards. They are not balance numbers.
        private const float ScreenMargin = 24f;
        private static readonly Vector2 SlotSize = new Vector2(240f, 130f);
        private const float SlotIconSize = 64f;
        private const float SlotTextLeft = 88f;

        private static readonly Color CastleBorderColor = new Color32(0x2E, 0x9B, 0x3A, 0xFF);
        private static readonly Color CastleFillColor = new Color32(0x8F, 0xD1, 0x8F, 0xFF);
        private static readonly Color MeteorBorderColor = new Color32(0xE0, 0x90, 0x2A, 0xFF);
        private static readonly Color MeteorPanelColor = new Color32(0xFC, 0xEB, 0xD9, 0xF2);
        private static readonly Color NextStatColor = new Color32(0x2E, 0x9B, 0x3A, 0xFF);
        private static readonly Color RageFillColor = new Color32(0x8E, 0x5B, 0xD9, 0xFF);
        private static readonly Color CooldownOverlayColor = new Color(0f, 0f, 0f, 0.45f);

        [MenuItem("Tools/Create HUD Layout")]
        public static void CreateFromMenu()
        {
            if (!TryLoadFonts("Create HUD Layout", out TMP_FontAsset regular, out TMP_FontAsset semiBold)) return;

            if (Create(PrefabFolder, regular, semiBold))
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(GetPrefabPath(PrefabFolder, HudName)));
        }

        public static string GetPrefabPath(string folder, string prefabName) => UiBuildKit.GetPrefabPath(folder, prefabName);

        /// <summary>
        /// Creates DeckSlot, CooldownSlot (only if missing) and HUD_Match in <paramref name="folder"/>.
        /// Returns false without touching anything when HUD_Match already exists.
        /// </summary>
        public static bool Create(string folder, TMP_FontAsset regular, TMP_FontAsset semiBold)
        {
            string hudPath = GetPrefabPath(folder, HudName);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(hudPath) != null)
            {
                Debug.LogError($"[Create HUD Layout] {hudPath} already exists; not overwritten. Delete it first to rebuild.");
                return false;
            }

            Begin(regular, semiBold);
            AssetFolderUtility.EnsureFolder(folder);

            // Build in a preview scene so the open scene is never touched or dirtied.
            Scene scene = EditorSceneManager.NewPreviewScene();
            try
            {
                GameObject deckSlot = LoadOrSave(scene, folder, DeckSlotName, BuildDeckSlot);
                GameObject cooldownSlot = LoadOrSave(scene, folder, CooldownSlotName, BuildCooldownSlot);
                GameObject hud = BuildHud(scene, deckSlot, cooldownSlot);
                PrefabUtility.SaveAsPrefabAsset(hud, hudPath);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
                End();
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Create HUD Layout] Created {hudPath}.");
            return true;
        }

        // ---------- Slots ----------

        private static GameObject BuildDeckSlot(Scene scene)
        {
            RectTransform root = Root(scene, DeckSlotName);
            root.sizeDelta = SlotSize;
            root.gameObject.AddComponent<CanvasGroup>(); // B fades the slot when Gold is short or the hero cap is reached.
            Preferred(root, SlotSize.x, SlotSize.y);

            RectTransform selected = Stretch(Node("SelectedFrame", root), -4f);
            AddImage(selected, SelectedColor);
            selected.gameObject.SetActive(false);

            Image frame = AddImage(Stretch(Node("RarityFrame", root), 0f), BorderColor);
            AddButton(root, frame);
            AddImage(Stretch(Node("Background", root), 3f), PanelColor);

            AddSlotIcon(root);
            AddText(Place(Node("KeyText", root), new Vector2(0f, 1f), new Vector2(8f, -4f), new Vector2(24f, 26f)),
                "1", SemiBold, 20f, MutedTextColor, TextAlignmentOptions.TopLeft);

            AddSlotTitle(root, "NameText", "Cung thủ");
            AddText(Row(Node("PriceText", root), SlotTextLeft, 10f, 70f, 32f), "100", Regular, 22f, MutedTextColor, TextAlignmentOptions.Left);
            AddText(Row(Node("CountText", root), SlotTextLeft, 14f, 70f, 32f), "2/6", Regular, 22f, MutedTextColor, TextAlignmentOptions.Right);
            return root.gameObject;
        }

        private static GameObject BuildCooldownSlot(Scene scene)
        {
            RectTransform root = Root(scene, CooldownSlotName);
            root.sizeDelta = SlotSize;
            Preferred(root, SlotSize.x, SlotSize.y);

            Image frame = AddImage(Stretch(Node("Frame", root), 0f), MeteorBorderColor);
            AddButton(root, frame);
            AddImage(Stretch(Node("Background", root), 3f), MeteorPanelColor);

            AddSlotIcon(root);
            AddSlotTitle(root, "TitleText", "Thiên thạch");
            AddText(Row(Node("KeyText", root), SlotTextLeft, 10f, 70f, 32f), "Q", SemiBold, 22f, MutedTextColor, TextAlignmentOptions.Left);
            AddText(Row(Node("TimerText", root), SlotTextLeft, 14f, 70f, 32f), "0:27", Regular, 22f, MutedTextColor, TextAlignmentOptions.Right);

            // Radial overlay over the whole slot; 0 = ready. B drives fillAmount from OnMeteorCooldownChanged.
            Image overlay = AddImage(Stretch(Node("Overlay", root), 3f), CooldownOverlayColor);
            overlay.sprite = White;
            overlay.type = Image.Type.Filled;
            overlay.fillMethod = Image.FillMethod.Radial360;
            overlay.fillOrigin = (int)Image.Origin360.Top;
            overlay.fillClockwise = false;
            overlay.fillAmount = 0f;
            return root.gameObject;
        }

        // ---------- HUD ----------

        private static GameObject BuildHud(Scene scene, GameObject deckSlot, GameObject cooldownSlot)
        {
            RectTransform root = Root(scene, HudName);
            AddScreenCanvas(root, 0);

            BuildTop(root);
            BuildTopRight(root);
            BuildHeroInfoPanel(root);
            BuildBottom(scene, root, deckSlot, cooldownSlot);
            return root.gameObject;
        }

        private static void BuildTop(RectTransform root)
        {
            RectTransform top = Stretch(Node("Top", root), 0f);

            RectTransform wave = Place(Node("WavePanel", top), new Vector2(0.5f, 1f), new Vector2(0f, -ScreenMargin), new Vector2(560f, 116f));
            AddImage(Stretch(Node("Frame", wave), 0f), BorderColor);
            AddImage(Stretch(Node("Background", wave), 2f), PanelColor);
            AddText(Row(Node("WaveText", wave), 12f, 12f, 12f, 50f), "Wave 8/15 · 0:18", SemiBold, 36f, TextColor, TextAlignmentOptions.Center);

            RectTransform bar = Place(Node("CastleBar", wave), new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(460f, 32f));
            BuildBar(bar, CastleBorderColor, CastleFillColor, 0.85f, "CastleText", "Thành 17/20");

            RectTransform skip = Place(Node("SkipWaveButton", top), new Vector2(0.5f, 1f), new Vector2(0f, -152f), new Vector2(300f, 60f));
            BuildButtonVisual(skip, "Bỏ qua wave (N)", 26f);
            skip.gameObject.SetActive(false); // Only during Preparing/Resting (S5).
        }

        private static void BuildTopRight(RectTransform root)
        {
            RectTransform group = Place(Node("TopRight", root), new Vector2(1f, 1f), new Vector2(-ScreenMargin, -ScreenMargin), new Vector2(400f, 64f));
            Horizontal(group, 16f, TextAnchor.UpperRight);
            BuildButton(group, "SpeedButton", "Tốc ×1", 170f, 64f, 26f);
            BuildButton(group, "SettingsButton", "Cài đặt", 190f, 64f, 26f);
        }

        private static void BuildHeroInfoPanel(RectTransform root)
        {
            RectTransform panel = Place(Node("HeroInfoPanel", root), new Vector2(0f, 0.5f), new Vector2(40f, 40f), new Vector2(560f, 0f));
            panel.gameObject.AddComponent<CanvasGroup>();
            // Height follows the content, so the panel has no empty space when UltimateGroup is hidden.
            Vertical(panel, 0f, TextAnchor.UpperLeft).padding = new RectOffset(24, 24, 24, 24);
            panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            AddImage(IgnoreLayout(Stretch(Node("RarityFrame", panel), 0f)), BorderColor); // B tints by rarity.
            AddImage(IgnoreLayout(Stretch(Node("Background", panel), 4f)), PanelColor);

            RectTransform content = Node("Content", panel);
            Vertical(content, 12f, TextAnchor.UpperLeft);

            RectTransform header = Node("Header", content);
            Vertical(header, 2f, TextAnchor.UpperLeft);
            AddText(Node("NameLevelText", header), "Thánh Nữ · Cấp 2/3", SemiBold, 30f, TextColor, TextAlignmentOptions.Left);
            AddText(Node("TagsText", header), "Epic · Lửa · Hỗ trợ", Regular, 22f, MutedTextColor, TextAlignmentOptions.Left);

            RectTransform stats = Node("Stats", content);
            Vertical(stats, 4f, TextAnchor.UpperLeft);
            BuildStatRow(stats, "DamageRow", "Sát thương", "120", "168");
            BuildStatRow(stats, "AttackSpeedRow", "Tốc đánh", "1.10/s", "1.21/s");
            BuildStatRow(stats, "RangeRow", "Tầm đánh", "8.0 m", "8.8 m");
            BuildStatRow(stats, "DpsRow", "DPS", "132", "203");

            // Legendary only (Ultimate, TEAM_ASSIGNMENT 3.1 #5).
            RectTransform ultimate = Node("UltimateGroup", content);
            Horizontal(ultimate, 12f, TextAnchor.MiddleLeft);
            Preferred(ultimate, -1f, 56f);
            RectTransform rage = Node("RageBar", ultimate);
            LayoutElement rageLayout = Preferred(rage, 260f, 28f);
            rageLayout.flexibleWidth = 1f;
            BuildBar(rage, BorderColor, RageFillColor, 0.6f, "RageText", "Nộ 60/100");
            BuildButton(ultimate, "UltimateButton", "Ultimate (R)", 200f, 52f, 22f);
            ultimate.gameObject.SetActive(false);

            RectTransform actions = Node("Actions", content);
            Horizontal(actions, 16f, TextAnchor.MiddleLeft);
            BuildButton(actions, "UpgradeButton", "Nâng (U) 525", 230f, 60f, 24f);
            BuildButton(actions, "SellButton", "Bán (X) 306", 230f, 60f, 24f);

            panel.gameObject.SetActive(false); // Only while a hero is selected (OnHeroSelected).
        }

        private static void BuildStatRow(RectTransform parent, string name, string label, string value, string next)
        {
            RectTransform row = Node(name, parent);
            Horizontal(row, 8f, TextAnchor.MiddleLeft);
            Preferred(row, -1f, 34f);
            Preferred(AddText(Node("Label", row), label, Regular, 24f, MutedTextColor, TextAlignmentOptions.Left).rectTransform, 200f, 34f);
            Preferred(AddText(Node("Value", row), value, SemiBold, 24f, TextColor, TextAlignmentOptions.Left).rectTransform, 130f, 34f);
            TextMeshProUGUI nextText = AddText(Node("Next", row), next, SemiBold, 24f, NextStatColor, TextAlignmentOptions.Left);
            Preferred(nextText.rectTransform, 130f, 34f);
            nextText.gameObject.SetActive(false); // Shown while hovering the Upgrade button.
        }

        private static void BuildBottom(Scene scene, RectTransform root, GameObject deckSlot, GameObject cooldownSlot)
        {
            RectTransform bottom = Node("Bottom", root);
            bottom.anchorMin = new Vector2(0f, 0f);
            bottom.anchorMax = new Vector2(1f, 0f);
            bottom.pivot = new Vector2(0.5f, 0f);
            bottom.anchoredPosition = new Vector2(0f, ScreenMargin);
            bottom.sizeDelta = new Vector2(0f, 140f);
            HorizontalLayoutGroup layout = Horizontal(bottom, 20f, TextAnchor.MiddleLeft);
            layout.padding = new RectOffset(40, 40, 0, 0);

            RectTransform economy = Node("Economy", bottom);
            Vertical(economy, 4f, TextAnchor.MiddleLeft);
            Preferred(economy, 200f, SlotSize.y);
            AddText(Node("GoldText", economy), "Vàng 450", SemiBold, 36f, TextColor, TextAlignmentOptions.Left);
            AddText(Node("UnitCountText", economy), "Quân số 12/15", Regular, 24f, TextColor, TextAlignmentOptions.Left);

            Instance(scene, cooldownSlot, bottom, "MeteorSlot");

            RectTransform deckBar = Node("DeckBar", bottom);
            Horizontal(deckBar, 16f, TextAnchor.MiddleLeft);
            string[] sampleNames = { "Cung thủ", "Phù thủy", "Người Tuyết", "Thánh Nữ", "Long Vương" };
            for (int i = 0; i < DeckSlotCount; i++)
            {
                RectTransform slot = Instance(scene, deckSlot, deckBar, DeckSlotName + "_" + (i + 1));
                slot.Find("KeyText").GetComponent<TextMeshProUGUI>().text = (i + 1).ToString();
                slot.Find("NameText").GetComponent<TextMeshProUGUI>().text = sampleNames[i];
            }
        }

        // ---------- Composite helpers ----------

        private static void AddSlotIcon(RectTransform slot)
        {
            Image icon = AddImage(Place(Node("Icon", slot), new Vector2(0f, 0.5f), new Vector2(12f, -6f), new Vector2(SlotIconSize, SlotIconSize)), IconPlaceholderColor);
            icon.preserveAspect = true;
        }

        private static void AddSlotTitle(RectTransform slot, string name, string sample)
        {
            // Auto size so long names ("Người Tuyết", "Long Vương") fit instead of being cut; the text only changes on deck setup.
            TextMeshProUGUI title = AddText(Row(Node(name, slot), SlotTextLeft, 10f, 24f, 40f), sample, SemiBold, 24f, TextColor, TextAlignmentOptions.Left);
            title.enableAutoSizing = true;
            title.fontSizeMin = 16f;
            title.fontSizeMax = 24f;
        }
    }
}
