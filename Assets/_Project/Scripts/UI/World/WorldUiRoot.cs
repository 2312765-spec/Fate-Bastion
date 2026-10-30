using System;
using System.Collections.Generic;
using UnityEngine;
using FateBastion.Enemies;

namespace FateBastion.UI.World
{
    /// <summary>
    /// Root of the dynamic world UI canvas (C-14, S10): listens to EnemyManager.OnEnemySpawned/Despawned
    /// (TEAM_ASSIGNMENT 3.1 #9) and runs the health bars, hit flashes and the off-screen warning from one LateUpdate.
    /// Reads enemy data only; never changes enemy HP or state.
    /// </summary>
    [DisallowMultipleComponent]
    public class WorldUiRoot : MonoBehaviour
    {
        [Tooltip("EnemyManager of the match (scene object, dragged in by hand).")]
        [SerializeField] private EnemyManager _enemyManager;

        [Tooltip("Gameplay camera. Empty = Camera.main, looked up once.")]
        [SerializeField] private Camera _camera;

        [SerializeField] private WorldUiSettings _settings;
        [SerializeField] private WorldHealthBar _healthBarPrefab;
        [SerializeField] private RectTransform _healthBarParent;
        [SerializeField] private OffscreenWarning _warning;

        private Action<EnemyController> _onSpawned;
        private Action<EnemyController> _onDespawned;
        private WorldHealthBarPresenter _healthBars;
        private bool _subscribed;

        /// <summary>Null until the component is enabled with valid references.</summary>
        public WorldHealthBarPresenter HealthBars => _healthBars;

        public OffscreenWarning Warning => _warning;

        /// <summary>For tests and scenes built in code. Call before the GameObject is activated.</summary>
        public void Configure(EnemyManager enemyManager, Camera camera, WorldUiSettings settings,
            WorldHealthBar healthBarPrefab, RectTransform healthBarParent, OffscreenWarning warning)
        {
            _enemyManager = enemyManager;
            _camera = camera;
            _settings = settings;
            _healthBarPrefab = healthBarPrefab;
            _healthBarParent = healthBarParent;
            _warning = warning;
        }

        private void Awake()
        {
            _onSpawned = HandleSpawned;
            _onDespawned = HandleDespawned;
        }

        private void OnEnable()
        {
            if (!EnsureInitialized()) return;

            _enemyManager.OnEnemySpawned += _onSpawned;
            _enemyManager.OnEnemyDespawned += _onDespawned;
            _subscribed = true;

            // Enemies spawned before the UI was enabled (or while it was disabled) still get a bar.
            IReadOnlyList<EnemyController> active = _enemyManager.ActiveEnemies;
            for (int i = 0; i < active.Count; i++) _healthBars.Attach(active[i]);
        }

        private void OnDisable()
        {
            if (_subscribed && _enemyManager != null)
            {
                _enemyManager.OnEnemySpawned -= _onSpawned;
                _enemyManager.OnEnemyDespawned -= _onDespawned;
            }
            _subscribed = false;

            _healthBars?.DetachAll();
            if (_warning != null) _warning.Hide();
        }

        private void LateUpdate()
        {
            // UI follows real time: the camera still moves at ×2 and the arrow stays correct while paused.
            Refresh(Time.unscaledDeltaTime);
        }

        /// <summary>One frame of world UI work; LateUpdate calls it, tests call it directly.</summary>
        public void Refresh(float unscaledDeltaTime)
        {
            if (_healthBars == null) return;

            _healthBars.UpdatePositions(_camera);
            _healthBars.TickFlashes(unscaledDeltaTime);
            if (_warning != null) _warning.Tick(_camera, _enemyManager.ActiveEnemies, _settings, unscaledDeltaTime);
        }

        private bool EnsureInitialized()
        {
            if (_healthBars != null) return true;

            if (_enemyManager == null || _settings == null || _healthBarPrefab == null || _healthBarParent == null)
            {
                Debug.LogError($"{name}: WorldUiRoot cần EnemyManager, WorldUiSettings, WorldHealthBar prefab và HealthBars parent trong Inspector.", this);
                return false;
            }
            if (_warning == null) Debug.LogWarning($"{name}: WorldUiRoot chưa gắn OffscreenWarning; không có mũi tên cảnh báo.", this);

            if (_camera == null)
            {
                // Once, on enable: not a per-frame lookup.
                _camera = Camera.main;
                if (_camera == null) Debug.LogError($"{name}: WorldUiRoot không có Camera (kéo vào hoặc tag MainCamera).", this);
            }

            _healthBars = new WorldHealthBarPresenter(_healthBarPrefab, _healthBarParent, _settings);
            return true;
        }

        private void HandleSpawned(EnemyController enemy)
        {
            _healthBars.Attach(enemy);
        }

        private void HandleDespawned(EnemyController enemy)
        {
            _healthBars.Detach(enemy);
            if (_warning != null) _warning.NotifyDespawned(enemy);
        }
    }
}
