using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using FateBastion.Combat;
using FateBastion.Enemies;
using FateBastion.Tests.PlayMode.S2;
using FateBastion.UI.World;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace FateBastion.Tests.PlayMode.UI
{
    /// <summary>Health bars attach on OnEnemySpawned, detach on OnEnemyDespawned, follow OnHealthChanged (C-14, 3.1 #9).</summary>
    public class WorldHealthBarPlayModeTests
    {
        private WorldUiTestRig _rig;

        [TearDown]
        public void TearDown()
        {
            _rig?.Dispose();
            _rig = null;
        }

        [UnityTest]
        public IEnumerator Spawn_AttachesOneBarPerEnemy()
        {
            _rig = new WorldUiTestRig();
            EnemyData data = _rig.CreateStillEnemy("normal");

            for (int i = 0; i < 3; i++) _rig.SpawnAt(data, i);
            yield return null;

            Assert.AreEqual(3, _rig.Root.HealthBars.BoundCount);
        }

        [UnityTest]
        public IEnumerator Spawn100_KillAll_EveryBarReturnsToPool()
        {
            _rig = new WorldUiTestRig();
            EnemyData data = _rig.CreateStillEnemy("normal");

            for (int i = 0; i < 100; i++) _rig.SpawnAt(data, i * 0.5f);
            yield return null;
            Assert.AreEqual(100, _rig.Root.HealthBars.BoundCount);

            _rig.Enemies.KillAllActive();
            yield return null;

            Assert.AreEqual(0, _rig.Root.HealthBars.BoundCount);
            Assert.AreEqual(0, _rig.Root.HealthBars.VisibleCount);
        }

        [UnityTest]
        public IEnumerator Prewarm_100Bars_SpawningDoesNotCreateMore()
        {
            _rig = new WorldUiTestRig();
            EnemyData data = _rig.CreateStillEnemy("normal");
            int created = _rig.Root.HealthBars.CreatedCount;
            Assert.GreaterOrEqual(created, _rig.Settings.HealthBarPrewarm);

            for (int i = 0; i < _rig.Settings.HealthBarPrewarm; i++) _rig.SpawnAt(data, i * 0.5f);
            yield return null;

            Assert.AreEqual(created, _rig.Root.HealthBars.CreatedCount, "Không được Instantiate thanh máu trong trận.");
        }

        [UnityTest]
        public IEnumerator FullHealth_BarHidden_AfterHit_ShownWithFill()
        {
            _rig = new WorldUiTestRig();
            EnemyController enemy = _rig.SpawnAt(_rig.CreateStillEnemy("normal", baseHP: 100f), 10f);
            yield return null;

            Assert.IsTrue(_rig.Root.HealthBars.TryGetBar(enemy, out WorldHealthBar bar));
            Assert.IsFalse(bar.IsShown, "Đầy máu thì ẩn thanh.");

            enemy.TakeDamage(EnemyTestRig.TrueHit(10f));
            yield return null;

            Assert.IsTrue(bar.IsShown);
            Assert.AreEqual(0.9f, bar.Fill, 1e-4f);
            Assert.AreEqual(1, _rig.Root.HealthBars.VisibleCount);
        }

        [UnityTest]
        public IEnumerator Boss_UsesBossScale()
        {
            _rig = new WorldUiTestRig();
            EnemyController boss = _rig.SpawnAt(_rig.CreateStillEnemy("boss", boss: true), 10f);
            EnemyController normal = _rig.SpawnAt(_rig.CreateStillEnemy("normal"), 11f);
            yield return null;

            _rig.Root.HealthBars.TryGetBar(boss, out WorldHealthBar bossBar);
            _rig.Root.HealthBars.TryGetBar(normal, out WorldHealthBar normalBar);

            Assert.IsTrue(bossBar.IsBoss);
            Assert.AreEqual(_rig.Settings.BossBarScale, bossBar.transform.localScale.x, 1e-4f);
            Assert.IsFalse(normalBar.IsBoss);
            Assert.AreEqual(1f, normalBar.transform.localScale.x, 1e-4f);
        }

        [UnityTest]
        public IEnumerator PooledBar_ReusedForNormalEnemy_ResetsBossScaleAndHides()
        {
            _rig = new WorldUiTestRig();
            EnemyController boss = _rig.SpawnAt(_rig.CreateStillEnemy("boss", boss: true), 10f);
            yield return null;
            boss.TakeDamage(EnemyTestRig.TrueHit(1e9f));
            yield return null;

            EnemyController normal = _rig.SpawnAt(_rig.CreateStillEnemy("normal"), 10f);
            yield return null;

            _rig.Root.HealthBars.TryGetBar(normal, out WorldHealthBar bar);
            Assert.IsFalse(bar.IsBoss);
            Assert.IsFalse(bar.IsShown);
            Assert.AreEqual(1f, bar.Fill, 1e-4f);
        }

        [UnityTest]
        public IEnumerator EnemyAlreadyOnField_WhenUiEnabled_GetsBar()
        {
            _rig = new WorldUiTestRig(enableRoot: false);
            EnemyController enemy = _rig.SpawnAt(_rig.CreateStillEnemy("normal"), 10f);

            _rig.Root.gameObject.SetActive(true);
            yield return null;

            Assert.IsTrue(_rig.Root.HealthBars.TryGetBar(enemy, out _));
        }

        [UnityTest]
        public IEnumerator Disable_DetachesEveryBar_LaterHitsAreIgnored()
        {
            _rig = new WorldUiTestRig();
            EnemyController enemy = _rig.SpawnAt(_rig.CreateStillEnemy("normal"), 10f);
            yield return null;

            _rig.Root.gameObject.SetActive(false);
            Assert.AreEqual(0, _rig.Root.HealthBars.BoundCount);

            Assert.DoesNotThrow(() => enemy.TakeDamage(EnemyTestRig.TrueHit(10f)));
            Assert.AreEqual(0, _rig.Root.HealthBars.VisibleCount);
        }

        [UnityTest]
        public IEnumerator ShownBar_FollowsAnchorOnScreen()
        {
            _rig = new WorldUiTestRig();
            EnemyController enemy = _rig.SpawnAt(_rig.CreateStillEnemy("normal"), 10f);
            enemy.TakeDamage(EnemyTestRig.TrueHit(10f));
            yield return null;

            _rig.Root.Refresh(0f);

            _rig.Root.HealthBars.TryGetBar(enemy, out WorldHealthBar bar);
            Vector3 expected = _rig.Camera.WorldToScreenPoint(enemy.HealthBarAnchor.position);
            Assert.AreEqual(expected.x, bar.transform.position.x, 0.5f);
            Assert.AreEqual(expected.y, bar.transform.position.y, 0.5f);
        }

        [UnityTest]
        public IEnumerator ShownBar_EnemyBehindCamera_IsHidden()
        {
            _rig = new WorldUiTestRig();
            EnemyController enemy = _rig.SpawnAt(_rig.CreateStillEnemy("normal"), 10f);
            enemy.TakeDamage(EnemyTestRig.TrueHit(10f));
            yield return null;

            // Turn the camera around: the enemy is now behind it.
            _rig.Camera.transform.rotation = Quaternion.LookRotation(Vector3.back);
            _rig.Root.Refresh(0f);

            _rig.Root.HealthBars.TryGetBar(enemy, out WorldHealthBar bar);
            Assert.IsFalse(bar.IsShown);
        }

        [UnityTest]
        public IEnumerator HitsAndRefresh_100Enemies_DoNotAllocate()
        {
            _rig = new WorldUiTestRig();
            EnemyData data = _rig.CreateStillEnemy("normal", baseHP: 1e6f);
            for (int i = 0; i < 100; i++) _rig.SpawnAt(data, i * 0.2f);

            // Warm-up: first hit shows each bar (one-time canvas work), then measure steady-state frames.
            DamageInfo hit = EnemyTestRig.TrueHit(1f);
            HitAll(hit);
            yield return null;
            _rig.Root.Refresh(0.016f);
            yield return null;

            Assert.That(() =>
            {
                HitAll(hit);
                _rig.Root.Refresh(0.016f);
            }, Is.Not.AllocatingGCMemory());
        }

        private void HitAll(in DamageInfo hit)
        {
            var active = _rig.Enemies.Manager.ActiveEnemies;
            for (int i = 0; i < active.Count; i++) active[i].TakeDamage(hit);
        }
    }
}
