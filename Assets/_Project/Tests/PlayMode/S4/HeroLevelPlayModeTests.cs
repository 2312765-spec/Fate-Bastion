using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using FateBastion.Core;
using FateBastion.Heroes;

namespace FateBastion.Tests.PlayMode.S4
{
    /// <summary>
    /// HeroLevel (S4, B-04) with A's real HeroController and HeroStats: level 1–3 multipliers reach the stats through
    /// SetLevelMultipliers (TEAM_ASSIGNMENT 3.1 #4), and returning to the pool resets them.
    /// </summary>
    public class HeroLevelPlayModeTests
    {
        private GameObject _hero;
        private HeroData _data;
        private HeroStatsSettings _settings;
        private PlacementRules _rules;
        private HeroLevelTable _table;

        [SetUp]
        public void SetUp()
        {
            _settings = ScriptableObject.CreateInstance<HeroStatsSettings>();
            foreach (string field in new[] { "_commonDamageMultiplier", "_rareDamageMultiplier", "_epicDamageMultiplier", "_legendaryDamageMultiplier" })
            {
                SetPrivate(_settings, field, 1f);
            }

            _data = ScriptableObject.CreateInstance<HeroData>();
            _data.id = "hero_test";
            _data.rarity = Rarity.Epic;
            _data.damage = 10f;
            _data.attacksPerSecond = 1f;
            _data.range = 4f;

            _rules = ScriptableObject.CreateInstance<PlacementRules>();
            _rules.Configure(new[] { 100, 200, 350, 600 }, new[] { 6, 5, 4, 2 }, 15, 0.8f, 0.5f);
            _table = ScriptableObject.CreateInstance<HeroLevelTable>();
            _table.Configure(new[]
            {
                new HeroLevelTable.LevelStep(0.75f, 0.40f, 0.10f, 0.10f),
                new HeroLevelTable.LevelStep(1.50f, 1.00f, 0.20f, 0.15f)
            });

            // Built inactive so every field is set before Awake runs.
            _hero = new GameObject("Hero");
            _hero.SetActive(false);
            var stats = _hero.AddComponent<HeroStats>();
            SetPrivate(stats, "_settings", _settings);
            _hero.AddComponent<HeroController>();
            new GameObject("Visual", typeof(HeroVisual)).transform.SetParent(_hero.transform, false);
            _hero.AddComponent<HeroLevel>();
            _hero.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_hero);
            Object.Destroy(_data);
            Object.Destroy(_settings);
            Object.Destroy(_rules);
            Object.Destroy(_table);
        }

        private static void SetPrivate(object target, string field, object value)
        {
            FieldInfo info = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(info, $"{target.GetType().Name}.{field} not found");
            info.SetValue(target, value);
        }

        [Test]
        public void Levels1To3_ApplyTableMultipliersToStats()
        {
            var controller = _hero.GetComponent<HeroController>();
            var stats = _hero.GetComponent<HeroStats>();
            var level = _hero.GetComponent<HeroLevel>();
            level.Setup(_table, null);
            controller.ResetState();
            controller.Init(_data);
            level.BeginPlaced(2, 350);
            var wallet = new Wallet(2000);

            float damage1 = stats.Damage;
            Assert.AreEqual(1, level.Level);
            Assert.AreEqual(4f, stats.Range, 1e-4f);
            Assert.AreEqual(1f, stats.AttacksPerSecond, 1e-4f);

            Assert.AreEqual(UpgradeError.None, level.TryUpgrade(_rules, wallet, out int cost2));
            Assert.AreEqual(262, cost2);
            Assert.AreEqual(1.4f * damage1, stats.Damage, 1e-3f);
            Assert.AreEqual(1.1f, stats.AttacksPerSecond, 1e-4f);
            Assert.AreEqual(4.4f, stats.Range, 1e-4f);

            Assert.AreEqual(UpgradeError.None, level.TryUpgrade(_rules, wallet, out int cost3));
            Assert.AreEqual(525, cost3);
            Assert.AreEqual(2f * damage1, stats.Damage, 1e-3f);
            Assert.AreEqual(1.2f, stats.AttacksPerSecond, 1e-4f);
            Assert.AreEqual(4.6f, stats.Range, 1e-4f);
            Assert.IsTrue(level.IsTopLevel);
            Assert.AreEqual(1137, level.GoldSpent);
            Assert.AreEqual(UpgradeError.MaxLevel, level.TryUpgrade(_rules, wallet, out _));
        }

        [Test]
        public void ReturnToPool_ResetsLevelAndStats()
        {
            var controller = _hero.GetComponent<HeroController>();
            var level = _hero.GetComponent<HeroLevel>();
            level.Setup(_table, null);
            controller.Init(_data);
            level.BeginPlaced(0, 350);
            level.TryUpgrade(_rules, new Wallet(1000), out _);

            controller.OnDespawn();
            level.Clear();

            Assert.IsFalse(level.IsPlaced);
            Assert.AreEqual(0, level.Level);

            controller.ResetState();
            controller.Init(_data);
            level.BeginPlaced(0, 350);
            Assert.AreEqual(4f, _hero.GetComponent<HeroStats>().Range, 1e-4f, "A reused hero starts again at level 1.");
        }
    }
}
