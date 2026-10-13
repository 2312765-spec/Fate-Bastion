using NUnit.Framework;
using FateBastion.Enemies;

namespace FateBastion.Tests.EditMode.S2
{
    /// <summary>
    /// A wave is finished when its schedule has spawned everything and none of its enemies (queued or alive,
    /// boss summons included) remain; each wave is counted on its own and reported once (S2).
    /// </summary>
    public class WaveEnemyCounterTests
    {
        private WaveEnemyCounter _counter;

        [SetUp]
        public void SetUp()
        {
            _counter = new WaveEnemyCounter();
        }

        [Test]
        public void MarkScheduleComplete_NoEnemies_FinishesNow()
        {
            _counter.BeginWave(1);
            Assert.IsTrue(_counter.MarkScheduleComplete(1));
            Assert.IsTrue(_counter.IsFinished(1));
        }

        [Test]
        public void Remove_LastEnemyBeforeScheduleComplete_DoesNotFinish()
        {
            _counter.BeginWave(1);
            _counter.Add(1);
            Assert.IsFalse(_counter.Remove(1));
            Assert.IsFalse(_counter.IsFinished(1));
        }

        [Test]
        public void MarkScheduleComplete_EnemiesRemaining_FinishesOnLastRemove()
        {
            _counter.BeginWave(1);
            _counter.Add(1);
            _counter.Add(1);

            Assert.IsFalse(_counter.MarkScheduleComplete(1));
            Assert.IsFalse(_counter.Remove(1));
            Assert.AreEqual(1, _counter.GetOutstanding(1));
            Assert.IsTrue(_counter.Remove(1));
            Assert.IsTrue(_counter.IsFinished(1));
        }

        [Test]
        public void Add_ExtraEnemyMidWave_KeepsWaveOpen()
        {
            // Boss summons count toward the boss's wave.
            _counter.BeginWave(5);
            _counter.Add(5);
            _counter.MarkScheduleComplete(5);
            _counter.Add(5);

            Assert.IsFalse(_counter.Remove(5));
            Assert.IsTrue(_counter.Remove(5));
        }

        [Test]
        public void TwoWavesOverlap_CountedSeparately()
        {
            _counter.BeginWave(1);
            _counter.Add(1);
            _counter.MarkScheduleComplete(1);
            _counter.BeginWave(2);
            _counter.Add(2);
            _counter.MarkScheduleComplete(2);

            Assert.IsTrue(_counter.Remove(2), "wave 2 can finish before wave 1");
            Assert.IsFalse(_counter.IsFinished(1));
            Assert.IsTrue(_counter.Remove(1));
        }

        [Test]
        public void Finished_ReportedOnlyOnce()
        {
            _counter.BeginWave(1);
            _counter.Add(1);
            _counter.MarkScheduleComplete(1);

            Assert.IsTrue(_counter.Remove(1));
            Assert.IsFalse(_counter.Remove(1), "extra remove after finishing");
            Assert.IsFalse(_counter.MarkScheduleComplete(1), "marking again after finishing");
            Assert.AreEqual(0, _counter.GetOutstanding(1));
        }

        [Test]
        public void Remove_UntrackedWave_Ignored()
        {
            Assert.IsFalse(_counter.Remove(0));
            Assert.IsFalse(_counter.Remove(9));
            Assert.AreEqual(0, _counter.GetOutstanding(9));
        }

        [Test]
        public void Add_WaveZeroOrNegative_NotTracked()
        {
            _counter.Add(0);
            _counter.Add(-1);
            Assert.AreEqual(0, _counter.GetOutstanding(0));
            Assert.IsFalse(_counter.MarkScheduleComplete(0));
        }

        [Test]
        public void Add_WithoutBeginWave_CreatesEntry()
        {
            _counter.Add(3);
            Assert.AreEqual(1, _counter.GetOutstanding(3));
            Assert.IsFalse(_counter.Remove(3), "schedule not marked complete yet");
        }

        [Test]
        public void BeginWave_SameNumberAgain_ResetsEntry()
        {
            _counter.BeginWave(1);
            _counter.MarkScheduleComplete(1);

            _counter.BeginWave(1);

            Assert.IsFalse(_counter.IsFinished(1));
            Assert.IsTrue(_counter.MarkScheduleComplete(1));
        }

        [Test]
        public void Clear_ForgetsAllWaves()
        {
            _counter.BeginWave(1);
            _counter.Add(1);
            _counter.Clear();

            Assert.AreEqual(0, _counter.GetOutstanding(1));
            Assert.IsFalse(_counter.IsFinished(1));
            Assert.IsFalse(_counter.Remove(1));
        }
    }
}
