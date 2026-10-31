using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using FateBastion.Audio;
using FateBastion.Combat;
using FateBastion.Editor.UI;
using FateBastion.Heroes;

namespace FateBastion.Tests.EditMode.S11
{
    /// <summary>Runs Tools > FX > Create FX Prefabs into a temporary folder: right flags, variants, never overwrites (C-09).</summary>
    public class FxPrefabBuilderTests
    {
        private const string TempFolder = "Assets/_FxPrefabBuilderTest";
        private const string PrefabFolder = TempFolder + "/Prefabs";
        private const string MaterialFolder = TempFolder + "/Materials";
        private const string TextureFolder = TempFolder + "/Textures";
        private const string SettingsPath = TempFolder + "/FxSettings.asset";
        private const string MarkerName = "Marker";
        // Per-layer cap so 100 enemies with hits, deaths and auras stay cheap (C-15).
        private const int MaxParticlesPerLayer = 40;

        [SetUp]
        public void SetUp()
        {
            AssetDatabase.DeleteAsset(TempFolder);
            Assume.That(AssetDatabase.LoadAssetAtPath<GameObject>(FxPrefabBuilder.ProjectileBasePath), Is.Not.Null,
                $"Thiếu {FxPrefabBuilder.ProjectileBasePath}.");
            Assume.That(Shader.Find("Universal Render Pipeline/Particles/Unlit"), Is.Not.Null, "Thiếu shader URP Particles.");
        }

        [TearDown]
        public void TearDown() => AssetDatabase.DeleteAsset(TempFolder);

        [Test]
        public void Create_EmptyFolder_CreatesEveryPrefab()
        {
            int expected = FxPrefabBuilder.OneShotNames.Length + FxPrefabBuilder.ProjectileNames.Length
                           + FxPrefabBuilder.AuraNames.Length + 1
                           + FxPrefabBuilder.SkillOneShotNames.Length + FxPrefabBuilder.SkillLoopNames.Length;

            Assert.AreEqual(expected, Create());

            foreach (string prefabName in AllNames()) Assert.IsNotNull(Load(prefabName), prefabName);
        }

        [Test]
        public void Create_OneShots_RootSystemNonLoopingScaledTimeHierarchy()
        {
            Create();

            foreach (string prefabName in OneShotNames())
            {
                GameObject prefab = Load(prefabName);
                Assert.IsNotNull(prefab.GetComponent<ParticleSystem>(), $"{prefabName}: gốc phải có ParticleSystem.");
                foreach (ParticleSystem system in prefab.GetComponentsInChildren<ParticleSystem>(true))
                {
                    ParticleSystem.MainModule main = system.main;
                    Assert.IsFalse(main.loop, $"{prefabName}/{system.name} không được lặp.");
                    AssertCommonFlags(prefabName, system);
                }
            }
        }

        [Test]
        public void Create_AurasAndProjectiles_Loop()
        {
            Create();

            foreach (string prefabName in LoopingNames())
            {
                ParticleSystem[] systems = Load(prefabName).GetComponentsInChildren<ParticleSystem>(true);
                Assert.IsNotEmpty(systems, prefabName);
                foreach (ParticleSystem system in systems)
                {
                    Assert.IsTrue(system.main.loop, $"{prefabName}/{system.name} phải lặp.");
                    AssertCommonFlags(prefabName, system);
                }
            }
        }

        [Test]
        public void Create_Projectiles_AreVariantsKeepingProjectile()
        {
            Create();
            var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FxPrefabBuilder.ProjectileBasePath);

            foreach (string prefabName in FxPrefabBuilder.ProjectileNames)
            {
                GameObject prefab = Load(prefabName);
                Assert.AreEqual(PrefabAssetType.Variant, PrefabUtility.GetPrefabAssetType(prefab), prefabName);
                Assert.AreEqual(basePrefab, PrefabUtility.GetCorrespondingObjectFromSource(prefab), prefabName);
                Assert.IsNotNull(prefab.GetComponent<Projectile>(), $"{prefabName}: mất component Projectile.");
                Assert.IsNotNull(prefab.GetComponent<ParticleRestartOnEnable>(), prefabName);

                Transform sphere = prefab.transform.Find("ProjectileVisual");
                if (sphere != null) Assert.IsFalse(sphere.gameObject.activeSelf, $"{prefabName}: quả cầu nháp phải tắt.");
            }
        }

        [Test]
        public void Create_SecondRun_CreatesNothingAndKeepsEdits()
        {
            Create();
            string path = FxPrefabBuilder.GetPrefabPath(PrefabFolder, FxPrefabBuilder.HitFireName);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            new GameObject(MarkerName).transform.SetParent(root.transform);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);

            Assert.AreEqual(0, Create());

            Assert.IsNotNull(Load(FxPrefabBuilder.HitFireName).transform.Find(MarkerName), "Prefab có sẵn bị ghi đè.");
        }

        [Test]
        public void Create_NoKenneyTextures_UsesGeneratedFallback()
        {
            Create();

            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Texture2D>(TextureFolder + "/FX_SoftCircle.png"));
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Texture2D>(TextureFolder + "/FX_SoftRing.png"));
            var renderer = Load(FxPrefabBuilder.HitFireName).GetComponent<ParticleSystemRenderer>();
            Assert.IsNotNull(renderer.sharedMaterial.GetTexture("_BaseMap"));
        }

        [Test]
        public void Create_KenneyTextureAddedLater_SecondRunSwapsFallback()
        {
            Create();
            string kenneyPath = TextureFolder + "/" + FxPrefabBuilder.KenneyFolderName + "/circle_05.png";
            Assert.IsTrue(AssetDatabase.CopyAsset(TextureFolder + "/FX_SoftCircle.png", kenneyPath));

            Create();

            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/M_FX_Glow_Add.mat");
            Assert.AreEqual(kenneyPath, AssetDatabase.GetAssetPath(material.GetTexture("_BaseMap")));
        }

        [TestCase(FxPrefabBuilder.HitFireName)]
        [TestCase(FxPrefabBuilder.HitIceName)]
        [TestCase(FxPrefabBuilder.HitLightningName)]
        public void Create_HitFx_EveryLayerAboveGround(string prefabName)
        {
            Create();

            // HitPoint is the enemy root on the ground; a layer emitting at y = 0 is half hidden by the terrain.
            foreach (ParticleSystem system in Load(prefabName).GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.ShapeModule shape = system.shape;
                float height = system.transform.localPosition.y + (shape.enabled ? shape.position.y : 0f);
                Assert.GreaterOrEqual(height, 0.5f, $"{prefabName}/{system.name}");
            }
        }

        [Test]
        public void Create_GroundRings_NotClampedWhenZoomed()
        {
            Create();

            int rings = 0;
            foreach (string prefabName in AllNames())
            {
                foreach (ParticleSystemRenderer renderer in Load(prefabName).GetComponentsInChildren<ParticleSystemRenderer>(true))
                {
                    if (renderer.renderMode != ParticleSystemRenderMode.HorizontalBillboard) continue;
                    rings++;
                    Assert.GreaterOrEqual(renderer.maxParticleSize, FxPrefabBuilder.FlatMaxParticleSize, $"{prefabName}/{renderer.name}");
                }
            }

            Assert.Greater(rings, 0);
        }

        [Test]
        public void FixFlatRingClamp_PrefabBuiltBeforeFix_LiftsClampOnly()
        {
            Create();
            string path = FxPrefabBuilder.GetPrefabPath(PrefabFolder, FxPrefabBuilder.CommandAuraName);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            var renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.maxParticleSize = 0.5f;
            Material hand = renderer.sharedMaterial;
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);

            Assert.AreEqual(1, FxPrefabBuilder.FixFlatRingClamp(PrefabFolder));

            var fixedRenderer = Load(FxPrefabBuilder.CommandAuraName).GetComponent<ParticleSystemRenderer>();
            Assert.AreEqual(FxPrefabBuilder.FlatMaxParticleSize, fixedRenderer.maxParticleSize);
            Assert.AreEqual(hand, fixedRenderer.sharedMaterial);
            Assert.AreEqual(0, FxPrefabBuilder.FixFlatRingClamp(PrefabFolder), "Chạy lần 2 không được sửa gì.");
        }

        [TestCase(FxPrefabBuilder.ConeFireName)]
        [TestCase(FxPrefabBuilder.DragonLineName)]
        public void Create_ForwardFx_StaysWithinUnitLengthAlongZ(string prefabName)
        {
            Create();

            // A scales Z by length/range, so anything outside z in [0, 1] would draw outside the damage area.
            foreach (ParticleSystem system in Load(prefabName).GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.MainModule main = system.main;
                ParticleSystem.ShapeModule shape = system.shape;
                float start = system.transform.localPosition.z + shape.position.z;
                float life = main.startLifetime.constantMax;
                float near = start;
                float far = start;
                if (shape.shapeType == ParticleSystemShapeType.Box)
                {
                    near -= shape.scale.z * 0.5f;
                    far += shape.scale.z * 0.5f;
                }
                else
                {
                    far += main.startSpeed.constantMax * life;
                }

                ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
                if (velocity.enabled) far += Mathf.Abs(velocity.z.constant) * life;

                Assert.GreaterOrEqual(near, -0.01f, $"{prefabName}/{system.name}");
                Assert.LessOrEqual(far, 1.01f, $"{prefabName}/{system.name}");
            }
        }

        [Test]
        public void Create_MeteorFall_LandsWhenMeteorExplodes()
        {
            Create();

            foreach (ParticleSystem system in Load(FxPrefabBuilder.MeteorFallName).GetComponentsInChildren<ParticleSystem>(true))
            {
                float startY = system.transform.localPosition.y + system.shape.position.y;
                float life = system.main.startLifetime.constantMax;
                float endY = startY + system.velocityOverLifetime.y.constant * life;
                Assert.AreEqual(FxPrefabBuilder.MeteorFallHeight, startY, 0.01f, system.name);
                Assert.AreEqual(FxPrefabBuilder.MeteorFallSeconds, life, 0.01f, system.name);
                Assert.AreEqual(0f, endY, 0.05f, $"{system.name} phải chạm đất đúng lúc nổ.");
            }
        }

        [TestCase(FxPrefabBuilder.BuffDamageName)]
        [TestCase(FxPrefabBuilder.BuffAttackSpeedName)]
        public void Create_BuffIcon_OneUprightIconWithoutFade(string prefabName)
        {
            Create();

            var system = Load(prefabName).GetComponent<ParticleSystem>();
            ParticleSystem.MainModule main = system.main;
            Assert.LessOrEqual(main.maxParticles, 2);
            Assert.AreEqual(0f, main.startRotation.constantMax, "Ký hiệu phải đứng thẳng.");
            Assert.IsFalse(system.colorOverLifetime.enabled, "Ký hiệu không được mờ dần rồi hiện lại.");
            Assert.Greater(main.startLifetime.constantMin, main.duration, "Phải luôn có một hạt đang sống.");
            Assert.IsNotNull(system.GetComponent<ParticleSystemRenderer>().sharedMaterial.GetTexture("_BaseMap"));
        }

        [Test]
        public void AddBuffIndicator_HeroWithOverhead_AddsHiddenIconsOnce()
        {
            Create();
            string heroPath = CreateHeroPrefab();

            Assert.IsTrue(FxPrefabBuilder.AddBuffIndicator(heroPath, PrefabFolder));
            Assert.IsFalse(FxPrefabBuilder.AddBuffIndicator(heroPath, PrefabFolder), "Chạy lần 2 không được thêm nữa.");

            var hero = AssetDatabase.LoadAssetAtPath<GameObject>(heroPath);
            Transform icons = hero.transform.Find("Model/Overhead/" + FxPrefabBuilder.BuffIconsObjectName);
            Assert.IsNotNull(icons);
            var indicator = icons.GetComponent<HeroBuffIndicator>();
            Assert.IsNotNull(indicator);
            Assert.AreEqual(2, icons.childCount);
            foreach (Transform icon in icons) Assert.IsFalse(icon.gameObject.activeSelf, $"{icon.name} phải ẩn khi chưa có buff.");
        }

        [Test]
        public void AddBuffIndicator_NoOverhead_ReturnsFalse()
        {
            Create();
            string heroPath = CreateHeroPrefab(withOverhead: false);

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Overhead"));
            Assert.IsFalse(FxPrefabBuilder.AddBuffIndicator(heroPath, PrefabFolder));
        }

        private static string CreateHeroPrefab(bool withOverhead = true)
        {
            var root = new GameObject("Hero_Test");
            var model = new GameObject("Model");
            model.transform.SetParent(root.transform);
            var visual = model.AddComponent<HeroVisual>();
            if (withOverhead)
            {
                var overhead = new GameObject("Overhead");
                overhead.transform.SetParent(model.transform);
                overhead.transform.localPosition = new Vector3(0f, 2f, 0f);
                var so = new SerializedObject(visual);
                so.FindProperty("_overhead").objectReferenceValue = overhead.transform;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            string path = TempFolder + "/Hero_Test.prefab";
            LogAssert.ignoreFailingMessages = true; // HeroVisual.OnValidate warns about the fields left empty here.
            PrefabUtility.SaveAsPrefabAsset(root, path);
            LogAssert.ignoreFailingMessages = false;
            Object.DestroyImmediate(root);
            return path;
        }

        [Test]
        public void CreateOrFillSettings_AfterCreate_EveryKindHasPrefab()
        {
            Create();

            FxSettings settings = FxPrefabBuilder.CreateOrFillSettings(SettingsPath, PrefabFolder);

            foreach (FxKind kind in System.Enum.GetValues(typeof(FxKind)))
                Assert.IsNotNull(settings.Get(kind).prefab, kind.ToString());
        }

        private static int Create() =>
            FxPrefabBuilder.Create(PrefabFolder, MaterialFolder, TextureFolder, FxPrefabBuilder.ProjectileBasePath);

        private static void AssertCommonFlags(string prefabName, ParticleSystem system)
        {
            ParticleSystem.MainModule main = system.main;
            Assert.IsFalse(main.useUnscaledTime, $"{prefabName}/{system.name}: phải dùng thời gian có tỉ lệ.");
            Assert.AreEqual(ParticleSystemScalingMode.Hierarchy, main.scalingMode, $"{prefabName}/{system.name}");
            Assert.LessOrEqual(main.maxParticles, MaxParticlesPerLayer, $"{prefabName}/{system.name}");
            Assert.IsNotNull(system.GetComponent<ParticleSystemRenderer>().sharedMaterial, $"{prefabName}/{system.name}");
        }

        private static GameObject Load(string prefabName) =>
            AssetDatabase.LoadAssetAtPath<GameObject>(FxPrefabBuilder.GetPrefabPath(PrefabFolder, prefabName));

        private static System.Collections.Generic.IEnumerable<string> LoopingNames()
        {
            foreach (string n in FxPrefabBuilder.ProjectileNames) yield return n;
            foreach (string n in FxPrefabBuilder.AuraNames) yield return n;
            yield return FxPrefabBuilder.CommandAuraName;
            foreach (string n in FxPrefabBuilder.SkillLoopNames) yield return n;
        }

        private static System.Collections.Generic.IEnumerable<string> OneShotNames()
        {
            foreach (string n in FxPrefabBuilder.OneShotNames) yield return n;
            foreach (string n in FxPrefabBuilder.SkillOneShotNames) yield return n;
        }

        private static System.Collections.Generic.IEnumerable<string> AllNames()
        {
            foreach (string n in OneShotNames()) yield return n;
            foreach (string n in LoopingNames()) yield return n;
        }
    }
}
