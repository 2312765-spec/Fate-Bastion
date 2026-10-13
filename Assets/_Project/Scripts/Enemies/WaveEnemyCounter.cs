using System.Collections.Generic;

namespace FateBastion.Enemies
{
    /// <summary>
    /// Per-wave count of enemies still queued or alive, boss summons included (S2). A wave is finished when its
    /// schedule has requested everything and the count is zero; each wave is counted on its own and reported once.
    /// </summary>
    public sealed class WaveEnemyCounter
    {
        private struct Entry
        {
            public int Outstanding;
            public bool ScheduleComplete;
            public bool Finished;
        }

        // One entry per wave: a few dozen per match, created when the wave starts, not per frame.
        private readonly Dictionary<int, Entry> _entries = new Dictionary<int, Entry>();

        /// <summary>Starts (or restarts) counting a wave.</summary>
        public void BeginWave(int wave)
        {
            if (wave <= 0) return;
            _entries[wave] = new Entry();
        }

        public void Add(int wave)
        {
            if (wave <= 0) return;
            _entries.TryGetValue(wave, out Entry entry);
            entry.Outstanding++;
            _entries[wave] = entry;
        }

        /// <summary>Returns true when this removal finishes the wave.</summary>
        public bool Remove(int wave)
        {
            if (wave <= 0 || !_entries.TryGetValue(wave, out Entry entry) || entry.Finished) return false;
            if (entry.Outstanding > 0) entry.Outstanding--;
            bool finished = TryFinish(ref entry);
            _entries[wave] = entry;
            return finished;
        }

        /// <summary>The schedule has requested every enemy. Returns true when the wave is finished right now.</summary>
        public bool MarkScheduleComplete(int wave)
        {
            if (wave <= 0) return false;
            _entries.TryGetValue(wave, out Entry entry);
            if (entry.Finished) return false;
            entry.ScheduleComplete = true;
            bool finished = TryFinish(ref entry);
            _entries[wave] = entry;
            return finished;
        }

        public int GetOutstanding(int wave)
        {
            return _entries.TryGetValue(wave, out Entry entry) ? entry.Outstanding : 0;
        }

        public bool IsFinished(int wave)
        {
            return _entries.TryGetValue(wave, out Entry entry) && entry.Finished;
        }

        public void Clear() => _entries.Clear();

        private static bool TryFinish(ref Entry entry)
        {
            if (entry.Finished || !entry.ScheduleComplete || entry.Outstanding > 0) return false;
            entry.Finished = true;
            return true;
        }
    }
}
