namespace FateBastion.Editor.Heroes
{
    /// <summary>Totals shown at the top of the Validate Hero Prefabs window (S0b).</summary>
    public readonly struct HeroValidationSummary
    {
        public readonly int HeroCount;
        public readonly int ErrorCount;
        public readonly int WarningCount;

        public HeroValidationSummary(int heroCount, int errorCount, int warningCount)
        {
            HeroCount = heroCount;
            ErrorCount = errorCount;
            WarningCount = warningCount;
        }
    }
}
