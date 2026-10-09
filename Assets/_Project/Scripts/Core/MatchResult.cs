namespace FateBastion.Core
{
    /// <summary>Outcome of one match, passed to the result screen and to the save layer (S5, S9).</summary>
    public readonly struct MatchResult
    {
        public readonly bool Won;
        public readonly int Stars;
        public readonly int WavesReached;
        public readonly int CastleHPLeft;
        public readonly int GemsAwarded;
        public readonly bool IsEndless;

        public MatchResult(bool won, int stars, int wavesReached, int castleHPLeft, int gemsAwarded, bool isEndless)
        {
            Won = won;
            Stars = stars;
            WavesReached = wavesReached;
            CastleHPLeft = castleHPLeft;
            GemsAwarded = gemsAwarded;
            IsEndless = isEndless;
        }
    }
}
