using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using FateBastion.Combat;
using FateBastion.Core;
using FateBastion.Editor.Heroes;
using FateBastion.Editor.UI;
using FateBastion.Heroes;
using FateBastion.Skills;

namespace FateBastion.Tests.EditMode.S0b
{
    /// <summary>Runs Build Hero Variants into a temporary folder with a generated 2 m test model (C-08).</summary>
    public class HeroVariantBuilderTests
    {
        private const string TempFolder = "Assets/_HeroVariantBuilderTest";
        private const string ControllerPath = "Assets/_Project/Art/Animations/Heroes/AC_Hero_Base.controller";
        private const string VariantName = "Test";
        private const float ModelHeight = 2f;
        private const float Tolerance = 0.001f;

        private HeroArtCatalog _catalog;
        private HeroArtEntry _entry;
        private HeroData _hero;

        private string VariantPath => HeroVariantBuilder.GetVariantPath(TempFolder, VariantName);

        [SetUp]
        public void SetUp()
        {
            AssetDatabase.DeleteAsset(TempFolder);
            AssetDatabase.CreateFolder("Assets", TempFolder.Substring("Assets/".Length));
            Assume.That(AssetDatabase.LoadAssetAtPath<GameObject>(HeroVariantBuilder.BasePrefabPath), Is.Not.Null,
                "Thiếu Hero_Base của A.");
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
            Assume.That(controller, Is.Not.Null, $"Thiếu {ControllerPath}.");

            var animations = new AnimatorOverrideController(controller);
            AssetDatabase.CreateAsset(animations, TempFolder + "/AOC_Test.overrideController");

            _catalog = ScriptableObject.CreateInstance<HeroArtCatalog>();
            _entry = new HeroArtEntry
            {
                heroId = "hero_test",
                variantName = VariantName,
                model = CreateTestModel(),
                animations = animations,
                // Generated model is not Humanoid: no bone, Muzzle uses the fallback point.
                muzzleBone = HumanBodyBones.LastBone
            };
            _catalog.heroes.Add(_entry);

            _hero = ScriptableObject.CreateInstance<HeroData>();
            _hero.id = _entry.heroId;
            _hero.rarity = Rarity.Epic;
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(TempFolder);
            Object.DestroyImmediate(_catalog);
            Object.DestroyImmediate(_hero);
        }

        [Test]
        public void Build_NewHero_CreatesVariantOfHeroBase()
        {
            Assert.IsTrue(Build());

            var variant = AssetDatabase.LoadAssetAtPath<GameObject>(VariantPath);
            Assert.IsNotNull(variant);
            Assert.AreEqual(PrefabAssetType.Variant, PrefabUtility.GetPrefabAssetType(variant));
            Assert.AreEqual(HeroVariantBuilder.BasePrefabPath,
                AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(variant)));
        }

        [Test]
        public void Build_NewHero_AssignsHeroDataPrefab()
        {
            Build();

            Assert.AreSame(AssetDatabase.LoadAssetAtPath<GameObject>(VariantPath), _hero.prefab);
        }

        [Test]
        public void Build_NewHero_HeroVisualHasAllFourFields()
        {
            Build();

            var visual = _hero.prefab.GetComponentInChildren<HeroVisual>(true);
            Assert.IsNotNull(visual);
            var missing = new System.Collections.Generic.List<string>();
            Assert.AreEqual(0, visual.CollectMissingFields(missing), string.Join(", ", missing));
            Assert.AreSame(_entry.animations, visual.Animator.runtimeAnimatorController);
        }

        [Test]
        public void Build_NewHero_AnchorsFollowModelBounds()
        {
            Build();

            var visual = _hero.prefab.GetComponentInChildren<HeroVisual>(true);
            Assert.That(visual.Overhead.position.y, Is.EqualTo(ModelHeight + _catalog.overheadMargin).Within(Tolerance));
            Assert.That(visual.AuraAnchor.position.y, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(visual.Muzzle.position.y, Is.EqualTo(ModelHeight * _catalog.fallbackMuzzleHeight).Within(Tolerance));
        }

        [Test]
        public void Build_Twice_KeepsOneModelAndOneVisual()
        {
            Build();
            Build();

            Transform modelRoot = _hero.prefab.transform.Find(HeroVariantBuilder.ModelRootName);
            Assert.AreEqual(1, modelRoot.childCount);
            Assert.AreEqual(1, _hero.prefab.GetComponentsInChildren<HeroVisual>(true).Length);
            Assert.AreEqual(1, _hero.prefab.GetComponentsInChildren<HeroBuffIndicator>(true).Length);
        }

        [Test]
        public void Build_ModelScale_AppliedToModelRootNotHeroRoot()
        {
            _entry.modelScale = 1.3f;

            Build();

            Assert.AreEqual(Vector3.one, _hero.prefab.transform.localScale);
            Assert.That(_hero.prefab.transform.Find(HeroVariantBuilder.ModelRootName).localScale.y, Is.EqualTo(1.3f).Within(Tolerance));
        }

        [Test]
        public void Build_EpicHero_PutsEpicAuraUnderAuraAnchor()
        {
            Build();

            var visual = _hero.prefab.GetComponentInChildren<HeroVisual>(true);
            Assert.AreEqual(1, visual.AuraAnchor.childCount);
            Assert.AreEqual(FxPrefabBuilder.AuraNames[(int)Rarity.Epic], visual.AuraAnchor.GetChild(0).name);
        }

        [Test]
        public void Build_ProjectileEntry_AssignsSkillRunnerProjectile()
        {
            _entry.projectile = AssetDatabase.LoadAssetAtPath<GameObject>(
                FxPrefabBuilder.GetPrefabPath(FxPrefabBuilder.PrefabFolder, "Projectile_Ice"));
            Assume.That(_entry.projectile, Is.Not.Null, "Thiếu Projectile_Ice (Tools > FX).");

            Build();

            var runner = new SerializedObject(_hero.prefab.GetComponent<SkillRunner>());
            Assert.AreSame(_entry.projectile.GetComponent<Projectile>(),
                runner.FindProperty("_projectilePrefab").objectReferenceValue);
        }

        [Test]
        public void Build_MissingModel_ReturnsFalseAndCreatesNothing()
        {
            _entry.model = null;

            LogAssert.Expect(LogType.Error, new Regex("required"));
            Assert.IsFalse(Build());

            Assert.IsNull(AssetDatabase.LoadAssetAtPath<GameObject>(VariantPath));
            Assert.IsNull(_hero.prefab);
        }

        private bool Build() =>
            HeroVariantBuilder.Build(_entry, _hero, _catalog, HeroVariantBuilder.BasePrefabPath, TempFolder,
                FxPrefabBuilder.PrefabFolder);

        // A cube 2 m tall standing on the origin, with an Animator but no avatar.
        private static GameObject CreateTestModel()
        {
            var root = new GameObject("TestModel");
            root.AddComponent<Animator>();
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(body.GetComponent<Collider>());
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, ModelHeight * 0.5f, 0f);
            body.transform.localScale = new Vector3(0.6f, ModelHeight, 0.6f);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, TempFolder + "/TestModel.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }
    }
}
