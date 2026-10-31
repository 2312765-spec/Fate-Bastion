using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using FateBastion.Editor.Import;
using FateBastion.Enemies;
using FateBastion.Game;
using FateBastion.Heroes;
using FateBastion.Meta;
using FateBastion.Player;
using FateBastion.UI;
using FateBastion.UI.DebugTools;
using Object = UnityEngine.Object;

namespace FateBastion.Editor.DebugTools
{
    /// <summary>
    /// Tools > B > Build Systems Prefabs and Scenes: B's runtime wiring without hand-editing YAML.
    /// <list type="bullet">
    /// <item><c>Prefabs/Systems/_GameSystems</c>: GameManager, TimeController, WaveManager, EnemyManager, Castle, PlacementSystem,
    /// HeroPool, GhostPreview, DebugPanel, every reference filled from Data/Settings.</item>
    /// <item><c>Prefabs/Systems/_UIRoot</c>: EventSystem + C's HUD_Match and MatchMenus_Canvas as nested prefabs, B's view
    /// scripts added as overrides (C's prefab files are never modified).</item>
    /// <item><c>Scenes/Lobby</c> and <c>Scenes/Boot</c> (B), and <c>Scenes/Sandbox/B_Match</c> = Map_Level1 + Player +
    /// _GameSystems + _UIRoot to play a full match. <c>Scenes/Game</c> is C's: C drags the two prefabs in.</item>
    /// </list>
    /// </summary>
    public static class GameSystemsBuilder
    {
        private const string Root = "Assets/_Project/";
        private const string SystemsFolder = Root + "Prefabs/Systems";
        public const string GameSystemsPath = SystemsFolder + "/_GameSystems.prefab";
        public const string UiRootPath = SystemsFolder + "/_UIRoot.prefab";
        public const string LobbyScenePath = Root + "Scenes/Lobby.unity";
        public const string BootScenePath = Root + "Scenes/Boot.unity";
        public const string MatchSandboxPath = Root + "Scenes/Sandbox/B_Match.unity";

        private const string Settings = Root + "Data/Settings/";
        private const string HeroBasePath = Root + "Prefabs/Heroes/Hero_Base.prefab";
        private const string HudPath = Root + "Prefabs/UI/HUD_Match.prefab";
        private const string MatchMenusPath = Root + "Prefabs/UI/MatchMenus_Canvas.prefab";
        private const string LobbyCanvasPath = Root + "Prefabs/UI/Lobby_Canvas.prefab";
        private const string MapPath = Root + "Prefabs/Map/Map_Level1.prefab";
        private const string PlayerPath = Root + "Prefabs/Player/Player.prefab";
        private const string AudioPath = Root + "Prefabs/Systems/_Audio.prefab";
        private const string InputActionsPath = Root + "Input/FateBastion.inputactions";
        private const string InputModuleType = "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem";
        private const float Fade = 0.2f;

        [MenuItem("Tools/B/Build Systems Prefabs and Scenes")]
        public static void BuildAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            MatchAssetsBuilder.Build(out string assetsReport);
            Debug.Log(assetsReport);

            if (!BuildGameSystems() || !BuildUiRoot()) return;
            BuildLobbyScene();
            BuildBootScene();
            BuildMatchSandbox();
            AssetDatabase.SaveAssets();
            Debug.Log("[B Systems] Built _GameSystems, _UIRoot, Lobby, Boot and Sandbox/B_Match. " +
                      "Play Boot (or Lobby) for the full flow, or Sandbox/B_Match for one match.");
        }

        // ---------- _GameSystems ----------

        public static bool BuildGameSystems()
        {
            var heroBase = Load<GameObject>(HeroBasePath);
            if (heroBase == null) return false;
            HeroController heroController = heroBase.GetComponent<HeroController>();

            Scene preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = new GameObject("_GameSystems");
                SceneManager.MoveGameObjectToScene(root, preview);

                var time = root.AddComponent<TimeController>();

                var enemiesGo = Child(root, "Enemies");
                var enemies = enemiesGo.AddComponent<EnemyManager>();
                Set(enemies, "_settings", Load<EnemySettings>(Settings + "EnemySettings.asset"));
                Set(enemies, "_combatSettings", LoadAny("CombatSettings"));
                Set(enemies, "_bossSummonSettings", Load<BossSummonSettings>(Settings + "BossSummonSettings.asset"));
                var waves = enemiesGo.AddComponent<WaveManager>();
                Set(waves, "_enemyManager", enemies);

                var castle = Child(root, "Castle").AddComponent<Castle>();
                Set(castle, "_enemyManager", enemies);

                var placementGo = Child(root, "Placement");
                var pool = placementGo.AddComponent<HeroPool>();
                pool.Configure(heroController);
                var ghost = placementGo.AddComponent<GhostPreview>();
                ghost.Configure(heroController);
                var placement = placementGo.AddComponent<PlacementSystem>();
                Set(placement, "_rules", Load<PlacementRules>(Settings + "PlacementRules.asset"));
                Set(placement, "_levelTable", Load<HeroLevelTable>(Settings + "HeroLevelTable.asset"));
                Set(placement, "_pool", pool);
                Set(placement, "_ghost", ghost);

                var heroCatalog = Load<HeroCatalog>(MatchAssetsBuilder.HeroCatalogPath);
                var matchCatalog = Load<MatchCatalog>(MatchAssetsBuilder.MatchCatalogPath);
                var starter = Load<StarterSettings>(MetaAssetsBuilder.StarterSettingsPath);

                var game = root.AddComponent<GameManager>();
                Set(game, "_waveManager", waves);
                Set(game, "_enemyManager", enemies);
                Set(game, "_castle", castle);
                Set(game, "_placement", placement);
                Set(game, "_heroPool", pool);
                Set(game, "_ghostPreview", ghost);
                Set(game, "_timeController", time);
                Set(game, "_economy", Load<MatchEconomySettings>(MatchAssetsBuilder.EconomyPath));
                Set(game, "_endlessRewards", Load<EndlessRewardSettings>(Settings + "EndlessRewardSettings.asset"));
                Set(game, "_catalog", matchCatalog);
                Set(game, "_heroCatalog", heroCatalog);
                Set(game, "_gachaTable", Load<GachaTable>(MetaAssetsBuilder.GachaTablePath));
                Set(game, "_starterSettings", starter);
                Set(game, "_defaultLevel", matchCatalog != null ? matchCatalog.LevelAt(0) : null);
                if (heroCatalog != null && starter != null) SetArray(game, "_defaultDeck", heroCatalog.Resolve(starter.DefaultDeck));

                var debug = Child(root, "DebugPanel (F1)").AddComponent<DebugPanel>();
                Set(debug, "_heroCatalog", heroCatalog);

                PrefabUtility.SaveAsPrefabAsset(root, GameSystemsPath);
                return true;
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(preview);
            }
        }

        // ---------- _UIRoot ----------

        public static bool BuildUiRoot()
        {
            var hudPrefab = Load<GameObject>(HudPath);
            var menusPrefab = Load<GameObject>(MatchMenusPath);
            if (hudPrefab == null || menusPrefab == null) return false;

            Scene preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = new GameObject("_UIRoot");
                SceneManager.MoveGameObjectToScene(root, preview);
                AddEventSystem(root.transform);

                var hud = (GameObject)PrefabUtility.InstantiatePrefab(hudPrefab, preview);
                hud.transform.SetParent(root.transform, false);
                hud.AddComponent<HudController>();
                hud.AddComponent<TutorialController>();

                var menus = (GameObject)PrefabUtility.InstantiatePrefab(menusPrefab, preview);
                menus.transform.SetParent(root.transform, false);
                Transform m = menus.transform;
                AddScreen<PauseScreen>(m, "Screen_Pause", true);
                AddScreen<SettingsScreen>(m, "Screen_Settings", true);
                AddScreen<ResultScreen>(m, "Screen_Result", true);
                AddConfirmDialog(m);
                var manager = menus.AddComponent<UIManager>();
                manager.Configure(null, false); // In a match GameManager switches the action maps.
                menus.AddComponent<MatchMenusController>();

                PrefabUtility.SaveAsPrefabAsset(root, UiRootPath);
                return true;
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(preview);
            }
        }

        // ---------- Scenes ----------

        public static void BuildLobbyScene()
        {
            var canvasPrefab = Load<GameObject>(LobbyCanvasPath);
            if (canvasPrefab == null) return;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            canvasPrefab = Load<GameObject>(LobbyCanvasPath); // A new single scene unloads unused assets.
            AddCamera(new Color32(0x1A, 0x12, 0x0D, 0xFF));
            AddEventSystem(null);
            AddInputRouter();

            var canvas = (GameObject)PrefabUtility.InstantiatePrefab(canvasPrefab, scene);
            Transform c = canvas.transform;
            UIScreen main = AddScreen<MainMenuScreen>(c, "Screen_Main", false);
            AddScreen<LevelSelectScreen>(c, "Screen_LevelSelect", false);
            AddScreen<GachaScreen>(c, "Screen_Gacha", false);
            AddScreen<GachaResultScreen>(c, "Screen_Gacha/ResultPanel", true);
            AddScreen<CollectionScreen>(c, "Screen_Collection", false);
            AddScreen<SettingsScreen>(c, "Screen_Settings", true);
            AddConfirmDialog(c);
            var manager = canvas.AddComponent<UIManager>();
            manager.Configure(main, true); // No GameManager in the lobby: the UI action map is turned on here.

            var lobby = canvas.AddComponent<LobbyController>();
            Set(lobby, "_gachaTable", Load<GachaTable>(MetaAssetsBuilder.GachaTablePath));
            Set(lobby, "_starterSettings", Load<StarterSettings>(MetaAssetsBuilder.StarterSettingsPath));
            Set(lobby, "_heroCatalog", Load<HeroCatalog>(MatchAssetsBuilder.HeroCatalogPath));
            Set(lobby, "_matchCatalog", Load<MatchCatalog>(MatchAssetsBuilder.MatchCatalogPath));

            var debug = new GameObject("DebugPanel (F1)").AddComponent<DebugPanel>();
            Set(debug, "_heroCatalog", Load<HeroCatalog>(MatchAssetsBuilder.HeroCatalogPath));

            AddAudio(scene);
            Save(scene, LobbyScenePath);
        }

        public static void BuildBootScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AddCamera(Color.black);
            var boot = new GameObject("BootLoader").AddComponent<BootLoader>();
            Set(boot, "_gachaTable", Load<GachaTable>(MetaAssetsBuilder.GachaTablePath));
            Set(boot, "_starterSettings", Load<StarterSettings>(MetaAssetsBuilder.StarterSettingsPath));
            Save(scene, BootScenePath);
        }

        public static void BuildMatchSandbox()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var light = new GameObject("Directional Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            foreach (string path in new[] { MapPath, PlayerPath, GameSystemsPath, UiRootPath, AudioPath })
            {
                var prefab = Load<GameObject>(path);
                if (prefab != null) PrefabUtility.InstantiatePrefab(prefab, scene);
            }

            Save(scene, MatchSandboxPath);
        }

        // ---------- Helpers ----------

        private static T AddScreen<T>(Transform root, string path, bool overlay) where T : UIScreen
        {
            Transform node = root.Find(path);
            if (node == null)
            {
                Debug.LogError($"[B Systems] '{root.name}/{path}' not found (hierarchy contract C-11/C-12).");
                return null;
            }

            T screen = node.GetComponent<T>();
            if (screen == null) screen = node.gameObject.AddComponent<T>();
            screen.Configure(overlay, true, Fade);
            RectTransform panel = node.Find("Panel") as RectTransform;
            if (overlay && panel != null) screen.ConfigureTransition(ScreenTransition.Pop, panel, 0.36f);
            else screen.ConfigureTransition(ScreenTransition.Slide, null, 0.34f);
            return screen;
        }

        private static void AddConfirmDialog(Transform root)
        {
            Transform node = root.Find("ConfirmDialog");
            if (node == null)
            {
                Debug.LogError($"[B Systems] '{root.name}/ConfirmDialog' not found.");
                return;
            }

            ConfirmDialog dialog = node.GetComponent<ConfirmDialog>();
            if (dialog == null) dialog = node.gameObject.AddComponent<ConfirmDialog>();
            dialog.Configure(true, true, Fade);
            dialog.ConfigureTransition(ScreenTransition.Pop, node.Find("Panel") as RectTransform, 0.36f);
            dialog.ConfigureReferences(null,
                Find<TMP_Text>(node, "Panel/TitleText"), Find<TMP_Text>(node, "Panel/MessageText"),
                Find<Button>(node, "Panel/Buttons/ConfirmButton"), Find<Button>(node, "Panel/Buttons/CancelButton"),
                Find<Toggle>(node, "Panel/DontAskToggle"));
        }

        private static void AddCamera(Color background)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = go.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = background;
        }

        private static void AddEventSystem(Transform parent)
        {
            var go = new GameObject("EventSystem", typeof(EventSystem));
            if (parent != null) go.transform.SetParent(parent, false);
            Type module = Type.GetType(InputModuleType);
            if (module != null) go.AddComponent(module);
            else Debug.LogError("[B Systems] InputSystemUIInputModule not found; buttons will not receive clicks.");
        }

        private static void AddInputRouter()
        {
            var router = new GameObject("InputRouter").AddComponent<SummonerInputRouter>();
            Set(router, "_actions", AssetDatabase.LoadMainAssetAtPath(InputActionsPath));
        }

        private static void AddAudio(Scene scene)
        {
            var audio = AssetDatabase.LoadAssetAtPath<GameObject>(AudioPath);
            if (audio != null) PrefabUtility.InstantiatePrefab(audio, scene);
        }

        private static void Save(Scene scene, string path)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, path);
        }

        private static GameObject Child(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        private static T Load<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) Debug.LogError($"[B Systems] Missing asset {path}.");
            return asset;
        }

        // CombatSettings lives in Combat (A); found by type so the builder does not depend on its exact path.
        private static Object LoadAny(string typeName)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:" + typeName, new[] { Root + "Data" }))
            {
                Object asset = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null) return asset;
            }

            Debug.LogError($"[B Systems] No {typeName} asset under Data/.");
            return null;
        }

        private static T Find<T>(Transform root, string path) where T : Component
        {
            Transform node = root.Find(path);
            return node != null ? node.GetComponent<T>() : null;
        }

        private static void Set(Object target, string field, Object value)
        {
            var data = new SerializedObject(target);
            SerializedProperty property = data.FindProperty(field);
            if (property == null)
            {
                Debug.LogError($"[B Systems] {target.GetType().Name} has no field '{field}'.");
                return;
            }

            property.objectReferenceValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetArray(Object target, string field, IReadOnlyList<Object> values)
        {
            var data = new SerializedObject(target);
            SerializedProperty property = data.FindProperty(field);
            if (property == null) return;
            property.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
