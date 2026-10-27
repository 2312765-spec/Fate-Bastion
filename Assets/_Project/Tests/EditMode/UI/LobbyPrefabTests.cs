using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using FateBastion.Editor.UI;

namespace FateBastion.Tests.EditMode.UI
{
    /// <summary>Checks the Lobby_Canvas and MatchMenus_Canvas prefab layouts (C-12) that B attaches view scripts to.</summary>
    public class LobbyPrefabTests
    {
        private const string Lobby = "Lobby/";
        private const string Menus = "Menus/";

        // Hierarchy contract with B (handover C-12). Renaming one of these breaks B's references: change both sides.
        private const string Main = Lobby + LobbyLayoutBuilder.MainScreenName + "/";
        private const string LevelSelect = Lobby + LobbyLayoutBuilder.LevelSelectScreenName + "/";
        private const string Gacha = Lobby + LobbyLayoutBuilder.GachaScreenName + "/";
        private const string Collection = Lobby + LobbyLayoutBuilder.CollectionScreenName + "/";
        private const string LobbySettings = Lobby + LobbyLayoutBuilder.SettingsScreenName + "/Panel/";
        private const string Pause = Menus + LobbyLayoutBuilder.PauseScreenName + "/Panel/";
        private const string Result = Menus + LobbyLayoutBuilder.ResultScreenName + "/Panel/";
        private const string Confirm = Menus + LobbyLayoutBuilder.ConfirmDialogName + "/Panel/";

        private static readonly string[] RequiredButtons =
        {
            Lobby + "TopBar/BackButton",
            Main + "Menu/PlayButton", Main + "Menu/GachaButton", Main + "Menu/CollectionButton", Main + "Menu/SettingsButton", Main + "Menu/QuitButton",
            LevelSelect + "Levels/LevelCard_1", LevelSelect + "Levels/LevelCard_Endless",
            LevelSelect + "Footer/EditDeckButton", LevelSelect + "Footer/StartButton",
            Gacha + "BannerPanel/PullButtons/Pull1Button", Gacha + "BannerPanel/PullButtons/Pull10Button",
            Gacha + "ResultPanel/Panel/Buttons/ContinueButton",
            Collection + "DeckBar/DeckCard_1", Collection + "HeroGrid/Viewport/Content/HeroCard_1",
            Collection + "MergePanel/MergeRow_Common/MergeButton", Collection + "MergePanel/MergeRow_Rare/MergeButton",
            Collection + "MergePanel/MergeRow_Epic/MergeButton",
            LobbySettings + "Buttons/CloseButton",
            Pause + "ResumeButton", Pause + "SettingsButton", Pause + "LobbyButton",
            Result + "Buttons/RetryButton", Result + "Buttons/LobbyButton",
            Confirm + "Buttons/CancelButton", Confirm + "Buttons/ConfirmButton",
        };

        private static readonly string[] RequiredTexts =
        {
            Lobby + "TopBar/TitleText", Lobby + "TopBar/GemPanel/GemText",
            LevelSelect + "Levels/LevelCard_1/Content/TitleText", LevelSelect + "Levels/LevelCard_Endless/Content/RecordText",
            LevelSelect + "Levels/LevelCard_1/Content/Enemies/EnemyRow_1/NameText", LevelSelect + "Levels/LevelCard_1/LockOverlay/LockText",
            LevelSelect + "Footer/Status/DeckWarningText",
            Gacha + "BannerPanel/PityText", Gacha + "BannerPanel/PityBar/PityBarText",
            Gacha + "RatePanel/RateRow_Legendary/RateText",
            Collection + "DeckBar/DeckInfo/DeckStatusText", Collection + "MergePanel/MergeRow_Epic/SpareText",
            Result + "ResultTitleText", Result + "RecordText", Result + "DetailText", Result + "GemsText",
            Confirm + "TitleText", Confirm + "MessageText",
        };

        private static readonly string[] HiddenByDefault =
        {
            Lobby + "TopBar/BackButton",
            Lobby + LobbyLayoutBuilder.LevelSelectScreenName, Lobby + LobbyLayoutBuilder.GachaScreenName,
            Lobby + LobbyLayoutBuilder.CollectionScreenName, Lobby + LobbyLayoutBuilder.SettingsScreenName,
            Lobby + LobbyLayoutBuilder.ConfirmDialogName, Gacha + "ResultPanel",
            Menus + LobbyLayoutBuilder.PauseScreenName, Menus + LobbyLayoutBuilder.SettingsScreenName,
            Menus + LobbyLayoutBuilder.ResultScreenName, Menus + LobbyLayoutBuilder.ConfirmDialogName,
            LevelSelect + "Footer/Status/DeckWarningText", Result + "RecordText", Confirm + "DontAskToggle",
        };

        private static readonly string[] Sliders = { "MasterVolumeRow", "MusicVolumeRow", "SfxVolumeRow" };
        private static readonly string[] Toggles = { "CameraShakeToggle", "ConfirmSkipToggle" };
        private static readonly string[] HeroCardChildren = { "SelectedFrame", "RarityFrame", "Icon", "NameText", "RarityText", "CopyBadge/CopyText", "InDeckMark", "NewBadge" };
        // Graphics that must receive raycasts without being part of a Selectable.
        private static readonly string[] RaycastNodeNames = { "Blocker", "Viewport" };

        private readonly Dictionary<string, GameObject> _roots = new Dictionary<string, GameObject>();

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            LoadRoot(Lobby, LobbyLayoutBuilder.LobbyCanvasName);
            LoadRoot(Menus, LobbyLayoutBuilder.MatchMenusCanvasName);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            foreach (GameObject root in _roots.Values) PrefabUtility.UnloadPrefabContents(root);
            _roots.Clear();
        }

        [TestCase(Lobby, 0)]
        [TestCase(Menus, LobbyLayoutBuilder.MatchMenusSortingOrder)]
        public void Root_Canvas_IsOverlayScaledFrom1920x1080(string root, int sortingOrder)
        {
            GameObject go = RequireRoot(root);
            var canvas = go.GetComponent<Canvas>();
            var scaler = go.GetComponent<CanvasScaler>();
            Assert.IsNotNull(canvas, $"{go.name} phải có Canvas.");
            Assert.IsNotNull(scaler, $"{go.name} phải có CanvasScaler.");
            Assert.IsNotNull(go.GetComponent<GraphicRaycaster>(), $"{go.name} phải có GraphicRaycaster để bấm nút.");
            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, canvas.renderMode);
            Assert.AreEqual(sortingOrder, canvas.sortingOrder);
            Assert.AreEqual(CanvasScaler.ScaleMode.ScaleWithScreenSize, scaler.uiScaleMode);
            Assert.AreEqual(new Vector2(1920f, 1080f), scaler.referenceResolution);
        }

        [Test]
        public void MatchMenus_DrawsAboveHud()
        {
            Assert.Greater(LobbyLayoutBuilder.MatchMenusSortingOrder, 0, "Menu trong trận phải vẽ trên HUD_Match (sorting order 0).");
        }

        [TestCaseSource(nameof(RequiredButtons))]
        public void RequiredButton_Exists(string path)
        {
            Assert.IsNotNull(Find<Button>(path).targetGraphic, $"{path}: Button thiếu Target Graphic.");
        }

        [TestCaseSource(nameof(RequiredTexts))]
        public void RequiredText_Exists(string path)
        {
            Find<TextMeshProUGUI>(path);
        }

        [TestCaseSource(nameof(HiddenByDefault))]
        public void OptionalNode_IsInactiveByDefault(string path)
        {
            Assert.IsFalse(FindTransform(path).gameObject.activeSelf, $"{path} chỉ hiện khi cần, mặc định phải tắt.");
        }

        [Test]
        public void Lobby_OpensOnMainScreen()
        {
            Assert.IsTrue(FindTransform(Lobby + LobbyLayoutBuilder.MainScreenName).gameObject.activeSelf, "Sảnh phải mở ở Screen_Main.");
        }

        [TestCase(LobbyLayoutBuilder.MainScreenName)]
        [TestCase(LobbyLayoutBuilder.LevelSelectScreenName)]
        [TestCase(LobbyLayoutBuilder.GachaScreenName)]
        [TestCase(LobbyLayoutBuilder.CollectionScreenName)]
        [TestCase(LobbyLayoutBuilder.SettingsScreenName)]
        [TestCase(LobbyLayoutBuilder.ConfirmDialogName)]
        public void LobbyScreen_IsPrefabInstanceWithCanvasGroup(string screen)
        {
            AssertScreenInstance(Lobby, screen);
        }

        [TestCase(LobbyLayoutBuilder.PauseScreenName)]
        [TestCase(LobbyLayoutBuilder.SettingsScreenName)]
        [TestCase(LobbyLayoutBuilder.ResultScreenName)]
        [TestCase(LobbyLayoutBuilder.ConfirmDialogName)]
        public void MatchMenu_IsPrefabInstanceWithCanvasGroup(string screen)
        {
            AssertScreenInstance(Menus, screen);
        }

        [Test]
        public void LevelSelect_HasThreeCampaignCardsAndEndlessCard()
        {
            Transform levels = FindTransform(LevelSelect + "Levels");
            Assert.AreEqual(LobbyLayoutBuilder.CampaignLevelCount + 1, levels.childCount);
            for (int i = 0; i < LobbyLayoutBuilder.CampaignLevelCount; i++)
            {
                Transform card = levels.GetChild(i);
                Assert.AreEqual("LevelCard_" + (i + 1), card.name);
                AssertInstanceOf(card, LobbyLayoutBuilder.LevelCardName);
                Assert.AreEqual(LobbyLayoutBuilder.StarCount, card.Find("Content/Stars").childCount);
                Assert.AreEqual(LobbyLayoutBuilder.EnemyRowCount, card.Find("Content/Enemies").childCount);
            }

            Transform endless = levels.GetChild(LobbyLayoutBuilder.CampaignLevelCount);
            Assert.AreEqual("LevelCard_Endless", endless.name);
            Assert.IsFalse(endless.Find("Content/Stars").gameObject.activeSelf, "Vô tận không có sao, chỉ có kỷ lục.");
            Assert.IsTrue(endless.Find("Content/RecordText").gameObject.activeSelf);
        }

        [Test]
        public void LevelSelect_DeckPreviewHasFiveIcons()
        {
            Assert.AreEqual(LobbyLayoutBuilder.DeckSize, FindTransform(LevelSelect + "Footer/DeckPreview").childCount);
        }

        [Test]
        public void Gacha_HasOneRateRowPerRarity()
        {
            foreach (string rarity in LobbyLayoutBuilder.RarityNames)
                Find<TextMeshProUGUI>(Gacha + "RatePanel/RateRow_" + rarity + "/RateText");
        }

        [Test]
        public void Gacha_ResultGridHasTenHeroCards()
        {
            Transform grid = FindTransform(Gacha + "ResultPanel/Panel/Grid");
            Assert.IsNotNull(grid.GetComponent<GridLayoutGroup>());
            Assert.AreEqual(LobbyLayoutBuilder.PullResultCount, grid.childCount, "Quay x10 cần đúng 10 ô kết quả.");
            for (int i = 0; i < grid.childCount; i++) AssertInstanceOf(grid.GetChild(i), LobbyLayoutBuilder.HeroCardName);
        }

        [Test]
        public void Collection_DeckBarHasFiveHeroCards()
        {
            for (int i = 0; i < LobbyLayoutBuilder.DeckSize; i++)
                AssertInstanceOf(FindTransform(Collection + "DeckBar/DeckCard_" + (i + 1)), LobbyLayoutBuilder.HeroCardName);
        }

        [Test]
        public void Collection_HeroGridIsVerticalScrollWithMaskedGrid()
        {
            var scroll = Find<ScrollRect>(Collection + "HeroGrid");
            Assert.IsFalse(scroll.horizontal);
            Assert.IsTrue(scroll.vertical);
            Assert.AreSame(FindTransform(Collection + "HeroGrid/Viewport"), scroll.viewport);
            Assert.AreSame(FindTransform(Collection + "HeroGrid/Viewport/Content"), scroll.content);
            Assert.IsNotNull(scroll.viewport.GetComponent<RectMask2D>(), "Viewport phải cắt thẻ tràn ra ngoài.");
            Assert.IsNotNull(scroll.content.GetComponent<GridLayoutGroup>());
            Assert.IsNotNull(scroll.content.GetComponent<ContentSizeFitter>(), "Content phải cao theo số thẻ để cuộn được.");
        }

        [Test]
        public void Collection_MergeRowsExcludeLegendary()
        {
            Transform panel = FindTransform(Collection + "MergePanel");
            Assert.IsNull(panel.Find("MergeRow_Legendary"), "Không ghép Legendary (S8).");
            foreach (string rarity in LobbyLayoutBuilder.MergeRarityNames) Assert.IsNotNull(panel.Find("MergeRow_" + rarity));
        }

        [Test]
        public void HeroCard_HasRequiredChildrenAndCanvasGroup()
        {
            Transform card = FindTransform(Collection + "HeroGrid/Viewport/Content/HeroCard_1");
            Assert.IsNotNull(card.GetComponent<CanvasGroup>(), "Thẻ tướng cần CanvasGroup để tắt blocksRaycasts khi kéo.");
            foreach (string child in HeroCardChildren) Assert.IsNotNull(card.Find(child), $"HeroCard thiếu {child}.");
            Assert.IsFalse(card.Find("SelectedFrame").gameObject.activeSelf);
            Assert.IsFalse(card.Find("NewBadge").gameObject.activeSelf);
        }

        [TestCaseSource(nameof(Sliders))]
        public void Settings_VolumeSlider_RangesZeroToOne(string row)
        {
            var slider = Find<Slider>(LobbySettings + row + "/Slider");
            Assert.AreEqual(0f, slider.minValue);
            Assert.AreEqual(1f, slider.maxValue);
            Assert.IsNotNull(slider.fillRect);
            Assert.IsNotNull(slider.handleRect);
            Find<TextMeshProUGUI>(LobbySettings + row + "/ValueText");
        }

        [TestCaseSource(nameof(Toggles))]
        public void Settings_Toggle_HasCheckmark(string name)
        {
            Assert.IsNotNull(Find<Toggle>(LobbySettings + name).graphic);
        }

        [TestCase(Lobby)]
        [TestCase(Menus)]
        public void AllTexts_UseBeVietnamProFontAssets(string root)
        {
            GameObject go = RequireRoot(root);
            var regular = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiBuildKit.RegularFontPath);
            var semiBold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiBuildKit.SemiBoldFontPath);
            TMP_Text[] texts = go.GetComponentsInChildren<TMP_Text>(true);
            Assert.IsNotEmpty(texts);
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_FontAsset font = texts[i].font;
                Assert.IsTrue(font != null && (font == regular || font == semiBold),
                    $"{PathOf(go, texts[i].transform)} dùng font {(font != null ? font.name : "null")}, phải là Be Vietnam Pro (C-10).");
            }
        }

        [TestCase(Lobby)]
        [TestCase(Menus)]
        public void RaycastTargets_AreOnlyControlsBlockersAndViewports(string root)
        {
            GameObject go = RequireRoot(root);
            Graphic[] graphics = go.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                if (!graphics[i].raycastTarget) continue;
                bool allowed = graphics[i].GetComponentInParent<Selectable>(true) != null
                    || System.Array.IndexOf(RaycastNodeNames, graphics[i].name) >= 0;
                Assert.IsTrue(allowed, $"{PathOf(go, graphics[i].transform)} không phải nút/thanh trượt nhưng bật Raycast Target.");
            }
        }

        // ---------- Helpers ----------

        private void LoadRoot(string key, string prefabName)
        {
            string path = UiBuildKit.GetPrefabPath(LobbyLayoutBuilder.PrefabFolder, prefabName);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) return;
            _roots[key] = PrefabUtility.LoadPrefabContents(path);
        }

        private GameObject RequireRoot(string key)
        {
            if (!_roots.TryGetValue(key, out GameObject root))
                Assert.Fail($"Chưa có prefab {(key == Lobby ? LobbyLayoutBuilder.LobbyCanvasName : LobbyLayoutBuilder.MatchMenusCanvasName)} – chạy Tools → Create Lobby Layout (C-12).");
            return root;
        }

        private Transform FindTransform(string path)
        {
            int slash = path.IndexOf('/');
            GameObject root = RequireRoot(path.Substring(0, slash + 1));
            string relative = path.Substring(slash + 1).TrimEnd('/');
            Transform child = root.transform.Find(relative);
            Assert.IsNotNull(child, $"{root.name} thiếu object {relative}.");
            return child;
        }

        private T Find<T>(string path) where T : Component
        {
            var component = FindTransform(path).GetComponent<T>();
            Assert.IsNotNull(component, $"{path} thiếu component {typeof(T).Name}.");
            return component;
        }

        private void AssertScreenInstance(string root, string screen)
        {
            Transform t = FindTransform(root + screen);
            AssertInstanceOf(t, screen);
            Assert.IsNotNull(t.GetComponent<CanvasGroup>(), $"{screen} cần CanvasGroup để UIManager ẩn/hiện.");
        }

        private static void AssertInstanceOf(Transform t, string prefabName)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UiBuildKit.GetPrefabPath(LobbyLayoutBuilder.PrefabFolder, prefabName));
            Assert.AreSame(prefab, PrefabUtility.GetCorrespondingObjectFromOriginalSource(t.gameObject),
                $"{t.name} phải là instance của {prefabName} để sửa một chỗ là đủ mọi nơi.");
        }

        private static string PathOf(GameObject root, Transform t)
        {
            string path = t.name;
            for (Transform p = t.parent; p != null && p != root.transform; p = p.parent) path = p.name + "/" + path;
            return path;
        }
    }
}
