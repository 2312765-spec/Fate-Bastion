using System;
using UnityEngine;
using FateBastion.Core;
using FateBastion.Enemies;

namespace FateBastion.Game
{
    /// <summary>Castle HP (S5), pure so damage, death and the debug "invincible" switch are unit-tested.</summary>
    public sealed class CastleHealth
    {
        public int Max { get; private set; }
        public int Current { get; private set; }
        public bool IsAlive => Current > 0;

        /// <summary>Debug panel (B-16): damage is ignored.</summary>
        public bool Invincible { get; set; }

        public void Reset(int maxHP)
        {
            if (maxHP <= 0) throw new ArgumentOutOfRangeException(nameof(maxHP), maxHP, "Castle HP must be positive.");
            Max = maxHP;
            Current = maxHP;
        }

        /// <summary>Applies damage; true when this hit destroyed the Castle.</summary>
        public bool TakeDamage(int amount, out bool changed)
        {
            changed = false;
            if (amount <= 0 || !IsAlive || Invincible) return false;

            Current = Math.Max(0, Current - amount);
            changed = true;
            return Current == 0;
        }
    }

    /// <summary>
    /// The Castle of the map (S5, B-06). Listens to EnemyManager.OnEnemyReachedCastle (TEAM_ASSIGNMENT 3.1 #1),
    /// subtracts HP and raises GameEvents.OnCastleDamaged so the HUD never reads the Castle. A never calls it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Castle : MonoBehaviour
    {
        [SerializeField] private EnemyManager _enemyManager;

        private readonly CastleHealth _health = new CastleHealth();
        private Action<int> _onReached;
        private EnemyManager _subscribed;

        /// <summary>HP reached 0 (raised once).</summary>
        public event Action Destroyed;

        public int CurrentHP => _health.Current;
        public int MaxHP => _health.Max;
        public bool IsAlive => _health.IsAlive;

        public bool Invincible
        {
            get => _health.Invincible;
            set => _health.Invincible = value;
        }

        private void Awake() => _onReached = TakeDamage;

        private void OnEnable() => Subscribe();

        private void OnDisable() => Unsubscribe();

        /// <summary>For tests and scenes built in code.</summary>
        public void SetEnemyManager(EnemyManager enemyManager)
        {
            Unsubscribe();
            _enemyManager = enemyManager;
            if (isActiveAndEnabled) Subscribe();
        }

        /// <summary>Start of a match: full HP, and the HUD gets the first value.</summary>
        public void Init(int maxHP)
        {
            _health.Reset(maxHP);
            GameEvents.RaiseCastleDamaged(_health.Current, _health.Max);
        }

        public void TakeDamage(int amount)
        {
            bool destroyed = _health.TakeDamage(amount, out bool changed);
            if (!changed) return;

            GameEvents.RaiseCastleDamaged(_health.Current, _health.Max);
            if (destroyed) Destroyed?.Invoke();
        }

        private void Subscribe()
        {
            if (_enemyManager == null || _subscribed == _enemyManager) return;
            if (_onReached == null) _onReached = TakeDamage;
            _enemyManager.OnEnemyReachedCastle += _onReached;
            _subscribed = _enemyManager;
        }

        private void Unsubscribe()
        {
            if (_subscribed == null) return;
            _subscribed.OnEnemyReachedCastle -= _onReached;
            _subscribed = null;
        }
    }
}
