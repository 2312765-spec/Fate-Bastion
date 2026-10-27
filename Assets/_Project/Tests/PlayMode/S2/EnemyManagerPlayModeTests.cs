using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using FateBastion.Enemies;

namespace FateBastion.Tests.PlayMode.S2
{
    /// <summary>EnemyManager + EnemyController: reaching the Castle, dying once, the pool and the UI data API (S2).</summary>
    public class EnemyManagerPlayModeTests
    {
        private const float PathLength = 5f;
        private const float TimeoutSeconds = 5f;

        private EnemyTestRig _rig;

        [SetUp]
        public void SetUp()
        {
            _rig = new EnemyTestRig(PathLength);
        }

        [TearDown]
        public void TearDown()
        {
            // Tests may change the time scale to check x2; only TimeController does this in the game.
            Time.timeScale = 1f;
            _rig.Dispose();
        }

        private IEnumerator WaitUntil(System.Func<bool> condition)
        {
            float elapsed = 0f;
            while (!condition() && elapsed < TimeoutSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private static IEnumerator Frames(int count)
        {
            for (int i = 0; i < count; i++) yield return null;
        }

        [UnityTest]
        public IEnumerator ReachCastle_TimeScale1_RaisesOnceWithCastleDamage()
        {
            yield return ReachCastleOnce(1f);
        }

        [UnityTest]
        public IEnumerator ReachCastle_TimeScale2_RaisesOnceWithCastleDamage()
        {
            yield return ReachCastleOnce(2f);
        }

        private IEnumerator ReachCastleOnce(float timeScale)
        {
            Time.timeScale = timeScale;
            EnemyData data = _rig.CreateEnemy("enemy_fast", moveSpeed: 10f, castleDamage: 2);

            _rig.Manager.RequestSpawn(data, 0, 1);
            float startTime = Time.time;
            yield return WaitUntil(() => _rig.ReachedCount > 0);
            float scaledTimeTaken = Time.time - startTime;
            yield return Frames(5);

            Assert.AreEqual(1, _rig.ReachedCount, "reached the Castle exactly once");
            Assert.AreEqual(2, _rig.LastCastleDamage);
            Assert.AreEqual(0, _rig.KilledCount, "reaching the Castle is not a kill (no gold)");
            Assert.AreEqual(0, _rig.Manager.ActiveEnemies.Count);
            Assert.AreEqual(1, _rig.SpawnedCount);
            Assert.AreEqual(1, _rig.DespawnedCount);
            // 5 m at 10 m/s = 0.5 s of game time, whatever the time scale.
            Assert.AreEqual(0.5f, scaledTimeTaken, 0.2f);
        }

        [UnityTest]
        public IEnumerator KilledAndReachedSameFrame_CountsAsKilledOnly()
        {
            EnemyData data = _rig.CreateEnemy("enemy_normal", castleDamage: 1);

            // Spawned already at the end of the path; killed in the same frame, before EnemyManager updates.
            _rig.Manager.RequestSpawn(data, 0, 1, startDistance: PathLength);
            _rig.Manager.ActiveEnemies[0].TakeDamage(EnemyTestRig.TrueHit(1e6f));
            yield return Frames(3);

            Assert.AreEqual(1, _rig.KilledCount);
            Assert.AreEqual(0, _rig.ReachedCount);
            Assert.AreEqual(1, _rig.DespawnedCount);
        }

        [UnityTest]
        public IEnumerator Kill_ThreeLethalHitsSameFrame_RaisesEnemyKilledOnceWithGoldReward()
        {
            EnemyData data = _rig.CreateEnemy("enemy_armor", baseHP: 60f, goldReward: 10);
            _rig.Manager.RequestSpawn(data, 0, 1);
            EnemyController enemy = _rig.Manager.ActiveEnemies[0];

            enemy.TakeDamage(EnemyTestRig.TrueHit(100f));
            enemy.TakeDamage(EnemyTestRig.TrueHit(100f));
            enemy.TakeDamage(EnemyTestRig.TrueHit(100f));
            yield return null;

            Assert.AreEqual(1, _rig.KilledCount);
            Assert.AreEqual("enemy_armor", _rig.LastKilled.EnemyId);
            Assert.AreEqual(10, _rig.LastKilled.GoldReward);
            Assert.IsFalse(_rig.LastKilled.IsBoss);
            Assert.AreEqual(0, _rig.Manager.ActiveEnemies.Count, "returned to the pool immediately");
            Assert.AreEqual(1, _rig.DespawnedCount);
        }

        [UnityTest]
        public IEnumerator TakeDamage_RaisesHealthChangedWithCurrentAndMax()
        {
            EnemyData data = _rig.CreateEnemy("enemy_normal", baseHP: 40f);
            _rig.Manager.RequestSpawn(data, 0, 1);
            EnemyController enemy = _rig.Manager.ActiveEnemies[0];
            float current = -1f, max = -1f;
            enemy.OnHealthChanged += (c, m) => { current = c; max = m; };

            enemy.TakeDamage(EnemyTestRig.TrueHit(15f));
            yield return null;

            Assert.AreEqual(25f, current, 1e-4f);
            Assert.AreEqual(40f, max, 1e-4f);
            Assert.AreEqual(25f, enemy.CurrentHP, 1e-4f);
            Assert.AreEqual(40f, enemy.MaxHP, 1e-4f);
        }

        [UnityTest]
        public IEnumerator TakeDamage_MeleeHitOnFlyingEnemy_Ignored()
        {
            EnemyData data = _rig.CreateEnemy("enemy_fly", baseHP: 32f, flying: true);
            _rig.Manager.RequestSpawn(data, 1, 1);
            EnemyController enemy = _rig.Manager.ActiveEnemies[0];

            enemy.TakeDamage(EnemyTestRig.TrueHit(1000f, canHitFlying: false));
            yield return null;

            Assert.IsTrue(enemy.IsAlive);
            Assert.AreEqual(32f, enemy.CurrentHP, 1e-4f);
            Assert.AreEqual(0, _rig.KilledCount);
        }

        [UnityTest]
        public IEnumerator Spawn_UsesHealthScalingAndExposesUiData()
        {
            EnemyData data = _rig.CreateEnemy("enemy_boss", baseHP: 40f, boss: true);
            _rig.Manager.SetHealthScaling(1.12f, 1.15f);

            _rig.Manager.RequestSpawn(data, 0, 5, startDistance: 1f);
            EnemyController enemy = _rig.Manager.ActiveEnemies[0];
            yield return null;

            Assert.AreEqual(EnemyHealthFormula.Calculate(40f, 1.12f, 5, 1.15f), enemy.MaxHP, 1e-3f);
            Assert.AreEqual(enemy.MaxHP, enemy.CurrentHP, 1e-3f);
            Assert.IsTrue(enemy.IsBoss);
            Assert.AreEqual(5, enemy.Wave);
            Assert.AreSame(data, enemy.Data);
            Assert.Greater(enemy.Progress, 0.2f - 1e-3f);
            Assert.IsNotNull(enemy.HealthBarAnchor);
        }

        [UnityTest]
        public IEnumerator Move_EnemyFollowsPathAndProgressIncreases()
        {
            EnemyData data = _rig.CreateEnemy("enemy_normal", moveSpeed: 1f);
            _rig.Manager.RequestSpawn(data, 0, 1);
            EnemyController enemy = _rig.Manager.ActiveEnemies[0];

            yield return new WaitForSeconds(0.5f);

            Assert.Greater(enemy.Progress, 0f);
            Assert.Greater(enemy.transform.position.x, 0.1f);
            Assert.AreEqual(enemy.Distance, enemy.transform.position.x, 1e-3f);
        }

        [UnityTest]
        public IEnumerator Pool_AfterPrewarm_NoNewInstances()
        {
            EnemyData data = _rig.CreateEnemy("enemy_normal");
            _rig.Manager.Prewarm(data, 5);
            int instancesAfterPrewarm = _rig.Root.transform.childCount;
            Assert.AreEqual(5, instancesAfterPrewarm);

            for (int round = 0; round < 3; round++)
            {
                for (int i = 0; i < 5; i++) _rig.Manager.RequestSpawn(data, 0, 0);
                _rig.KillAllActive();
                yield return null;
            }

            Assert.AreEqual(instancesAfterPrewarm, _rig.Root.transform.childCount);
            Assert.AreEqual(15, _rig.SpawnedCount);
        }

        [UnityTest]
        public IEnumerator Pool_BeyondPrewarm_GrowsAndWarnsOnce()
        {
            EnemyData data = _rig.CreateEnemy("enemy_normal");
            _rig.Manager.Prewarm(data, 1);

            LogAssert.Expect(LogType.Warning, new Regex("enemy_normal"));
            _rig.Manager.RequestSpawn(data, 0, 0);
            _rig.Manager.RequestSpawn(data, 0, 0);
            _rig.Manager.RequestSpawn(data, 0, 0);
            yield return null;

            Assert.AreEqual(3, _rig.Manager.ActiveEnemies.Count);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator RequestSpawn_NullPrefab_LogsErrorAndSpawnsNothing()
        {
            EnemyData data = _rig.CreateEnemy("enemy_loi");
            data.prefab = null;

            LogAssert.Expect(LogType.Error, new Regex("enemy_loi"));
            _rig.Manager.RequestSpawn(data, 0, 1);
            yield return null;

            Assert.AreEqual(0, _rig.Manager.ActiveEnemies.Count);
            Assert.AreEqual(0, _rig.Manager.GetOutstandingCount(1), "a request that cannot spawn must not block the wave");
        }

        [UnityTest]
        public IEnumerator OverCap_ExtraRequestsQueuedThenReleasedAsSlotsFree()
        {
            EnemyData data = _rig.CreateEnemy("enemy_normal");
            int cap = _rig.Settings.MaxActiveEnemies;

            for (int i = 0; i < cap + 10; i++) _rig.Manager.RequestSpawn(data, 0, 1);

            Assert.AreEqual(cap, _rig.Manager.ActiveEnemies.Count);
            Assert.AreEqual(10, _rig.Manager.QueuedCount);
            Assert.AreEqual(cap + 10, _rig.Manager.GetOutstandingCount(1));

            _rig.Manager.ActiveEnemies[0].TakeDamage(EnemyTestRig.TrueHit(1e6f));
            _rig.Manager.ActiveEnemies[0].TakeDamage(EnemyTestRig.TrueHit(1e6f));
            yield return null;

            Assert.AreEqual(cap, _rig.Manager.ActiveEnemies.Count, "freed slots are refilled from the queue");
            Assert.AreEqual(8, _rig.Manager.QueuedCount);
            Assert.LessOrEqual(_rig.MaxActiveSeen, cap);
        }

        [UnityTest]
        public IEnumerator ClearAll_ReturnsEverythingAndEmptiesQueue_NoKillOrCastleEvents()
        {
            EnemyData data = _rig.CreateEnemy("enemy_normal");
            int cap = _rig.Settings.MaxActiveEnemies;
            for (int i = 0; i < cap + 5; i++) _rig.Manager.RequestSpawn(data, 0, 1);

            _rig.Manager.ClearAll();
            yield return Frames(2);

            Assert.AreEqual(0, _rig.Manager.ActiveEnemies.Count);
            Assert.AreEqual(0, _rig.Manager.QueuedCount);
            Assert.AreEqual(cap, _rig.DespawnedCount);
            Assert.AreEqual(0, _rig.KilledCount);
            Assert.AreEqual(0, _rig.ReachedCount);
            Assert.AreEqual(0, _rig.Manager.GetOutstandingCount(1));
        }
    }
}
