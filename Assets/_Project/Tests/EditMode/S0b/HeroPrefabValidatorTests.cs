using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using FateBastion.Editor.Heroes;
using FateBastion.Heroes;

namespace FateBastion.Tests.EditMode.S0b
{
    /// <summary>Tests for HeroVisual and Tools > Validate Hero Prefabs (S0b).</summary>
    public class HeroPrefabValidatorTests
    {
        private const string InsideHeroesFolder = "Assets/_Project/Prefabs/Heroes/Hero_Test.prefab";
        private const string OutsideHeroesFolder = "Assets/_Project/Prefabs/Enemies/Hero_Test.prefab";

        private readonly List<Object> _created = new List<Object>();
        private readonly List<HeroValidationIssue> _issues = new List<HeroValidationIssue>();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _created.Count; i++)
            {
                if (_created[i] != null)
                {
                    Object.DestroyImmediate(_created[i]);
                }
            }

            _created.Clear();
            _issues.Clear();
        }

        // ---------- HeroVisual ----------

        [Test]
        public void CollectMissingFields_AllAssigned_ReturnsZero()
        {
            HeroVisual visual = CreateHeroPrefab(out _);
            var missing = new List<string>();

            Assert.AreEqual(0, visual.CollectMissingFields(missing));
            Assert.IsEmpty(missing);
        }

        [Test]
        public void CollectMissingFields_NoneAssigned_ListsAllFourFields()
        {
            var go = Track(new GameObject("Model"));
            var visual = go.AddComponent<HeroVisual>();
            var missing = new List<string>();

            Assert.AreEqual(4, visual.CollectMissingFields(missing));
            CollectionAssert.AreEquivalent(
                new[] { HeroVisual.MuzzleField, HeroVisual.OverheadField, HeroVisual.AuraAnchorField, HeroVisual.AnimatorField },
                missing);
        }

        [Test]
        public void Properties_Assigned_ReturnSerializedReferences()
        {
            HeroVisual visual = CreateHeroPrefab(out _);

            Assert.IsNotNull(visual.Muzzle);
            Assert.IsNotNull(visual.Overhead);
            Assert.IsNotNull(visual.AuraAnchor);
            Assert.IsNotNull(visual.Animator);
            Assert.AreEqual("Muzzle", visual.Muzzle.name);
        }

        // ---------- Validator ----------

        [Test]
        public void Validate_AllFieldsAssigned_NoIssues()
        {
            HeroData hero = CreateHero("hero_cung_thu", withIcon: true);
            CreateHeroPrefab(out GameObject prefab);
            hero.prefab = prefab;

            HeroPrefabValidator.ValidateHero(hero, InsideHeroesFolder, _issues);

            Assert.IsEmpty(_issues);
        }

        [Test]
        public void Validate_PrefabNull_ReportsError()
        {
            HeroData hero = CreateHero("hero_cung_thu", withIcon: true);

            HeroPrefabValidator.ValidateHero(hero, null, _issues);

            Assert.AreEqual(1, CountErrors());
            StringAssert.Contains("hero_cung_thu", _issues[0].HeroId);
            StringAssert.Contains("prefab", _issues[0].Message);
        }

        [Test]
        public void Validate_NoHeroVisualInChildren_ReportsError()
        {
            HeroData hero = CreateHero("hero_cung_thu", withIcon: true);
            var root = Track(new GameObject("Hero_Test"));
            new GameObject("ModelRoot").transform.SetParent(root.transform);
            hero.prefab = root;

            HeroPrefabValidator.ValidateHero(hero, InsideHeroesFolder, _issues);

            Assert.AreEqual(1, CountErrors());
            StringAssert.Contains(nameof(HeroVisual), _issues[0].Message);
        }

        [Test]
        public void Validate_MuzzleMissing_ReportsErrorNamingMuzzle()
        {
            HeroData hero = CreateHero("hero_cung_thu", withIcon: true);
            HeroVisual visual = CreateHeroPrefab(out GameObject prefab);
            SetField(visual, "_muzzle", null);
            hero.prefab = prefab;

            HeroPrefabValidator.ValidateHero(hero, InsideHeroesFolder, _issues);

            Assert.AreEqual(1, CountErrors());
            Assert.AreEqual("hero_cung_thu", _issues[0].HeroId);
            StringAssert.Contains(HeroVisual.MuzzleField, _issues[0].Message);
            StringAssert.DoesNotContain(HeroVisual.OverheadField, _issues[0].Message);
        }

        [Test]
        public void Validate_AllFourFieldsMissing_ReportsFourErrors()
        {
            HeroData hero = CreateHero("hero_cung_thu", withIcon: true);
            var root = Track(new GameObject("Hero_Test"));
            var model = new GameObject("Model");
            model.transform.SetParent(root.transform);
            model.AddComponent<HeroVisual>();
            hero.prefab = root;

            HeroPrefabValidator.ValidateHero(hero, InsideHeroesFolder, _issues);

            Assert.AreEqual(4, CountErrors());
        }

        [Test]
        public void Validate_AnimatorOnPrefabRootNotModel_IsValid()
        {
            HeroData hero = CreateHero("hero_cung_thu", withIcon: true);
            HeroVisual visual = CreateHeroPrefab(out GameObject prefab);
            Animator rootAnimator = prefab.AddComponent<Animator>();
            SetField(visual, "_animator", rootAnimator);
            hero.prefab = prefab;

            HeroPrefabValidator.ValidateHero(hero, InsideHeroesFolder, _issues);

            Assert.IsEmpty(_issues);
        }

        [Test]
        public void Validate_IconMissing_ReportsWarningOnly()
        {
            HeroData hero = CreateHero("hero_cung_thu", withIcon: false);
            CreateHeroPrefab(out GameObject prefab);
            hero.prefab = prefab;

            HeroPrefabValidator.ValidateHero(hero, InsideHeroesFolder, _issues);

            Assert.AreEqual(0, CountErrors());
            Assert.AreEqual(1, _issues.Count);
            Assert.AreEqual(HeroValidationSeverity.Warning, _issues[0].Severity);
            StringAssert.Contains("icon", _issues[0].Message);
        }

        [Test]
        public void Validate_PrefabOutsideHeroesFolder_ReportsWarning()
        {
            HeroData hero = CreateHero("hero_cung_thu", withIcon: true);
            CreateHeroPrefab(out GameObject prefab);
            hero.prefab = prefab;

            HeroPrefabValidator.ValidateHero(hero, OutsideHeroesFolder, _issues);

            Assert.AreEqual(0, CountErrors());
            Assert.AreEqual(1, _issues.Count);
            Assert.AreEqual(HeroValidationSeverity.Warning, _issues[0].Severity);
        }

        [Test]
        public void Validate_ErrorIssue_ContextPointsToHeroData()
        {
            HeroData hero = CreateHero("hero_cung_thu", withIcon: true);

            HeroPrefabValidator.ValidateHero(hero, null, _issues);

            Assert.AreSame(hero, _issues[0].Context);
        }

        [Test]
        public void Validate_SeveralHeroes_CountsErrorsAndWarnings()
        {
            HeroData ok = CreateHero("hero_ok", withIcon: true);
            CreateHeroPrefab(out GameObject okPrefab);
            ok.prefab = okPrefab;
            HeroData noPrefab = CreateHero("hero_no_prefab", withIcon: false);

            var heroes = new List<HeroData> { ok, noPrefab };
            HeroValidationSummary summary = HeroPrefabValidator.Validate(heroes, _ => InsideHeroesFolder, _issues);

            Assert.AreEqual(1, summary.ErrorCount);
            Assert.AreEqual(1, summary.WarningCount);
            Assert.AreEqual(2, summary.HeroCount);
        }

        [Test]
        public void Validate_NullHeroInList_IsSkipped()
        {
            var heroes = new List<HeroData> { null };

            HeroValidationSummary summary = HeroPrefabValidator.Validate(heroes, _ => InsideHeroesFolder, _issues);

            Assert.AreEqual(0, summary.ErrorCount);
            Assert.IsEmpty(_issues);
        }

        // ---------- Render Hero Icons ----------

        [Test]
        public void GetIconAssetPath_HeroId_ReturnsArtIconsPng()
        {
            Assert.AreEqual("Assets/_Project/Art/Icons/hero_cung_thu.png",
                HeroIconRenderer.GetIconAssetPath("hero_cung_thu"));
        }

        [Test]
        public void GetIconAssetPath_CalledTwice_ReturnsSamePathSoRerunOverwrites()
        {
            Assert.AreEqual(HeroIconRenderer.GetIconAssetPath("hero_linh_bang"),
                HeroIconRenderer.GetIconAssetPath("hero_linh_bang"));
        }

        // ---------- helpers ----------

        private T Track<T>(T obj) where T : Object
        {
            _created.Add(obj);
            return obj;
        }

        private HeroData CreateHero(string id, bool withIcon)
        {
            HeroData hero = Track(ScriptableObject.CreateInstance<HeroData>());
            hero.id = id;
            if (withIcon)
            {
                Texture2D tex = Track(new Texture2D(2, 2));
                hero.icon = Track(Sprite.Create(tex, new Rect(0, 0, 2, 2), Vector2.zero));
            }

            return hero;
        }

        /// <summary>Builds Hero_Test > ModelRoot > Model(HeroVisual, Animator) + Muzzle/Overhead/AuraAnchor, all fields assigned.</summary>
        private HeroVisual CreateHeroPrefab(out GameObject root)
        {
            root = Track(new GameObject("Hero_Test"));
            var modelRoot = new GameObject("ModelRoot");
            modelRoot.transform.SetParent(root.transform);
            var model = new GameObject("Model");
            model.transform.SetParent(modelRoot.transform);

            var muzzle = new GameObject("Muzzle").transform;
            var overhead = new GameObject("Overhead").transform;
            var aura = new GameObject("AuraAnchor").transform;
            muzzle.SetParent(model.transform);
            overhead.SetParent(model.transform);
            aura.SetParent(model.transform);

            Animator animator = model.AddComponent<Animator>();
            var visual = model.AddComponent<HeroVisual>();
            SetField(visual, "_muzzle", muzzle);
            SetField(visual, "_overhead", overhead);
            SetField(visual, "_auraAnchor", aura);
            SetField(visual, "_animator", animator);
            return visual;
        }

        private static void SetField(HeroVisual visual, string fieldName, Object value)
        {
            var so = new SerializedObject(visual);
            SerializedProperty prop = so.FindProperty(fieldName);
            Assert.IsNotNull(prop, "Missing serialized field " + fieldName);
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private int CountErrors()
        {
            int count = 0;
            for (int i = 0; i < _issues.Count; i++)
            {
                if (_issues[i].Severity == HeroValidationSeverity.Error)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
