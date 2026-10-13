using System;
using UnityEngine;
using FateBastion.Core;
using FateBastion.Combat;

namespace FateBastion.Enemies
{
    /// <summary>
    /// One enemy on the field (S2): walks its path by distance, takes damage through DamageCalculator, carries burn
    /// and slow, and dies once. Has no Update of its own: EnemyManager ticks every enemy and owns the pool.
    /// UI reads CurrentHP, MaxHP, IsBoss, Progress, HealthBarAnchor and OnHealthChanged (TEAM_ASSIGNMENT 3.1 #9).
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyController : MonoBehaviour, IDamageable
    {
        [Tooltip("Point above the head for the health bar; the enemy root is used when empty.")]
        [SerializeField] private Transform _healthBarAnchor;

        /// <summary>(current, max) after every hit or burn tick. Cleared when the enemy returns to the pool.</summary>
        public event Action<float, float> OnHealthChanged;

        private readonly Health _health = new Health();
        private readonly PathFollower _follower = new PathFollower();
        private StatusEffectController _status;
        private EnemyManager _owner;
        private Transform _transform;

        public EnemyData Data { get; private set; }

        /// <summary>Wave this enemy counts toward; boss summons carry the boss's wave.</summary>
        public int Wave { get; private set; }

        public bool IsAlive => _health.IsAlive;
        public float CurrentHP => _health.Current;
        public float MaxHP => _health.Max;
        public bool IsBoss => Data != null && Data.boss;
        public bool IsFlying => Data != null && Data.flying;

        /// <summary>Metres walked from the Gate; compare this for "first in line" targeting.</summary>
        public float Distance => _follower.Distance;

        /// <summary>0 at the Gate, 1 at the Castle.</summary>
        public float Progress => _follower.Progress;

        public Transform HealthBarAnchor => _healthBarAnchor != null ? _healthBarAnchor : transform;
        public StatusEffectController Status => _status;

        /// <summary>Slot in EnemyManager's active list; -1 while in the pool.</summary>
        internal int ActiveIndex { get; set; } = -1;

        private void Awake()
        {
            _transform = transform;
        }

        /// <summary>Called once by EnemyManager right after the instance is created for the pool.</summary>
        internal void Initialize(EnemyManager owner, CombatSettings combatSettings)
        {
            _owner = owner;
            _transform = transform;
            _status = new StatusEffectController(combatSettings);
        }

        /// <summary>Called by EnemyManager when taken from the pool.</summary>
        internal void Spawn(EnemyData data, IEnemyPath path, float maxHP, int wave, float startDistance)
        {
            Data = data;
            Wave = wave;
            _follower.Reset(path, startDistance);
            _health.Reset(maxHP);
            ResetState();
            UpdateTransform();
        }

        /// <summary>Clears burn and slow; part of every take-from-pool.</summary>
        public void ResetState()
        {
            _status?.ResetState(IsBoss);
        }

        /// <summary>Called by EnemyManager before returning to the pool: drops UI subscribers and status effects.</summary>
        internal void OnDespawn()
        {
            OnHealthChanged = null;
            _status?.ResetState(IsBoss);
            _health.TryMarkDead();
        }

        /// <summary>One scaled-time step: burn, then movement. Returns true when the enemy reached the Castle this step.</summary>
        internal bool Tick(float deltaTime)
        {
            if (!IsAlive) return false;

            float burn = _status.Tick(deltaTime);
            if (burn > 0f)
            {
                int damage = DamageCalculator.Calculate(burn, 1f, _status.BurnDamageType, Data.armor, Data.magicResist);
                if (ApplyDamage(damage, _status.BurnDamageType, _status.BurnElement, _transform.position)) return false;
            }

            bool reachedEnd = _follower.Step(Data.moveSpeed, _status.SpeedMultiplier, deltaTime);
            UpdateTransform();

            // TryMarkDead fails if the enemy was killed first, so "killed and reached" counts as killed (S2).
            return reachedEnd && _health.TryMarkDead();
        }

        public void TakeDamage(in DamageInfo info)
        {
            if (!IsAlive || Data == null) return;
            if (Data.flying && !info.CanHitFlying) return;

            int damage = DamageCalculator.Calculate(info, Data.armor, Data.magicResist);
            if (ApplyDamage(damage, info.Type, info.Element, info.HitPoint)) return;

            _status.Apply(info.Status, info.Amount, info.Type);
        }

        /// <summary>Applies resisted damage and raises the events. Returns true on the killing hit.</summary>
        private bool ApplyDamage(int damage, DamageType type, Element element, Vector3 hitPoint)
        {
            bool killed = _health.ApplyDamage(damage);
            CombatEvents.RaiseDamageDealt(new DamageDealtInfo(damage, type, element, hitPoint, killed));
            OnHealthChanged?.Invoke(_health.Current, _health.Max);

            if (killed)
            {
                _status.ResetState(IsBoss);
                CombatEvents.RaiseEnemyKilled(new EnemyKilledInfo(Data.id, _transform.position, Data.goldReward, Data.boss));
                if (_owner != null) _owner.NotifyKilled(this);
            }
            return killed;
        }

        private void UpdateTransform()
        {
            if (_follower.Path == null) return;

            _follower.Path.Evaluate(_follower.Distance, out Vector3 position, out Vector3 forward);
            forward.y = 0f;
            Quaternion rotation = forward.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(forward) : _transform.rotation;
            _transform.SetPositionAndRotation(position, rotation);
        }
    }
}
