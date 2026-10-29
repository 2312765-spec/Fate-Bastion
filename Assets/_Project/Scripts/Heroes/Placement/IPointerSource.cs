using UnityEngine;

namespace FateBastion.Heroes
{
    /// <summary>
    /// Screen position of the mouse for the placement raycast (S4 step 2). InputRouter only republishes actions and has
    /// no pointer position yet, so PlacementSystem asks this interface. Meant to be implemented by
    /// SummonerInputRouter (C, issue "[Cần C] vị trí con trỏ cho PlacementSystem"); until then
    /// <see cref="InputSystemPointerStub"/> reads it.
    /// </summary>
    public interface IPointerSource
    {
        /// <summary>False when there is no pointer (no mouse connected).</summary>
        bool TryGetPointerPosition(out Vector2 screenPosition);
    }
}
