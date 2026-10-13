using UnityEngine;
using Unity.Cinemachine;
using FateBastion.Core;

namespace FateBastion.Player
{
    /// <summary>
    /// Third-person camera (S3): orbits a pivot that follows the Summoner, rotated by Look while the right mouse is
    /// held and zoomed by the scroll wheel. Cinemachine Third Person Follow places the camera, a Deoccluder keeps it
    /// out of walls, an Impulse Listener takes camera shake. Runs in real time.
    /// </summary>
    // Before the Cinemachine Brain's LateUpdate, so the pivot is placed before the camera reads it.
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public class CameraRig : MonoBehaviour
    {
        [SerializeField] private CameraSettings _settings;

        [Tooltip("The Summoner (feet position).")]
        [SerializeField] private Transform _followTarget;

        [Tooltip("Empty pivot the Cinemachine Camera tracks; this script moves and rotates it. Not a child of the Summoner.")]
        [SerializeField] private Transform _pivot;

        [SerializeField] private CinemachineCamera _cinemachineCamera;
        [SerializeField] private CinemachineThirdPersonFollow _thirdPersonFollow;

        [Tooltip("Brain on the Main Camera; Ignore Time Scale is forced on so the camera works while paused.")]
        [SerializeField] private CinemachineBrain _brain;

        private CameraOrbit _orbit;
        private InputRouter _router;
        private bool _valid;

        /// <summary>Current camera yaw in degrees.</summary>
        public float Yaw => _orbit != null ? _orbit.Yaw : 0f;

        private void Awake()
        {
            _valid = Validate();
            if (!_valid)
            {
                enabled = false;
                return;
            }

            _brain.IgnoreTimeScale = true;
            _cinemachineCamera.Follow = _pivot;

            _orbit = new CameraOrbit(_settings.MinDistance, _settings.MaxDistance, _settings.MinPitch, _settings.MaxPitch,
                _followTarget.eulerAngles.y, _settings.DefaultPitch, _settings.DefaultDistance);
            PlacePivot();
        }

        private bool Validate()
        {
            bool ok = true;
            ok &= Require(_settings, nameof(_settings));
            ok &= Require(_followTarget, nameof(_followTarget));
            ok &= Require(_pivot, nameof(_pivot));
            ok &= Require(_cinemachineCamera, nameof(_cinemachineCamera));
            ok &= Require(_thirdPersonFollow, nameof(_thirdPersonFollow));
            ok &= Require(_brain, nameof(_brain));
            return ok;
        }

        private bool Require(Object reference, string fieldName)
        {
            if (reference != null)
            {
                return true;
            }

            UnityEngine.Debug.LogError($"{nameof(CameraRig)} on '{name}': {fieldName} is not assigned.", this);
            return false;
        }

        private void Start()
        {
            _router = InputRouter.Instance;
            if (_router == null)
            {
                UnityEngine.Debug.LogError($"{nameof(CameraRig)} on '{name}': no {nameof(InputRouter)} in the scene.", this);
                return;
            }

            _router.Look += OnLook;
            _router.Zoom += OnZoom;
        }

        private void OnDestroy()
        {
            if (_router != null)
            {
                _router.Look -= OnLook;
                _router.Zoom -= OnZoom;
            }
        }

        private void OnLook(Vector2 delta) => _orbit.ApplyLook(delta, _settings.YawSpeed, _settings.PitchSpeed);

        private void OnZoom(float scroll) => _orbit.ApplyZoom(scroll, _settings.ZoomStep);

        private void LateUpdate()
        {
            // Real time: zoom easing keeps working at x2 and while paused.
            _orbit.Tick(Time.unscaledDeltaTime, _settings.ZoomSmoothTime);
            PlacePivot();
        }

        private void PlacePivot()
        {
            _pivot.SetPositionAndRotation(
                _followTarget.position + Vector3.up * _settings.TargetHeight,
                Quaternion.Euler(_orbit.Pitch, _orbit.Yaw, 0f));
            _thirdPersonFollow.CameraDistance = _orbit.Distance;
        }
    }
}
