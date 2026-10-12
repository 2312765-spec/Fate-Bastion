#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using FateBastion.Core;

namespace FateBastion.Combat.Sandbox
{
    /// <summary>
    /// Sandbox-only stand-in for EnemyController (S2): a cube that walks back and forth, takes damage through
    /// DamageCalculator, Health and StatusEffectController, and raises the CombatEvents like a real enemy would.
    /// </summary>
    public class SandboxDummyTarget : MonoBehaviour, IDamageable
    {
        [SerializeField] private CombatSettings _settings;

        [SerializeField, Min(1f)] private float _maxHP = 500f;
        [SerializeField, Range(0f, 1f)] private float _armor;
        [SerializeField, Range(0f, 1f)] private float _magicResist;
        [SerializeField] private bool _isBoss;

        [Tooltip("Walk speed in m/s, so the slow is visible.")]
        [SerializeField, Min(0f)] private float _moveSpeed = 2.5f;

        [Tooltip("Walks this far left and right of its start position, along X.")]
        [SerializeField, Min(0f)] private float _patrolDistance = 4f;

        [SerializeField] private Color _aliveColor = new Color(0.85f, 0.3f, 0.3f);
        [SerializeField] private Color _deadColor = new Color(0.3f, 0.3f, 0.3f);

        private readonly Health _health = new Health();
        private StatusEffectController _status;
        private Renderer _renderer;
        private Vector3 _startPosition;
        private float _direction = 1f;

        public bool IsAlive => _health.IsAlive;
        public Health Health => _health;
        public StatusEffectController Status => _status;
        public int LastDamage { get; private set; }

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _startPosition = transform.position;

            if (_settings == null)
            {
                Debug.LogError($"{name}: kéo CombatSettings vào field Settings.", this);
                enabled = false;
                return;
            }

            _status = new StatusEffectController(_settings);
            Revive();
        }

        public void Revive()
        {
            _health.Reset(_maxHP);
            _status.ResetState(_isBoss);
            transform.position = _startPosition;
            SetColor(_aliveColor);
        }

        /// <summary>Kills instantly, as if another hero landed the last hit.</summary>
        public void KillNow()
        {
            if (_health.ApplyDamage(_health.Current)) OnKilled(transform.position);
        }

        public void TakeDamage(in DamageInfo info)
        {
            if (!IsAlive) return;

            int damage = DamageCalculator.Calculate(info, _armor, _magicResist);
            bool killed = _health.ApplyDamage(damage);
            Report(damage, info.Type, info.Element, info.HitPoint, killed);

            if (killed) OnKilled(info.HitPoint);
            else _status.Apply(info.Status, info.Amount, info.Type);
        }

        private void Update()
        {
            if (!IsAlive) return;

            float burn = _status.Tick(Time.deltaTime);
            if (burn > 0f)
            {
                int damage = DamageCalculator.Calculate(burn, 1f, _status.BurnDamageType, _armor, _magicResist);
                bool killed = _health.ApplyDamage(damage);
                Report(damage, _status.BurnDamageType, _status.BurnElement, transform.position, killed);
                if (killed)
                {
                    OnKilled(transform.position);
                    return;
                }
            }

            Vector3 position = transform.position;
            position.x += _direction * _moveSpeed * _status.SpeedMultiplier * Time.deltaTime;
            float offset = position.x - _startPosition.x;
            if (Mathf.Abs(offset) >= _patrolDistance)
            {
                position.x = _startPosition.x + Mathf.Sign(offset) * _patrolDistance;
                _direction = -_direction;
            }
            transform.position = position;
        }

        private void Report(int damage, DamageType type, Element element, Vector3 hitPoint, bool killed)
        {
            LastDamage = damage;
            CombatEvents.RaiseDamageDealt(new DamageDealtInfo(damage, type, element, hitPoint, killed));
        }

        private void OnKilled(Vector3 position)
        {
            _status.ResetState(_isBoss);
            SetColor(_deadColor);
            CombatEvents.RaiseEnemyKilled(new EnemyKilledInfo(name, position, 0, _isBoss));
        }

        private void SetColor(Color color)
        {
            // Sandbox only: .material makes a per-object copy, fine outside a match.
            if (_renderer != null) _renderer.material.color = color;
        }
    }
}
#endif
