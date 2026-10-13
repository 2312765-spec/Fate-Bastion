using UnityEngine;

namespace FateBastion.Player
{
    /// <summary>Pure yaw / pitch / zoom state of the third-person camera with clamping and zoom smoothing (S3).</summary>
    public sealed class CameraOrbit
    {
        private readonly float _minDistance;
        private readonly float _maxDistance;
        private readonly float _minPitch;
        private readonly float _maxPitch;

        public CameraOrbit(float minDistance, float maxDistance, float minPitch, float maxPitch,
            float initialYaw, float initialPitch, float initialDistance)
        {
            _minDistance = minDistance;
            _maxDistance = maxDistance;
            _minPitch = minPitch;
            _maxPitch = maxPitch;

            Yaw = Mathf.Repeat(initialYaw, 360f);
            Pitch = Mathf.Clamp(initialPitch, minPitch, maxPitch);
            TargetDistance = Mathf.Clamp(initialDistance, minDistance, maxDistance);
            Distance = TargetDistance;
        }

        /// <summary>Degrees around Y, in [0, 360).</summary>
        public float Yaw { get; private set; }

        /// <summary>Degrees looking down, clamped to the settings range.</summary>
        public float Pitch { get; private set; }

        /// <summary>Smoothed camera distance actually used this frame.</summary>
        public float Distance { get; private set; }

        /// <summary>Distance the zoom is moving towards.</summary>
        public float TargetDistance { get; private set; }

        /// <summary>Mouse delta in pixels; speeds are degrees per pixel. Mouse up looks up (lower pitch).</summary>
        public void ApplyLook(Vector2 delta, float yawSpeed, float pitchSpeed)
        {
            Yaw = Mathf.Repeat(Yaw + delta.x * yawSpeed, 360f);
            Pitch = Mathf.Clamp(Pitch - delta.y * pitchSpeed, _minPitch, _maxPitch);
        }

        /// <summary>
        /// One zoom step per scroll event, whatever its size: Windows can report 120 per notch,
        /// newer Input System 1, so only the sign is used. Scroll up = closer.
        /// </summary>
        public void ApplyZoom(float scroll, float zoomStep)
        {
            if (Mathf.Approximately(scroll, 0f))
            {
                return;
            }

            TargetDistance = Mathf.Clamp(TargetDistance - Mathf.Sign(scroll) * zoomStep, _minDistance, _maxDistance);
        }

        /// <summary>Moves Distance towards TargetDistance; frame-rate independent exponential smoothing.</summary>
        public void Tick(float dt, float smoothTime)
        {
            if (smoothTime <= 0f)
            {
                Distance = TargetDistance;
                return;
            }

            if (dt <= 0f)
            {
                return;
            }

            float t = 1f - Mathf.Exp(-dt / smoothTime);
            Distance = Mathf.Lerp(Distance, TargetDistance, t);
        }
    }
}
