using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using FateBastion.Enemies;

namespace FateBastion.Tests.EditMode.S2
{
    /// <summary>Spawn timing of one wave: parallel groups, startDelay, interval, large scaled steps (S2).</summary>
    public class WaveScheduleTests
    {
        private EnemyData _normal;
        private EnemyData _fast;
        private WaveData _wave;
        private WaveSchedule _schedule;
        private List<SpawnRequest> _output;

        [SetUp]
        public void SetUp()
        {
            _normal = ScriptableObject.CreateInstance<EnemyData>();
            _normal.id = "enemy_thuong";
            _fast = ScriptableObject.CreateInstance<EnemyData>();
            _fast.id = "enemy_nhanh";
            _wave = ScriptableObject.CreateInstance<WaveData>();
            _schedule = new WaveSchedule();
            _output = new List<SpawnRequest>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_normal);
            Object.DestroyImmediate(_fast);
            Object.DestroyImmediate(_wave);
        }

        private static SpawnGroup Group(EnemyData enemy, int count, float interval, float startDelay, int pathIndex = 0)
        {
            return new SpawnGroup { enemy = enemy, count = count, interval = interval, startDelay = startDelay, pathIndex = pathIndex };
        }

        private int CountOf(EnemyData enemy)
        {
            int n = 0;
            foreach (SpawnRequest r in _output)
                if (r.Enemy == enemy) n++;
            return n;
        }

        [Test]
        public void Begin_SetsWaveAndTotal_NothingSpawnedYet()
        {
            _wave.groups = new[] { Group(_normal, 3, 1f, 0f), Group(_fast, 2, 0.5f, 2f) };
            _schedule.Begin(_wave, 4);

            Assert.AreEqual(4, _schedule.Wave);
            Assert.AreEqual(5, _schedule.TotalCount);
            Assert.AreEqual(0, _schedule.SpawnedCount);
            Assert.IsFalse(_schedule.IsComplete);
        }

        [Test]
        public void Tick_ParallelGroups_FollowStartDelayAndInterval()
        {
            _wave.groups = new[] { Group(_normal, 3, 1f, 0f), Group(_fast, 2, 0.5f, 2f) };
            _schedule.Begin(_wave, 1);

            Assert.AreEqual(1, _schedule.Tick(0f, _output), "t=0: first normal");
            Assert.AreEqual(0, _schedule.Tick(0.99f, _output), "t=0.99: nothing");
            Assert.AreEqual(1, _schedule.Tick(0.01f, _output), "t=1.0: second normal");
            Assert.AreEqual(0, _schedule.Tick(0.9f, _output), "t=1.9: fast group still waiting for startDelay 2");
            Assert.AreEqual(2, _schedule.Tick(0.1f, _output), "t=2.0: third normal + first fast");
            Assert.IsFalse(_schedule.IsComplete);
            Assert.AreEqual(1, _schedule.Tick(0.5f, _output), "t=2.5: second fast");

            Assert.IsTrue(_schedule.IsComplete);
            Assert.AreEqual(3, CountOf(_normal));
            Assert.AreEqual(2, CountOf(_fast));
        }

        [Test]
        public void Tick_LargeStep_SpawnsEverythingDueWithoutLosingAny()
        {
            // At x2 (or after a hitch) one scaled step can cover several intervals.
            _wave.groups = new[] { Group(_normal, 7, 1.2f, 0f), Group(_fast, 4, 1.2f, 2f) };
            _schedule.Begin(_wave, 1);

            int spawned = _schedule.Tick(100f, _output);

            Assert.AreEqual(11, spawned);
            Assert.AreEqual(11, _output.Count);
            Assert.AreEqual(7, CountOf(_normal));
            Assert.AreEqual(4, CountOf(_fast));
            Assert.IsTrue(_schedule.IsComplete);
        }

        [Test]
        public void Tick_RequestsCarryWaveAndPathIndex()
        {
            _wave.groups = new[] { Group(_fast, 1, 1f, 0f, pathIndex: 1) };
            _schedule.Begin(_wave, 7);

            _schedule.Tick(0f, _output);

            Assert.AreEqual(1, _output.Count);
            Assert.AreEqual(_fast, _output[0].Enemy);
            Assert.AreEqual(1, _output[0].PathIndex);
            Assert.AreEqual(7, _output[0].Wave);
            Assert.AreEqual(0f, _output[0].StartDistance);
        }

        [Test]
        public void Tick_ZeroInterval_SpawnsWholeGroupAtStartDelay()
        {
            _wave.groups = new[] { Group(_normal, 5, 0f, 1f) };
            _schedule.Begin(_wave, 1);

            Assert.AreEqual(0, _schedule.Tick(0.5f, _output));
            Assert.AreEqual(5, _schedule.Tick(0.5f, _output));
            Assert.IsTrue(_schedule.IsComplete);
        }

        [Test]
        public void Tick_AppendsToOutputWithoutClearing()
        {
            _wave.groups = new[] { Group(_normal, 2, 0f, 0f) };
            _schedule.Begin(_wave, 1);
            _output.Add(new SpawnRequest(_fast, 0, 1));

            _schedule.Tick(0f, _output);

            Assert.AreEqual(3, _output.Count);
        }

        [Test]
        public void Begin_GroupWithZeroCount_IsSkipped()
        {
            _wave.groups = new[] { Group(_normal, 0, 1f, 0f), Group(_fast, 1, 1f, 0f) };
            _schedule.Begin(_wave, 1);

            Assert.AreEqual(1, _schedule.TotalCount);
            _schedule.Tick(0f, _output);
            Assert.IsTrue(_schedule.IsComplete);
        }

        [Test]
        public void Begin_GroupWithNullEnemy_SkippedWithWarning()
        {
            _wave.name = "L1_W3";
            _wave.groups = new[] { Group(null, 4, 1f, 0f), Group(_fast, 1, 1f, 0f) };

            LogAssert.Expect(LogType.Warning, new Regex("L1_W3"));
            _schedule.Begin(_wave, 3);

            Assert.AreEqual(1, _schedule.TotalCount);
            _schedule.Tick(0f, _output);
            Assert.IsTrue(_schedule.IsComplete);
            Assert.AreEqual(1, _output.Count);
        }

        [Test]
        public void Begin_NoGroups_CompleteImmediately()
        {
            _wave.groups = new SpawnGroup[0];
            _schedule.Begin(_wave, 1);

            Assert.AreEqual(0, _schedule.TotalCount);
            Assert.IsTrue(_schedule.IsComplete);
            Assert.AreEqual(0, _schedule.Tick(1f, _output));
        }

        [Test]
        public void Begin_Reused_ForgetsPreviousWave()
        {
            _wave.groups = new[] { Group(_normal, 3, 1f, 0f), Group(_fast, 3, 1f, 0f) };
            _schedule.Begin(_wave, 1);
            _schedule.Tick(100f, _output);

            WaveData next = ScriptableObject.CreateInstance<WaveData>();
            next.groups = new[] { Group(_normal, 1, 1f, 5f) };
            _schedule.Begin(next, 2);
            _output.Clear();

            Assert.AreEqual(2, _schedule.Wave);
            Assert.AreEqual(1, _schedule.TotalCount);
            Assert.AreEqual(0, _schedule.Tick(4.9f, _output));
            Assert.AreEqual(1, _schedule.Tick(0.1f, _output));
            Object.DestroyImmediate(next);
        }
    }
}
