using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using FateBastion.Enemies;

namespace FateBastion.Tests.PlayMode.S2
{
    /// <summary>WaveManager: wave finishes once when everything spawned and died, over the 100 cap and with summons (S2).</summary>
    public class WaveManagerPlayModeTests
    {
        private const float TimeoutSeconds = 5f;

        private EnemyTestRig _rig;
        private WaveManager _waves;
        private List<int> _finished;

        [SetUp]
        public void SetUp()
        {
            // Long path so nothing reaches the Castle during the test.
            _rig = new EnemyTestRig(1000f);
            _waves = _rig.AddWaveManager();
            _finished = new List<int>();
            _waves.OnWaveFinished += wave => _finished.Add(wave);
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            _rig.Dispose();
        }

        private static SpawnGroup Group(EnemyData enemy, int count, float interval = 0f, float startDelay = 0f)
        {
            return new SpawnGroup { enemy = enemy, count = count, interval = interval, startDelay = startDelay };
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
        public IEnumerator Wave_AllSpawnedAndKilled_FinishesOnce()
        {
            EnemyData data = _rig.CreateEnemy("enemy_normal");
            _waves.Init(_rig.CreateLevel(_rig.CreateWave(Group(data, 3, interval: 0.1f))), null);

            _waves.StartWave(1);
            yield return WaitUntil(() => _rig.SpawnedCount == 3);
            Assert.AreEqual(0, _finished.Count, "enemies still alive");

            _rig.KillAllActive();
            yield return Frames(3);

            CollectionAssert.AreEqual(new[] { 1 }, _finished);
        }

        [UnityTest]
        public IEnumerator Wave_KilledBeforeScheduleEnds_NotFinishedEarly()
        {
            EnemyData data = _rig.CreateEnemy("enemy_normal");
            _waves.Init(_rig.CreateLevel(_rig.CreateWave(Group(data, 2, interval: 0.5f))), null);

            _waves.StartWave(1);
            yield return WaitUntil(() => _rig.SpawnedCount == 1);
            _rig.KillAllActive();
            yield return Frames(2);
            Assert.AreEqual(0, _finished.Count, "second enemy not spawned yet");

            yield return WaitUntil(() => _rig.SpawnedCount == 2);
            _rig.KillAllActive();
            yield return Frames(2);
            CollectionAssert.AreEqual(new[] { 1 }, _finished);
        }

        [UnityTest]
        public IEnumerator Wave_150EnemiesOverCap_NeverExceeds100AndFinishes()
        {
            EnemyData data = _rig.CreateEnemy("enemy_normal");
            _waves.Init(_rig.CreateLevel(_rig.CreateWave(Group(data, 150))), null);

            _waves.StartWave(1);
            yield return null;

            Assert.AreEqual(_rig.Settings.MaxActiveEnemies, _rig.Manager.ActiveEnemies.Count);
            Assert.AreEqual(150 - _rig.Settings.MaxActiveEnemies, _rig.Manager.QueuedCount);

            float elapsed = 0f;
            while (_finished.Count == 0 && elapsed < TimeoutSeconds)
            {
                Assert.AreEqual(0, _finished.Count, "must not finish while the queue still holds enemies");
                _rig.KillAllActive();
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.LessOrEqual(_rig.MaxActiveSeen, _rig.Settings.MaxActiveEnemies);
            Assert.AreEqual(150, _rig.KilledCount);
            CollectionAssert.AreEqual(new[] { 1 }, _finished);
        }

        [UnityTest]
        public IEnumerator Wave_ExtraSpawnMidWave_FinishesOnlyAfterExtraDies()
        {
            // Stands in for BossSummonAbility (A-05): summons are requested with the boss's wave number.
            EnemyData boss = _rig.CreateEnemy("enemy_boss", boss: true);
            EnemyData fast = _rig.CreateEnemy("enemy_fast");
            _waves.Init(_rig.CreateLevel(_rig.CreateWave(Group(boss, 1))), null);

            _waves.StartWave(1);
            yield return WaitUntil(() => _rig.SpawnedCount == 1);
            EnemyController bossEnemy = _rig.Manager.ActiveEnemies[0];
            _rig.Manager.RequestSpawn(fast, 0, bossEnemy.Wave, bossEnemy.Distance);

            bossEnemy.TakeDamage(EnemyTestRig.TrueHit(1e6f));
            yield return Frames(2);
            Assert.AreEqual(0, _finished.Count, "summoned enemy still alive");

            _rig.KillAllActive();
            yield return Frames(2);
            CollectionAssert.AreEqual(new[] { 1 }, _finished);
        }

        [UnityTest]
        public IEnumerator Wave_TimeScale2_SpawnsInHalfTheRealTime()
        {
            Time.timeScale = 2f;
            EnemyData data = _rig.CreateEnemy("enemy_normal");
            _waves.Init(_rig.CreateLevel(_rig.CreateWave(Group(data, 1, startDelay: 1f))), null);

            _waves.StartWave(1);
            float realStart = Time.unscaledTime;
            yield return WaitUntil(() => _rig.SpawnedCount == 1);

            Assert.AreEqual(0.5f, Time.unscaledTime - realStart, 0.2f);
        }

        [UnityTest]
        public IEnumerator TwoWavesOverlap_EachFinishesSeparately()
        {
            EnemyData data = _rig.CreateEnemy("enemy_normal");
            _waves.Init(_rig.CreateLevel(
                _rig.CreateWave(Group(data, 1)),
                _rig.CreateWave(Group(data, 1))), null);

            _waves.StartWave(1);
            yield return null;
            _waves.StartWave(2);
            yield return WaitUntil(() => _rig.SpawnedCount == 2);

            EnemyController wave2Enemy = _rig.Manager.ActiveEnemies[0].Wave == 2
                ? _rig.Manager.ActiveEnemies[0]
                : _rig.Manager.ActiveEnemies[1];
            wave2Enemy.TakeDamage(EnemyTestRig.TrueHit(1e6f));
            yield return Frames(2);
            CollectionAssert.AreEqual(new[] { 2 }, _finished);

            _rig.KillAllActive();
            yield return Frames(2);
            CollectionAssert.AreEqual(new[] { 2, 1 }, _finished);
        }

        [UnityTest]
        public IEnumerator StopAndClear_MidWave_RemovesEnemiesAndNeverFinishes()
        {
            EnemyData data = _rig.CreateEnemy("enemy_normal");
            _waves.Init(_rig.CreateLevel(_rig.CreateWave(Group(data, 5, interval: 0.2f))), null);

            _waves.StartWave(1);
            yield return WaitUntil(() => _rig.SpawnedCount >= 2);

            _waves.StopAndClear();
            int spawnedAtStop = _rig.SpawnedCount;
            yield return new WaitForSeconds(1.5f);

            Assert.AreEqual(0, _rig.Manager.ActiveEnemies.Count);
            Assert.AreEqual(spawnedAtStop, _rig.SpawnedCount, "schedule stopped");
            Assert.AreEqual(0, _finished.Count);
            Assert.AreEqual(0, _rig.KilledCount);
        }

        [UnityTest]
        public IEnumerator Init_PrewarmsPoolFromLevelWaves()
        {
            EnemyData normal = _rig.CreateEnemy("enemy_normal");
            EnemyData fast = _rig.CreateEnemy("enemy_fast");
            _waves.Init(_rig.CreateLevel(
                _rig.CreateWave(Group(normal, 4)),
                _rig.CreateWave(Group(normal, 7), Group(fast, 3))), null);
            yield return null;

            int extra = _rig.Settings.ExtraPrewarmPerType;
            Assert.AreEqual(7 + extra + 3 + extra, _rig.Root.transform.childCount);
        }

        [UnityTest]
        public IEnumerator Init_SetsHealthScalingFromLevel()
        {
            EnemyData data = _rig.CreateEnemy("enemy_normal", baseHP: 40f);
            LevelData level = _rig.CreateLevel(_rig.CreateWave(Group(data, 1)), _rig.CreateWave(Group(data, 1)));
            level.hpGrowth = 1.12f;
            level.hpMultiplier = 1.3f;
            _waves.Init(level, null);

            _waves.StartWave(2);
            yield return WaitUntil(() => _rig.SpawnedCount == 1);

            Assert.AreEqual(EnemyHealthFormula.Calculate(40f, 1.12f, 2, 1.3f), _rig.Manager.ActiveEnemies[0].MaxHP, 1e-3f);
        }

        [UnityTest]
        public IEnumerator StartWave_OutOfRange_LogsErrorAndDoesNothing()
        {
            EnemyData data = _rig.CreateEnemy("enemy_normal");
            _waves.Init(_rig.CreateLevel(_rig.CreateWave(Group(data, 1))), null);

            LogAssert.Expect(LogType.Error, new Regex("wave 2"));
            _waves.StartWave(2);
            yield return Frames(2);

            Assert.AreEqual(0, _rig.SpawnedCount);
            Assert.AreEqual(0, _finished.Count);
        }

        [UnityTest]
        public IEnumerator Endless_WavesBeyondLast_RepeatLoopBlockWithRealWaveNumbers()
        {
            EnemyData data = _rig.CreateEnemy("enemy_normal", baseHP: 40f);
            LevelData level = _rig.CreateLevel(
                _rig.CreateWave(Group(data, 1)),
                _rig.CreateWave(Group(data, 2)),
                _rig.CreateWave(Group(data, 3)));
            level.hpGrowth = 1.12f;
            level.hpMultiplier = 1.3f;
            _waves.Init(level, _rig.CreateEndlessSettings(2, 3));

            _waves.StartWave(4); // offset 0 -> wave 2 (2 enemies)
            yield return WaitUntil(() => _rig.SpawnedCount == 2);
            Assert.AreEqual(2, _rig.Manager.ActiveEnemies.Count);
            Assert.AreEqual(4, _rig.Manager.ActiveEnemies[0].Wave, "quái mang wave thật 4, không phải 2");
            Assert.AreEqual(EnemyHealthFormula.Calculate(40f, 1.12f, 4, 1.3f), _rig.Manager.ActiveEnemies[0].MaxHP, 1e-1f);

            _rig.KillAllActive();
            yield return Frames(3);

            _waves.StartWave(7); // offset 3 -> 3 % 2 = 1 -> wave 3 (3 enemies)
            yield return WaitUntil(() => _rig.SpawnedCount == 2 + 3);
            Assert.AreEqual(3, _rig.Manager.ActiveEnemies.Count);
        }
    }
}
