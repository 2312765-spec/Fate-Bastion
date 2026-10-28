namespace FateBastion.Audio
{
    /// <summary>UI cues other systems ask for by name, so they never need a SoundData reference (S11).</summary>
    public enum UiSound
    {
        Click,
        PanelOpen,
        PanelClose,
        /// <summary>Not enough Gold, invalid placement spot, target out of range.</summary>
        Error,
        Merge
    }
}
