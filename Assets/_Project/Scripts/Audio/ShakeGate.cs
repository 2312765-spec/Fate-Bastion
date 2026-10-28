namespace FateBastion.Audio
{
    /// <summary>
    /// Decides whether a shake may start now: each kind keeps its own minimum spacing (S11, C-18).
    /// Plain C# so the timing rules are unit-tested; FeedbackService feeds it real time.
    /// </summary>
    public class ShakeGate
    {
        private readonly float[] _lastTime;

        public ShakeGate(int kindCount)
        {
            _lastTime = new float[kindCount];
            Reset();
        }

        /// <summary>True and records the time when <paramref name="minInterval"/> has passed since this kind last shook.</summary>
        public bool TryPass(int kind, float now, float minInterval)
        {
            if (now - _lastTime[kind] < minInterval) return false;
            _lastTime[kind] = now;
            return true;
        }

        public void Reset()
        {
            for (int i = 0; i < _lastTime.Length; i++) _lastTime[i] = float.NegativeInfinity;
        }
    }
}
