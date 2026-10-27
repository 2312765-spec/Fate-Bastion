using System;
using UnityEngine;
using FateBastion.Enemies;

namespace FateBastion.Heroes
{
    /// <summary>Owns one pooled hero's initialized data, stats and target selection (S6, API 3.1 #3/#4).</summary>
    [DisallowMultipleComponent]
    public sealed class HeroController : MonoBehaviour
    {
        // 0.2 scaled seconds gives five target scans per second without querying every frame.
        private const float TargetSearchInterval = 0.2f;

        [SerializeField] private EnemyManager _enemyManager;

        private HeroStats _stats;
        private AttackModule _attackModule;
        private readonly TargetingModule _targeting = new TargetingModule();
        private HeroVisual _visual;
        private Transform _anchorFallback;
        private float _targetSearchTimer;

        public HeroData Data { get; private set; }
        public HeroStats Stats => _stats;
        public EnemyController CurrentTarget { get; private set; }
        public Transform Muzzle => _visual != null && _visual.Muzzle != null ? _visual.Muzzle : _anchorFallback;

        private void Awake()
        {
            _stats = GetComponent<HeroStats>();
            if (_stats == null)
                Debug.LogError($"{name}: HeroStats component is missing from this hero.", this);
            _attackModule = GetComponent<AttackModule>();

            _visual = GetComponentInChildren<HeroVisual>();
            _anchorFallback = transform;
            if (_visual == null)
                Debug.LogError($"{name}: no HeroVisual found under this hero; using the hero root as the visual anchor.", this);
        }

        /// <summary>Sets the shared enemy source when the hero is acquired by the placement/pool system.</summary>
        public void SetEnemyManager(EnemyManager enemyManager)
        {
            if (enemyManager == null)
            {
                Debug.LogError($"{name}: HeroController.SetEnemyManager received null.", this);
                _enemyManager = null;
                CurrentTarget = null;
                return;
            }

            _enemyManager = enemyManager;
            _targetSearchTimer = 0f;
            RefreshTarget();
        }

        /// <summary>Initializes a pooled hero from its data asset.</summary>
        public void Init(HeroData data)
        {
            if (data == null)
            {
                Debug.LogError($"{name}: HeroController.Init received null HeroData.", this);
                return;
            }
            if (_stats == null)
            {
                Debug.LogError($"{name}: HeroStats component is missing; cannot initialize this hero.", this);
                return;
            }

            ResetState();
            Data = data;
            try
            {
                _stats.Initialize(data);
            }
            catch (InvalidOperationException exception)
            {
                Data = null;
                _stats.Clear();
                _targeting.Clear();
                Debug.LogError(exception.Message, this);
                return;
            }
            _targeting.Initialize(data);
            RefreshTarget();
        }

        /// <summary>Clears transient state and level factors whenever this hero is taken from its pool.</summary>
        public void ResetState()
        {
            CurrentTarget = null;
            _targetSearchTimer = 0f;
            _stats?.ResetLevelMultipliers();
            _attackModule?.ResetState();
        }

        /// <summary>Clears references and calculated stats before this hero is returned to its pool.</summary>
        public void OnDespawn()
        {
            ResetState();
            Data = null;
            _stats?.Clear();
            _targeting.Clear();
        }

        private void Update()
        {
            if (Data == null || _enemyManager == null) return;

            _targetSearchTimer -= Time.deltaTime;
            if (_targetSearchTimer > 0f) return;

            _targetSearchTimer = TargetSearchInterval;
            RefreshTarget();
        }

        private void RefreshTarget()
        {
            if (_enemyManager == null || Data == null)
            {
                CurrentTarget = null;
                return;
            }

            CurrentTarget = _targeting.FindTarget(transform.position, _enemyManager.ActiveEnemies, _stats.Range);
        }
    }
}
