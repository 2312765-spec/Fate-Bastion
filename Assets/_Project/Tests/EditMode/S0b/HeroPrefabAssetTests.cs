using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using FateBastion.Core;
using FateBastion.Editor.Heroes;
using FateBastion.Heroes;

namespace FateBastion.Tests.EditMode.S0b
{
    /// <summary>
    /// Checks the real hero Variants built from HeroArtCatalog (C-08). Heroes whose model has not arrived yet are
    /// ignored with a reason, so the suite stays green while the art is being made.
    /// </summary>
    public class HeroPrefabAssetTests
    {
        private const string BaseControllerPath = "Assets/_Project/Art/Animations/Heroes/AC_Hero_Base.controller";

        // ASSUMPTION: triangle budget per hero so 15 heroes + 100 enemies stay within 30 FPS on Intel UHD (GDD 13).
        private const int MaxTriangles = 10000;
        // GDD 12: adult heroes ~1.8 m, Legendary ~1.3x (2.34 m; Long Vương's antler tips reach 2.53 m).
        // ASSUMPTION: tolerances leave room for hats, hair and bulky silhouettes (Người Tuyết).
        private const float MinHeight = 1.4f;
        private const float MaxHeight = 2.1f;
        private const float MinLegendaryHeight = 2.1f;
        private const float MaxLegendaryHeight = 2.7f;

        private static readonly string[] HeroIds =
        {
            "hero_cung_thu", "hero_phu_thuy_lua", "hero_linh_bang", "hero_tu_si", "hero_nguoi_tuyet",
            "hero_phap_su_sam", "hero_chien_binh_sam", "hero_thanh_nu", "hero_long_vuong"
        };

        [TestCaseSource(nameof(HeroIds))]
        public void Variant_BuiltFromCatalog_IsAssignedAndValid(string heroId)
        {
            HeroData hero = RequireBuiltHero(heroId, out HeroArtEntry entry);

            string path = HeroVariantBuilder.GetVariantPath(HeroVariantBuilder.PrefabFolder, entry.variantName);
            Assert.AreEqual(path, AssetDatabase.GetAssetPath(hero.prefab), "HeroData.prefab phải trỏ tới Variant do công cụ dựng.");
            Assert.AreEqual(PrefabAssetType.Variant, PrefabUtility.GetPrefabAssetType(hero.prefab));

            var issues = new List<HeroValidationIssue>();
            HeroPrefabValidator.ValidateHero(hero, path, issues);
            for (int i = 0; i < issues.Count; i++)
            {
                Assert.AreNotEqual(HeroValidationSeverity.Error, issues[i].Severity, issues[i].Message);
            }
        }

        [TestCaseSource(nameof(HeroIds))]
        public void Animations_OverrideBaseController_AllClipsAssigned(string heroId)
        {
            RequireBuiltHero(heroId, out HeroArtEntry entry);

            Assert.AreEqual(BaseControllerPath, AssetDatabase.GetAssetPath(entry.animations.runtimeAnimatorController),
                "Override Controller phải kế thừa AC_Hero_Base.");
            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            entry.animations.GetOverrides(overrides);
            for (int i = 0; i < overrides.Count; i++)
            {
                Assert.IsNotNull(overrides[i].Value, $"Clip '{overrides[i].Key.name}' chưa được thay.");
            }
        }

        [TestCaseSource(nameof(HeroIds))]
        public void Model_Triangles_WithinBudget(string heroId)
        {
            HeroData hero = RequireBuiltHero(heroId, out _);

            // Only one LOD level draws at a time: the budget applies to LOD0, lower levels are skipped.
            var lowerLods = new HashSet<Renderer>();
            var lodGroup = hero.prefab.GetComponentInChildren<LODGroup>(true);
            if (lodGroup != null)
            {
                LOD[] lods = lodGroup.GetLODs();
                for (int i = 1; i < lods.Length; i++) lowerLods.UnionWith(lods[i].renderers);
            }

            int triangles = 0;
            foreach (var skinned in hero.prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (skinned.sharedMesh != null && !lowerLods.Contains(skinned)) triangles += CountTriangles(skinned.sharedMesh);
            }

            foreach (var filter in hero.prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                // FX (auras, buff icons) are particle renderers, not MeshFilters, so only the model counts here.
                var renderer = filter.GetComponent<Renderer>();
                if (filter.sharedMesh != null && !lowerLods.Contains(renderer)) triangles += CountTriangles(filter.sharedMesh);
            }

            Assert.LessOrEqual(triangles, MaxTriangles);
        }

        [TestCaseSource(nameof(HeroIds))]
        public void Model_Height_MatchesRaritySize(string heroId)
        {
            HeroData hero = RequireBuiltHero(heroId, out _);

            GameObject root = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(hero.prefab));
            try
            {
                Transform model = root.transform.Find(HeroVariantBuilder.ModelRootName);
                float height = 0f;
                foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer is ParticleSystemRenderer) continue;
                    height = Mathf.Max(height, renderer.bounds.max.y);
                }

                if (hero.rarity == Rarity.Legendary) Assert.That(height, Is.InRange(MinLegendaryHeight, MaxLegendaryHeight));
                else Assert.That(height, Is.InRange(MinHeight, MaxHeight));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [TestCaseSource(nameof(HeroIds))]
        public void Variant_ClickOnChest_HitsHeroCollider(string heroId)
        {
            HeroData hero = RequireBuiltHero(heroId, out _);

            // PlacementProbe.PickHero raycasts from the camera; a ray at chest height must find the hero's colliders.
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(hero.prefab);
            try
            {
                Physics.SyncTransforms();
                var ray = new Ray(new Vector3(0f, ChestHeight, -5f), Vector3.forward);
                bool hit = false;
                foreach (Collider c in instance.GetComponents<Collider>())
                {
                    hit |= c.Raycast(ray, out _, 10f);
                }

                Assert.IsTrue(hit, $"{heroId}: click vào ngực tướng ({ChestHeight} m) không trúng collider nào.");
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private const float ChestHeight = 1.2f;

        private static HeroData RequireBuiltHero(string heroId, out HeroArtEntry entry)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<HeroArtCatalog>(HeroVariantBuilder.CatalogPath);
            if (catalog == null) Assert.Ignore("Chưa có HeroArtCatalog: chạy Tools > Heroes > Build Hero Variants.");

            entry = catalog.Find(heroId);
            Assert.IsNotNull(entry, $"HeroArtCatalog thiếu dòng {heroId}.");
            if (entry.model == null) Assert.Ignore($"{heroId}: chưa có model (C-08 đang làm).");

            HeroData hero = FindHero(heroId);
            Assert.IsNotNull(hero, $"Không có HeroData {heroId}.");
            Assert.IsNotNull(hero.prefab, $"{heroId}: có model nhưng chưa dựng Variant – chạy Build Hero Variants.");
            Assert.IsNotNull(entry.animations, $"{heroId}: thiếu Override Controller.");
            return hero;
        }

        private static HeroData FindHero(string heroId)
        {
            string[] guids = AssetDatabase.FindAssets("t:" + nameof(HeroData), new[] { HeroVariantBuilder.HeroDataFolder });
            for (int i = 0; i < guids.Length; i++)
            {
                var hero = AssetDatabase.LoadAssetAtPath<HeroData>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (hero != null && hero.id == heroId) return hero;
            }

            return null;
        }

        private static int CountTriangles(Mesh mesh)
        {
            int count = 0;
            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                count += (int)mesh.GetIndexCount(i) / 3;
            }

            return count;
        }
    }
}
