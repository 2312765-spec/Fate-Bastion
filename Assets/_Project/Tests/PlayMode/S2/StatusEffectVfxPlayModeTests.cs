using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using FateBastion.Combat;
using FateBastion.Core;
using FateBastion.Enemies;

namespace FateBastion.Tests.PlayMode.S2
{
    /// <summary>Enemy status visuals are pooled with enemies and follow status transitions (C-09 Issue #56).</summary>
    public class StatusEffectVfxPlayModeTests
    {
        private EnemyTestRig _rig;
        private StatusEffectData _status;
        private GameObject _vfxPrefab;

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            _rig = new EnemyTestRig(20f);
            _status = ScriptableObject.CreateInstance<StatusEffectData>();
            _status.id = "status_burn_test";
            _status.element = Element.Fire;
            _status.duration = 0.1f;
            _status.tickInterval = 1f;
            _status.value = 0.2f;

            _vfxPrefab = new GameObject("StatusBurnVfx");
            _vfxPrefab.SetActive(false);
            ParticleSystem particles = _vfxPrefab.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.loop = true;
            main.duration = 1f;
            main.startLifetime = 1f;
            _status.vfx = _vfxPrefab;
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            _rig.Dispose();
            Object.Destroy(_status);
            Object.Destroy(_vfxPrefab);
        }

        [UnityTest]
        public IEnumerator BurnVfx_StatusExpires_DisablesPrewarmedLoop()
        {
            _rig.Manager.RegisterStatusVfx(_status);
            EnemyData data = _rig.CreateEnemy("enemy_status_vfx");
            _rig.Manager.RequestSpawn(data, 0, 1);
            EnemyController enemy = _rig.Manager.ActiveEnemies[0];
            var hit = new DamageInfo(1f, DamageType.Magic, Element.Fire, null, true, _status, Vector3.zero);

            enemy.TakeDamage(hit);

            ParticleSystem statusVfx = enemy.GetComponentInChildren<ParticleSystem>(true);
            Assert.IsNotNull(statusVfx);
            Assert.IsTrue(statusVfx.gameObject.activeSelf);

            yield return new WaitForSeconds(0.2f);

            Assert.IsFalse(statusVfx.gameObject.activeSelf);
        }

        [TestCase(false, 1f)]
        [TestCase(true, 2.5f)]
        public void RegisterStatusVfx_ScaleUsesEnemyTypeAndConfiguredBossMultiplier(bool boss, float expectedScale)
        {
            SetField(_rig.Settings, "_bossStatusVfxScale", 2.5f);
            _rig.Manager.RegisterStatusVfx(_status);
            EnemyData data = _rig.CreateEnemy(boss ? "boss_status_vfx" : "enemy_status_vfx_scale", boss: boss);
            _rig.Manager.Prewarm(data, 1);
            _rig.Manager.RequestSpawn(data, 0, 1);
            EnemyController enemy = _rig.Manager.ActiveEnemies[0];
            ParticleSystem statusVfx = enemy.GetComponentInChildren<ParticleSystem>(true);

            Assert.IsNotNull(statusVfx);
            Assert.AreEqual(Vector3.one * expectedScale, statusVfx.transform.localScale);
        }

        private static void SetField<T>(object target, string name, T value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Missing serialized field {name}.");
            field.SetValue(target, value);
        }
    }
}
