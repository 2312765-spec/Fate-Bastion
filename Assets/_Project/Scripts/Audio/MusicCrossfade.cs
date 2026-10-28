namespace FateBastion.Audio
{
    /// <summary>
    /// Volume curve for switching tracks (S11): the incoming track rises 0 → 1 while the outgoing one falls from
    /// wherever it was to 0, over a fixed duration. Plain C#; MusicPlayer feeds it real (unscaled) time.
    /// </summary>
    public class MusicCrossfade
    {
        private float _duration;
        private float _elapsed;
        private float _outgoingStart;

        public bool IsActive { get; private set; }

        /// <summary>Incoming volume factor, 0–1.</summary>
        public float IncomingVolume { get; private set; } = 1f;

        /// <summary>Outgoing volume factor, 0–1.</summary>
        public float OutgoingVolume { get; private set; }

        /// <param name="outgoingVolume">Current factor of the track being faded out (lower than 1 when cut mid-fade).</param>
        public void Begin(float duration, float outgoingVolume)
        {
            _duration = duration;
            _elapsed = 0f;
            _outgoingStart = outgoingVolume;
            IsActive = true;
            Apply(duration <= 0f ? 1f : 0f);
        }

        /// <summary>Advances by <paramref name="deltaTime"/> seconds.</summary>
        public void Tick(float deltaTime)
        {
            if (!IsActive) return;
            _elapsed += deltaTime;
            Apply(_duration <= 0f ? 1f : _elapsed / _duration);
        }

        private void Apply(float t)
        {
            if (t >= 1f)
            {
                t = 1f;
                IsActive = false;
            }

            IncomingVolume = t;
            OutgoingVolume = _outgoingStart * (1f - t);
        }
    }
}
