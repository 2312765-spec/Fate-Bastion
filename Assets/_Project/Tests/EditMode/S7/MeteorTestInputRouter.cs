using FateBastion.Core;

namespace FateBastion.Tests.EditMode.S7
{
    /// <summary>Exposes ability input events for deterministic EditMode tests.</summary>
    public sealed class MeteorTestInputRouter : InputRouter
    {
        protected override void Awake()
        {
            // The test injects this router directly and must not replace the scene's singleton.
        }

        public void PressMeteor() => RaiseMeteor();
        public void SelectDeckSlot(int slot) => RaiseDeckSlot(slot);
    }
}
