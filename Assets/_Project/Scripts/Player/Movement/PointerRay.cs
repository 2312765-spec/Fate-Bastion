using UnityEngine;

namespace FateBastion.Player
{
    /// <summary>Pure rule for when the mouse may aim into the world (S3, S7): Summoner map, a pointer, a camera, not over UI.</summary>
    public static class PointerRay
    {
        /// <summary>
        /// Builds the camera ray through <paramref name="screenPosition"/>. False (and a default ray) when the Summoner
        /// map is off, there is no pointer, the camera is missing/disabled, or the pointer is over UI.
        /// </summary>
        public static bool TryBuild(bool summonerMapActive, bool hasPointer, Vector2 screenPosition, Camera camera,
            bool pointerOverUi, out Ray ray)
        {
            if (!summonerMapActive || !hasPointer || pointerOverUi || camera == null
                || !camera.enabled || !camera.gameObject.activeInHierarchy)
            {
                ray = default;
                return false;
            }

            ray = camera.ScreenPointToRay(screenPosition);
            return true;
        }
    }
}
