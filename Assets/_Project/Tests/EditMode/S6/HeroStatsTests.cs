using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using FateBastion.Core;
using FateBastion.Heroes;

namespace FateBastion.Tests.EditMode.S6
{
    /// <summary>Rarity and level stat calculations use configured data rather than embedded balance numbers.</summary>
    public class HeroStatsTests
    {
        private HeroStatsSettings _settings;
        private HeroData _hero;
        private GameObject _heroObject;
        private HeroStats _stats;

        [SetUp]
        public void SetUp()
        {
            _settings = ScriptableObject.CreateInstance<HeroStatsSettings>();
            var serialized = new SerializedObject(_settings);
            serialized.FindProperty("_commonDamageMultiplier").floatValue = 1f;
            serialized.FindProperty("_rareDamageMultiplier").floatValue = 1.6f;
            serialized.FindProperty("_epicDamageMultiplier").floatValue = 2.5f;
            serialized.FindProperty("_legendaryDamageMultiplier").floatValue = 4f;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            _hero = ScriptableObject.CreateInstance<HeroData>();
            _hero.damage = 12f;
            _hero.attacksPerSecond = 1.25f;
            _hero.range = 9f;

            _heroObject = new GameObject("HeroStatsTest");
            _stats = _heroObject.AddComponent<HeroStats>();
            var statsSerialized = new SerializedObject(_stats);
            statsSerialized.FindProperty("_settings").objectReferenceValue = _settings;
            statsSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_hero);
            UnityEngine.Object.DestroyImmediate(_settings);
            UnityEngine.Object.DestroyImmediate(_heroObject);
        }

        [TestCase(Rarity.Common, 12f)]
        [TestCase(Rarity.Rare, 19.2f)]
        [TestCase(Rarity.Epic, 30f)]
        [TestCase(Rarity.Legendary, 48f)]
        public void Initialize_RarityMultiplierApplied(Rarity rarity, float expectedDamage)
        {
            _hero.rarity = rarity;
            _stats.Initialize(_hero);

            Assert.AreEqual(expectedDamage, _stats.Damage, 1e-5f);
            Assert.AreEqual(1.25f, _stats.AttacksPerSecond, 1e-5f);
            Assert.AreEqual(9f, _stats.Range, 1e-5f);
        }

        [Test]
        public void SetLevelMultipliers_AppliesToAllStats()
        {
            _hero.rarity = Rarity.Epic;
            _stats.Initialize(_hero);
            _stats.SetLevelMultipliers(1.4f, 1.1f, 1.1f);

            Assert.AreEqual(42f, _stats.Damage, 1e-5f);
            Assert.AreEqual(1.375f, _stats.AttacksPerSecond, 1e-5f);
            Assert.AreEqual(9.9f, _stats.Range, 1e-5f);
        }

        [Test]
        public void ResetLevelMultipliers_RestoresBaseStatsWithoutClearingHero()
        {
            _hero.rarity = Rarity.Rare;
            _stats.Initialize(_hero);
            _stats.SetLevelMultipliers(2f, 2f, 2f);

            _stats.ResetLevelMultipliers();

            Assert.AreEqual(19.2f, _stats.Damage, 1e-5f);
            Assert.AreEqual(1.25f, _stats.AttacksPerSecond, 1e-5f);
            Assert.AreEqual(9f, _stats.Range, 1e-5f);
        }

        [Test]
        public void Initialize_UnconfiguredRarityMultiplier_Throws()
        {
            var serialized = new SerializedObject(_settings);
            serialized.FindProperty("_rareDamageMultiplier").floatValue = 0f;
            LogAssert.Expect(LogType.Error,
                ": set a positive damage multiplier for every rarity in HeroStatsSettings.");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            _hero.rarity = Rarity.Rare;

            Assert.Throws<InvalidOperationException>(() => _stats.Initialize(_hero));
        }

        [Test]
        public void SetLevelMultipliers_NonPositiveValue_Throws()
        {
            _stats.Initialize(_hero);

            Assert.Throws<ArgumentOutOfRangeException>(() => _stats.SetLevelMultipliers(0f, 1f, 1f));
        }
    }
}
