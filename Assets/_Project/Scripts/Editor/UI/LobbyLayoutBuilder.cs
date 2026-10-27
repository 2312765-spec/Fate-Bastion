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
    /// Tools > Create Lobby Layout: builds the out-of-match screens and the in-match menus (C-12, GDD section 9).
    /// One-shot: existing prefabs are reused, never overwritten, so view scripts B attached later are not lost.
    /// </summary>
    public static class LobbyLayoutBuilder
    {
        public const string PrefabFolder = HudLayoutBuilder.PrefabFolder;

        public const string LobbyCanvasName = "Lobby_Canvas";
        public const string MatchMenusCanvasName = "MatchMenus_Canvas";
        public const string HeroCardName = "HeroCard";
        public const string LevelCardName = "LevelCard";
        public const string MainScreenName = "Screen_Main";
        public const string LevelSelectScreenName = "Screen_LevelSelect";
        public const string GachaScreenName = "Screen_Gacha";
        public const string CollectionScreenName = "Screen_Collection";
        public const string SettingsScreenName = "Screen_Settings";
        public const string PauseScreenName = "Screen_Pause";
        public const string ResultScreenName = "Screen_Result";
        public const string ConfirmDialogName = "ConfirmDialog";

        /// <summary>Every prefab this tool creates, children before the prefabs that nest them.</summary>
        public static readonly string[] AllPrefabNames =
        {
            HeroCardName, LevelCardName, MainScreenName, LevelSelectScreenName, GachaScreenName, CollectionScreenName,
            SettingsScreenName, PauseScreenName, ResultScreenName, ConfirmDialogName, LobbyCanvasName, MatchMenusCanvasName,
        };

        // Slot counts of the layout (mirrors the rules in GDD 5 and 9); the views fill and hide them, they are not balance values.
        public const int CampaignLevelCount = 3;
        public const int DeckSize = 5;
        public const int PullResultCount = 10;
        public const int EnemyRowCount = 5;
        public const int StarCount = 3;
        public const int SampleHeroCount = 9;
        public const int MatchMenusSortingOrder = 10; // Above HUD_Match (0).

        public static readonly string[] RarityNames = { "Common", "Rare", "Epic", "Legendary" };
        public static readonly string[] MergeRarityNames = { "Common", "Rare", "Epic" }; // Legendary cannot be merged (S8).

        // Layout values at 1920x1080; C tweaks them in the prefabs afterwards.
        private const float TopBarHeight = 96f;
        private const float SideMargin = 90f;
        private static readonly Vector2 HeroCardSize = new Vector2(180f, 232f);
        private static readonly Vector2 LevelCardSize = new Vector2(400f, 500f);
        private const float GridSpacing = 20f;
        private const int CollectionColumns = 6;
        private const int ResultColumns = 5;

        // Sample colors only: B tints frames by rarity at runtime. Rarity hues follow GDD 5 (grey, blue, purple, orange).
        private static readonly Color[] RarityColors =
        {
            new Color32(0x9E, 0x9E, 0x9E, 0xFF), new Color32(0x3D, 0x7B, 0xE0, 0xFF),
            new Color32(0x8E, 0x5B, 0xD9, 0xFF), new Color32(0xF0, 0x9A, 0x2A, 0xFF),
        };
        private static readonly Color LobbyBackgroundColor = new Color32(0xE6, 0xE4, 0xDC, 0xFF);
        private static readonly Color BlockerColor = new Color(0f, 0f, 0f, 0.55f);
        private static readonly Color LockColor = new Color(0.1f, 0.1f, 0.1f, 0.85f);
        private static readonly Color StarOffColor = new Color32(0xD8, 0xD8, 0xD2, 0xFF);
        private static readonly Color StarOnColor = new Color32(0xF2, 0xB5, 0x1F, 0xFF);
        private static readonly Color GemColor = new Color32(0x2B, 0xB3, 0xC0, 0xFF);
        private static readonly Color BadgeColor = new Color32(0x22, 0x22, 0x22, 0xE6);
        private static readonly Color NewBadgeColor = new Color32(0xE0, 0x4A, 0x3A, 0xFF);
        private static readonly Color WarningColor = new Color32(0xC6, 0x28, 0x28, 0xFF);
        private static readonly Color PityFillColor = new Color32(0xF0, 0x9A, 0x2A, 0xFF);

        private static readonly string[] SampleHeroNames =
        {
            "Cung thủ Làng", "Phù thủy Lửa", "Lính Băng", "Tu sĩ Làng", "Người Tuyết",
            "Pháp sư Sấm", "Chiến binh Sấm", "Thánh Nữ", "Long Vương",
        };
        private static readonly int[] SampleHeroRarities = { 0, 0, 0, 0, 1, 1, 2, 2, 3 };
        private static readonly string[] SampleEnemyNames = { "Thường", "Nhanh", "Giáp nặng", "Bay", "Boss" };
        private static readonly string[] SampleRates = { "65%", "25%", "8.5%", "1.5%" };

        [MenuItem("Tools/Create Lobby Layout")]
        public static void CreateFromMenu()
        {
            if (!TryLoadFonts("Create Lobby Layout", out TMP_FontAsset regular, out TMP_FontAsset semiBold)) return;

            if (Create(PrefabFolder, regular, semiBold))
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(GetPrefabPath(PrefabFolder, LobbyCanvasName)));
        }

        /// <summary>
        /// Creates every missing prefab of <see cref="AllPrefabNames"/> in <paramref name="folder"/>, reusing the ones that exist.
        /// Returns false without touching anything when both canvases already exist.
        /// </summary>
        public static bool Create(string folder, TMP_FontAsset regular, TMP_FontAsset semiBold)
        {
            string lobbyPath = GetPrefabPath(folder, LobbyCanvasName);
            string menusPath = GetPrefabPath(folder, MatchMenusCanvasName);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(lobbyPath) != null && AssetDatabase.LoadAssetAtPath<GameObject>(menusPath) != null)
            {
                Debug.LogError($"[Create Lobby Layout] {lobbyPath} and {menusPath} already exist; not overwritten. Delete one first to rebuild it.");
                return false;
            }

            Begin(regular, semiBold);
            AssetFolderUtility.EnsureFolder(folder);

            // Build in a preview scene so the open scene is never touched or dirtied.
            Scene scene = EditorSceneManager.NewPreviewScene();
            try
            {
                GameObject heroCard = LoadOrSave(scene, folder, HeroCardName, BuildHeroCard);
                GameObject levelCard = LoadOrSave(scene, folder, LevelCardName, BuildLevelCard);
                GameObject main = LoadOrSave(scene, folder, MainScreenName, BuildMainScreen);
                GameObject levelSelect = LoadOrSave(scene, folder, LevelSelectScreenName, s => BuildLevelSelectScreen(s, levelCard));
                GameObject gacha = LoadOrSave(scene, folder, GachaScreenName, s => BuildGachaScreen(s, heroCard));
                GameObject collection = LoadOrSave(scene, folder, CollectionScreenName, s => BuildCollectionScreen(s, heroCard));
                GameObject settings = LoadOrSave(scene, folder, SettingsScreenName, BuildSettingsScreen);
                GameObject pause = LoadOrSave(scene, folder, PauseScreenName, BuildPauseScreen);
                GameObject result = LoadOrSave(scene, folder, ResultScreenName, BuildResultScreen);
                GameObject confirm = LoadOrSave(scene, folder, ConfirmDialogName, BuildConfirmDialog);

                LoadOrSave(scene, folder, LobbyCanvasName, s => BuildLobbyCanvas(s, main, levelSelect, gacha, collection, settings, confirm));
                LoadOrSave(scene, folder, MatchMenusCanvasName, s => BuildMatchMenusCanvas(s, pause, settings, result, confirm));
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
                End();
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Create Lobby Layout] Created lobby and match menu prefabs in {folder}.");
            return true;
        }

        // ---------- Canvases ----------

        private static GameObject BuildLobbyCanvas(Scene scene, GameObject main, GameObject levelSelect, GameObject gacha,
            GameObject collection, GameObject settings, GameObject confirm)
        {
            RectTransform root = Root(scene, LobbyCanvasName);
            AddScreenCanvas(root, 0);
            AddImage(Stretch(Node("Background", root), 0f), LobbyBackgroundColor).sprite = White;

            // TopBar sits below the pages so the Gacha result blocker also covers it; pages have no raycast graphics there.
            BuildTopBar(root);
            Instance(scene, main, root, MainScreenName);
            Instance(scene, levelSelect, root, LevelSelectScreenName).gameObject.SetActive(false);
            Instance(scene, gacha, root, GachaScreenName).gameObject.SetActive(false);
            Instance(scene, collection, root, CollectionScreenName).gameObject.SetActive(false);
            Instance(scene, settings, root, SettingsScreenName).gameObject.SetActive(false);
            Instance(scene, confirm, root, ConfirmDialogName).gameObject.SetActive(false);
            return root.gameObject;
        }

        private static GameObject BuildMatchMenusCanvas(Scene scene, GameObject pause, GameObject settings, GameObject result, GameObject confirm)
        {
            // Separate from HUD_Match so opening a menu does not rebuild the static HUD canvas.
            RectTransform root = Root(scene, MatchMenusCanvasName);
            AddScreenCanvas(root, MatchMenusSortingOrder);
            Instance(scene, pause, root, PauseScreenName).gameObject.SetActive(false);
            Instance(scene, settings, root, SettingsScreenName).gameObject.SetActive(false);
            Instance(scene, result, root, ResultScreenName).gameObject.SetActive(false);
            Instance(scene, confirm, root, ConfirmDialogName).gameObject.SetActive(false);
            return root.gameObject;
        }

        private static void BuildTopBar(RectTransform root)
        {
            RectTransform bar = Node("TopBar", root);
            bar.anchorMin = new Vector2(0f, 1f);
            bar.anchorMax = new Vector2(1f, 1f);
            bar.pivot = new Vector2(0.5f, 1f);
            bar.offsetMin = new Vector2(0f, -TopBarHeight);
            bar.offsetMax = Vector2.zero;
            AddImage(Stretch(Node("Background", bar), 0f), PanelColor);

            RectTransform back = Place(Node("BackButton", bar), new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(200f, 64f));
            BuildButtonVisual(back, "Quay lại", 26f);
            back.gameObject.SetActive(false); // Hidden on Screen_Main.

            AddText(Place(Node("TitleText", bar), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800f, 64f)),
                "Sảnh", SemiBold, 36f, TextColor, TextAlignmentOptions.Center);

            RectTransform gems = Place(Node("GemPanel", bar), new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(280f, 64f));
            HorizontalLayoutGroup layout = Horizontal(gems, 12f, TextAnchor.MiddleLeft);
            layout.padding = new RectOffset(16, 16, 0, 0);
            AddImage(IgnoreLayout(Stretch(Node("Frame", gems), 0f)), BorderColor);
            AddImage(IgnoreLayout(Stretch(Node("Background", gems), 2f)), Color.white);
            Preferred(AddImage(Node("GemIcon", gems), GemColor).rectTransform, 36f, 36f);
            Preferred(AddText(Node("GemText", gems), "1.000 Ngọc", SemiBold, 28f, TextColor, TextAlignmentOptions.Left).rectTransform, 200f, 64f);
        }

        // ---------- Cards ----------

        private static GameObject BuildHeroCard(Scene scene)
        {
            RectTransform root = Root(scene, HeroCardName);
            root.sizeDelta = HeroCardSize;
            // B turns blocksRaycasts off while the card is dragged into a deck slot.
            root.gameObject.AddComponent<CanvasGroup>();
            Preferred(root, HeroCardSize.x, HeroCardSize.y);

            RectTransform selected = Stretch(Node("SelectedFrame", root), -4f);
            AddImage(selected, SelectedColor);
            selected.gameObject.SetActive(false);

            Image frame = AddImage(Stretch(Node("RarityFrame", root), 0f), RarityColors[2]); // B tints by rarity.
            AddButton(root, frame);
            AddImage(Stretch(Node("Background", root), 4f), PanelColor);

            Image icon = AddImage(Place(Node("Icon", root), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(132f, 132f)), IconPlaceholderColor);
            icon.preserveAspect = true;

            TextMeshProUGUI name = AddText(Row(Node("NameText", root), 8f, 8f, 152f, 36f), "Thánh Nữ", SemiBold, 22f, TextColor, TextAlignmentOptions.Center);
            name.enableAutoSizing = true;
            name.fontSizeMin = 14f;
            name.fontSizeMax = 22f;
            AddText(Row(Node("RarityText", root), 8f, 8f, 188f, 28f), "Epic", Regular, 18f, MutedTextColor, TextAlignmentOptions.Center);

            RectTransform copies = Place(Node("CopyBadge", root), new Vector2(1f, 1f), new Vector2(-6f, -6f), new Vector2(56f, 34f));
            AddImage(copies, BadgeColor);
            AddText(Stretch(Node("CopyText", copies), 0f), "×3", SemiBold, 20f, Color.white, TextAlignmentOptions.Center);

            BuildTag(root, "InDeckMark", "Deck", SelectedColor).gameObject.SetActive(false);
            BuildTag(root, "NewBadge", "Mới", NewBadgeColor).gameObject.SetActive(false); // Gacha result: first time owned.
            return root.gameObject;
        }

        private static GameObject BuildLevelCard(Scene scene)
        {
            RectTransform root = Root(scene, LevelCardName);
            root.sizeDelta = LevelCardSize;
            Preferred(root, LevelCardSize.x, LevelCardSize.y);

            RectTransform selected = Stretch(Node("SelectedFrame", root), -4f);
            AddImage(selected, SelectedColor);
            selected.gameObject.SetActive(false);

            Image frame = AddImage(Stretch(Node("Frame", root), 0f), BorderColor);
            AddButton(root, frame);
            AddImage(Stretch(Node("Background", root), 3f), PanelColor);

            RectTransform content = Stretch(Node("Content", root), 28f);
            Vertical(content, 12f, TextAnchor.UpperLeft);
            Label(content, "TitleText", "Màn 1", SemiBold, 36f, TextColor, TextAlignmentOptions.Left, 48f);
            BuildStars(content, 56f, TextAnchor.MiddleLeft);
            Label(content, "RecordText", "Kỷ lục: wave 23", SemiBold, 28f, TextColor, TextAlignmentOptions.Left, 56f)
                .gameObject.SetActive(false); // Endless only.
            Label(content, "EnemyLabel", "Quái xuất hiện", Regular, 22f, MutedTextColor, TextAlignmentOptions.Left, 32f);

            RectTransform enemies = Node("Enemies", content);
            Vertical(enemies, 6f, TextAnchor.UpperLeft);
            for (int i = 0; i < EnemyRowCount; i++)
            {
                RectTransform row = Node("EnemyRow_" + (i + 1), enemies);
                Horizontal(row, 12f, TextAnchor.MiddleLeft);
                Preferred(row, -1f, 40f);
                Image enemyIcon = AddImage(Node("Icon", row), IconPlaceholderColor);
                enemyIcon.preserveAspect = true;
                Preferred(enemyIcon.rectTransform, 40f, 40f);
                Preferred(AddText(Node("NameText", row), SampleEnemyNames[i], Regular, 24f, TextColor, TextAlignmentOptions.Left).rectTransform, 260f, 40f);
            }

            RectTransform locked = Stretch(Node("LockOverlay", root), 3f);
            AddImage(locked, LockColor);
            TextMeshProUGUI lockText = AddText(Stretch(Node("LockText", locked), 24f), "Qua màn 1 để mở", SemiBold, 28f, Color.white, TextAlignmentOptions.Center);
            lockText.textWrappingMode = TextWrappingModes.Normal;
            locked.gameObject.SetActive(false);
            return root.gameObject;
        }

        // ---------- Lobby pages ----------

        private static GameObject BuildMainScreen(Scene scene)
        {
            RectTransform root = Page(scene, MainScreenName);
            // Taller than the 80 pt line height: with Ellipsis, TMP hides a line that does not fit vertically.
            AddText(Place(Node("TitleText", root), new Vector2(0.5f, 0.5f), new Vector2(0f, 260f), new Vector2(1200f, 130f)),
                "Thủ Thành Vận Mệnh", SemiBold, 80f, TextColor, TextAlignmentOptions.Center);
            AddText(Place(Node("SubtitleText", root), new Vector2(0.5f, 0.5f), new Vector2(0f, 175f), new Vector2(800f, 50f)),
                "Fate Bastion", Regular, 32f, MutedTextColor, TextAlignmentOptions.Center);

            RectTransform menu = Place(Node("Menu", root), new Vector2(0.5f, 0.5f), new Vector2(0f, -110f), new Vector2(440f, 0f));
            Vertical(menu, 18f, TextAnchor.UpperCenter);
            menu.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            BuildButton(menu, "PlayButton", "Chơi", 440f, 92f, 36f);
            BuildButton(menu, "GachaButton", "Gacha", 440f, 72f, 28f);
            BuildButton(menu, "CollectionButton", "Túi & Deck", 440f, 72f, 28f);
            BuildButton(menu, "SettingsButton", "Cài đặt", 440f, 72f, 28f);
            BuildButton(menu, "QuitButton", "Thoát", 440f, 72f, 28f);
            return root.gameObject;
        }

        private static GameObject BuildLevelSelectScreen(Scene scene, GameObject levelCard)
        {
            RectTransform root = Page(scene, LevelSelectScreenName);

            float levelsWidth = (CampaignLevelCount + 1) * LevelCardSize.x + CampaignLevelCount * 32f;
            RectTransform levels = Place(Node("Levels", root), new Vector2(0.5f, 0.5f), new Vector2(0f, 50f), new Vector2(levelsWidth, LevelCardSize.y));
            Horizontal(levels, 32f, TextAnchor.MiddleCenter);
            for (int i = 0; i < CampaignLevelCount; i++)
            {
                RectTransform card = Instance(scene, levelCard, levels, LevelCardName + "_" + (i + 1));
                SetText(card, "Content/TitleText", "Màn " + (i + 1));
            }
            RectTransform endless = Instance(scene, levelCard, levels, LevelCardName + "_Endless");
            SetText(endless, "Content/TitleText", "Vô tận");
            endless.Find("Content/Stars").gameObject.SetActive(false);
            endless.Find("Content/RecordText").gameObject.SetActive(true);
            endless.Find("LockOverlay").gameObject.SetActive(true); // Unlocked after clearing level 1 (GDD 9).

            RectTransform footer = Place(Node("Footer", root), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(levelsWidth, 120f));
            HorizontalLayoutGroup layout = Horizontal(footer, 24f, TextAnchor.MiddleLeft);
            layout.padding = new RectOffset(24, 24, 0, 0);
            AddImage(IgnoreLayout(Stretch(Node("Frame", footer), 0f)), BorderColor);
            AddImage(IgnoreLayout(Stretch(Node("Background", footer), 2f)), PanelColor);

            Preferred(AddText(Node("DeckLabel", footer), "Deck", SemiBold, 30f, TextColor, TextAlignmentOptions.Left).rectTransform, 100f, 72f);
            RectTransform preview = Node("DeckPreview", footer);
            Horizontal(preview, 12f, TextAnchor.MiddleLeft);
            for (int i = 0; i < DeckSize; i++)
            {
                Image icon = AddImage(Node("DeckIcon_" + (i + 1), preview), IconPlaceholderColor);
                icon.preserveAspect = true;
                Preferred(icon.rectTransform, 72f, 72f);
            }

            RectTransform status = Node("Status", footer);
            Preferred(status, 200f, 72f).flexibleWidth = 1f;
            AddText(Stretch(Node("DeckWarningText", status), 0f), "Deck chưa đủ 5 tướng khác nhau", SemiBold, 24f, WarningColor, TextAlignmentOptions.Left)
                .gameObject.SetActive(false);

            BuildButton(footer, "EditDeckButton", "Sửa deck", 220f, 72f, 26f);
            BuildButton(footer, "StartButton", "Vào trận", 260f, 72f, 30f);
            return root.gameObject;
        }

        private static GameObject BuildGachaScreen(Scene scene, GameObject heroCard)
        {
            RectTransform root = Page(scene, GachaScreenName);

            RectTransform banner = Place(Node("BannerPanel", root), new Vector2(0.5f, 0.5f), new Vector2(-290f, -10f), new Vector2(1100f, 780f));
            Panel(banner, 40, 20f);
            Label(banner, "BannerTitle", "Triệu hồi tướng", SemiBold, 44f, TextColor, TextAlignmentOptions.Left, 56f);
            Label(banner, "BannerText", "Mỗi lượt nhận 1 tướng thuộc 4 độ hiếm.", Regular, 24f, MutedTextColor, TextAlignmentOptions.Left, 32f);
            Image art = AddImage(Node("BannerArt", banner), IconPlaceholderColor);
            Preferred(art.rectTransform, -1f, 200f).flexibleHeight = 1f;
            Label(banner, "PityText", "Pity 12/25 · lượt 25 chắc chắn ra Legendary", Regular, 26f, TextColor, TextAlignmentOptions.Left, 36f);
            RectTransform pityBar = Node("PityBar", banner);
            Preferred(pityBar, -1f, 32f);
            BuildBar(pityBar, BorderColor, PityFillColor, 12f / 25f, "PityBarText", "12/25");

            RectTransform buttons = Node("PullButtons", banner);
            Horizontal(buttons, 24f, TextAnchor.MiddleCenter);
            Preferred(buttons, -1f, 96f);
            BuildButton(buttons, "Pull1Button", "Quay x1 · 100 Ngọc", 400f, 96f, 30f);
            BuildButton(buttons, "Pull10Button", "Quay x10 · 900 Ngọc", 400f, 96f, 30f);
            Label(banner, "Pull10HintText", "x10 chắc chắn có ít nhất 1 Epic", Regular, 22f, MutedTextColor, TextAlignmentOptions.Center, 30f);

            RectTransform rates = Place(Node("RatePanel", root), new Vector2(0.5f, 0.5f), new Vector2(590f, -10f), new Vector2(620f, 780f));
            Panel(rates, 32, 16f);
            Label(rates, "RateTitle", "Tỉ lệ công khai", SemiBold, 32f, TextColor, TextAlignmentOptions.Left, 48f);
            for (int i = 0; i < RarityNames.Length; i++)
            {
                RectTransform row = Node("RateRow_" + RarityNames[i], rates);
                Horizontal(row, 16f, TextAnchor.MiddleLeft);
                Preferred(row, -1f, 64f);
                Preferred(AddImage(Node("Swatch", row), RarityColors[i]).rectTransform, 36f, 36f);
                Preferred(AddText(Node("NameText", row), RarityNames[i], SemiBold, 28f, TextColor, TextAlignmentOptions.Left).rectTransform, 200f, 64f)
                    .flexibleWidth = 1f;
                Preferred(AddText(Node("RateText", row), SampleRates[i], SemiBold, 28f, TextColor, TextAlignmentOptions.Right).rectTransform, 120f, 64f);
            }
            WrappedLabel(rates, "RateNote", "Quay x10 bảo đảm ít nhất 1 Epic. Pity 25: lượt thứ 25 không ra Legendary thì chắc chắn ra Legendary.",
                22f, MutedTextColor, TextAlignmentOptions.Left);

            RectTransform result = Stretch(Node("ResultPanel", root), 0f);
            float gridWidth = ResultColumns * HeroCardSize.x + (ResultColumns - 1) * 24f;
            RectTransform panel = ModalShell(result, gridWidth + 80f);
            Label(panel, "ResultTitle", "Kết quả triệu hồi", SemiBold, 36f, TextColor, TextAlignmentOptions.Center, 48f);
            RectTransform grid = Node("Grid", panel);
            Grid(grid, 24f, ResultColumns, TextAnchor.UpperCenter, new RectOffset(0, 0, 0, 0));
            Preferred(grid, gridWidth, 2 * HeroCardSize.y + 24f);
            for (int i = 0; i < PullResultCount; i++)
            {
                RectTransform card = Instance(scene, heroCard, grid, HeroCardName + "_" + (i + 1));
                SampleHero(card, i % SampleHeroCount);
                card.Find("CopyBadge").gameObject.SetActive(false);
                if (i == SampleHeroCount - 1) card.Find("NewBadge").gameObject.SetActive(true); // Sample: first Legendary.
            }
            ButtonRow(panel, TextAnchor.MiddleCenter, "ContinueButton", "Tiếp tục");
            result.gameObject.SetActive(false); // B shows it after a pull, with the reveal effect (B-15).
            return root.gameObject;
        }

        private static GameObject BuildCollectionScreen(Scene scene, GameObject heroCard)
        {
            RectTransform root = Page(scene, CollectionScreenName);
            float contentWidth = 1920f - 2f * SideMargin;

            RectTransform deck = Place(Node("DeckBar", root), new Vector2(0.5f, 1f), new Vector2(0f, -(TopBarHeight + 24f)), new Vector2(contentWidth, 272f));
            HorizontalLayoutGroup deckLayout = Horizontal(deck, GridSpacing, TextAnchor.MiddleLeft);
            deckLayout.padding = new RectOffset(24, 24, 0, 0);
            AddImage(IgnoreLayout(Stretch(Node("Frame", deck), 0f)), BorderColor);
            AddImage(IgnoreLayout(Stretch(Node("Background", deck), 2f)), PanelColor);
            RectTransform info = Node("DeckInfo", deck);
            Vertical(info, 6f, TextAnchor.MiddleLeft);
            Preferred(info, 300f, HeroCardSize.y);
            Label(info, "DeckTitle", "Deck", SemiBold, 36f, TextColor, TextAlignmentOptions.Left, 48f);
            Label(info, "DeckStatusText", "5/5 tướng khác nhau", Regular, 24f, TextColor, TextAlignmentOptions.Left, 32f);
            Label(info, "DeckHintText", "Kéo tướng từ Túi vào ô", Regular, 20f, MutedTextColor, TextAlignmentOptions.Left, 28f);
            for (int i = 0; i < DeckSize; i++)
            {
                RectTransform card = Instance(scene, heroCard, deck, "DeckCard_" + (i + 1));
                SampleHero(card, i);
                card.Find("CopyBadge").gameObject.SetActive(false);
            }

            float gridHeight = 1080f - TopBarHeight - 24f - 272f - 24f - 40f;
            float gridWidth = CollectionColumns * HeroCardSize.x + (CollectionColumns - 1) * GridSpacing + 2f * 16f + 2f * 12f;
            RectTransform scroll = Place(Node("HeroGrid", root), Vector2.zero, new Vector2(SideMargin, 40f), new Vector2(gridWidth, gridHeight));
            AddImage(Stretch(Node("Frame", scroll), 0f), BorderColor);
            AddImage(Stretch(Node("Background", scroll), 2f), PanelColor);
            RectTransform viewport = Stretch(Node("Viewport", scroll), 12f);
            // Transparent but raycastable so the wheel and drag scroll over gaps between cards.
            AddImage(viewport, new Color(1f, 1f, 1f, 0f)).raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();
            RectTransform content = Node("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            Grid(content, GridSpacing, CollectionColumns, TextAnchor.UpperLeft, new RectOffset(16, 16, 16, 16));
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            for (int i = 0; i < SampleHeroCount; i++)
            {
                RectTransform card = Instance(scene, heroCard, content, HeroCardName + "_" + (i + 1));
                SampleHero(card, i);
                if (i < DeckSize) card.Find("InDeckMark").gameObject.SetActive(true);
            }
            var scrollRect = scroll.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 40f;
            scrollRect.viewport = viewport;
            scrollRect.content = content;

            RectTransform merge = Place(Node("MergePanel", root), new Vector2(1f, 0f), new Vector2(-SideMargin, 40f),
                new Vector2(contentWidth - gridWidth - GridSpacing, gridHeight));
            Panel(merge, 28, 16f);
            Label(merge, "MergeTitle", "Ghép", SemiBold, 36f, TextColor, TextAlignmentOptions.Left, 48f);
            WrappedLabel(merge, "MergeText", "3 bản thừa cùng độ hiếm → 1 tướng ngẫu nhiên bậc kế. Luôn giữ lại 1 bản mỗi tướng.",
                22f, MutedTextColor, TextAlignmentOptions.Left);
            for (int i = 0; i < MergeRarityNames.Length; i++)
            {
                RectTransform row = Node("MergeRow_" + MergeRarityNames[i], merge);
                Horizontal(row, 12f, TextAnchor.MiddleLeft);
                Preferred(row, -1f, 72f);
                BuildButton(row, "MergeButton", MergeRarityNames[i] + " → " + RarityNames[i + 1], 250f, 72f, 22f);
                Preferred(AddText(Node("SpareText", row), "Thừa: 4", Regular, 22f, TextColor, TextAlignmentOptions.Right).rectTransform, 100f, 72f)
                    .flexibleWidth = 1f;
            }
            return root.gameObject;
        }

        // ---------- Modals (lobby and match) ----------

        private static GameObject BuildSettingsScreen(Scene scene)
        {
            RectTransform root = Page(scene, SettingsScreenName);
            RectTransform panel = ModalShell(root, 820f);
            Label(panel, "TitleText", "Cài đặt", SemiBold, 40f, TextColor, TextAlignmentOptions.Left, 56f);
            BuildSliderRow(panel, "MasterVolumeRow", "Âm lượng tổng");
            BuildSliderRow(panel, "MusicVolumeRow", "Nhạc");
            BuildSliderRow(panel, "SfxVolumeRow", "Hiệu ứng");
            BuildToggle(panel, "CameraShakeToggle", "Rung camera");
            BuildToggle(panel, "ConfirmSkipToggle", "Hỏi khi bỏ qua wave");
            ButtonRow(panel, TextAnchor.MiddleRight, "CloseButton", "Đóng");
            return root.gameObject;
        }

        private static GameObject BuildPauseScreen(Scene scene)
        {
            RectTransform root = Page(scene, PauseScreenName);
            RectTransform panel = ModalShell(root, 560f);
            Label(panel, "TitleText", "Tạm dừng", SemiBold, 44f, TextColor, TextAlignmentOptions.Center, 64f);
            BuildButton(panel, "ResumeButton", "Tiếp tục", 480f, 80f, 30f);
            BuildButton(panel, "SettingsButton", "Cài đặt", 480f, 80f, 30f);
            BuildButton(panel, "LobbyButton", "Về sảnh", 480f, 80f, 30f);
            return root.gameObject;
        }

        private static GameObject BuildResultScreen(Scene scene)
        {
            RectTransform root = Page(scene, ResultScreenName);
            RectTransform panel = ModalShell(root, 820f);
            Label(panel, "ResultTitleText", "Chiến thắng!", SemiBold, 56f, TextColor, TextAlignmentOptions.Center, 72f);
            BuildStars(panel, 88f, TextAnchor.MiddleCenter);
            Label(panel, "RecordText", "Kỷ lục mới: wave 23", SemiBold, 30f, TextColor, TextAlignmentOptions.Center, 44f)
                .gameObject.SetActive(false); // Endless only.
            Label(panel, "DetailText", "Thành còn 17/20 · Màn 1", Regular, 26f, MutedTextColor, TextAlignmentOptions.Center, 36f);
            Label(panel, "GemsText", "+350 Ngọc", SemiBold, 40f, TextColor, TextAlignmentOptions.Center, 56f);
            ButtonRow(panel, TextAnchor.MiddleCenter, "RetryButton", "Chơi lại", "LobbyButton", "Về sảnh");
            return root.gameObject;
        }

        private static GameObject BuildConfirmDialog(Scene scene)
        {
            RectTransform root = Page(scene, ConfirmDialogName);
            RectTransform panel = ModalShell(root, 760f);
            Label(panel, "TitleText", "Bỏ qua thời gian chờ?", SemiBold, 34f, TextColor, TextAlignmentOptions.Left, 50f);
            WrappedLabel(panel, "MessageText", "Wave sau bắt đầu ngay, bạn nhận +50 Vàng.", 26f, TextColor, TextAlignmentOptions.Left);
            BuildToggle(panel, "DontAskToggle", "Không hỏi lại").gameObject.SetActive(false); // Optional per dialog (S10).
            ButtonRow(panel, TextAnchor.MiddleRight, "CancelButton", "Hủy", "ConfirmButton", "Xác nhận");
            return root.gameObject;
        }

        // ---------- Composite helpers ----------

        /// <summary>Full-screen prefab root; UIManager (B) fades and blocks it through the CanvasGroup.</summary>
        private static RectTransform Page(Scene scene, string name)
        {
            RectTransform root = Stretch(Root(scene, name), 0f);
            root.gameObject.AddComponent<CanvasGroup>();
            return root;
        }

        /// <summary>Dim Blocker over the whole screen plus a centered Panel whose height follows its content.</summary>
        private static RectTransform ModalShell(RectTransform root, float width)
        {
            AddImage(Stretch(Node("Blocker", root), 0f), BlockerColor).raycastTarget = true; // Swallows clicks behind the modal.
            RectTransform panel = Place(Node("Panel", root), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width, 0f));
            Panel(panel, 40, 20f);
            panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return panel;
        }

        /// <summary>Framed box with a vertical layout; Frame and Background ignore the layout.</summary>
        private static void Panel(RectTransform panel, int padding, float spacing)
        {
            Vertical(panel, spacing, TextAnchor.UpperLeft).padding = new RectOffset(padding, padding, padding, padding);
            AddImage(IgnoreLayout(Stretch(Node("Frame", panel), 0f)), BorderColor);
            AddImage(IgnoreLayout(Stretch(Node("Background", panel), 3f)), PanelColor);
        }

        private static TextMeshProUGUI Label(RectTransform parent, string name, string text, TMP_FontAsset font, float size, Color color,
            TextAlignmentOptions alignment, float height)
        {
            TextMeshProUGUI label = AddText(Node(name, parent), text, font, size, color, alignment);
            Preferred(label.rectTransform, -1f, height);
            return label;
        }

        /// <summary>Multi-line text; its height comes from TMP's preferred size inside the parent layout.</summary>
        private static TextMeshProUGUI WrappedLabel(RectTransform parent, string name, string text, float size, Color color, TextAlignmentOptions alignment)
        {
            TextMeshProUGUI label = AddText(Node(name, parent), text, Regular, size, color, alignment);
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Overflow;
            return label;
        }

        /// <summary>Buttons laid out in one row; names and labels are given in pairs.</summary>
        private static void ButtonRow(RectTransform parent, TextAnchor alignment, params string[] namesAndLabels)
        {
            RectTransform row = Node("Buttons", parent);
            Horizontal(row, 20f, alignment);
            Preferred(row, -1f, 80f);
            for (int i = 0; i + 1 < namesAndLabels.Length; i += 2)
                BuildButton(row, namesAndLabels[i], namesAndLabels[i + 1], 240f, 72f, 28f);
        }

        /// <summary>Stars/Star_1..3, each a grey slot with an "On" child that B turns on for earned stars.</summary>
        private static void BuildStars(RectTransform parent, float size, TextAnchor alignment)
        {
            RectTransform stars = Node("Stars", parent);
            Horizontal(stars, size * 0.2f, alignment);
            Preferred(stars, -1f, size);
            for (int i = 0; i < StarCount; i++)
            {
                RectTransform star = Node("Star_" + (i + 1), stars);
                Preferred(AddImage(star, StarOffColor).rectTransform, size, size);
                RectTransform on = Stretch(Node("On", star), 0f);
                AddImage(on, StarOnColor);
                on.gameObject.SetActive(i < 2); // Sample: 2 stars.
            }
        }

        private static RectTransform BuildTag(RectTransform card, string name, string text, Color color)
        {
            RectTransform tag = Place(Node(name, card), new Vector2(0f, 1f), new Vector2(6f, -6f), new Vector2(64f, 30f));
            AddImage(tag, color);
            AddText(Stretch(Node("Label", tag), 0f), text, SemiBold, 18f, Color.white, TextAlignmentOptions.Center);
            return tag;
        }

        private static Toggle BuildToggle(RectTransform parent, string name, string label)
        {
            RectTransform row = Node(name, parent);
            Horizontal(row, 16f, TextAnchor.MiddleLeft);
            Preferred(row, -1f, 48f);

            RectTransform box = Node("Box", row);
            Image boxImage = AddImage(box, BorderColor);
            boxImage.raycastTarget = true;
            Preferred(box, 40f, 40f);
            Image check = AddImage(Stretch(Node("Checkmark", box), 8f), SelectedColor);
            Preferred(AddText(Node("Label", row), label, Regular, 26f, TextColor, TextAlignmentOptions.Left).rectTransform, 500f, 48f);

            var toggle = row.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = boxImage;
            toggle.graphic = check;
            toggle.isOn = true;
            return toggle;
        }

        /// <summary>Label, Slider (0–1, built like Unity's default slider) and ValueText ("80%").</summary>
        private static void BuildSliderRow(RectTransform parent, string name, string label)
        {
            RectTransform row = Node(name, parent);
            Horizontal(row, 16f, TextAnchor.MiddleLeft);
            Preferred(row, -1f, 56f);
            Preferred(AddText(Node("Label", row), label, Regular, 26f, TextColor, TextAlignmentOptions.Left).rectTransform, 240f, 56f);

            RectTransform sliderRect = Node("Slider", row);
            Preferred(sliderRect, 300f, 32f).flexibleWidth = 1f;
            RectTransform background = Node("Background", sliderRect);
            background.anchorMin = new Vector2(0f, 0.25f);
            background.anchorMax = new Vector2(1f, 0.75f);
            background.offsetMin = Vector2.zero;
            background.offsetMax = Vector2.zero;
            AddImage(background, BorderColor).raycastTarget = true; // Clicking the track jumps the handle.

            RectTransform fillArea = Node("FillArea", sliderRect);
            fillArea.anchorMin = new Vector2(0f, 0.25f);
            fillArea.anchorMax = new Vector2(1f, 0.75f);
            fillArea.offsetMin = new Vector2(5f, 0f);
            fillArea.offsetMax = new Vector2(-15f, 0f);
            RectTransform fill = Node("Fill", fillArea);
            fill.sizeDelta = new Vector2(10f, 0f);
            AddImage(fill, SelectedColor);

            RectTransform handleArea = Node("HandleSlideArea", sliderRect);
            Stretch(handleArea, 0f);
            handleArea.offsetMin = new Vector2(10f, 0f);
            handleArea.offsetMax = new Vector2(-10f, 0f);
            RectTransform handle = Node("Handle", handleArea);
            handle.sizeDelta = new Vector2(28f, 8f);
            Image handleImage = AddImage(handle, Color.white);
            handleImage.raycastTarget = true;

            var slider = sliderRect.gameObject.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.8f;

            Preferred(AddText(Node("ValueText", row), "80%", Regular, 26f, TextColor, TextAlignmentOptions.Right).rectTransform, 90f, 56f);
        }

        private static void Grid(RectTransform rect, float spacing, int columns, TextAnchor alignment, RectOffset padding)
        {
            var grid = rect.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = HeroCardSize;
            grid.spacing = new Vector2(spacing, spacing);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            grid.childAlignment = alignment;
            grid.padding = padding;
        }

        private static void SampleHero(RectTransform card, int index)
        {
            int rarity = SampleHeroRarities[index];
            SetText(card, "NameText", SampleHeroNames[index]);
            SetText(card, "RarityText", RarityNames[rarity]);
            card.Find("RarityFrame").GetComponent<Image>().color = RarityColors[rarity];
        }

        private static void SetText(Transform root, string path, string text) =>
            root.Find(path).GetComponent<TextMeshProUGUI>().text = text;
    }
}
