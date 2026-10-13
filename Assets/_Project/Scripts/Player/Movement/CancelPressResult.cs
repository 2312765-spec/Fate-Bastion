namespace FateBastion.Player
{
    /// <summary>What one Esc or right mouse tap did (S3).</summary>
    public enum CancelPressResult
    {
        /// <summary>Right mouse tap with nothing to cancel: does nothing.</summary>
        Ignored,

        /// <summary>The top CancelStack entry was cancelled.</summary>
        Cancelled,

        /// <summary>Esc with nothing to cancel: the pause screen should open.</summary>
        PauseRequested
    }
}
