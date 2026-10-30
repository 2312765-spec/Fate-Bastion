using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using FateBastion.Enemies;
using FateBastion.Tests.PlayMode.S2;
using FateBastion.UI.World;

namespace FateBastion.Tests.PlayMode.UI
{
    /// <summary>Enemies flash white through a MaterialPropertyBlock when damaged, then go back to normal (S11, C-14).</summary>
    public class HitFlashPlayModeTests
    {
        private WorldUiTestRig _rig;

        [TearDown]
        public void TearDown()
        {
            _rig?.Dispose();
            _rig = null;
        }

        /// <summary>Enemy whose template has a cube model with HitFlash, like C's enemy variants.</summary>
        private EnemyController SpawnFlashingEnemy(out Renderer renderer)
        {
            EnemyData data = _rig.CreateStillEnemy("normal", baseHP: 100f);
            var model = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(model.GetComponent<Collider>());
            model.transform.SetParent(data.prefab.transform, false);
            model.AddComponent<HitFlash>();

            EnemyController enemy = _rig.SpawnAt(data, 10f);
            renderer = enemy.GetComponentInChildren<Renderer>();
            return enemy;
        }

        [UnityTest]
        public IEnumerator HealthDecreased_FlashesThenClearsPropertyBlock()
        {
            _rig = new WorldUiTestRig();
            EnemyController enemy = SpawnFlashingEnemy(out Renderer renderer);
            yield return null;

            enemy.TakeDamage(EnemyTestRig.TrueHit(10f));

            Assert.IsTrue(renderer.HasPropertyBlock(), "Trúng đòn phải nháy.");
            Assert.AreEqual(1, _rig.Root.HealthBars.FlashingCount);

            yield return new WaitForSecondsRealtime(_rig.Settings.HitFlashDuration + 0.1f);

            Assert.IsFalse(renderer.HasPropertyBlock(), "Hết nháy phải gỡ PropertyBlock để quay lại SRP Batcher.");
            Assert.AreEqual(0, _rig.Root.HealthBars.FlashingCount);
        }

        [UnityTest]
        public IEnumerator HealthUnchangedOnSpawn_NoFlash()
        {
            _rig = new WorldUiTestRig();
            SpawnFlashingEnemy(out Renderer renderer);
            yield return null;

            Assert.IsFalse(renderer.HasPropertyBlock());
            Assert.AreEqual(0, _rig.Root.HealthBars.FlashingCount);
        }

        [UnityTest]
        public IEnumerator Despawn_WhileFlashing_ClearsFlash()
        {
            _rig = new WorldUiTestRig();
            EnemyController enemy = SpawnFlashingEnemy(out Renderer renderer);
            yield return null;

            enemy.TakeDamage(EnemyTestRig.TrueHit(10f));
            _rig.Enemies.KillAllActive();
            yield return null;

            Assert.IsFalse(renderer.HasPropertyBlock());
            Assert.AreEqual(0, _rig.Root.HealthBars.FlashingCount);
        }

        [UnityTest]
        public IEnumerator HitTwice_StaysOneFlash()
        {
            _rig = new WorldUiTestRig();
            EnemyController enemy = SpawnFlashingEnemy(out _);
            yield return null;

            enemy.TakeDamage(EnemyTestRig.TrueHit(10f));
            enemy.TakeDamage(EnemyTestRig.TrueHit(10f));

            Assert.AreEqual(1, _rig.Root.HealthBars.FlashingCount);
        }

        [UnityTest]
        public IEnumerator EnemyWithoutHitFlash_HitDoesNothing()
        {
            _rig = new WorldUiTestRig();
            EnemyController enemy = _rig.SpawnAt(_rig.CreateStillEnemy("plain", baseHP: 100f), 10f);
            yield return null;

            enemy.TakeDamage(EnemyTestRig.TrueHit(10f));

            Assert.AreEqual(0, _rig.Root.HealthBars.FlashingCount);
        }
    }
}
