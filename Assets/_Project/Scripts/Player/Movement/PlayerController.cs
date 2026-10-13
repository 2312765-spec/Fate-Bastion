using UnityEngine;
using FateBastion.Core;

namespace FateBastion.Player
{
    /// <summary>
    /// Moves the Summoner with a CharacterController, relative to the camera, in real time (S3).
    /// Input only arrives through InputRouter events; no jump; falling off the map returns to the spawn point.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [DisallowMultipleComponent]
    public class PlayerController : MonoBehaviour
    {
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int CastHash = Animator.StringToHash("Cast");

        [SerializeField] private PlayerMovementSettings _settings;

        [Tooltip("Camera whose yaw defines 'forward' (the Main Camera driven by the Cinemachine Brain).")]
        [SerializeField] private Transform _cameraTransform;

        [Tooltip("Where the Summoner appears next to the Castle, and returns after falling off the map.")]
        [SerializeField] private Transform _spawnPoint;

        private CharacterController _characterController;
        private Animator _animator;
        private SummonerMotor _motor;
        private InputRouter _router;
        private Vector2 _moveInput;
        private bool _sprint;
        private float _turnVelocity;
        private bool _started;

        /// <summary>Planar speed of the last frame in m/s (real time); drives the Idle / Walk / Run blend.</summary>
        public float CurrentSpeed { get; private set; }

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            // Default 0.001 m drops whole frames of movement at high FPS (6 m/s × 1/6000 s); Unity recommends 0.
            _characterController.minMoveDistance = 0f;
            _animator = GetComponentInChildren<Animator>();
            if (_animator != null)
            {
                // The Summoner lives in real time, like its movement.
                _animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            }
        }

        /// <summary>Sets references from code (tests, or a spawner); call right after AddComponent, before Start.</summary>
        public void Configure(PlayerMovementSettings settings, Transform cameraTransform, Transform spawnPoint)
        {
            _settings = settings;
            _cameraTransform = cameraTransform;
            _spawnPoint = spawnPoint;
        }

        private void Start()
        {
            if (_settings == null)
            {
                UnityEngine.Debug.LogError($"{nameof(PlayerController)} on '{name}': {nameof(PlayerMovementSettings)} is not assigned.", this);
                enabled = false;
                return;
            }

            if (_cameraTransform == null)
            {
                UnityEngine.Debug.LogError($"{nameof(PlayerController)} on '{name}': camera transform is not assigned; moving along world axes.", this);
            }

            _motor = new SummonerMotor(_settings.WalkSpeed, _settings.SprintSpeed, _settings.Gravity);
            if (_spawnPoint != null)
            {
                TeleportTo(_spawnPoint.position);
            }

            // Subscribed in Start, not OnEnable, so the router's Awake has set InputRouter.Instance whatever the order.
            _started = true;
            Subscribe();
        }

        private void OnEnable()
        {
            if (_started)
            {
                Subscribe();
            }
        }

        private void OnDisable()
        {
            Unsubscribe();
            _moveInput = Vector2.zero;
            _sprint = false;
        }

        private void Subscribe()
        {
            if (_router != null)
            {
                return;
            }

            _router = InputRouter.Instance;
            if (_router == null)
            {
                UnityEngine.Debug.LogError($"{nameof(PlayerController)} on '{name}': no {nameof(InputRouter)} in the scene.", this);
                return;
            }

            _router.Move += OnMove;
            _router.SprintChanged += OnSprintChanged;
        }

        private void Unsubscribe()
        {
            if (_router == null)
            {
                return;
            }

            _router.Move -= OnMove;
            _router.SprintChanged -= OnSprintChanged;
            _router = null;
        }

        private void OnMove(Vector2 value) => _moveInput = value;

        private void OnSprintChanged(bool value) => _sprint = value;

        private void Update()
        {
            // Real time: x2 does not speed the Summoner up; never divide by timeScale (0 while paused).
            float dt = Time.unscaledDeltaTime;

            if (transform.position.y < _settings.FallResetY)
            {
                TeleportTo(_spawnPoint != null ? _spawnPoint.position : Vector3.zero);
                return;
            }

            float cameraYaw = _cameraTransform != null ? _cameraTransform.eulerAngles.y : 0f;
            Vector3 velocity = _motor.ComputeVelocity(_moveInput, cameraYaw, _sprint, _characterController.isGrounded, dt);
            _characterController.Move(velocity * dt);

            Vector3 planar = new Vector3(velocity.x, 0f, velocity.z);
            CurrentSpeed = planar.magnitude;
            if (CurrentSpeed > 0.01f)
            {
                float targetYaw = Mathf.Atan2(planar.x, planar.z) * Mathf.Rad2Deg;
                float yaw = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetYaw, ref _turnVelocity,
                    _settings.TurnSmoothTime, Mathf.Infinity, dt);
                transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            }

            if (_animator != null)
            {
                _animator.SetFloat(SpeedHash, CurrentSpeed);
            }
        }

        /// <summary>Moves the Summoner instantly (spawn, falling off the map).</summary>
        public void TeleportTo(Vector3 position)
        {
            // A CharacterController overrides transform changes while enabled.
            _characterController.enabled = false;
            transform.position = position;
            _characterController.enabled = true;
            _motor?.ResetVertical();
        }

        /// <summary>Returns to the spawn point next to the Castle (S3 flow step 1).</summary>
        public void TeleportToSpawn()
        {
            if (_spawnPoint != null)
            {
                TeleportTo(_spawnPoint.position);
            }
        }

        /// <summary>Plays the Cast animation (e.g. Meteor thrown). Visual only.</summary>
        public void PlayCast()
        {
            if (_animator != null)
            {
                _animator.SetTrigger(CastHash);
            }
        }
    }
}
