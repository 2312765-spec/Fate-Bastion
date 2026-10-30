using System;
using FateBastion.Core;

namespace FateBastion.Game
{
    /// <summary>
    /// State machine of one match (S5 transition table), pure so every row is unit-tested. GameManager feeds it time,
    /// wave and Castle news and turns its events into WaveManager calls, gold and GameEvents.
    /// <code>
    /// Loading   --MarkReady-->                 Preparing
    /// Preparing --timer 0 or Skip-->           InWave (wave 1)
    /// InWave    --wave cleared, not last-->    Resting   (Endless: always)
    /// InWave    --last wave cleared, Castle alive--> Won (campaign only)
    /// Resting   --timer 0 or Skip (+gold)-->   InWave (next wave)
    /// any playing state --Castle HP 0-->       Lost
    /// </code>
    /// Pause is not a state: it is an overlay handled by GameManager and TimeController.
    /// </summary>
    public sealed class MatchFlow
    {
        private readonly float _prepareSeconds;
        private readonly float _restSeconds;

        public MatchFlow(int waveCount, bool isEndless, float prepareSeconds, float restSeconds)
        {
            if (!isEndless && waveCount <= 0) throw new ArgumentOutOfRangeException(nameof(waveCount), waveCount, "A campaign level needs waves.");

            WaveCount = waveCount;
            IsEndless = isEndless;
            _prepareSeconds = Math.Max(0f, prepareSeconds);
            _restSeconds = Math.Max(0f, restSeconds);
        }

        /// <summary>New state, after it was set.</summary>
        public event Action<GameState> StateChanged;

        /// <summary>Start spawning wave n (1-based): WaveManager.StartWave + GameEvents.OnWaveStarted.</summary>
        public event Action<int> WaveStartRequested;

        /// <summary>Wave n is clear and the Castle stands: wave-end gold + GameEvents.OnWaveCleared.</summary>
        public event Action<int> WaveCleared;

        /// <summary>The match is over: true = Won, false = Lost.</summary>
        public event Action<bool> Ended;

        public GameState State { get; private set; } = GameState.Loading;
        public int WaveCount { get; }
        public bool IsEndless { get; }

        /// <summary>Last wave started (0 before wave 1).</summary>
        public int CurrentWave { get; private set; }

        /// <summary>Waves cleared so far; the Endless result reports this.</summary>
        public int WavesCleared { get; private set; }

        /// <summary>Seconds left in Preparing or Resting (scaled time), 0 otherwise.</summary>
        public float TimeLeft { get; private set; }

        public bool IsPlaying => State == GameState.Preparing || State == GameState.InWave || State == GameState.Resting;
        public bool IsOver => State == GameState.Won || State == GameState.Lost;

        /// <summary>Skip is only offered while waiting (Preparing or Resting).</summary>
        public bool CanSkip => State == GameState.Preparing || State == GameState.Resting;

        /// <summary>Loading → Preparing: scene, pools and deck are ready.</summary>
        public void MarkReady()
        {
            if (State != GameState.Loading) return;
            TimeLeft = _prepareSeconds;
            SetState(GameState.Preparing);
        }

        /// <summary>Counts the Preparing / Resting timer down; at 0 the next wave starts.</summary>
        public void Tick(float deltaTime)
        {
            if (!CanSkip || deltaTime <= 0f) return;

            TimeLeft -= deltaTime;
            if (TimeLeft <= 0f) StartNextWave();
        }

        /// <summary>
        /// Skips the wait. Returns false when there is nothing to skip. <paramref name="paysGold"/> is true when the
        /// rest between waves was skipped (S5: "Nghỉ → Bỏ qua (+50 Vàng)").
        /// </summary>
        // ASSUMPTION: skipping the 15 s preparation before wave 1 pays no gold; the S5 table only pays it for Resting.
        public bool Skip(out bool paysGold)
        {
            paysGold = State == GameState.Resting;
            if (!CanSkip)
            {
                paysGold = false;
                return false;
            }

            StartNextWave();
            return true;
        }

        /// <summary>WaveManager.OnWaveFinished(n). <paramref name="castleAlive"/> false → Lost first (S5 same-frame rule).</summary>
        public void NotifyWaveFinished(int wave, bool castleAlive)
        {
            if (State != GameState.InWave || wave != CurrentWave) return;

            if (!castleAlive)
            {
                End(false);
                return;
            }

            WavesCleared = wave;
            bool last = !IsEndless && wave >= WaveCount;
            if (!last)
            {
                TimeLeft = _restSeconds;
                SetState(GameState.Resting);
            }

            WaveCleared?.Invoke(wave);
            if (last) End(true);
        }

        /// <summary>Castle HP reached 0 in any playing state.</summary>
        public void NotifyCastleDestroyed()
        {
            if (IsPlaying) End(false);
        }

        private void StartNextWave()
        {
            CurrentWave++;
            TimeLeft = 0f;
            SetState(GameState.InWave);
            WaveStartRequested?.Invoke(CurrentWave);
        }

        private void End(bool won)
        {
            TimeLeft = 0f;
            SetState(won ? GameState.Won : GameState.Lost);
            Ended?.Invoke(won);
        }

        private void SetState(GameState state)
        {
            State = state;
            StateChanged?.Invoke(state);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>Debug panel (B-16): next wave to start is <paramref name="wave"/>; the caller clears the field.</summary>
        public void DebugJumpTo(int wave)
        {
            if (!IsPlaying || wave < 1) return;
            if (!IsEndless && wave > WaveCount) wave = WaveCount;

            CurrentWave = wave - 1;
            WavesCleared = Math.Max(0, wave - 1);
            StartNextWave();
        }

        /// <summary>Debug panel (B-16): win or lose now.</summary>
        public void DebugEnd(bool won)
        {
            if (IsPlaying) End(won);
        }
#endif
    }
}
