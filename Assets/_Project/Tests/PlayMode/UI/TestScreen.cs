using FateBastion.UI;

namespace FateBastion.Tests.PlayMode.UI
{
    /// <summary>UIScreen that counts its open/close callbacks for the UIManager tests.</summary>
    public sealed class TestScreen : UIScreen
    {
        public int OpenedCount { get; private set; }
        public int ClosedCount { get; private set; }

        protected override void OnOpened() => OpenedCount++;
        protected override void OnClosed() => ClosedCount++;
    }
}
