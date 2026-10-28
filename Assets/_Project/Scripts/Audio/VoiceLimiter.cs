using System.Collections.Generic;

namespace FateBastion.Audio
{
    /// <summary>
    /// Counts how many copies of each sound are playing and refuses new ones past the cap (S11:
    /// "100 enemies die at once → at most 4 death sounds"). Plain C# so it can be unit-tested.
    /// </summary>
    public class VoiceLimiter<TKey>
    {
        private readonly Dictionary<TKey, int> _playing;

        /// <param name="capacity">Expected number of distinct sounds; sized up front so adding keys does not allocate.</param>
        public VoiceLimiter(int capacity)
        {
            _playing = new Dictionary<TKey, int>(capacity);
        }

        public int CountPlaying(TKey key) => _playing.TryGetValue(key, out int count) ? count : 0;

        /// <summary>Takes one slot for <paramref name="key"/>; false when <paramref name="maxConcurrent"/> are already playing.</summary>
        public bool TryAcquire(TKey key, int maxConcurrent)
        {
            int count = CountPlaying(key);
            if (count >= maxConcurrent) return false;
            _playing[key] = count + 1;
            return true;
        }

        /// <summary>Frees one slot when a copy finished playing.</summary>
        public void Release(TKey key)
        {
            int count = CountPlaying(key);
            if (count > 0) _playing[key] = count - 1;
        }

        public void Clear() => _playing.Clear();
    }
}
