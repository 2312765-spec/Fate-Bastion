using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using FateBastion.Core;
using FateBastion.Heroes;

namespace FateBastion.Tests.PlayMode.S6
{
    /// <summary>HeroController initializes, resets and releases pooled hero state without requiring a model.</summary>
    public class HeroControllerPlayModeTests
    {
        private HeroStatsSettings _settings;
        private HeroRageSettings _rageSettings;
        private HeroData _hero;
        private GameObject _root;
        private HeroController _controller;

        [SetUp]
        public void SetUp()
        {
            CombatEvents.ResetAll();
            _settings = ScriptableObject.CreateInstance<HeroStatsSettings>();
            SetField(_settings, "_commonDamageMultiplier", 1f);
            SetField(_settings, "_rareDamageMultiplier", 1.6f);
            SetField(_settings, "_epicDamageMultiplier", 2.5f);
            SetField(_settings, "_legendaryDamageMultiplier", 4f);
            _rageSettings = ScriptableObject.CreateInstance<HeroRageSettings>();

            _hero = ScriptableObject.CreateInstance<HeroData>();
            _hero.id = "hero_test";
            _hero.damage = 12f;
            _hero.attacksPerSecond = 1f;
            _hero.range = 6f;
            _hero.rarity = Rarity.Common;

            _root = new GameObject("HeroControllerTest");
            _root.SetActive(false);
            HeroStats stats = _root.AddComponent<HeroStats>();
            SetField(stats, "_settings", _settings);
            _controller = _root.AddComponent<HeroController>();
            SetField(_controller, "_rageSettings", _rageSettings);
            LogAssert.Expect(LogType.Error,
                "HeroControllerTest: no HeroVisual found under this hero; using the hero root as the visual anchor.");
            _root.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            CombatEvents.ResetAll();
            Object.Destroy(_root);
            Object.Destroy(_hero);
            Object.Destroy(_settings);
            Object.Destroy(_rageSettings);
        }

        [Test]
        public void Init_ValidData_InitializesStatsAndUsesRootFallback()
        {
            _controller.Init(_hero);

            Assert.AreSame(_hero, _controller.Data);
            Assert.AreEqual(12f, _controller.Stats.Damage, 1e-5f);
            Assert.AreSame(_root.transform, _controller.Muzzle);
        }

        [Test]
        public void ResetState_ClearsTargetAndRestoresBaseMultipliers()
        {
            _controller.Init(_hero);
            _controller.Stats.SetLevelMultipliers(2f, 2f, 2f);

            _controller.ResetState();

            Assert.AreSame(_hero, _controller.Data);
            Assert.AreEqual(12f, _controller.Stats.Damage, 1e-5f);
            Assert.AreEqual(1f, _controller.Stats.AttacksPerSecond, 1e-5f);
        }

        [Test]
        public void OnDespawn_ClearsDataAndStats()
        {
            _controller.Init(_hero);

            _controller.OnDespawn();

            Assert.IsNull(_controller.Data);
            Assert.Throws<System.InvalidOperationException>(() => _ = _controller.Stats.Damage);
        }

        [Test]
        public void TryActivateUltimate_BeforeRageIsFull_ReturnsFalse()
        {
            _hero.rarity = Rarity.Legendary;
            _controller.Init(_hero);

            Assert.IsFalse(_controller.TryActivateUltimate());
            Assert.AreEqual(0f, _controller.CurrentRage);
        }

        private static void SetField<T>(object instance, string name, T value)
        {
            FieldInfo field = instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Missing serialized field {name}.");
            field.SetValue(instance, value);
        }
    }
}
