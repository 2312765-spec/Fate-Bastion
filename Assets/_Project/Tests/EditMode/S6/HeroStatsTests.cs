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
        private HeroData _auraType;
        private HeroData _otherBuffType;
        private GameObject _heroObject;
        private GameObject _buffSourceA;
        private GameObject _buffSourceB;
        private GameObject _buffSourceC;
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
            serialized.FindProperty("_attackSpeedBuffCap").floatValue = 0.6f;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            _hero = ScriptableObject.CreateInstance<HeroData>();
            _hero.damage = 12f;
            _hero.attacksPerSecond = 1.25f;
            _hero.range = 9f;
            _auraType = ScriptableObject.CreateInstance<HeroData>();
            _otherBuffType = ScriptableObject.CreateInstance<HeroData>();

            _heroObject = new GameObject("HeroStatsTest");
            _stats = _heroObject.AddComponent<HeroStats>();
            var statsSerialized = new SerializedObject(_stats);
            statsSerialized.FindProperty("_settings").objectReferenceValue = _settings;
            statsSerialized.ApplyModifiedPropertiesWithoutUndo();

            _buffSourceA = new GameObject("BuffSourceA");
            _buffSourceB = new GameObject("BuffSourceB");
            _buffSourceC = new GameObject("BuffSourceC");
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_hero);
            UnityEngine.Object.DestroyImmediate(_auraType);
            UnityEngine.Object.DestroyImmediate(_otherBuffType);
            UnityEngine.Object.DestroyImmediate(_settings);
            UnityEngine.Object.DestroyImmediate(_buffSourceA);
            UnityEngine.Object.DestroyImmediate(_buffSourceB);
            UnityEngine.Object.DestroyImmediate(_buffSourceC);
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

        [Test]
        public void SetBuff_TwoSourcesOfSameType_UsesStrongestAttackSpeedBonus()
        {
            _stats.Initialize(_hero);
            _stats.SetBuff(_buffSourceA, _hero, BuffType.AttackSpeed, 0.3f);
            _stats.SetBuff(_buffSourceB, _hero, BuffType.AttackSpeed, 0.2f);

            Assert.AreEqual(1.625f, _stats.AttacksPerSecond, 1e-5f);
        }

        [Test]
        public void SetBuff_ReplaceExistingSource_RecalculatesAttackSpeedBonus()
        {
            _stats.Initialize(_hero);
            _stats.SetBuff(_buffSourceA, _hero, BuffType.AttackSpeed, 0.3f);

            _stats.SetBuff(_buffSourceA, _hero, BuffType.AttackSpeed, 0.2f);

            Assert.AreEqual(1.5f, _stats.AttacksPerSecond, 1e-5f);
        }

        [Test]
        public void SetBuff_SupportAndAuraOfDifferentTypes_AddTheirAttackSpeedBonuses()
        {
            _stats.Initialize(_hero);
            _stats.SetBuff(_buffSourceA, _hero, BuffType.AttackSpeed, 0.3f);
            _stats.SetBuff(_buffSourceB, _auraType, BuffType.AttackSpeed, 0.25f);

            Assert.AreEqual(1.9375f, _stats.AttacksPerSecond, 1e-5f);
        }

        [Test]
        public void SetBuff_AttackSpeedSourcesExceedingCap_AreLimitedToConfiguredCap()
        {
            _stats.Initialize(_hero);
            _stats.SetBuff(_buffSourceA, _hero, BuffType.AttackSpeed, 0.3f);
            _stats.SetBuff(_buffSourceB, _auraType, BuffType.AttackSpeed, 0.25f);
            _stats.SetBuff(_buffSourceC, _otherBuffType, BuffType.AttackSpeed, 0.2f);

            Assert.AreEqual(2f, _stats.AttacksPerSecond, 1e-5f);
        }

        [Test]
        public void RemoveBuff_StrongestSameTypeSource_RemainingContributionIsRecalculated()
        {
            _stats.Initialize(_hero);
            _stats.SetBuff(_buffSourceA, _hero, BuffType.AttackSpeed, 0.3f);
            _stats.SetBuff(_buffSourceB, _hero, BuffType.AttackSpeed, 0.2f);

            _stats.RemoveBuff(_buffSourceA);

            Assert.AreEqual(1.5f, _stats.AttacksPerSecond, 1e-5f);
        }

        [Test]
        public void SetBuff_DamageSourcesExceedingAttackSpeedCap_AreNotCapped()
        {
            _stats.Initialize(_hero);
            _stats.SetBuff(_buffSourceA, _hero, BuffType.Damage, 0.8f);
            _stats.SetBuff(_buffSourceB, _auraType, BuffType.Damage, 0.8f);

            Assert.AreEqual(31.2f, _stats.Damage, 1e-5f);
        }
    }
}
