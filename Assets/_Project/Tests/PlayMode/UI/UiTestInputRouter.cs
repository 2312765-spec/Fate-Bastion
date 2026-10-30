using FateBastion.Core;

namespace FateBastion.Tests.PlayMode.UI
{
    /// <summary>InputRouter whose Cancel (Esc) the UIManager tests raise by hand.</summary>
    public sealed class UiTestInputRouter : InputRouter
    {
        public void SendCancel() => RaiseCancel();
    }
}
