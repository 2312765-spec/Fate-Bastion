using System;
using System.Collections.Generic;
using UnityEngine;
using FateBastion.Core;
using FateBastion.Enemies;
using FateBastion.Skills;

namespace FateBastion.Heroes
{
    /// <summary>Owns one pooled hero's initialized data, stats and target selection (S6, API 3.1 #3/#4).</summary>
    [DisallowMultipleComponent]
    public sealed class HeroController : MonoBehaviour, ISkillContext
    {
        // 0.2 scaled seconds gives five target scans per second without querying every frame.
        private const float TargetSearchInterval = 0.2f;
        private static readonly List<HeroController> ActiveHeroes = new List<HeroController>(15);

        [SerializeField] private EnemyManager _enemyManager;

        private HeroStats _stats;
        private AttackModule _attackModule;
        private SkillRunner _skillRunner;
        private readonly TargetingModule _targeting = new TargetingModule();
        private HeroVisual _visual;
        private Transform _anchorFallback;
        private float _targetSearchTimer;

        public HeroData Data { get; private set; }
        public HeroStats Stats => _stats;
        public EnemyManager EnemyManager => _enemyManager;
        public EnemyController CurrentTarget { get; private set; }
        public Transform Muzzle => _visual != null && _visual.Muzzle != null ? _visual.Muzzle : _anchorFallback;
        float ISkillContext.Damage => _stats != null ? _stats.Damage : 0f;
        float ISkillContext.Range => _stats != null ? _stats.Range : 0f;
        AttackType ISkillContext.AttackType => Data != null ? Data.attackType : default;
        DamageType ISkillContext.DamageType => Data != null ? Data.damageType : default;
        Element ISkillContext.Element => Data != null ? Data.element : default;
        SkillData ISkillContext.PassiveSkill => Data != null ? Data.passiveSkill : null;
        EnemyController ISkillContext.CurrentTarget => CurrentTarget;
        Transform ISkillContext.Muzzle => Muzzle;
        EnemyManager ISkillContext.EnemyManager => _enemyManager;
        Vector3 ISkillContext.Position => transform.position;
        bool ISkillContext.IsActive => isActiveAndEnabled && Data != null;
        int ISkillContext.ActiveHeroCount => ActiveHeroes.Count;
        ISkillContext ISkillContext.GetActiveHero(int index) => ActiveHeroes[index];

        private void Awake()
        {
            _stats = GetComponent<HeroStats>();
            if (_stats == null)
                Debug.LogError($"{name}: HeroStats component is missing from this hero.", this);
            _attackModule = GetComponent<AttackModule>();
            _skillRunner = GetComponent<SkillRunner>();

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
            RegisterActiveHero();
            RefreshTarget();
        }

        /// <summary>Clears transient state and level factors whenever this hero is taken from its pool.</summary>
        public void ResetState()
        {
            CurrentTarget = null;
            _targetSearchTimer = 0f;
            _stats?.ResetLevelMultipliers();
            _attackModule?.ResetState();
            _skillRunner?.ResetState();
        }

        /// <summary>Clears references and calculated stats before this hero is returned to its pool.</summary>
        public void OnDespawn()
        {
            ResetState();
            UnregisterActiveHero();
            Data = null;
            _stats?.Clear();
            _targeting.Clear();
        }

        void ISkillContext.SetBuff(UnityEngine.Object source, UnityEngine.Object sourceType, BuffType type, float value)
        {
            if (_stats == null || Data == null) return;
            _stats.SetBuff(source, sourceType, type, value);
        }

        void ISkillContext.RemoveBuff(UnityEngine.Object source)
        {
            _stats?.RemoveBuff(source);
        }

        private void OnEnable()
        {
            if (Data != null) RegisterActiveHero();
        }

        private void OnDisable()
        {
            _skillRunner?.ResetState();
            UnregisterActiveHero();
        }

        private void OnDestroy()
        {
            UnregisterActiveHero();
        }

        private void RegisterActiveHero()
        {
            for (int i = 0; i < ActiveHeroes.Count; i++)
                if (ActiveHeroes[i] == this) return;
            ActiveHeroes.Add(this);
        }

        private void UnregisterActiveHero()
        {
            for (int i = ActiveHeroes.Count - 1; i >= 0; i--)
            {
                if (ActiveHeroes[i] != this) continue;
                ActiveHeroes.RemoveAt(i);
                break;
            }
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
