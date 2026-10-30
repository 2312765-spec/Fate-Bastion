using FateBastion.Core;
using UnityEngine;

namespace FateBastion.UI.World
{
    /// <summary>
    /// Floating damage numbers (S10, S11, C-14) from CombatEvents.OnDamageDealt: a fixed pool of numbers created at
    /// load, at most N on screen (smaller numbers dropped first), colour by element, meteor bigger. Runs in real
    /// time like the rest of the world UI. Lives on the dynamic world UI canvas.
    /// </summary>
    // After default scripts, so a CombatEvents.ResetAll in the match bootstrap's Start does not wipe this subscription.
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public class DamageNumberSpawner : MonoBehaviour
    {
        [SerializeField] private WorldUiSettings _settings;
        [SerializeField] private DamageNumber _numberPrefab;
        [SerializeField] private RectTransform _numberParent;

        [Tooltip("Gameplay camera. Empty = Camera.main, looked up once.")]
        [SerializeField] private Camera _camera;

        private DamageNumber[] _numbers;
        private DamageNumberBudget _budget;
        private bool _started;
        private bool _subscribed;

        /// <summary>Numbers on screen now.</summary>
        public int ActiveCount => _budget != null ? _budget.ActiveCount : 0;

        /// <summary>Numbers created so far; fixed after load.</summary>
        public int CreatedCount => _numbers != null ? _numbers.Length : 0;

        /// <summary>For tests and scenes built in code. Call before the GameObject is activated.</summary>
        public void Configure(WorldUiSettings settings, DamageNumber numberPrefab, RectTransform numberParent, Camera camera)
        {
            _settings = settings;
            _numberPrefab = numberPrefab;
            _numberParent = numberParent;
            _camera = camera;
        }

        private void Start()
        {
            if (!EnsureInitialized()) return;
            _started = true;
            Subscribe();
        }

        private void OnEnable()
        {
            if (_started) Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            if (_numbers == null) return;
            for (int i = 0; i < _numbers.Length; i++) _numbers[i].Hide();
            _budget.ReleaseAll();
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            _subscribed = true;
            CombatEvents.OnDamageDealt += HandleDamageDealt;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;
            CombatEvents.OnDamageDealt -= HandleDamageDealt;
        }

        private bool EnsureInitialized()
        {
            if (_numbers != null) return true;

            if (_settings == null || _numberPrefab == null || _numberParent == null)
            {
                Debug.LogError($"{name}: DamageNumberSpawner cần WorldUiSettings, DamageNumber prefab và parent trong Inspector.", this);
                return false;
            }

            if (_camera == null)
            {
                // Once, at load: not a per-frame lookup.
                _camera = Camera.main;
                if (_camera == null) Debug.LogError($"{name}: DamageNumberSpawner không có Camera (kéo vào hoặc tag MainCamera).", this);
            }

            // Prewarm the whole budget at load; no Instantiate during the match.
            int max = _settings.DamageNumberMax;
            _budget = new DamageNumberBudget(max);
            _numbers = new DamageNumber[max];
            for (int i = 0; i < max; i++)
            {
                DamageNumber number = Instantiate(_numberPrefab, _numberParent, false);
                number.InitializeHidden();
                _numbers[i] = number;
            }

            return true;
        }

        private void HandleDamageDealt(DamageDealtInfo info)
        {
            if (info.Amount <= 0) return;

            int slot = _budget.TryReserve(info.Amount);
            if (slot < 0) return;

            Vector3 point = info.HitPoint + Vector3.up * _settings.DamageNumberHeight;
            _numbers[slot].Show(info.Amount,
                DamageNumberStyle.GetColor(info.Type, info.Element, _settings),
                DamageNumberStyle.GetFontSize(info.Type, _settings),
                point);
        }

        private void LateUpdate()
        {
            Refresh(Time.unscaledDeltaTime);
        }

        /// <summary>One frame of damage number work; LateUpdate calls it, tests call it directly.</summary>
        public void Refresh(float unscaledDeltaTime)
        {
            if (_numbers == null || _camera == null || _budget.ActiveCount == 0) return;

            float lifetime = _settings.DamageNumberLifetime;
            float rise = _settings.DamageNumberRise;
            for (int i = 0; i < _numbers.Length; i++)
            {
                if (!_budget.IsActive(i)) continue;
                if (_numbers[i].Tick(_camera, unscaledDeltaTime, lifetime, rise)) continue;

                _numbers[i].Hide();
                _budget.Release(i);
            }
        }
    }
}
