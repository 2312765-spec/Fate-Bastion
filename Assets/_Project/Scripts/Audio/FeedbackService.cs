using FateBastion.Core;
using Unity.Cinemachine;
using UnityEngine;

namespace FateBastion.Audio
{
    /// <summary>
    /// Camera shake (S11, C-18) through Cinemachine Impulse: meteor explosion, boss wave start, castle damage and
    /// Ultimate (via <see cref="AnimatorShakeBehaviour"/>). The only system that shakes the camera; it runs in real
    /// time like the camera itself. Lives in the _Audio prefab, one per scene.
    /// </summary>
    // After default scripts, so a GameEvents.ResetAll in GameManager.Start does not wipe these subscriptions.
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public class FeedbackService : MonoBehaviour
    {
        [SerializeField] private FeedbackSettings _settings;

        [Tooltip("Shared AudioConfig: its bossWaveInterval tells which waves are boss waves.")]
        [SerializeField] private AudioConfig _audioConfig;

        public static FeedbackService Instance { get; private set; }

        /// <summary>Settings toggle "Rung camera"; static so it carries over when the next scene loads (S9 default: on).</summary>
        public static bool ShakeEnabled { get; private set; } = true;

        private static readonly int KindCount = System.Enum.GetValues(typeof(ShakeKind)).Length;

        private readonly ShakeGate _gate = new ShakeGate(KindCount);
        private bool _started;
        private bool _subscribed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError($"{nameof(FeedbackService)}: a second instance on '{name}' was disabled; keep one _Audio per scene.", this);
                enabled = false;
                return;
            }

            Instance = this;
            CinemachineImpulseManager.Instance.IgnoreTimeScale = true;
            if (_settings == null) Debug.LogError($"{nameof(FeedbackService)} on '{name}': {nameof(_settings)} is not assigned.", this);
            if (_audioConfig == null) Debug.LogWarning($"{nameof(FeedbackService)} on '{name}': no {nameof(AudioConfig)}; boss waves will not shake.", this);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Sets the assets from code (tests: add to an inactive object, configure, then activate).</summary>
        public void Configure(FeedbackSettings settings, AudioConfig audioConfig)
        {
            _settings = settings;
            _audioConfig = audioConfig;
        }

        /// <summary>
        /// Settings (B) calls this when the toggle changes and when applying the saved value. Turning it off also
        /// stops a shake already running, so the change is felt at once (S11).
        /// </summary>
        public static void SetShakeEnabled(bool shakeEnabled)
        {
            ShakeEnabled = shakeEnabled;
            if (!shakeEnabled) CinemachineImpulseManager.Instance.Clear();
        }

        private void Start()
        {
            _started = true;
            Subscribe();
        }

        private void OnEnable()
        {
            if (_started) Subscribe();
        }

        private void OnDisable() => Unsubscribe();

        private void Subscribe()
        {
            if (_subscribed) return;
            _subscribed = true;
            AbilityEvents.OnMeteorExploded += HandleMeteorExploded;
            GameEvents.OnWaveStarted += HandleWaveStarted;
            GameEvents.OnCastleDamaged += HandleCastleDamaged;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;
            AbilityEvents.OnMeteorExploded -= HandleMeteorExploded;
            GameEvents.OnWaveStarted -= HandleWaveStarted;
            GameEvents.OnCastleDamaged -= HandleCastleDamaged;
        }

        private void HandleMeteorExploded(Vector3 point, float radius) => Shake(ShakeKind.MeteorExplosion, point);

        private void HandleWaveStarted(int wave)
        {
            if (_audioConfig != null && _audioConfig.IsBossWave(wave)) Shake(ShakeKind.BossAppear, transform.position);
        }

        private void HandleCastleDamaged(int currentHP, int maxHP) => Shake(ShakeKind.CastleHit, transform.position);

        /// <summary>
        /// Starts a shake of <paramref name="kind"/> from <paramref name="position"/> (only matters for Dissipating
        /// profiles). False when shake is off in Settings, the kind is still in its minimum interval, or its force is 0.
        /// </summary>
        public bool Shake(ShakeKind kind, Vector3 position)
        {
            if (!ShakeEnabled || _settings == null) return false;

            ShakeProfile profile = _settings.Get(kind);
            if (profile == null || profile.definition == null || profile.force <= 0f) return false;
            if (!_gate.TryPass((int)kind, Time.unscaledTime, profile.minInterval)) return false;

            profile.definition.CreateEvent(position, Vector3.down * profile.force);
            return true;
        }

#if UNITY_EDITOR
        // Right-click the component in Play mode to feel each shake while tuning FeedbackSettings.
        [ContextMenu("Test Shake/Meteor Explosion")]
        private void TestMeteor() => Shake(ShakeKind.MeteorExplosion, Camera.main != null ? Camera.main.transform.position : transform.position);

        [ContextMenu("Test Shake/Boss Appear")]
        private void TestBoss() => Shake(ShakeKind.BossAppear, transform.position);

        [ContextMenu("Test Shake/Castle Hit")]
        private void TestCastle() => Shake(ShakeKind.CastleHit, transform.position);

        [ContextMenu("Test Shake/Ultimate")]
        private void TestUltimate() => Shake(ShakeKind.Ultimate, Camera.main != null ? Camera.main.transform.position : transform.position);
#endif

        /// <summary>True while any shake is felt at <paramref name="listenerPosition"/>; for tests and debugging.</summary>
        public static bool IsShakingAt(Vector3 listenerPosition) =>
            CinemachineImpulseManager.Instance.GetImpulseAt(listenerPosition, false, ~0, out _, out _);
    }
}
