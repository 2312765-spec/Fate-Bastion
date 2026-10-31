using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using FateBastion.Audio;
using FateBastion.Core;

namespace FateBastion.Tests.PlayMode.S11
{
    /// <summary>Pooled hit, death and meteor effects from Core events: caps, spacing, pool return, pause (C-09).</summary>
    public class FxServicePlayModeTests
    {
        private const float FxLifetimeSeconds = 0.1f;
        private const int HitCap = 5;

        private GameObject _go;
        private GameObject _template;
        private FxService _service;
        private FxSettings _settings;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            CombatEvents.ResetAll();
            AbilityEvents.ResetAll();
            Time.timeScale = 1f;

            _template = CreateTemplate();
            _settings = ScriptableObject.CreateInstance<FxSettings>();
            foreach (FxKind kind in System.Enum.GetValues(typeof(FxKind)))
            {
                FxEntry entry = _settings.Get(kind);
                entry.prefab = _template.GetComponent<ParticleSystem>();
                entry.minInterval = 0f;
            }
            _settings.hitFire.maxConcurrent = HitCap;

            _go = new GameObject("_Audio");
            _go.SetActive(false);
            _service = _go.AddComponent<FxService>();
            _service.Configure(_settings);
            _go.SetActive(true); // Awake: pools filled.

            yield return null; // Start: subscribe.
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            CombatEvents.ResetAll();
            AbilityEvents.ResetAll();
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_template);
            Object.DestroyImmediate(_settings);
        }

        [Test]
        public void MeteorExploded_PlaysExplosionAtPointScaledByRadius()
        {
            var point = new Vector3(3f, 0f, 7f);

            AbilityEvents.RaiseMeteorExploded(point, 4f);

            Assert.AreEqual(1, _service.CountPlaying(FxKind.MeteorExplosion));
            Transform fx = FirstActiveChild();
            Assert.AreEqual(point, fx.position);
            Assert.AreEqual(Vector3.one * 4f, fx.localScale);
        }

        [Test]
        public void DamageDealt_HundredInOneFrame_NoMoreThanCap()
        {
            for (int i = 0; i < 100; i++) RaiseHit(DamageType.Magic, Element.Fire);

            Assert.AreEqual(HitCap, _service.CountPlaying(FxKind.HitFire));
            Assert.AreEqual(HitCap, CountActiveChildren());
        }

        [Test]
        public void DamageDealt_WithinMinInterval_OnlyFirstPlays()
        {
            _settings.hitIce.minInterval = 10f;

            for (int i = 0; i < 3; i++) RaiseHit(DamageType.Magic, Element.Ice);

            Assert.AreEqual(1, _service.CountPlaying(FxKind.HitIce));
        }

        [Test]
        public void DamageDealt_TrueDamage_NoEffect()
        {
            RaiseHit(DamageType.True, Element.Fire);

            Assert.AreEqual(0, CountActiveChildren());
        }

        [Test]
        public void EnemyKilled_Boss_PlaysBossDeath()
        {
            CombatEvents.RaiseEnemyKilled(new EnemyKilledInfo("enemy_boss", Vector3.zero, 150, isBoss: true));

            Assert.AreEqual(1, _service.CountPlaying(FxKind.BossDeath));
            Assert.AreEqual(0, _service.CountPlaying(FxKind.EnemyDeath));
        }

        [UnityTest]
        public IEnumerator Play_AfterLifetime_ReturnsToPool()
        {
            RaiseHit(DamageType.Magic, Element.Lightning);

            yield return new WaitForSeconds(FxLifetimeSeconds * 3f);

            Assert.AreEqual(0, _service.CountPlaying(FxKind.HitLightning));
            Assert.AreEqual(0, CountActiveChildren());
        }

        [UnityTest]
        public IEnumerator Play_Paused_StaysUntilResumed()
        {
            RaiseHit(DamageType.Magic, Element.Lightning);
            Time.timeScale = 0f;

            yield return new WaitForSecondsRealtime(FxLifetimeSeconds * 3f);
            Assert.AreEqual(1, _service.CountPlaying(FxKind.HitLightning), "Tạm dừng thì hiệu ứng phải đứng yên.");

            Time.timeScale = 1f;
            yield return new WaitForSeconds(FxLifetimeSeconds * 3f);
            Assert.AreEqual(0, _service.CountPlaying(FxKind.HitLightning));
        }

        [UnityTest]
        public IEnumerator Play_ManyRounds_NeverInstantiatesAfterLoad()
        {
            int pooled = _go.transform.childCount;

            for (int round = 0; round < 3; round++)
            {
                for (int i = 0; i < 20; i++)
                {
                    RaiseHit(DamageType.Magic, Element.Fire);
                    CombatEvents.RaiseEnemyKilled(new EnemyKilledInfo("enemy_normal", Vector3.zero, 5, isBoss: false));
                }

                yield return new WaitForSeconds(FxLifetimeSeconds * 3f);
            }

            Assert.AreEqual(pooled, _go.transform.childCount);
        }

        private static void RaiseHit(DamageType type, Element element) =>
            CombatEvents.RaiseDamageDealt(new DamageDealtInfo(10, type, element, Vector3.zero, false));

        private static GameObject CreateTemplate()
        {
            var template = new GameObject("FxTemplate");
            template.SetActive(false);
            var system = template.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = system.main;
            main.loop = false;
            main.duration = FxLifetimeSeconds * 0.5f;
            main.startLifetime = FxLifetimeSeconds * 0.5f;
            return template;
        }

        private Transform FirstActiveChild()
        {
            foreach (Transform child in _go.transform)
                if (child.gameObject.activeSelf) return child;
            return null;
        }

        private int CountActiveChildren()
        {
            int count = 0;
            foreach (Transform child in _go.transform)
                if (child.gameObject.activeSelf) count++;
            return count;
        }
    }
}
