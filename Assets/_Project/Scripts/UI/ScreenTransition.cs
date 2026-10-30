namespace FateBastion.UI
{
    /// <summary>How a <see cref="UIScreen"/> enters and leaves (S10). All variants also fade the CanvasGroup.</summary>
    public enum ScreenTransition
    {
        /// <summary>Alpha only.</summary>
        Fade,

        /// <summary>Full pages: the content slides in from the right and out to the left.</summary>
        Slide,

        /// <summary>Dialogs: the content pops from a smaller scale with a small overshoot.</summary>
        Pop
    }
}
