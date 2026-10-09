namespace FateBastion.Combat
{
    /// <summary>How two instances of the same status effect combine (S1).</summary>
    public enum StackRule
    {
        /// <summary>Keep the current value, restart the duration.</summary>
        RefreshDuration,

        /// <summary>Keep the stronger value and restart the duration.</summary>
        StrongestWins
    }
}
