using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using FateBastion.Combat;
using FateBastion.Core;
using FateBastion.Editor.UI;
using FateBastion.Heroes;
using FateBastion.Skills;

namespace FateBastion.Editor.Heroes
{
    /// <summary>
    /// Tools > Heroes > Build Hero Variants (C-08): for each entry of HeroArtCatalog that has a model, creates or updates
    /// Prefabs/Heroes/Hero_&lt;Name&gt; as a Variant of Hero_Base, puts the model under ModelRoot with HeroVisual and its
    /// three anchors, the rarity aura, buff icons and projectile, then assigns HeroData.prefab. A re-run replaces the
    /// model subtree from the catalog, so per-hero tuning belongs in the catalog, not in the Variant.
    /// </summary>
    public static class HeroVariantBuilder
    {
        public const string PrefabFolder = "Assets/_Project/Prefabs/Heroes";
        public const string BasePrefabPath = PrefabFolder + "/Hero_Base.prefab";
        public const string CatalogPath = "Assets/_Project/Data/Settings/HeroArtCatalog.asset";
        public const string HeroDataFolder = ValidateHeroPrefabsWindow.HeroDataFolder;

        public const string ModelRootName = "ModelRoot";
        public const string ModelName = "Model";
        public const string MuzzleName = "Muzzle";
        public const string OverheadName = "Overhead";
        public const string AuraAnchorName = "AuraAnchor";

        // Selection capsule over the body: bottom 0.6 m up and 0.4 m wide keeps it clear of the 0.8 m occupancy sphere
        // the placement check casts at ground level (sphere centre + capsule bottom cap never meet beyond 0.66 m).
        private const float PickColliderBottom = 0.6f;
        private const float PickColliderRadius = 0.4f;

        private const string LongVuongModelPath = "Assets/_Project/Art/Models/Heroes/LongVuong/CH_LongVuong.fbx";
        private const string LongVuongAnimationsPath = "Assets/_Project/Art/Animations/Heroes/AOC_LongVuong.overrideController";
        // Long Vương breathes fire (Cone): the muzzle sits in front of the face, not in the hand.
        private static readonly Vector3 LongVuongMuzzleOffset = new Vector3(0f, 0f, 0.25f);

        [MenuItem("Tools/Heroes/Build Hero Variants")]
        public static void BuildAllFromMenu()
        {
            HeroArtCatalog catalog = LoadOrCreateCatalog(CatalogPath);
            int built = BuildAll(catalog, LoadHeroes(HeroDataFolder), BasePrefabPath, PrefabFolder, FxPrefabBuilder.PrefabFolder);
            Debug.Log($"[Hero Variants] Built {built} of {catalog.heroes.Count} heroes (entries without a model are skipped).");
            ValidateHeroPrefabsWindow.Open();
        }

        [MenuItem("Tools/Heroes/Select Hero Art Catalog")]
        public static void SelectCatalog()
        {
            HeroArtCatalog catalog = LoadOrCreateCatalog(CatalogPath);
            Selection.activeObject = catalog;
            EditorGUIUtility.PingObject(catalog);
        }

        /// <summary>Builds every entry that has a model and a matching HeroData; returns how many Variants were saved.</summary>
        public static int BuildAll(HeroArtCatalog catalog, IReadOnlyList<HeroData> heroes, string basePath, string folder,
            string fxFolder)
        {
            int built = 0;
            for (int i = 0; i < catalog.heroes.Count; i++)
            {
                HeroArtEntry entry = catalog.heroes[i];
                if (entry == null) continue;
                if (entry.model == null)
                {
                    Debug.LogWarning($"[Hero Variants] {entry.heroId}: no model in the catalog yet; skipped.");
                    continue;
                }

                HeroData hero = FindHero(heroes, entry.heroId);
                if (hero == null)
                {
                    Debug.LogError($"[Hero Variants] {entry.heroId}: no HeroData with this id; skipped.");
                    continue;
                }

                if (Build(entry, hero, catalog, basePath, folder, fxFolder)) built++;
            }

            AssetDatabase.SaveAssets();
            return built;
        }

        public static string GetVariantPath(string folder, string variantName) => folder + "/Hero_" + variantName + ".prefab";

        /// <summary>Creates or updates the Variant of one hero and assigns it to <paramref name="hero"/>.prefab.</summary>
        public static bool Build(HeroArtEntry entry, HeroData hero, HeroArtCatalog catalog, string basePath, string folder,
            string fxFolder)
        {
            if (entry.model == null || entry.animations == null || string.IsNullOrEmpty(entry.variantName))
            {
                Debug.LogError($"[Hero Variants] {entry.heroId}: model, animations and variantName are required.");
                return false;
            }

            var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(basePath);
            if (basePrefab == null)
            {
                Debug.LogError($"[Hero Variants] {basePath} not found.");
                return false;
            }

            string path = GetVariantPath(folder, entry.variantName);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null && !CreateVariant(basePrefab, folder, path)) return false;

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Transform modelRoot = root.transform.Find(ModelRootName);
                if (modelRoot == null)
                {
                    Debug.LogError($"[Hero Variants] {path} has no {ModelRootName}; check Hero_Base.");
                    return false;
                }

                ClearChildren(modelRoot);
                // Scale lives on ModelRoot, never on the root: the root is the gameplay frame (range, footprint) – S0b.
                modelRoot.localScale = Vector3.one * entry.modelScale;
                GameObject model = PlaceModel(entry, modelRoot);
                HeroVisual visual = AddVisual(entry, catalog, model);
                AddPickCollider(root, ModelBounds(model));
                AddRarityAura(hero.rarity, visual.AuraAnchor, fxFolder);
                AssignProjectile(entry, root);

                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            // Buff icons hang under Overhead, which the rebuild just replaced, so they are added again every time.
            FxPrefabBuilder.AddBuffIndicator(path, fxFolder);

            hero.prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            EditorUtility.SetDirty(hero);
            Debug.Log($"[Hero Variants] {entry.heroId} → {path}");
            return true;
        }

        /// <summary>Loads the catalog, creating it with one entry per hero (and Long Vương's existing art) if missing.</summary>
        public static HeroArtCatalog LoadOrCreateCatalog(string path)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<HeroArtCatalog>(path);
            if (catalog != null) return catalog;

            catalog = ScriptableObject.CreateInstance<HeroArtCatalog>();
            // Projectiles only for attackType Projectile (heroes.csv); the arrow keeps Cung thủ physical-looking.
            // The arrow leaves from the bow, which Cung thủ holds in the left hand.
            AddDefault(catalog, "hero_cung_thu", "CungThu", "Projectile_Arrow").muzzleBone = HumanBodyBones.LeftHand;
            // Phù thủy Lửa casts from the flame on her staff, 0.87 m ahead of the right hand (Unity space).
            AddDefault(catalog, "hero_phu_thuy_lua", "PhuThuyLua", null).muzzleOffset = new Vector3(0.12f, 0.27f, 0.87f);
            // Lính Băng shoots from the ice head of the crossbow in the right hand.
            AddDefault(catalog, "hero_linh_bang", "LinhBang", "Projectile_Ice").muzzleOffset = new Vector3(0.11f, -0.35f, 0.77f);
            // Tu sĩ's fire leaves from the paper lantern hanging 0.37 m under the right hand.
            AddDefault(catalog, "hero_tu_si", "TuSi", "Projectile_Fire").muzzleOffset = new Vector3(0.02f, -0.37f, 0f);
            // Người Tuyết's area slam lands where the head of the ice club hits, 0.95 m ahead of the right hand.
            AddDefault(catalog, "hero_nguoi_tuyet", "NguoiTuyet", null).muzzleOffset = new Vector3(0.05f, 0f, 0.95f);
            // Pháp sư Sấm casts from the lightning crystal on his staff, 0.73 m ahead of the right hand.
            AddDefault(catalog, "hero_phap_su_sam", "PhapSuSam", null).muzzleOffset = new Vector3(0.22f, 0.28f, 0.73f);
            AddDefault(catalog, "hero_chien_binh_sam", "ChienBinhSam", null);
            // Thánh Nữ holds the sun staff in the LEFT hand; the holy flame on top is 0.55 m above it.
            HeroArtEntry thanhNu = AddDefault(catalog, "hero_thanh_nu", "ThanhNu", "Projectile_Fire");
            thanhNu.muzzleBone = HumanBodyBones.LeftHand;
            thanhNu.muzzleOffset = new Vector3(-0.16f, 0.55f, 0.01f);
            HeroArtEntry longVuong = AddDefault(catalog, "hero_long_vuong", "LongVuong", null);
            longVuong.model = AssetDatabase.LoadAssetAtPath<GameObject>(LongVuongModelPath);
            longVuong.animations = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(LongVuongAnimationsPath);
            longVuong.muzzleBone = HumanBodyBones.Head;
            longVuong.muzzleOffset = LongVuongMuzzleOffset;

            AssetDatabase.CreateAsset(catalog, path);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Hero Variants] Created {path}; fill model + animations for each hero as the art arrives.");
            return catalog;
        }

        private static HeroArtEntry AddDefault(HeroArtCatalog catalog, string heroId, string variantName, string projectileName)
        {
            var entry = new HeroArtEntry { heroId = heroId, variantName = variantName };
            if (projectileName != null)
            {
                entry.projectile = AssetDatabase.LoadAssetAtPath<GameObject>(
                    FxPrefabBuilder.GetPrefabPath(FxPrefabBuilder.PrefabFolder, projectileName));
            }

            catalog.heroes.Add(entry);
            return entry;
        }

        private static bool CreateVariant(GameObject basePrefab, string folder, string path)
        {
            EnsureFolder(folder);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            try
            {
                instance.name = System.IO.Path.GetFileNameWithoutExtension(path);
                PrefabUtility.SaveAsPrefabAsset(instance, path, out bool success);
                if (!success) Debug.LogError($"[Hero Variants] Could not save {path}.");
                return success;
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(parent.GetChild(i).gameObject);
            }
        }

        private static GameObject PlaceModel(HeroArtEntry entry, Transform modelRoot)
        {
            var model = (GameObject)PrefabUtility.InstantiatePrefab(entry.model, modelRoot);
            model.name = ModelName;
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;

            var animator = model.GetComponentInChildren<Animator>(true);
            if (animator == null) animator = model.AddComponent<Animator>();
            animator.runtimeAnimatorController = entry.animations;
            // Heroes stand still; HeroController owns the transform, the clips must not move it.
            animator.applyRootMotion = false;
            return model;
        }

        private static HeroVisual AddVisual(HeroArtEntry entry, HeroArtCatalog catalog, GameObject model)
        {
            Bounds bounds = ModelBounds(model);
            var animator = model.GetComponentInChildren<Animator>(true);
            Transform bone = FindBone(animator, entry.muzzleBone);
            if (bone == null && entry.muzzleBone != HumanBodyBones.LastBone)
            {
                Debug.LogWarning($"[Hero Variants] {entry.heroId}: bone {entry.muzzleBone} not found (model not Humanoid?); " +
                                 "Muzzle uses the fallback point in front of the model.");
            }

            Vector3? bonePosition = bone != null ? bone.position : (Vector3?)null;
            Transform muzzle = CreateAnchor(MuzzleName, bone != null ? bone : model.transform,
                HeroAnchorLayout.Muzzle(bounds, bonePosition, entry.muzzleOffset, catalog.fallbackMuzzleHeight));
            Transform overhead = CreateAnchor(OverheadName, model.transform, HeroAnchorLayout.Overhead(bounds, catalog.overheadMargin));
            Transform aura = CreateAnchor(AuraAnchorName, model.transform, HeroAnchorLayout.AuraAnchor());

            var visual = model.AddComponent<HeroVisual>();
            var serialized = new SerializedObject(visual);
            serialized.FindProperty("_muzzle").objectReferenceValue = muzzle;
            serialized.FindProperty("_overhead").objectReferenceValue = overhead;
            serialized.FindProperty("_auraAnchor").objectReferenceValue = aura;
            serialized.FindProperty("_animator").objectReferenceValue = animator;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return visual;
        }

        /// <summary>
        /// Adds a capsule over the model's body so a click on the hero selects it (PlacementProbe.PickHero raycasts the
        /// Hero layer). Hero_Base's own capsule (A) is 1 m tall and centred on the ground, so it only covers the feet;
        /// it stays untouched because the placement CheckSphere relies on it. This one starts PickColliderBottom above
        /// the ground, out of reach of the occupancy sphere, so placing heroes behaves exactly as before.
        /// </summary>
        private static void AddPickCollider(GameObject root, Bounds model)
        {
            foreach (CapsuleCollider existing in root.GetComponents<CapsuleCollider>())
            {
                if (PrefabUtility.IsAddedComponentOverride(existing)) Object.DestroyImmediate(existing);
            }

            float top = Mathf.Max(model.max.y, PickColliderBottom + 2f * PickColliderRadius);
            var pick = root.AddComponent<CapsuleCollider>();
            pick.direction = 1;
            pick.radius = PickColliderRadius;
            pick.height = top - PickColliderBottom;
            pick.center = new Vector3(0f, (top + PickColliderBottom) * 0.5f, 0f);
        }

        private static Transform CreateAnchor(string anchorName, Transform parent, Vector3 worldPosition)
        {
            var anchor = new GameObject(anchorName).transform;
            anchor.SetParent(parent, false);
            anchor.position = worldPosition;
            // Anchors face the hero's forward even when parented to a rotated bone.
            anchor.rotation = parent.root.rotation;
            return anchor;
        }

        /// <summary>World bounds of every renderer under the model; the prefab root sits at the origin while editing.</summary>
        private static Bounds ModelBounds(GameObject model)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                Debug.LogWarning($"[Hero Variants] '{model.name}' has no renderer; anchors use a 1 m box.");
                return new Bounds(new Vector3(0f, 0.5f, 0f), Vector3.one);
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        /// <summary>
        /// Finds a Humanoid bone through the avatar's bone map, by name. Editor-only and run once per build, so walking the
        /// hierarchy is fine here; gameplay never does this (S0b).
        /// </summary>
        private static Transform FindBone(Animator animator, HumanBodyBones bone)
        {
            if (animator == null || bone == HumanBodyBones.LastBone) return null;
            Avatar avatar = animator.avatar;
            if (avatar == null || !avatar.isHuman) return null;

            string humanName = HumanTrait.BoneName[(int)bone];
            HumanBone[] map = avatar.humanDescription.human;
            for (int i = 0; i < map.Length; i++)
            {
                if (map[i].humanName == humanName) return FindDeep(animator.transform, map[i].boneName);
            }

            return null;
        }

        private static Transform FindDeep(Transform parent, string childName)
        {
            if (parent.name == childName) return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindDeep(parent.GetChild(i), childName);
                if (found != null) return found;
            }

            return null;
        }

        private static void AddRarityAura(Rarity rarity, Transform auraAnchor, string fxFolder)
        {
            string auraPath = FxPrefabBuilder.GetPrefabPath(fxFolder, FxPrefabBuilder.AuraNames[(int)rarity]);
            var aura = AssetDatabase.LoadAssetAtPath<GameObject>(auraPath);
            if (aura == null)
            {
                Debug.LogWarning($"[Hero Variants] {auraPath} not found; run Tools > FX > 1. Create FX Prefabs. No rarity aura.");
                return;
            }

            PrefabUtility.InstantiatePrefab(aura, auraAnchor);
        }

        private static void AssignProjectile(HeroArtEntry entry, GameObject root)
        {
            if (entry.projectile == null) return;
            var projectile = entry.projectile.GetComponent<Projectile>();
            var runner = root.GetComponent<SkillRunner>();
            if (projectile == null || runner == null)
            {
                Debug.LogError($"[Hero Variants] {entry.heroId}: projectile '{entry.projectile.name}' has no Projectile " +
                               "or Hero_Base has no SkillRunner; projectile not assigned.");
                return;
            }

            var serialized = new SerializedObject(runner);
            serialized.FindProperty("_projectilePrefab").objectReferenceValue = projectile;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static HeroData FindHero(IReadOnlyList<HeroData> heroes, string heroId)
        {
            for (int i = 0; i < heroes.Count; i++)
            {
                if (heroes[i] != null && heroes[i].id == heroId) return heroes[i];
            }

            return null;
        }

        private static List<HeroData> LoadHeroes(string folder)
        {
            var heroes = new List<HeroData>();
            string[] guids = AssetDatabase.FindAssets("t:" + nameof(HeroData), new[] { folder });
            for (int i = 0; i < guids.Length; i++)
            {
                var hero = AssetDatabase.LoadAssetAtPath<HeroData>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (hero != null) heroes.Add(hero);
            }

            return heroes;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = System.IO.Path.GetDirectoryName(folder)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(folder));
        }
    }
}
