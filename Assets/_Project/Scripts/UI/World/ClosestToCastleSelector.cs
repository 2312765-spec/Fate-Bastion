namespace FateBastion.UI.World
{
    /// <summary>Keeps the index with the highest path progress at or above a threshold during one scan (no allocation).</summary>
    public sealed class ClosestToCastleSelector
    {
        private readonly float _threshold;

        public ClosestToCastleSelector(float threshold)
        {
            _threshold = threshold;
            Begin();
        }

        /// <summary>Index passed to Consider with the highest progress; -1 when none reached the threshold.</summary>
        public int BestIndex { get; private set; }

        public float BestProgress { get; private set; }

        public void Begin()
        {
            BestIndex = -1;
            BestProgress = float.NegativeInfinity;
        }

        public void Consider(int index, float progress)
        {
            if (progress < _threshold || progress <= BestProgress) return;
            BestIndex = index;
            BestProgress = progress;
        }
    }
}
