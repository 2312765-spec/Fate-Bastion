using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using FateBastion.Combat;
using FateBastion.Core;
using FateBastion.Enemies;
using FateBastion.Skills;

namespace FateBastion.Heroes
{
    /// <summary>Runs basic melee and projectile attacks on a scaled-time timer (S1, S6).</summary>
    [DisallowMultipleComponent]
    public sealed class AttackModule : MonoBehaviour
    {
        [SerializeField] private Projectile _projectilePrefab;
        [SerializeField, Min(1)] private int _projectilePrewarmCount = 8;

        private readonly AttackTimer _attackTimer = new AttackTimer();
        private readonly List<Projectile> _ownedProjectiles = new List<Projectile>();
        private Projectile[] _activeProjectiles;
        private HeroController _hero;
        private ObjectPool<Projectile> _projectiles;
        private int _activeProjectileCount;
        private bool _reportedMissingProjectile;
        private bool _reportedPoolExhaustion;
        private bool _reportedUnsupportedType;

        private void Awake()
        {
            _hero = GetComponent<HeroController>();
            if (_hero == null)
            {
                Debug.LogError($"{name}: AttackModule requires a HeroController on the same GameObject.", this);
                enabled = false;
                return;
            }

            InitializeProjectilePool();
        }

        private void Update()
        {
            if (_hero == null || _hero.Data == null || _hero.Stats == null)
            {
                _attackTimer.Reset();
                return;
            }

            HeroData data = _hero.Data;
            if (data.attackType != AttackType.Melee && data.attackType != AttackType.Projectile)
            {
                if (!_reportedUnsupportedType)
                {
                    Debug.LogWarning(
                        $"{name}: basic {data.attackType} attacks are handled by the S6 skill runner; T-06 currently supports Melee and Projectile.",
                        this);
                    _reportedUnsupportedType = true;
                }
                _attackTimer.Reset();
                return;
            }

            EnemyController target = _hero.CurrentTarget;
            bool canAttack = IsTargetInRange(target, data.attackType);
            if (data.attackType == AttackType.Projectile)
                canAttack &= HasProjectileAvailable();

            if (!_attackTimer.Tick(Time.deltaTime, _hero.Stats.AttacksPerSecond, canAttack)) return;

            if (data.attackType == AttackType.Melee) ApplyMeleeHit(target);
            else LaunchProjectile(target);
        }

        /// <summary>Clears the attack cadence and returns in-flight projectiles when the hero enters its pool.</summary>
        public void ResetState()
        {
            _attackTimer.Reset();

            for (int i = _activeProjectileCount - 1; i >= 0; i--)
            {
                Projectile projectile = _activeProjectiles[i];
                projectile.ResetState();
                projectile.gameObject.SetActive(false);
                _projectiles.Release(projectile);
                _activeProjectiles[i] = null;
            }
            _activeProjectileCount = 0;
            _reportedPoolExhaustion = false;
        }

        private bool IsTargetInRange(EnemyController target, AttackType attackType)
        {
            if (target == null || !target.IsAlive || target.Data == null) return false;
            if (attackType == AttackType.Melee && target.IsFlying) return false;

            Vector3 offset = target.transform.position - transform.position;
            offset.y = 0f;
            float range = Mathf.Max(0f, _hero.Stats.Range);
            return offset.sqrMagnitude <= range * range;
        }

        private bool HasProjectileAvailable()
        {
            if (_projectiles == null)
            {
                if (!_reportedMissingProjectile)
                {
                    Debug.LogError($"{name}: Projectile attack requires a pooled Projectile prefab assigned in the Inspector.", this);
                    _reportedMissingProjectile = true;
                }
                return false;
            }

            if (_projectiles.CountInactive > 0) return true;
            if (!_reportedPoolExhaustion)
            {
                Debug.LogWarning($"{name}: projectile pool is exhausted; attack delayed until a projectile returns.", this);
                _reportedPoolExhaustion = true;
            }
            return false;
        }

        private void ApplyMeleeHit(EnemyController target)
        {
            if (target == null || !target.IsAlive) return;

            var damage = new DamageInfo(
                _hero.Stats.Damage,
                _hero.Data.damageType,
                _hero.Data.element,
                gameObject,
                false,
                null,
                target.transform.position);
            target.TakeDamage(damage);
        }

        private void LaunchProjectile(EnemyController target)
        {
            if (target == null || !target.IsAlive) return;

            SkillData passive = _hero.Data.passiveSkill;
            if (passive == null || passive.projectileSpeed <= 0f ||
                float.IsNaN(passive.projectileSpeed) || float.IsInfinity(passive.projectileSpeed))
            {
                Debug.LogError($"{name}: projectile attack requires a positive projectileSpeed in its passive SkillData.", this);
                return;
            }

            Projectile projectile = _projectiles.Get();
            _activeProjectiles[_activeProjectileCount++] = projectile;
            var damage = new DamageInfo(
                _hero.Stats.Damage,
                _hero.Data.damageType,
                _hero.Data.element,
                gameObject,
                true,
                null,
                _hero.Muzzle.position);
            projectile.Launch(_hero.Muzzle.position, target, target.transform, damage, passive.projectileSpeed);
            _reportedPoolExhaustion = false;
        }

        private void InitializeProjectilePool()
        {
            if (_projectilePrefab == null) return;
            if (_projectilePrewarmCount < 1)
            {
                Debug.LogError($"{name}: projectile prewarm count must be at least one.", this);
                return;
            }

            _activeProjectiles = new Projectile[_projectilePrewarmCount];
            _projectiles = new ObjectPool<Projectile>(
                CreateProjectile,
                projectile =>
                {
                    projectile.ResetState();
                    projectile.gameObject.SetActive(false);
                },
                projectile => projectile.gameObject.SetActive(false),
                null,
                false,
                _projectilePrewarmCount,
                _projectilePrewarmCount);

            var prewarmed = new List<Projectile>(_projectilePrewarmCount);
            for (int i = 0; i < _projectilePrewarmCount; i++)
                prewarmed.Add(_projectiles.Get());
            for (int i = 0; i < prewarmed.Count; i++)
                _projectiles.Release(prewarmed[i]);
        }

        private Projectile CreateProjectile()
        {
            Projectile projectile = Instantiate(_projectilePrefab, transform);
            projectile.gameObject.SetActive(false);
            projectile.Finished += HandleProjectileFinished;
            _ownedProjectiles.Add(projectile);
            return projectile;
        }

        private void HandleProjectileFinished(Projectile projectile)
        {
            for (int i = 0; i < _activeProjectileCount; i++)
            {
                if (_activeProjectiles[i] != projectile) continue;

                int last = --_activeProjectileCount;
                _activeProjectiles[i] = _activeProjectiles[last];
                _activeProjectiles[last] = null;
                _projectiles.Release(projectile);
                return;
            }
        }

        private void OnDestroy()
        {
            ResetState();
            for (int i = 0; i < _ownedProjectiles.Count; i++)
                if (_ownedProjectiles[i] != null) _ownedProjectiles[i].Finished -= HandleProjectileFinished;
        }
    }
}
