using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using FateBastion.Enemies;

namespace FateBastion.Tests.PlayMode.S2
{
    /// <summary>Boss summoning and flying enemies on the field (S2/A-05).</summary>
    public class BossSummonPlayModeTests
    {
        private const float TimeoutSeconds = 5f;

        private EnemyTestRig _rig;
        private WaveManager _waves;

        [SetUp]
        public void SetUp()
        {
            _rig = new EnemyTestRig(1000f);
            _waves = _rig.AddWaveManager();
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            _rig.Dispose();
        }

        private static SpawnGroup Group(EnemyData enemy, int count, int pathIndex = 0)
        {
            return new SpawnGroup { enemy = enemy, count = count, interval = 0f, startDelay = 0f, pathIndex = pathIndex };
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
        public IEnumerator Boss_Alive_SummonsBatchEveryInterval()
        {
            EnemyData boss = _rig.CreateEnemy("enemy_boss", boss: true);
            EnemyData fast = _rig.CreateEnemy("enemy_fast");
            _rig.Manager.SetBossSummonSettings(_rig.CreateBossSummonSettings(fast, count: 3, intervalSeconds: 0.2f));
            _waves.Init(_rig.CreateLevel(_rig.CreateWave(Group(boss, 1))), null);

            _waves.StartWave(1);
            yield return WaitUntil(() => _rig.SpawnedCount == 1);
            Assert.AreEqual(1, _rig.Manager.ActiveEnemies.Count, "chỉ có boss");

            yield return WaitUntil(() => _rig.SpawnedCount == 4);
            Assert.AreEqual(4, _rig.Manager.ActiveEnemies.Count, "boss + 3 quái Nhanh");
            Assert.AreEqual(1, _rig.Manager.ActiveEnemies[1].Wave, "quái gọi mang wave của boss");
        }

        [UnityTest]
        public IEnumerator Boss_SummonsKeepTheWaveOpen()
        {
            EnemyData boss = _rig.CreateEnemy("enemy_boss", boss: true);
            EnemyData fast = _rig.CreateEnemy("enemy_fast");
            _rig.Manager.SetBossSummonSettings(_rig.CreateBossSummonSettings(fast, count: 3, intervalSeconds: 0.2f));
            _waves.Init(_rig.CreateLevel(_rig.CreateWave(Group(boss, 1))), null);
            var finished = new System.Collections.Generic.List<int>();
            _waves.OnWaveFinished += wave => finished.Add(wave);

            _waves.StartWave(1);
            yield return WaitUntil(() => _rig.SpawnedCount == 4);
            Assert.AreEqual(0, finished.Count, "wave chưa xong khi quái gọi còn sống");

            _rig.KillAllActive();
            yield return Frames(3);
            CollectionAssert.AreEqual(new[] { 1 }, finished);
        }

        [UnityTest]
        public IEnumerator Boss_Killed_StopsSummoning()
        {
            EnemyData boss = _rig.CreateEnemy("enemy_boss", boss: true);
            EnemyData fast = _rig.CreateEnemy("enemy_fast");
            _rig.Manager.SetBossSummonSettings(_rig.CreateBossSummonSettings(fast, count: 3, intervalSeconds: 0.25f));
            _waves.Init(_rig.CreateLevel(_rig.CreateWave(Group(boss, 1))), null);

            _waves.StartWave(1);
            yield return WaitUntil(() => _rig.SpawnedCount == 1);

            _rig.Manager.ActiveEnemies[0].TakeDamage(EnemyTestRig.TrueHit(1e9f));
            yield return new WaitForSeconds(0.6f);

            Assert.AreEqual(1, _rig.SpawnedCount, "boss chết thì không gọi thêm");
        }

        [UnityTest]
        public IEnumerator Flying_MeleeIgnored_ProjectileDamageApplies()
        {
            EnemyData flying = _rig.CreateEnemy("enemy_fly", baseHP: 100f, flying: true);
            _waves.Init(_rig.CreateLevel(_rig.CreateWave(Group(flying, 1, pathIndex: 1))), null);

            _waves.StartWave(1);
            yield return WaitUntil(() => _rig.SpawnedCount == 1);

            EnemyController enemy = _rig.Manager.ActiveEnemies[0];
            Assert.IsTrue(enemy.IsFlying);

            enemy.TakeDamage(EnemyTestRig.TrueHit(50f, canHitFlying: false));
            Assert.AreEqual(100f, enemy.CurrentHP, 1e-3f, "đòn cận chiến không trúng quái Bay");

            enemy.TakeDamage(EnemyTestRig.TrueHit(50f, canHitFlying: true));
            Assert.AreEqual(50f, enemy.CurrentHP, 1e-3f, "đòn đánh được Bay thì trúng");
        }
    }
}
