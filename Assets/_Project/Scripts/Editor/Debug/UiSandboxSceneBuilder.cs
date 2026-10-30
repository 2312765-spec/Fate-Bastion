using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using FateBastion.Player;
using FateBastion.UI;
using FateBastion.UI.Juice;
using FateBastion.Editor.Import;
using FateBastion.Game;
using FateBastion.Heroes;
using FateBastion.Meta;
using FateBastion.UI.DebugTools;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace FateBastion.Editor.DebugTools
{
    /// <summary>
    /// Tools > Sandbox > Build B_UI Scene: builds Scenes/Sandbox/B_UI.unity, the working lobby (B-15 screens over the
    /// real save, UIManager and ConfirmDialog of B-13, F1 debug panel of B-16) with the "Fantasy Wooden GUI" skin and the B motion components. Uses an instance of C's
    /// Lobby_Canvas prefab: every skin change is an override on the scene instance only, the prefab asset and C's
    /// layout builder are never modified (C can port the look later).
    /// </summary>
    public static class UiSandboxSceneBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Sandbox/B_UI.unity";

        private const string LobbyCanvasPath = "Assets/_Project/Prefabs/UI/Lobby_Canvas.prefab";
        private const string InputActionsPath = "Assets/_Project/Input/FateBastion.inputactions";
        private const string WoodFolder = "Assets/Fantasy Wooden GUI  Free/normal_ui_set A/";

        // The Editor asmdef does not reference the Input System package, so the UI module is added by type name.
        private const string InputModuleType = "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem";

        // Technical UI timing (not balance): page fade and slide.
        private const float FadeSeconds = 0.22f;

        // ---------- Palette: warm fortress (dark wood, parchment, gold) ----------
        private static readonly Color Backdrop = new Color32(0x1A, 0x12, 0x0D, 0xFF);
        private static readonly Color FireGlow = new Color32(0xB0, 0x52, 0x1A, 0xFF);
        private static readonly Color Ink = new Color32(0x3E, 0x27, 0x16, 0xFF);
        private static readonly Color InkMuted = new Color32(0x7B, 0x5A, 0x3A, 0xFF);
        private static readonly Color Cream = new Color32(0xF8, 0xE9, 0xC9, 0xFF);
        private static readonly Color Gold = new Color32(0xF6, 0xC4, 0x5A, 0xFF);
        private static readonly Color GoldLight = new Color32(0xFF, 0xE7, 0x9A, 0xFF);
        private static readonly Color GoldDeep = new Color32(0xD9, 0x86, 0x22, 0xFF);
        private static readonly Color WoodDark = new Color32(0x2C, 0x1F, 0x16, 0xF2);
        private static readonly Color WoodMid = new Color32(0x5A, 0x40, 0x2A, 0xFF);
        private static readonly Color Amber = new Color32(0xE8, 0x9B, 0x2E, 0xFF);
        private static readonly Color Placeholder = new Color32(0xCF, 0xB4, 0x86, 0xFF);
        private static readonly Color Blocker = new Color(0.06f, 0.03f, 0.02f, 0.72f);
        private static readonly Color OutlineBrown = new Color32(0x2A, 0x16, 0x08, 0xFF);

        // C's layout colours (UiBuildKit) that the skin replaces.
        private static readonly Color LayoutText = new Color32(0x22, 0x22, 0x22, 0xFF);
        private static readonly Color LayoutMuted = new Color32(0x77, 0x77, 0x77, 0xFF);
        private static readonly Color LayoutPlaceholder = new Color32(0xD8, 0xD8, 0xD2, 0xFF);
        private static readonly Color LayoutSelected = new Color32(0x2F, 0x7B, 0xE0, 0xFF);

        private static readonly string[] PrimaryButtons = { "PlayButton", "StartButton", "Pull1Button", "Pull10Button", "ConfirmButton", "ContinueButton" };
        private static readonly string[] Pages = { "Screen_Main", "Screen_LevelSelect", "Screen_Gacha", "Screen_Collection" };
        private static readonly string[] PaddedPanels =
        {
            "Screen_Settings/Panel", "ConfirmDialog/Panel", "Screen_Gacha/ResultPanel/Panel",
            "Screen_Gacha/BannerPanel", "Screen_Gacha/RatePanel", "Screen_Collection/MergePanel"
        };
        private static readonly string[] MergeRows = { "MergeRow_Common", "MergeRow_Rare", "MergeRow_Epic" };

        private sealed class Skin
        {
            public Sprite BoardLarge, BoardSmall, Parchment, Button, ButtonPressed, ButtonBig, ButtonBigPressed;
            public Sprite Plate, Divider, Exclamation;
        }

        [MenuItem("Tools/Sandbox/Build B_UI Scene (Lobby B-15)")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            var lobbyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LobbyCanvasPath);
            if (lobbyPrefab == null)
            {
                Debug.LogError($"[B_UI] '{LobbyCanvasPath}' not found. Run Tools > Create Lobby Layout first.");
                return;
            }

            Skin skin = LoadSkin();
            if (skin == null)
            {
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildSceneBasics();

            var canvas = (GameObject)PrefabUtility.InstantiatePrefab(lobbyPrefab, scene);
            Transform root = canvas.transform;
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)root);

            ApplySkin(root, skin);
            UIManager manager = WireNavigation(root, skin);
            AddLobby(root);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[B_UI] Built {ScenePath}. Press Play, click the lobby buttons, Esc closes the top screen.");
        }

        // ---------- Scene ----------

        private static void BuildSceneBasics()
        {
            var cameraGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Backdrop;

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem));
            Type module = Type.GetType(InputModuleType);
            if (module != null)
            {
                eventSystem.AddComponent(module);
            }
            else
            {
                Debug.LogError("[B_UI] InputSystemUIInputModule not found; buttons will not receive clicks.");
            }

            // Esc in the UI action map raises InputRouter.Cancel, which UIManager uses to close the top screen.
            var router = new GameObject("InputRouter").AddComponent<SummonerInputRouter>();
            var routerData = new SerializedObject(router);
            routerData.FindProperty("_actions").objectReferenceValue = AssetDatabase.LoadMainAssetAtPath(InputActionsPath);
            routerData.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------- Skin ----------

        private static Skin LoadSkin()
        {
            // 9-slice borders in source pixels (left, bottom, right, top), measured on the Black Hammer sprites.
            var skin = new Skin
            {
                BoardLarge = Sliced("UI board Large Set.png", new Vector4(110, 110, 110, 110)),
                BoardSmall = Sliced("UI board Small Set.png", new Vector4(90, 90, 90, 90)),
                Parchment = Sliced("UI board Small  parchment.png", new Vector4(60, 60, 60, 60)),
                Button = Sliced("TextBTN_Medium.png", new Vector4(45, 36, 45, 30)),
                ButtonPressed = Sliced("TextBTN_Medium_Pressed.png", new Vector4(45, 36, 45, 30)),
                ButtonBig = Sliced("TextBTN_Big.png", new Vector4(48, 36, 48, 30)),
                ButtonBigPressed = Sliced("TextBTN_Big_Pressed.png", new Vector4(48, 36, 48, 30)),
                Plate = Sliced("IRONY TITLE  empty.png", new Vector4(75, 25, 75, 25)),
                Divider = Sliced("Division line.png", new Vector4(60, 0, 60, 0)),
                Exclamation = Sliced("Exclamation_Yellow.png", Vector4.zero),
            };

            if (skin.BoardLarge == null || skin.Button == null)
            {
                Debug.LogError($"[B_UI] Fantasy Wooden GUI sprites not found in '{WoodFolder}'. Import the package first.");
                return null;
            }

            return skin;
        }

        /// <summary>Makes sure the texture imports as a full-rect sprite with the given 9-slice border.</summary>
        private static Sprite Sliced(string file, Vector4 border)
        {
            string path = WoodFolder + file;
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[B_UI] Sprite '{path}' not found.");
                return null;
            }

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            bool changed = importer.textureType != TextureImporterType.Sprite || importer.spriteBorder != border
                || importer.mipmapEnabled || settings.spriteMeshType != SpriteMeshType.FullRect;
            if (changed)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.spriteBorder = border;
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect; // Tight meshes break 9-slicing.
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void ApplySkin(Transform root, Skin skin)
        {
            // Text colours first (C's neutral greys → ink on parchment); later steps override texts on wood and dark.
            foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (Near(text.color, LayoutText)) text.color = Ink;
                else if (Near(text.color, LayoutMuted)) text.color = InkMuted;
            }

            foreach (Image image in root.GetComponentsInChildren<Image>(true))
            {
                if (Near(image.color, LayoutPlaceholder)) image.color = Placeholder;
                else if (Near(image.color, LayoutSelected)) image.color = Gold;
                if (image.name == "Blocker") image.color = Blocker;
            }

            SkinBackdrop(root);
            SkinTopBar(root, skin);
            SkinMainTitle(root);

            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                if (button.transform.Find("Label") != null && button.transform.Find("Frame") != null)
                {
                    SkinTextButton(button, skin);
                }

                if (button.GetComponent<UiButtonJuice>() == null)
                {
                    button.gameObject.AddComponent<UiButtonJuice>();
                }
            }

            foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
            {
                SkinPanel(node, skin);
            }

            // Progress bars (Frame + Track + Fill): dark wooden groove with an amber fill.
            foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
            {
                if (node.Find("Track") != null && node.Find("Fill") != null && node.Find("Frame") != null)
                {
                    SetColor(node, "Frame", OutlineBrown);
                    SetColor(node, "Track", WoodMid);
                    SetColor(node, "Fill", Amber);
                    foreach (TMP_Text label in node.GetComponentsInChildren<TMP_Text>(true))
                    {
                        label.color = Cream;
                        label.fontStyle = FontStyles.Bold;
                    }
                }
            }

            foreach (Toggle toggle in root.GetComponentsInChildren<Toggle>(true))
            {
                if (toggle.targetGraphic != null) toggle.targetGraphic.color = WoodMid;
                if (toggle.graphic != null) toggle.graphic.color = Gold;
            }

            foreach (Slider slider in root.GetComponentsInChildren<Slider>(true))
            {
                SetColor(slider.transform, "Background", WoodMid);
                if (slider.fillRect != null) slider.fillRect.GetComponent<Image>().color = Amber;
                if (slider.handleRect != null)
                {
                    slider.handleRect.GetComponent<Image>().color = GoldLight;
                    slider.handleRect.sizeDelta = new Vector2(22f, 18f);
                }
            }

            foreach (string path in PaddedPanels)
            {
                Transform panel = root.Find(path);
                if (panel != null && panel.TryGetComponent(out VerticalLayoutGroup layout))
                {
                    // The stone frame plus torn parchment edge is thicker than C's flat border.
                    layout.padding = new RectOffset(72, 72, 64, 60);
                }
            }

            foreach (string path in new[] { "Screen_LevelSelect/Footer", "Screen_Collection/DeckBar" })
            {
                Transform bar = root.Find(path);
                if (bar != null && bar.TryGetComponent(out HorizontalLayoutGroup layout))
                {
                    layout.padding = new RectOffset(64, 64, 0, 0);
                }
            }

            Transform viewport = root.Find("Screen_Collection/HeroGrid/Viewport");
            if (viewport != null)
            {
                var rect = (RectTransform)viewport;
                rect.offsetMin = new Vector2(34f, 46f);
                rect.offsetMax = new Vector2(-34f, -46f);

                // C sized the grid for a 12-unit inset; win back the 44 units the thicker frame takes so all columns fit.
                Transform content = viewport.Find("Content");
                if (content != null && content.TryGetComponent(out GridLayoutGroup grid))
                {
                    grid.padding = new RectOffset(6, 6, grid.padding.top, grid.padding.bottom);
                    grid.spacing = new Vector2(Mathf.Max(8f, grid.spacing.x - 5f), grid.spacing.y);
                }
            }

            // Hero cards: parchment body inside the rarity-coloured rim.
            foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
            {
                if (node.Find("RarityFrame") != null && node.Find("Background") is Transform background)
                {
                    var image = background.GetComponent<Image>();
                    image.sprite = skin.Parchment;
                    image.type = Image.Type.Sliced;
                    image.color = Color.white;
                    image.pixelsPerUnitMultiplier = 2.5f;
                }
            }

            SkinTitles(root);
        }

        private static void SkinBackdrop(Transform root)
        {
            Transform background = root.Find("Background");
            if (background == null)
            {
                return;
            }

            background.GetComponent<Image>().color = Backdrop;
            int index = background.GetSiblingIndex() + 1;

            // Warm firelight from below the menu, slowly breathing and drifting.
            RectTransform glow = NewChild(root, "FireGlow", index++);
            glow.anchorMin = glow.anchorMax = new Vector2(0.5f, 0.35f);
            glow.sizeDelta = new Vector2(2200f, 1500f);
            var glowImage = glow.gameObject.AddComponent<Image>();
            glowImage.color = Transparent(FireGlow); // UiPulse brings the alpha up at runtime, when the glow sprite exists.
            glowImage.raycastTarget = false;
            glow.gameObject.AddComponent<UiProceduralSpriteBinder>();
            SetFields(glow.gameObject.AddComponent<UiPulse>(), ("_minAlpha", 0.28f), ("_maxAlpha", 0.48f), ("_halfPeriod", 3.2f));
            glow.gameObject.AddComponent<UiAmbientDrift>();

            RectTransform embers = NewChild(root, "Embers", index++);
            Stretch(embers);
            embers.gameObject.AddComponent<UiEmbers>();

            RectTransform vignette = NewChild(root, "Vignette", index);
            Stretch(vignette);
            // Clear until UiVignette assigns the generated texture at runtime (an empty RawImage would be solid white).
            vignette.gameObject.AddComponent<RawImage>().color = Color.clear;
            vignette.gameObject.AddComponent<UiVignette>();
        }

        private static void SkinTopBar(Transform root, Skin skin)
        {
            Transform bar = root.Find("TopBar");
            if (bar == null)
            {
                return;
            }

            SetColor(bar, "Background", WoodDark);

            RectTransform divider = NewChild(bar, "Divider", 1);
            divider.anchorMin = new Vector2(0f, 0f);
            divider.anchorMax = new Vector2(1f, 0f);
            divider.pivot = new Vector2(0.5f, 0.5f);
            divider.sizeDelta = new Vector2(0f, 14f);
            divider.anchoredPosition = Vector2.zero;
            var dividerImage = divider.gameObject.AddComponent<Image>();
            dividerImage.sprite = skin.Divider;
            dividerImage.type = Image.Type.Sliced;
            dividerImage.raycastTarget = false;

            Transform title = bar.Find("TitleText");
            if (title != null)
            {
                RectTransform plate = NewChild(bar, "TitlePlate", title.GetSiblingIndex());
                plate.anchorMin = plate.anchorMax = new Vector2(0.5f, 0.5f);
                plate.sizeDelta = new Vector2(480f, 92f);
                var plateImage = plate.gameObject.AddComponent<Image>();
                plateImage.sprite = skin.Plate;
                plateImage.type = Image.Type.Sliced;
                plateImage.raycastTarget = false;
                ((RectTransform)title).sizeDelta = new Vector2(440f, 64f);
            }

            Transform gems = bar.Find("GemPanel");
            if (gems != null)
            {
                SetColor(gems, "Frame", GoldDeep);
                SetColor(gems, "Background", WoodDark);
                SetTextColor(gems, "GemText", Cream);
                Transform icon = gems.Find("GemIcon");
                if (icon != null)
                {
                    // A rotated square reads as a gem.
                    icon.localRotation = Quaternion.Euler(0f, 0f, 45f);
                    icon.localScale = Vector3.one * 0.8f;
                }
            }
        }

        private static void SkinMainTitle(Transform root)
        {
            Transform title = root.Find("Screen_Main/TitleText");
            if (title != null && title.TryGetComponent(out TMP_Text text))
            {
                text.color = Color.white;
                text.enableVertexGradient = true;
                text.colorGradient = new VertexGradient(GoldLight, GoldLight, GoldDeep, GoldDeep);
                text.fontSize = 92f;
                text.characterSpacing = 2f;
                title.gameObject.AddComponent<UiTitleIntro>();
            }

            Transform subtitle = root.Find("Screen_Main/SubtitleText");
            if (subtitle != null && subtitle.TryGetComponent(out TMP_Text sub))
            {
                sub.color = new Color(Cream.r, Cream.g, Cream.b, 0.85f);
                sub.characterSpacing = 18f;
                sub.text = "FATE BASTION";
                SetFields(subtitle.gameObject.AddComponent<UiTitleIntro>(), ("_dropDistance", 60f), ("_delay", 0.3f), ("_floatAmplitude", 0f));
            }

            Transform menu = root.Find("Screen_Main/Menu");
            if (menu != null)
            {
                SetFields(menu.gameObject.AddComponent<UiStaggerIn>(), ("_initialDelay", 0.35f), ("_step", 0.07f));
            }

            Transform levels = root.Find("Screen_LevelSelect/Levels");
            if (levels != null)
            {
                SetFields(levels.gameObject.AddComponent<UiStaggerIn>(), ("_initialDelay", 0.12f), ("_step", 0.08f), ("_fromScale", 0.8f));
            }
            // The gacha result grid is animated by GachaResultScreen (card flip), not by UiStaggerIn.
        }

        private static void SkinTextButton(Button button, Skin skin)
        {
            Transform t = button.transform;
            float height = ((RectTransform)t).rect.height;
            bool big = height >= 88f;
            var frame = t.Find("Frame").GetComponent<Image>();
            frame.sprite = big ? skin.ButtonBig : skin.Button;
            frame.type = Image.Type.Sliced;
            frame.color = Color.white;
            // Shrink the corners on short buttons so the top and bottom borders never overlap.
            frame.pixelsPerUnitMultiplier = Mathf.Max(1f, 66f / Mathf.Max(1f, height * 0.85f));

            Transform background = t.Find("Background");
            if (background != null)
            {
                background.gameObject.SetActive(false);
            }

            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState
            {
                highlightedSprite = frame.sprite,
                selectedSprite = frame.sprite,
                pressedSprite = big ? skin.ButtonBigPressed : skin.ButtonPressed,
                disabledSprite = big ? skin.ButtonBigPressed : skin.ButtonPressed,
            };

            var label = t.Find("Label").GetComponent<TMP_Text>();
            bool primary = Array.IndexOf(PrimaryButtons, t.name) >= 0;
            label.color = primary ? Gold : Cream;
            label.fontStyle = FontStyles.Bold;
            // Pressed sprite sits lower; nudge the label a touch up so it looks centred on the wooden face.
            ((RectTransform)label.transform).offsetMin = new Vector2(10f, 10f);
            ((RectTransform)label.transform).offsetMax = new Vector2(-10f, -4f);

            if (t.name == "PlayButton")
            {
                RectTransform glow = NewChild(t, "Glow", 0);
                Stretch(glow);
                glow.offsetMin = new Vector2(-90f, -60f);
                glow.offsetMax = new Vector2(90f, 60f);
                var glowImage = glow.gameObject.AddComponent<Image>();
                glowImage.color = Transparent(Gold);
                glowImage.raycastTarget = false;
                glow.gameObject.AddComponent<UiProceduralSpriteBinder>();
                glow.gameObject.AddComponent<UiPulse>();
            }
        }

        /// <summary>C's framed boxes (Frame + Background children) become parchment boards in stone frames.</summary>
        private static void SkinPanel(Transform node, Skin skin)
        {
            Transform frameNode = node.Find("Frame");
            Transform backgroundNode = node.Find("Background");
            if (frameNode == null || backgroundNode == null || node.Find("Label") != null || node.name == "GemPanel")
            {
                return;
            }

            var frame = frameNode.GetComponent<Image>();
            if (frame == null)
            {
                return;
            }

            Rect rect = ((RectTransform)node).rect;
            float width = rect.width;
            // Modal panels get their height from a ContentSizeFitter that has not run in an inactive page yet.
            float height = rect.height < 10f ? width * 0.6f : rect.height;
            float minSide = Mathf.Max(1f, Mathf.Min(width, height));
            bool small = height < 300f;
            frame.sprite = small ? skin.BoardSmall : skin.BoardLarge;
            float border = small ? 90f : 110f;
            frame.type = Image.Type.Sliced;
            frame.color = Color.white;
            // Thinner frames on small boxes (level cards, footer); never let opposite borders overlap.
            frame.pixelsPerUnitMultiplier = Mathf.Max(1.15f, 2f * border / (minSide * 0.7f));
            backgroundNode.gameObject.SetActive(false);

            if (node.name.StartsWith("LevelCard"))
            {
                // Narrow cards: slimmer stone, and keep the text off the frame and torn edge.
                frame.pixelsPerUnitMultiplier = 1.9f;
                if (node.Find("Content") is RectTransform content)
                {
                    content.offsetMin = new Vector2(44f, 40f);
                    content.offsetMax = new Vector2(-44f, -44f);
                }

                if (node.Find("LockOverlay") is RectTransform locked)
                {
                    locked.offsetMin = new Vector2(16f, 16f);
                    locked.offsetMax = new Vector2(-16f, -16f);
                }
            }
        }

        private static void SkinTitles(Transform root)
        {
            // Headings on parchment get the semi-bold weight and a deeper ink.
            string[] headings =
            {
                "Screen_Settings/Panel/TitleText", "ConfirmDialog/Panel/TitleText", "Screen_Gacha/ResultPanel/Panel/ResultTitle",
                "Screen_Gacha/BannerPanel/BannerTitle", "Screen_Gacha/RatePanel/RateTitle", "Screen_Collection/MergePanel/MergeTitle",
                "Screen_Collection/DeckBar/DeckInfo/DeckTitle", "Screen_LevelSelect/Footer/DeckLabel"
            };

            foreach (string path in headings)
            {
                Transform t = root.Find(path);
                if (t != null && t.TryGetComponent(out TMP_Text text))
                {
                    text.color = OutlineBrown;
                    text.fontStyle = FontStyles.Bold;
                }
            }

            Transform barTitle = root.Find("TopBar/TitleText");
            if (barTitle != null && barTitle.TryGetComponent(out TMP_Text bar))
            {
                bar.color = Cream;
                bar.fontStyle = FontStyles.Bold;
            }
        }

        // ---------- Navigation ----------

        private static UIManager WireNavigation(Transform root, Skin skin)
        {
            UIScreen main = AddScreen<MainMenuScreen>(root, "Screen_Main", false, ScreenTransition.Slide, null);
            AddScreen<LevelSelectScreen>(root, "Screen_LevelSelect", false, ScreenTransition.Slide, null);
            AddScreen<GachaScreen>(root, "Screen_Gacha", false, ScreenTransition.Slide, null);
            AddScreen<CollectionScreen>(root, "Screen_Collection", false, ScreenTransition.Slide, null);
            AddScreen<SettingsScreen>(root, "Screen_Settings", true, ScreenTransition.Pop, "Screen_Settings/Panel");
            AddScreen<GachaResultScreen>(root, "Screen_Gacha/ResultPanel", true, ScreenTransition.Pop, "Screen_Gacha/ResultPanel/Panel");

            ConfirmDialog confirm = Get<Transform>(root, "ConfirmDialog").gameObject.AddComponent<ConfirmDialog>();
            confirm.Configure(true, true, FadeSeconds);
            confirm.ConfigureTransition(ScreenTransition.Pop, Get<RectTransform>(root, "ConfirmDialog/Panel"), 0.36f);
            confirm.ConfigureReferences(null,
                Get<TMP_Text>(root, "ConfirmDialog/Panel/TitleText"),
                Get<TMP_Text>(root, "ConfirmDialog/Panel/MessageText"),
                Get<Button>(root, "ConfirmDialog/Panel/Buttons/ConfirmButton"),
                Get<Button>(root, "ConfirmDialog/Panel/Buttons/CancelButton"),
                Get<Toggle>(root, "ConfirmDialog/Panel/DontAskToggle"));

            // "!" badge on the top edge of the dialog, popping and wiggling each time it opens.
            Transform panel = root.Find("ConfirmDialog/Panel");
            if (panel != null && skin.Exclamation != null)
            {
                RectTransform badge = NewChild(panel, "AlertBadge", panel.childCount);
                badge.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                badge.anchorMin = badge.anchorMax = new Vector2(0.5f, 1f);
                badge.sizeDelta = new Vector2(84f, 84f);
                badge.anchoredPosition = new Vector2(0f, 6f);
                var badgeImage = badge.gameObject.AddComponent<Image>();
                badgeImage.sprite = skin.Exclamation;
                badgeImage.preserveAspect = true;
                badgeImage.raycastTarget = false;
                badge.gameObject.AddComponent<UiWiggle>();
            }

            var manager = root.gameObject.AddComponent<UIManager>();
            manager.Configure(main, true); // Lobby: no GameManager, so the UIManager turns the UI action map on.
            return manager;
        }

        private static T AddScreen<T>(Transform root, string path, bool isOverlay, ScreenTransition transition, string contentPath)
            where T : UIScreen
        {
            Transform node = Get<Transform>(root, path);
            if (node == null)
            {
                return null;
            }

            var screen = node.gameObject.AddComponent<T>();
            screen.Configure(isOverlay, true, FadeSeconds);
            RectTransform content = contentPath != null ? Get<RectTransform>(root, contentPath) : null;
            screen.ConfigureTransition(transition, content, transition == ScreenTransition.Pop ? 0.36f : 0.34f);
            return screen;
        }

        // Real lobby views (B-15): LobbyController binds every screen by C-12 node names and opens the save session.
        private static void AddLobby(Transform root)
        {
            var lobby = root.gameObject.AddComponent<LobbyController>();
            var data = new SerializedObject(lobby);
            Ref(data, "_gachaTable", AssetDatabase.LoadAssetAtPath<GachaTable>(MetaAssetsBuilder.GachaTablePath));
            Ref(data, "_starterSettings", AssetDatabase.LoadAssetAtPath<StarterSettings>(MetaAssetsBuilder.StarterSettingsPath));
            var heroes = AssetDatabase.LoadAssetAtPath<HeroCatalog>(MatchAssetsBuilder.HeroCatalogPath);
            Ref(data, "_heroCatalog", heroes);
            Ref(data, "_matchCatalog", AssetDatabase.LoadAssetAtPath<MatchCatalog>(MatchAssetsBuilder.MatchCatalogPath));
            data.ApplyModifiedPropertiesWithoutUndo();

            var debug = new GameObject("DebugPanel (F1)").AddComponent<DebugPanel>();
            var debugData = new SerializedObject(debug);
            Ref(debugData, "_heroCatalog", heroes);
            debugData.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------- Helpers ----------

        private static RectTransform NewChild(Transform parent, string name, int siblingIndex)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.SetSiblingIndex(Mathf.Clamp(siblingIndex, 0, parent.childCount - 1));
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetColor(Transform parent, string child, Color color)
        {
            Transform t = parent.Find(child);
            if (t != null && t.TryGetComponent(out Graphic graphic))
            {
                graphic.color = color;
            }
        }

        private static void SetTextColor(Transform parent, string child, Color color) => SetColor(parent, child, color);

        private static Color Transparent(Color color) => new Color(color.r, color.g, color.b, 0f);

        private static bool Near(Color a, Color b) =>
            Mathf.Abs(a.r - b.r) < 0.02f && Mathf.Abs(a.g - b.g) < 0.02f && Mathf.Abs(a.b - b.b) < 0.02f;

        private static void SetFields(Object target, params (string name, float value)[] fields)
        {
            var data = new SerializedObject(target);
            foreach ((string name, float value) in fields)
            {
                SerializedProperty property = data.FindProperty(name);
                if (property == null)
                {
                    Debug.LogError($"[B_UI] {target.GetType().Name} has no field '{name}'.");
                    continue;
                }

                property.floatValue = value;
            }

            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Ref(SerializedObject data, string field, Object value)
        {
            SerializedProperty property = data.FindProperty(field);
            if (property == null)
            {
                Debug.LogError($"[B_UI] {data.targetObject.GetType().Name} has no field '{field}'.");
                return;
            }

            property.objectReferenceValue = value;
        }

        private static T Get<T>(Transform root, string path) where T : Component
        {
            Transform node = root.Find(path);
            if (node == null)
            {
                Debug.LogError($"[B_UI] '{path}' not found in Lobby_Canvas (hierarchy contract C-12).");
                return null;
            }

            T component = node.GetComponent<T>();
            if (component == null)
            {
                Debug.LogError($"[B_UI] '{path}' has no {typeof(T).Name}.");
            }

            return component;
        }
    }
}
