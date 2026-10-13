using UnityEngine;

namespace FateBastion.Player
{
    /// <summary>
    /// Tracks how far the mouse travelled while the right button is down, so a short drag of the camera is
    /// never taken as a cancel tap (S3: tap = released within 0.2 s and moved less than 5 px).
    /// </summary>
    public sealed class TapMovementGuard
    {
        private float _travelled;

        public bool IsPressed { get; private set; }

        public void Begin()
        {
            IsPressed = true;
            _travelled = 0f;
        }

        public void End()
        {
            IsPressed = false;
        }

        /// <summary>Adds a mouse delta in pixels; path length counts, so moving away and back is not a tap.</summary>
        public void AddDelta(Vector2 delta)
        {
            if (IsPressed)
            {
                _travelled += delta.magnitude;
            }
        }

        public bool IsTap(float maxPixels) => IsPressed && _travelled < maxPixels;
    }
}
