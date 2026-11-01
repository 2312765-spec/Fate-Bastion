namespace FateBastion.Audio
{
    /// <summary>
    /// Decides whether an effect may start now: a cap on copies alive per kind (never more than the pool holds) and a
    /// minimum spacing per kind (100 hits in one frame → a handful of sparks). Plain C# so it can be unit-tested (C-09).
    /// </summary>
    public class FxBudget
    {
        private readonly int[] _playing;
        private readonly float[] _lastStart;

        public FxBudget(int kindCount)
        {
            _playing = new int[kindCount];
            _lastStart = new float[kindCount];
            Reset();
        }

        public int CountPlaying(int kind) => _playing[kind];

        /// <summary>
        /// Takes one slot and records the time when fewer than <paramref name="maxConcurrent"/> are alive and
        /// <paramref name="minInterval"/> has passed since this kind last started. A refused attempt changes nothing.
        /// </summary>
        public bool TryStart(int kind, float now, int maxConcurrent, float minInterval)
        {
            if (_playing[kind] >= maxConcurrent) return false;
            if (now - _lastStart[kind] < minInterval) return false;
            _playing[kind]++;
            _lastStart[kind] = now;
            return true;
        }

        /// <summary>Frees one slot when a copy finished.</summary>
        public void Finish(int kind)
        {
            if (_playing[kind] > 0) _playing[kind]--;
        }

        public void Reset()
        {
            for (int i = 0; i < _playing.Length; i++)
            {
                _playing[i] = 0;
                _lastStart[i] = float.NegativeInfinity;
            }
        }
    }
}
