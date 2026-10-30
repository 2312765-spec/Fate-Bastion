using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using FateBastion.Core;
using FateBastion.Heroes;
using FateBastion.Heroes.Sandbox;
using FateBastion.Player;
using Object = UnityEngine.Object;

namespace FateBastion.Editor.DebugTools
{
    /// <summary>
    /// Tools > Sandbox > Build B_Placement Scene (B-02/B-04): a flat test field with Placeable ground, a road (Path), water,
    /// a cliff (Obstacle), the real InputRouter, PlacementSystem, HeroPool and GhostPreview, and a 5-hero deck (one of each
    /// rarity when possible). Press Play: 1–5 preview, left click place, Esc / right tap cancel, click a hero then U / X.
    /// </summary>
    public static class PlacementSandboxSceneBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Sandbox/B_Placement.unity";

        private const string HeroBasePath = "Assets/_Project/Prefabs/Heroes/Hero_Base.prefab";
        private const string InputActionsPath = "Assets/_Project/Input/FateBastion.inputactions";
        private const string PlacementRulesPath = "Assets/_Project/Data/Settings/PlacementRules.asset";
        private const string LevelTablePath = "Assets/_Project/Data/Settings/HeroLevelTable.asset";
        private const string InputModuleType = "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem";

        [MenuItem("Tools/Sandbox/Build B_Placement Scene (đặt tướng)")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var heroBase = AssetDatabase.LoadAssetAtPath<GameObject>(HeroBasePath);
            var rules = AssetDatabase.LoadAssetAtPath<PlacementRules>(PlacementRulesPath);
            var levels = AssetDatabase.LoadAssetAtPath<HeroLevelTable>(LevelTablePath);
            if (heroBase == null || rules == null || levels == null)
            {
                Debug.LogError("[B_Placement] Hero_Base, PlacementRules or HeroLevelTable is missing.");
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // A new single scene unloads unused assets, so the objects loaded above are reloaded before being referenced.
            heroBase = AssetDatabase.LoadAssetAtPath<GameObject>(HeroBasePath);
            rules = AssetDatabase.LoadAssetAtPath<PlacementRules>(PlacementRulesPath);
            levels = AssetDatabase.LoadAssetAtPath<HeroLevelTable>(LevelTablePath);
            Camera camera = Camera.main;
            camera.transform.SetPositionAndRotation(new Vector3(0f, 17f, -15f), Quaternion.Euler(50f, 0f, 0f));

            BuildInput();
            BuildField();

            var systems = new GameObject("_PlacementSystems");
            var pool = systems.AddComponent<HeroPool>();
            var heroController = heroBase.GetComponent<HeroController>();
            pool.Configure(heroController);
            var ghost = systems.AddComponent<GhostPreview>();
            ghost.Configure(heroController);
            var placement = systems.AddComponent<PlacementSystem>();
            Set(placement, "_rules", rules);
            Set(placement, "_levelTable", levels);
            Set(placement, "_pool", pool);
            Set(placement, "_ghost", ghost);
            Set(placement, "_camera", camera);

            var demo = systems.AddComponent<PlacementSandboxDemo>();
            Set(demo, "_placement", placement);
            Set(demo, "_pool", pool);
            Set(demo, "_ghost", ghost);
            SetArray(demo, "_deck", PickDeck());

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[B_Placement] Built {ScenePath}. Play: 1–5, left click, Esc, click a hero then U/X.");
        }

        private static void BuildInput()
        {
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem));
            Type module = Type.GetType(InputModuleType);
            if (module != null) eventSystem.AddComponent(module);

            var router = new GameObject("InputRouter").AddComponent<SummonerInputRouter>();
            Set(router, "_actions", AssetDatabase.LoadMainAssetAtPath(InputActionsPath));
        }

        private static void BuildField()
        {
            var field = new GameObject("Field").transform;
            Box(field, "Ground (Placeable)", "Placeable", new Vector3(0f, -0.5f, 0f), new Vector3(30f, 1f, 24f), new Color(0.42f, 0.62f, 0.32f));
            Box(field, "Road (Path)", "Path", new Vector3(4f, 0.01f, 0f), new Vector3(2.5f, 0.02f, 24f), new Color(0.62f, 0.5f, 0.34f));
            Box(field, "Water", "Water", new Vector3(-9f, 0.02f, 2f), new Vector3(5f, 0.04f, 8f), new Color(0.2f, 0.45f, 0.85f));
            Box(field, "Cliff (Obstacle)", "Obstacle", new Vector3(-2f, 1.5f, 7f), new Vector3(5f, 3f, 3f), new Color(0.45f, 0.42f, 0.4f));
            Box(field, "Rock (Obstacle)", "Obstacle", new Vector3(9f, 0.6f, -5f), new Vector3(1.6f, 1.2f, 1.6f), new Color(0.5f, 0.5f, 0.52f));
        }

        private static void Box(Transform parent, string name, string layer, Vector3 center, Vector3 size, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.layer = LayerMask.NameToLayer(layer);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localScale = size;

            var renderer = go.GetComponent<MeshRenderer>();
            var material = new Material(renderer.sharedMaterial) { color = color, name = name };
            renderer.sharedMaterial = material;
        }

        // One hero per rarity first, then any others, up to five (the deck needs no save here).
        private static HeroData[] PickDeck()
        {
            var all = new List<HeroData>();
            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(HeroData), new[] { "Assets/_Project/Data" }))
            {
                var hero = AssetDatabase.LoadAssetAtPath<HeroData>(AssetDatabase.GUIDToAssetPath(guid));
                if (hero != null && !string.IsNullOrEmpty(hero.id)) all.Add(hero);
            }

            all.Sort((a, b) => string.CompareOrdinal(a.id, b.id));
            var deck = new List<HeroData>(MatchDeck.Size);
            for (int r = 0; r <= (int)Rarity.Legendary; r++)
            {
                HeroData hero = all.Find(h => (int)h.rarity == r && !deck.Contains(h));
                if (hero != null) deck.Add(hero);
            }

            foreach (HeroData hero in all)
            {
                if (deck.Count >= MatchDeck.Size) break;
                if (!deck.Contains(hero)) deck.Add(hero);
            }

            return deck.ToArray();
        }

        private static void Set(Object target, string field, Object value)
        {
            var data = new SerializedObject(target);
            SerializedProperty property = data.FindProperty(field);
            if (property == null)
            {
                Debug.LogError($"[B_Placement] {target.GetType().Name} has no field '{field}'.");
                return;
            }

            property.objectReferenceValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetArray(Object target, string field, Object[] values)
        {
            var data = new SerializedObject(target);
            SerializedProperty property = data.FindProperty(field);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
