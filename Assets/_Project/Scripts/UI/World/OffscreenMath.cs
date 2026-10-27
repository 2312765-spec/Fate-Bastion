using UnityEngine;

namespace FateBastion.UI.World
{
    /// <summary>Pure screen math for the off-screen warning arrow: on-screen test and clamping to the screen edge.</summary>
    public static class OffscreenMath
    {
        private const float Epsilon = 1e-6f;

        /// <summary>True when a Camera.WorldToScreenPoint result is in front of the camera and inside the screen.</summary>
        public static bool IsOnScreen(Vector3 screenPoint, Vector2 screenSize)
        {
            return screenPoint.z > 0f
                && screenPoint.x >= 0f && screenPoint.x <= screenSize.x
                && screenPoint.y >= 0f && screenPoint.y <= screenSize.y;
        }

        /// <summary>
        /// Point on the screen rectangle inset by <paramref name="margin"/> in the direction of the target, and the
        /// arrow angle in degrees (0 = right, 90 = up). Points behind the camera are mirrored by WorldToScreenPoint,
        /// so their direction is flipped back.
        /// </summary>
        public static Vector2 GetEdgePosition(Vector3 screenPoint, Vector2 screenSize, float margin, out float angleDegrees)
        {
            Vector2 center = screenSize * 0.5f;
            Vector2 direction = new Vector2(screenPoint.x, screenPoint.y) - center;
            if (screenPoint.z < 0f) direction = -direction;
            // Straight behind the camera has no direction on screen; point down (towards the player's back).
            if (direction.sqrMagnitude < Epsilon) direction = Vector2.down;

            angleDegrees = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            float halfWidth = Mathf.Max(0f, center.x - margin);
            float halfHeight = Mathf.Max(0f, center.y - margin);
            float scaleX = Mathf.Abs(direction.x) > Epsilon ? halfWidth / Mathf.Abs(direction.x) : float.PositiveInfinity;
            float scaleY = Mathf.Abs(direction.y) > Epsilon ? halfHeight / Mathf.Abs(direction.y) : float.PositiveInfinity;
            return center + direction * Mathf.Min(scaleX, scaleY);
        }
    }
}
