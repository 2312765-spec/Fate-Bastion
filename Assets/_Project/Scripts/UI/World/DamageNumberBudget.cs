namespace FateBastion.UI.World
{
    /// <summary>
    /// Which damage number slot to use (S11): at most N numbers on screen; when full, a new number larger than the
    /// smallest one shown takes its slot, otherwise the new one is dropped. Plain C#, fixed arrays, no allocation.
    /// </summary>
    public sealed class DamageNumberBudget
    {
        private readonly int[] _amounts;
        private readonly bool[] _active;

        public DamageNumberBudget(int capacity)
        {
            _amounts = new int[capacity];
            _active = new bool[capacity];
        }

        public int Capacity => _active.Length;
        public int ActiveCount { get; private set; }

        public bool IsActive(int slot) => _active[slot];

        /// <summary>Slot for a number of <paramref name="amount"/>, or -1 when it is dropped. A returned slot may be one that was showing.</summary>
        public int TryReserve(int amount)
        {
            int smallest = -1;
            for (int i = 0; i < _active.Length; i++)
            {
                if (!_active[i])
                {
                    Take(i, amount);
                    return i;
                }

                if (smallest < 0 || _amounts[i] < _amounts[smallest]) smallest = i;
            }

            // Full: "bỏ bớt số nhỏ" – replace the smallest only when the new number is bigger.
            if (smallest < 0 || amount <= _amounts[smallest]) return -1;
            _amounts[smallest] = amount;
            return smallest;
        }

        public void Release(int slot)
        {
            if (!_active[slot]) return;
            _active[slot] = false;
            ActiveCount--;
        }

        public void ReleaseAll()
        {
            for (int i = 0; i < _active.Length; i++) _active[i] = false;
            ActiveCount = 0;
        }

        private void Take(int slot, int amount)
        {
            _active[slot] = true;
            _amounts[slot] = amount;
            ActiveCount++;
        }
    }
}
