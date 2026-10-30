using System;

namespace FateBastion.Heroes
{
    /// <summary>Tracks bounded rage without depending on Unity scene objects.</summary>
    public sealed class RageMeter
    {
        public float Current { get; private set; }
        public float Maximum { get; }
        public bool IsFull => Current >= Maximum;

        public RageMeter(float maximum)
        {
            if (!IsPositiveFinite(maximum))
                throw new ArgumentOutOfRangeException(nameof(maximum), maximum, "Maximum rage must be positive and finite.");
            Maximum = maximum;
        }

        public bool Add(float amount)
        {
            if (!IsPositiveFinite(amount))
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Rage gain must be positive and finite.");

            float next = Math.Min(Maximum, Current + amount);
            if (next == Current) return false;
            Current = next;
            return true;
        }

        public bool TryConsumeFull()
        {
            if (!IsFull) return false;
            Current = 0f;
            return true;
        }

        public bool Reset()
        {
            if (Current == 0f) return false;
            Current = 0f;
            return true;
        }

        private static bool IsPositiveFinite(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f;
    }
}
