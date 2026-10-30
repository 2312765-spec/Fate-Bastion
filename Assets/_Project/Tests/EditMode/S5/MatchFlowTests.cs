using System.Collections.Generic;
using NUnit.Framework;
using FateBastion.Core;
using FateBastion.Game;

namespace FateBastion.Tests.EditMode.S5
{
    /// <summary>MatchFlow (S5, B-06): every row of the transition table, skip gold, and the same-frame rule.</summary>
    public class MatchFlowTests
    {
        private const float Prepare = 15f;
        private const float Rest = 8f;

        private MatchFlow _flow;
        private List<GameState> _states;
        private List<int> _started;
        private List<int> _cleared;
        private bool? _ended;

        private void Create(int waves = 3, bool endless = false)
        {
            _flow = new MatchFlow(waves, endless, Prepare, Rest);
            _states = new List<GameState>();
            _started = new List<int>();
            _cleared = new List<int>();
            _ended = null;
            _flow.StateChanged += s => _states.Add(s);
            _flow.WaveStartRequested += w => _started.Add(w);
            _flow.WaveCleared += w => _cleared.Add(w);
            _flow.Ended += won => _ended = won;
        }

        private void ToWave1()
        {
            _flow.MarkReady();
            _flow.Skip(out _);
        }

        [Test]
        public void Loading_MarkReady_Preparing15s()
        {
            Create();
            Assert.AreEqual(GameState.Loading, _flow.State);

            _flow.MarkReady();

            Assert.AreEqual(GameState.Preparing, _flow.State);
            Assert.AreEqual(Prepare, _flow.TimeLeft, 1e-4f);
            CollectionAssert.AreEqual(new[] { GameState.Preparing }, _states);
        }

        [Test]
        public void Preparing_TimerEnds_Wave1()
        {
            Create();
            _flow.MarkReady();

            _flow.Tick(14.9f);
            Assert.AreEqual(GameState.Preparing, _flow.State);
            _flow.Tick(0.2f);

            Assert.AreEqual(GameState.InWave, _flow.State);
            CollectionAssert.AreEqual(new[] { 1 }, _started);
        }

        [Test]
        public void Preparing_Skip_Wave1_NoGold()
        {
            Create();
            _flow.MarkReady();

            Assert.IsTrue(_flow.Skip(out bool paysGold));
            Assert.IsFalse(paysGold);
            Assert.AreEqual(GameState.InWave, _flow.State);
            Assert.AreEqual(1, _flow.CurrentWave);
        }

        [Test]
        public void InWave_ClearedNotLast_Resting8s()
        {
            Create();
            ToWave1();

            _flow.NotifyWaveFinished(1, true);

            Assert.AreEqual(GameState.Resting, _flow.State);
            Assert.AreEqual(Rest, _flow.TimeLeft, 1e-4f);
            CollectionAssert.AreEqual(new[] { 1 }, _cleared);
            Assert.IsNull(_ended);
        }

        [Test]
        public void Resting_TimerEnds_NextWave()
        {
            Create();
            ToWave1();
            _flow.NotifyWaveFinished(1, true);

            _flow.Tick(8f);

            Assert.AreEqual(GameState.InWave, _flow.State);
            CollectionAssert.AreEqual(new[] { 1, 2 }, _started);
        }

        [Test]
        public void Resting_Skip_NextWave_PaysGold()
        {
            Create();
            ToWave1();
            _flow.NotifyWaveFinished(1, true);

            Assert.IsTrue(_flow.Skip(out bool paysGold));

            Assert.IsTrue(paysGold);
            Assert.AreEqual(GameState.InWave, _flow.State);
            Assert.AreEqual(2, _flow.CurrentWave);
        }

        [Test]
        public void InWave_Skip_NotAllowed()
        {
            Create();
            ToWave1();

            Assert.IsFalse(_flow.Skip(out bool paysGold));
            Assert.IsFalse(paysGold);
            Assert.AreEqual(1, _flow.CurrentWave);
        }

        [Test]
        public void LastWaveCleared_CastleAlive_Won()
        {
            Create(waves: 2);
            ToWave1();
            _flow.NotifyWaveFinished(1, true);
            _flow.Skip(out _);

            _flow.NotifyWaveFinished(2, true);

            Assert.AreEqual(GameState.Won, _flow.State);
            Assert.AreEqual(true, _ended);
            CollectionAssert.AreEqual(new[] { 1, 2 }, _cleared);
            Assert.AreEqual(2, _flow.WavesCleared);
        }

        [Test]
        public void LastWaveCleared_SameFrameCastleZero_LostFirst()
        {
            Create(waves: 1);
            ToWave1();

            _flow.NotifyWaveFinished(1, false);

            Assert.AreEqual(GameState.Lost, _flow.State);
            Assert.AreEqual(false, _ended);
            Assert.IsEmpty(_cleared);
        }

        [TestCase(GameState.Preparing)]
        [TestCase(GameState.InWave)]
        [TestCase(GameState.Resting)]
        public void CastleDestroyed_AnyPlayingState_Lost(GameState from)
        {
            Create();
            _flow.MarkReady();
            if (from != GameState.Preparing) _flow.Skip(out _);
            if (from == GameState.Resting) _flow.NotifyWaveFinished(1, true);
            Assert.AreEqual(from, _flow.State);

            _flow.NotifyCastleDestroyed();

            Assert.AreEqual(GameState.Lost, _flow.State);
            Assert.AreEqual(false, _ended);
        }

        [Test]
        public void Endless_NeverWins_KeepsGoingPastWaveCount()
        {
            Create(waves: 2, endless: true);
            ToWave1();
            _flow.NotifyWaveFinished(1, true);
            _flow.Skip(out _);
            _flow.NotifyWaveFinished(2, true);

            Assert.AreEqual(GameState.Resting, _flow.State);
            _flow.Tick(Rest);
            Assert.AreEqual(3, _flow.CurrentWave);

            _flow.NotifyCastleDestroyed();
            Assert.AreEqual(GameState.Lost, _flow.State);
            Assert.AreEqual(2, _flow.WavesCleared);
        }

        [Test]
        public void AfterEnd_NothingChanges()
        {
            Create(waves: 1);
            ToWave1();
            _flow.NotifyCastleDestroyed();
            int stateEvents = _states.Count;

            _flow.Tick(100f);
            _flow.NotifyWaveFinished(1, true);
            _flow.NotifyCastleDestroyed();

            Assert.IsFalse(_flow.Skip(out _));
            Assert.AreEqual(GameState.Lost, _flow.State);
            Assert.AreEqual(stateEvents, _states.Count);
        }

        [Test]
        public void WaveFinished_ForAnotherWave_Ignored()
        {
            Create();
            ToWave1();

            _flow.NotifyWaveFinished(2, true);

            Assert.AreEqual(GameState.InWave, _flow.State);
            Assert.IsEmpty(_cleared);
        }

        [Test]
        public void Loading_IgnoresTicksAndSkip()
        {
            Create();
            _flow.Tick(100f);
            Assert.IsFalse(_flow.Skip(out _));
            Assert.AreEqual(GameState.Loading, _flow.State);
            Assert.IsEmpty(_started);
        }
    }
}
