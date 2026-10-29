using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using FateBastion.Core;
using FateBastion.Heroes;
using FateBastion.Player;

namespace FateBastion.Tests.PlayMode.S7
{
    /// <summary>Command Aura changes hero attack speed only on membership transitions and teardown.</summary>
    public class CommandAuraPlayModeTests
    {
        private HeroStatsSettings _heroStatsSettings;
        private HeroRageSettings _heroRageSettings;
        private CommandAuraSettings _auraSettings;
        private HeroData _heroData;
        private GameObject _heroObject;
        private HeroController _hero;
        private GameObject _auraObject;
        private CommandAura _aura;

        [SetUp]
        public void SetUp()
        {
            _heroStatsSettings = ScriptableObject.CreateInstance<HeroStatsSettings>();
            SetField(_heroStatsSettings, "_commonDamageMultiplier", 1f);
            SetField(_heroStatsSettings, "_rareDamageMultiplier", 1.6f);
            SetField(_heroStatsSettings, "_epicDamageMultiplier", 2.5f);
            SetField(_heroStatsSettings, "_legendaryDamageMultiplier", 4f);
            _heroRageSettings = ScriptableObject.CreateInstance<HeroRageSettings>();
            _auraSettings = ScriptableObject.CreateInstance<CommandAuraSettings>();
            SetField(_auraSettings, "_radius", 6f);
            SetField(_auraSettings, "_attackSpeedBonus", 0.25f);
            SetField(_auraSettings, "_checkInterval", 0.2f);

            _heroData = ScriptableObject.CreateInstance<HeroData>();
            _heroData.id = "aura_test_hero";
            _heroData.damage = 10f;
            _heroData.attacksPerSecond = 1f;
            _heroData.range = 5f;
            _heroData.rarity = Rarity.Common;

            _heroObject = new GameObject("CommandAuraHero");
            _heroObject.SetActive(false);
            HeroStats stats = _heroObject.AddComponent<HeroStats>();
            SetField(stats, "_settings", _heroStatsSettings);
            _hero = _heroObject.AddComponent<HeroController>();
            SetField(_hero, "_rageSettings", _heroRageSettings);
            LogAssert.Expect(LogType.Error,
                "CommandAuraHero: no HeroVisual found under this hero; using the hero root as the visual anchor.");
            _heroObject.SetActive(true);
            _hero.Init(_heroData);

            _auraObject = new GameObject("CommandAura");
            _auraObject.SetActive(false);
            _aura = _auraObject.AddComponent<CommandAura>();
            SetField(_aura, "_settings", _auraSettings);
            _auraObject.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_auraObject);
            Object.Destroy(_heroObject);
            Object.Destroy(_heroData);
            Object.Destroy(_auraSettings);
            Object.Destroy(_heroRageSettings);
            Object.Destroy(_heroStatsSettings);
        }

        [UnityTest]
        public IEnumerator Aura_EnterExitAndDisable_UpdatesBuffAndPublishesOnlyEntryTransitions()
        {
            int entryEvents = 0;
            _aura.OnHeroEnteredAura += hero =>
            {
                Assert.AreSame(_heroObject, hero);
                entryEvents++;
            };

            yield return new WaitForSeconds(0.25f);
            Assert.AreEqual(1.25f, _hero.Stats.AttacksPerSecond, 1e-5f);
            Assert.AreEqual(1, entryEvents);

            _heroObject.transform.position = Vector3.right * 7f;
            yield return new WaitForSeconds(0.25f);
            Assert.AreEqual(1f, _hero.Stats.AttacksPerSecond, 1e-5f);
            Assert.AreEqual(1, entryEvents);

            _heroObject.transform.position = Vector3.right * 2f;
            yield return new WaitForSeconds(0.25f);
            Assert.AreEqual(1.25f, _hero.Stats.AttacksPerSecond, 1e-5f);
            Assert.AreEqual(2, entryEvents);

            _hero.OnDespawn();
            _hero.Init(_heroData);
            yield return new WaitForSeconds(0.25f);
            Assert.AreEqual(1.25f, _hero.Stats.AttacksPerSecond, 1e-5f);
            Assert.AreEqual(3, entryEvents);

            _aura.enabled = false;
            Assert.AreEqual(1f, _hero.Stats.AttacksPerSecond, 1e-5f);
        }

        private static void SetField<T>(object instance, string name, T value)
        {
            FieldInfo field = instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Missing serialized field {name}.");
            field.SetValue(instance, value);
        }
    }
}
