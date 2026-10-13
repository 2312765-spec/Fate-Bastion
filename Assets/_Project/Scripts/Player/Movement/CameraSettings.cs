using UnityEngine;

namespace FateBastion.Player
{
    /// <summary>Third-person camera limits and speeds (S3). One asset at Data/Settings/CameraSettings, owned by C.</summary>
    [CreateAssetMenu(menuName = "Fate Bastion/Settings/Camera", fileName = "CameraSettings")]
    public class CameraSettings : ScriptableObject
    {
        [Header("Zoom (m)")]
        [SerializeField, Min(0.5f)] private float _minDistance = 4f;
        [SerializeField, Min(0.5f)] private float _maxDistance = 25f;
        [SerializeField, Min(0.5f)] private float _defaultDistance = 12f;

        [Tooltip("Metres per scroll notch.")]
        [SerializeField, Min(0f)] private float _zoomStep = 1.5f;

        [Tooltip("Seconds to ease towards the new zoom distance; 0 = instant.")]
        [SerializeField, Min(0f)] private float _zoomSmoothTime = 0.1f;

        [Header("Angle (degrees looking down)")]
        [SerializeField, Range(-89f, 89f)] private float _minPitch = 10f;
        [SerializeField, Range(-89f, 89f)] private float _maxPitch = 60f;
        [SerializeField, Range(-89f, 89f)] private float _defaultPitch = 35f;

        [Header("Rotation (degrees per mouse pixel)")]
        [SerializeField, Min(0f)] private float _yawSpeed = 0.15f;
        [SerializeField, Min(0f)] private float _pitchSpeed = 0.1f;

        [Tooltip("Height of the follow target above the Summoner's feet, m.")]
        [SerializeField, Min(0f)] private float _targetHeight = 1.5f;

        public float MinDistance => _minDistance;
        public float MaxDistance => _maxDistance;
        public float DefaultDistance => _defaultDistance;
        public float ZoomStep => _zoomStep;
        public float ZoomSmoothTime => _zoomSmoothTime;
        public float MinPitch => _minPitch;
        public float MaxPitch => _maxPitch;
        public float DefaultPitch => _defaultPitch;
        public float YawSpeed => _yawSpeed;
        public float PitchSpeed => _pitchSpeed;
        public float TargetHeight => _targetHeight;
    }
}
