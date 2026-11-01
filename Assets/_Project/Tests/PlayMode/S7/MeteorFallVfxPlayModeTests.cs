using System.Reflection;
using FateBastion.Core;
using FateBastion.Player;
using FateBastion.Skills;
using NUnit.Framework;
using UnityEngine;

namespace FateBastion.Tests.PlayMode.S7
{
    /// <summary>Meteor fall VFX follows the committed impact point and scaled fall state (C-09 Issue #56).</summary>
    public class MeteorFallVfxPlayModeTests
    {
        private GameObject _indicatorObject;
        private GameObject _routerObject;
        private GameObject _vfxPrefab;
        private MeteorSettings _settings;
        private SkillData _skill;
        private MeteorAbility _ability;

        [SetUp]
        public void SetUp()
        {
            CancelStack.ResetAll();
            AbilityEvents.ResetAll();

            _skill = ScriptableObject.CreateInstance<SkillData>();
            _skill.radius = 4f;
            _settings = ScriptableObject.CreateInstance<MeteorSettings>();
            _settings.Configure(_skill, null, 20f, 2f);

            _vfxPrefab = new GameObject("MeteorFallVfx");
            _vfxPrefab.SetActive(false);
            ParticleSystem particles = _vfxPrefab.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.loop = false;
            main.duration = 0.5f;
            main.startLifetime = 0.5f;

            _routerObject = new GameObject("TestInputRouter");
            InputRouter router = _routerObject.AddComponent<InputRouter>();
            _ability = new MeteorAbility(router, 45f, 0.8f);
            _ability.BeginAiming();
            _ability.ConfirmAim(new Vector3(3f, 0f, 7f));

            _indicatorObject = new GameObject("MeteorIndicator");
            _indicatorObject.SetActive(false);
            MeteorAimIndicator indicator = _indicatorObject.AddComponent<MeteorAimIndicator>();
            SetField(indicator, "_settings", _settings);
            SetField(indicator, "_fallFxPrefab", _vfxPrefab);
            SetField(indicator, "_ability", _ability);
            SetField(indicator, "_rangeIndicator", CreateChild("RangeRing").transform);
            SetField(indicator, "_blastPreview", CreateChild("BlastPreview").AddComponent<MeshRenderer>());
            SetField(indicator, "_impactMarker", CreateChild("ImpactMarker").AddComponent<MeshRenderer>());
            Invoke(indicator, "PrepareFallFx");
        }

        [TearDown]
        public void TearDown()
        {
            _ability?.Dispose();
            CancelStack.ResetAll();
            AbilityEvents.ResetAll();
            Object.Destroy(_indicatorObject);
            Object.Destroy(_routerObject);
            Object.Destroy(_vfxPrefab);
            Object.Destroy(_settings);
            Object.Destroy(_skill);
        }

        [Test]
        public void StateChanged_Falling_ShowsScaledVfxAtImpact_ThenHidesIt()
        {
            MeteorAimIndicator indicator = _indicatorObject.GetComponent<MeteorAimIndicator>();
            Invoke(indicator, "OnStateChanged", MeteorAbility.State.Falling);

            GameObject fallVfx = GetField<GameObject>(indicator, "_fallFx");
            Assert.IsNotNull(fallVfx);
            Assert.IsTrue(fallVfx.activeSelf);
            Assert.AreEqual(_ability.ImpactPoint, fallVfx.transform.position);
            Assert.AreEqual(Vector3.one * _skill.radius, fallVfx.transform.localScale);

            Invoke(indicator, "OnStateChanged", MeteorAbility.State.Cooldown);

            Assert.IsFalse(fallVfx.activeSelf);
        }

        private static void SetField<T>(object target, string name, T value)
        {
            typeof(MeteorAimIndicator).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }

        private static T GetField<T>(object target, string name)
        {
            return (T)typeof(MeteorAimIndicator).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        }

        private static void Invoke(object target, string name, params object[] arguments)
        {
            typeof(MeteorAimIndicator).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, arguments);
        }

        private GameObject CreateChild(string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(_indicatorObject.transform, false);
            return child;
        }

    }
}
