namespace FateBastion.Core
{
    /// <summary>A mode that Esc or a right mouse tap can cancel (placement preview, meteor aim, hero info panel).</summary>
    public interface ICancelable
    {
        /// <summary>Called by <see cref="CancelStack"/> after this mode was removed from the stack; exit the mode here.</summary>
        void OnCancel();
    }
}
