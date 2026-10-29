using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using FateBastion.Combat;
using FateBastion.Core;
using FateBastion.Enemies;

namespace FateBastion.Skills
{
    /// <summary>Executes a hero's basic attacks and active skills, and owns its prewarmed projectile pool (S6).</summary>
    [DisallowMultipleComponent]
    public sealed class SkillRunner : MonoBehaviour
    {
        // Technical scan interval matching the five checks per second used by Command Aura.
        private const float BuffRefreshInterval = 0.2f;
        // Keep field hit tracking bounded by the maximum number of simultaneous Line/Zone fields.
        private const int MaxActiveFields = 2;
        // Matches EnemySettings.MaxActiveEnemies for the current project balance.
        private const int MaxEnemiesPerField = 100;
        private static readonly ISkillExecutor[] SkillExecutors =
        {
            new MeleeHitExecutor(),
            new ProjectileExecutor(),
            new AreaAtPointExecutor(),
            new ConeExecutor(),
            new LineExecutor(),
            new ZoneExecutor(),
            new BuffExecutor()
        };

        [SerializeField] private Projectile _projectilePrefab;
        [SerializeField, Min(1)] private int _projectilePrewarmCount = 8;

        private readonly List<Projectile> _ownedProjectiles = new List<Projectile>();
        private readonly SkillData[] _fieldSkills = new SkillData[MaxActiveFields];
        private readonly Vector3[] _fieldOrigins = new Vector3[MaxActiveFields];
        private readonly Vector3[] _fieldDirections = new Vector3[MaxActiveFields];
        private readonly float[] _fieldRemaining = new float[MaxActiveFields];
        private readonly float[] _fieldStatusRefreshRemaining = new float[MaxActiveFields];
        private readonly EnemyController[][] _fieldHits =
        {
            new EnemyController[MaxEnemiesPerField],
            new EnemyController[MaxEnemiesPerField]
        };
        private readonly uint[][] _fieldHitGenerations =
        {
            new uint[MaxEnemiesPerField],
            new uint[MaxEnemiesPerField]
        };
        private Projectile[] _activeProjectiles;
        private ISkillContext _context;
        private ObjectPool<Projectile> _projectiles;
        private int _activeProjectileCount;
        private int _basicAttackCount;
        private float _cooldownRemaining;
        private float _abilityCooldownRemaining;
        private SkillData _pendingSkill;
        private EnemyController _pendingTarget;
        private Vector3 _pendingPoint;
        private Vector3 _pendingOrigin;
        private float _pendingRemaining;
        private float _pendingMultiplier;
        private bool _pendingBasicAttack;
        private float _buffRefreshTimer;
        private bool _hasAppliedBuffs;
        private bool _reportedMissingProjectile;
        private bool _reportedPoolExhaustion;
        private bool _reportedInvalidSkill;

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            TickCooldown(deltaTime);
            TickPendingCast(deltaTime);
            TickActiveFields(deltaTime);
            TickPassiveBuff(deltaTime);
        }

        private void Awake()
        {
            MonoBehaviour[] components = GetComponents<MonoBehaviour>();
            for (int i = 0; i < components.Length; i++)
            {
                if (!(components[i] is ISkillContext context)) continue;
                _context = context;
                break;
            }

            if (_context == null)
            {
                Debug.LogError("SkillRunner requires a component implementing ISkillContext on the same GameObject.", this);
                enabled = false;
                return;
            }

            InitializeProjectilePool();
        }

        /// <summary>Advances passive cooldowns with scaled gameplay time.</summary>
        public void TickCooldown(float scaledDeltaTime)
        {
            if (_cooldownRemaining > 0f)
                _cooldownRemaining = Mathf.Max(0f, _cooldownRemaining - scaledDeltaTime);
            if (_abilityCooldownRemaining > 0f)
                _abilityCooldownRemaining = Mathf.Max(0f, _abilityCooldownRemaining - scaledDeltaTime);
        }

        /// <summary>Returns whether the next basic attack can be executed without growing the projectile pool.</summary>
        public bool CanExecuteBasicAttack()
        {
            if (_context == null || !_context.IsActive) return false;
            if (_pendingSkill != null) return false;
            if (_context.AttackType != AttackType.Projectile) return true;

            if (_projectiles == null)
            {
                if (!_reportedMissingProjectile)
                {
                    Debug.LogError("Projectile attacks require a Projectile prefab assigned to SkillRunner.", this);
                    _reportedMissingProjectile = true;
                }
                return false;
            }

            if (_projectiles.CountInactive > 0) return true;
            if (!_reportedPoolExhaustion)
            {
                Debug.LogWarning("SkillRunner projectile pool is exhausted; attack delayed until a projectile returns.", this);
                _reportedPoolExhaustion = true;
            }
            return false;
        }

        /// <summary>Executes one valid basic attack; on-hit passives fire once, not once per target hit.</summary>
        public bool ExecuteBasicAttack(EnemyController target)
        {
            if (_context == null || !_context.IsActive || target == null || !target.IsAlive || target.Data == null)
                return false;
            SkillKind attackKind = KindFromAttackType(_context.AttackType);
            if (!IsSupportedAttackKind(attackKind))
            {
                ReportInvalidSkill("The basic attack kind is not supported by this SkillRunner.");
                return false;
            }
            if (attackKind == SkillKind.MeleeHit && target.IsFlying) return false;
            if (!CanExecuteBasicAttack()) return false;

            SkillData passive = _context.PassiveSkill;
            SkillData triggeredSkill = GetTriggeredSkill(passive);
            if (triggeredSkill != null && !IsValidSkill(triggeredSkill)) return false;
            if (triggeredSkill != null && triggeredSkill.kind != SkillKind.Buff
                && !MatchesAttackType(triggeredSkill.kind, _context.AttackType))
            {
                ReportInvalidSkill("Passive SkillData kind does not match the hero's basic attack type.");
                triggeredSkill = null;
            }
            else if (triggeredSkill != null && triggeredSkill.kind != SkillKind.Buff)
            {
                attackKind = triggeredSkill.kind;
            }
            float multiplier = triggeredSkill != null ? triggeredSkill.damageMultiplier : 1f;
            if (float.IsNaN(multiplier) || float.IsInfinity(multiplier) || multiplier < 0f)
            {
                ReportInvalidSkill("Passive SkillData has an invalid damageMultiplier.");
                return false;
            }

            if (triggeredSkill != null && triggeredSkill.castDelay > 0f)
            {
                _cooldownRemaining = triggeredSkill.cooldown;
                _pendingSkill = triggeredSkill;
                _pendingTarget = target;
                _pendingPoint = target.transform.position;
                _pendingOrigin = _context.Position;
                _pendingRemaining = triggeredSkill.castDelay;
                _pendingMultiplier = multiplier;
                _pendingBasicAttack = true;
                return true;
            }

            bool executed = ExecuteBasicAttackNow(attackKind, target, target.transform.position, _context.Position,
                passive, triggeredSkill, multiplier);
            if (executed && triggeredSkill != null)
                _cooldownRemaining = triggeredSkill.cooldown;
            return executed;
        }

        /// <summary>Attempts to cast one active skill at an automatically selected dense enemy point.</summary>
        public bool TryCastAuto(SkillData skill)
        {
            if (skill == null || _context == null || !_context.IsActive || _context.EnemyManager == null
                || skill.kind == SkillKind.Buff)
                return false;
            if (!IsValidSkill(skill)) return false;
            if (!TryFindDensePoint(skill, out Vector3 aimPoint)) return false;
            return TryCast(skill, aimPoint);
        }

        /// <summary>Attempts to cast one active skill at a specified world point.</summary>
        public bool TryCast(SkillData skill, Vector3 aimPoint)
        {
            if (skill == null || _context == null || !_context.IsActive || skill.kind == SkillKind.Buff) return false;
            if (_pendingSkill != null || _abilityCooldownRemaining > 0f) return false;
            if (!IsValidSkill(skill)) return false;
            if (_context.EnemyManager == null)
            {
                ReportInvalidSkill("Active skills require an EnemyManager assigned to HeroController.");
                return false;
            }
            Vector3 offsetFromHero = aimPoint - _context.Position;
            offsetFromHero.y = 0f;
            if (offsetFromHero.sqrMagnitude > _context.Range * _context.Range) return false;
            if (skill.kind == SkillKind.Line && offsetFromHero.sqrMagnitude <= 1e-6f) return false;

            EnemyController target = FindTargetForSkill(aimPoint);
            if ((skill.kind == SkillKind.MeleeHit || skill.kind == SkillKind.Projectile) && target == null)
                return false;
            if (skill.kind == SkillKind.Projectile && !CanLaunchProjectile()) return false;
            if ((skill.kind == SkillKind.Line || skill.kind == SkillKind.Zone) && skill.duration > 0f
                && FindAvailableField() < 0)
                return false;

            if (skill.castDelay > 0f)
            {
                _abilityCooldownRemaining = skill.cooldown;
                _pendingSkill = skill;
                _pendingTarget = target;
                _pendingPoint = aimPoint;
                _pendingOrigin = _context.Position;
                _pendingRemaining = skill.castDelay;
                _pendingMultiplier = skill.damageMultiplier;
                _pendingBasicAttack = false;
                return true;
            }

            if (!ExecuteSkillNow(skill, target, aimPoint, _context.Position)) return false;
            _abilityCooldownRemaining = skill.cooldown;
            return true;
        }

        /// <summary>Clears trigger/cooldown state and returns in-flight projectiles to the pool.</summary>
        public void ResetState()
        {
            _basicAttackCount = 0;
            _cooldownRemaining = 0f;
            _abilityCooldownRemaining = 0f;
            _pendingSkill = null;
            _pendingTarget = null;
            _pendingPoint = default;
            _pendingOrigin = default;
            _pendingRemaining = 0f;
            _pendingBasicAttack = false;
            _buffRefreshTimer = 0f;
            ClearActiveFields();
            RemoveBuffFromAllHeroes();

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

        private SkillData GetTriggeredSkill(SkillData passive)
        {
            _basicAttackCount++;
            if (passive == null || passive.kind == SkillKind.Buff || _cooldownRemaining > 0f) return null;
            if (float.IsNaN(passive.cooldown) || float.IsInfinity(passive.cooldown) || passive.cooldown < 0f)
            {
                ReportInvalidSkill("Passive SkillData has an invalid cooldown.");
                return null;
            }

            bool trigger;
            switch (passive.trigger)
            {
                case PassiveTrigger.OnEveryHit:
                    trigger = true;
                    break;
                case PassiveTrigger.OnNthHit:
                    if (passive.nthHit < 1)
                    {
                        ReportInvalidSkill("Passive SkillData has nthHit less than one.");
                        return null;
                    }
                    trigger = _basicAttackCount % passive.nthHit == 0;
                    break;
                default:
                    trigger = false;
                    break;
            }

            if (!trigger) return null;
            return passive;
        }

        private bool ExecuteBasicAttackNow(SkillKind attackKind, EnemyController target, Vector3 aimPoint, Vector3 origin,
            SkillData shape, SkillData triggeredSkill, float multiplier)
        {
            ISkillExecutor executor = GetExecutor(attackKind);
            if (executor == null || !executor.SupportsBasicAttack)
            {
                ReportInvalidSkill("The basic attack kind is not supported by this SkillRunner.");
                return false;
            }

            return executor.ExecuteBasic(this, target, aimPoint, origin, shape, triggeredSkill, multiplier);
        }

        private void TickPendingCast(float deltaTime)
        {
            if (_pendingSkill == null) return;
            _pendingRemaining -= deltaTime;
            if (_pendingRemaining > 0f) return;

            SkillData skill = _pendingSkill;
            EnemyController target = _pendingTarget;
            Vector3 point = _pendingPoint;
            Vector3 origin = _pendingOrigin;
            float multiplier = _pendingMultiplier;
            bool basicAttack = _pendingBasicAttack;
            _pendingSkill = null;
            _pendingTarget = null;
            _pendingPoint = default;
            _pendingOrigin = default;
            _pendingRemaining = 0f;
            _pendingBasicAttack = false;

            if (basicAttack)
            {
                SkillKind kind = KindFromAttackType(_context.AttackType);
                if (skill != null) kind = skill.kind;
                ExecuteBasicAttackNow(kind, target, point, origin, _context.PassiveSkill, skill, multiplier);
            }
            else
            {
                ExecuteSkillNow(skill, target, point, origin);
            }
        }

        private void TickActiveFields(float deltaTime)
        {
            EnemyManager enemyManager = _context != null ? _context.EnemyManager : null;
            if (enemyManager == null) return;

            IReadOnlyList<EnemyController> enemies = enemyManager.ActiveEnemies;
            for (int fieldIndex = 0; fieldIndex < MaxActiveFields; fieldIndex++)
            {
                SkillData skill = _fieldSkills[fieldIndex];
                if (skill == null) continue;

                float statusRefreshInterval = GetStatusRefreshInterval(skill);
                bool refreshStatus = false;
                if (statusRefreshInterval > 0f)
                {
                    _fieldStatusRefreshRemaining[fieldIndex] -= deltaTime;
                    if (_fieldStatusRefreshRemaining[fieldIndex] <= 0f)
                    {
                        _fieldStatusRefreshRemaining[fieldIndex] = statusRefreshInterval;
                        refreshStatus = true;
                    }
                }

                PruneFieldHits(fieldIndex);
                for (int i = 0; i < enemies.Count; i++)
                {
                    EnemyController enemy = enemies[i];
                    if (enemy == null || !enemy.IsAlive || enemy.Data == null)
                        continue;
                    if (!IsInsideField(skill, _fieldOrigins[fieldIndex], _fieldDirections[fieldIndex], enemy.transform.position))
                        continue;

                    if (HasFieldHit(fieldIndex, enemy))
                    {
                        if (refreshStatus) RefreshFieldStatus(enemy, skill);
                    }
                    else
                    {
                        RememberFieldHit(fieldIndex, enemy);
                        ApplyHit(enemy, skill.damageMultiplier, skill, true);
                    }
                }

                _fieldRemaining[fieldIndex] -= deltaTime;
                if (_fieldRemaining[fieldIndex] <= 0f) ClearField(fieldIndex);
            }
        }

        private void TickPassiveBuff(float deltaTime)
        {
            _buffRefreshTimer -= deltaTime;
            if (_buffRefreshTimer > 0f) return;
            _buffRefreshTimer = BuffRefreshInterval;

            SkillData skill = _context != null ? _context.PassiveSkill : null;
            if (_context == null || !_context.IsActive || skill == null || skill.kind != SkillKind.Buff)
            {
                RemoveBuffFromAllHeroes();
                return;
            }
            if (skill.buffType == BuffType.None || skill.buffRadius < 0f
                || float.IsNaN(skill.buffRadius) || float.IsInfinity(skill.buffRadius)
                || skill.buffValue < 0f || float.IsNaN(skill.buffValue) || float.IsInfinity(skill.buffValue))
            {
                ReportInvalidSkill("Buff SkillData needs a valid buffType, buffRadius, and buffValue.");
                RemoveBuffFromAllHeroes();
                return;
            }

            float radiusSquared = skill.buffRadius * skill.buffRadius;
            Vector3 sourcePosition = _context.Position;
            for (int i = 0; i < _context.ActiveHeroCount; i++)
            {
                ISkillContext hero = _context.GetActiveHero(i);
                if (hero == null || !hero.IsActive) continue;

                Vector3 offset = hero.Position - sourcePosition;
                offset.y = 0f;
                if (offset.sqrMagnitude <= radiusSquared)
                {
                    hero.SetBuff(this, skill, skill.buffType, skill.buffValue);
                    _hasAppliedBuffs = true;
                }
                else
                    hero.RemoveBuff(this);
            }
        }

        private void RemoveBuffFromAllHeroes()
        {
            if (_context == null || !_hasAppliedBuffs) return;
            for (int i = 0; i < _context.ActiveHeroCount; i++)
                _context.GetActiveHero(i)?.RemoveBuff(this);
            _hasAppliedBuffs = false;
        }

        private bool ExecuteSkillNow(SkillData skill, EnemyController target, Vector3 aimPoint, Vector3 origin)
        {
            ISkillExecutor executor = GetExecutor(skill.kind);
            return executor != null && executor.ExecuteActive(this, skill, target, aimPoint, origin);
        }

        private bool StartField(SkillData skill, Vector3 aimPoint, Vector3 origin)
        {
            int fieldIndex = skill.duration > 0f ? FindAvailableField() : -1;
            Vector3 direction = aimPoint - origin;
            direction.y = 0f;
            if (skill.kind == SkillKind.Line && direction.sqrMagnitude <= 1e-6f) return false;
            if (direction.sqrMagnitude > 1e-6f) direction.Normalize();

            if (skill.duration <= 0f)
            {
                IReadOnlyList<EnemyController> enemies = _context.EnemyManager.ActiveEnemies;
                for (int i = 0; i < enemies.Count; i++)
                {
                    EnemyController enemy = enemies[i];
                    if (enemy == null || !enemy.IsAlive || enemy.Data == null) continue;
                    if (IsInsideField(skill, origin, direction, enemy.transform.position))
                        ApplyHit(enemy, skill.damageMultiplier, skill, true);
                }
                return true;
            }

            if (fieldIndex < 0) return false;
            ClearField(fieldIndex);
            _fieldSkills[fieldIndex] = skill;
            _fieldOrigins[fieldIndex] = origin;
            _fieldDirections[fieldIndex] = direction;
            _fieldRemaining[fieldIndex] = skill.duration;
            _fieldStatusRefreshRemaining[fieldIndex] = GetStatusRefreshInterval(skill);
            return true;
        }

        private bool IsInsideField(SkillData skill, Vector3 origin, Vector3 direction, Vector3 point)
        {
            Vector3 offset = point - origin;
            offset.y = 0f;
            if (skill.kind == SkillKind.Zone)
                return offset.sqrMagnitude <= skill.radius * skill.radius;

            float along = Vector3.Dot(offset, direction);
            if (along < 0f || along > skill.length) return false;
            float lateralSquared = offset.sqrMagnitude - along * along;
            float halfWidth = Mathf.Max(0f, skill.width * 0.5f);
            return lateralSquared <= halfWidth * halfWidth;
        }

        private bool ApplyConeHit(Vector3 aimPoint, Vector3 origin, SkillData shape, SkillData skill, float multiplier)
        {
            if (_context == null || _context.EnemyManager == null || shape == null || shape.kind != SkillKind.Cone)
            {
                ReportInvalidSkill("Cone requires an EnemyManager and configured SkillData.");
                return false;
            }

            Vector3 forward = aimPoint - origin;
            forward.y = 0f;
            if (forward.sqrMagnitude <= 1e-6f) return false;
            forward.Normalize();

            float range = Mathf.Max(0f, _context.Range);
            if (range <= 0f) return false;
            IReadOnlyList<EnemyController> enemies = _context.EnemyManager.ActiveEnemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyController enemy = enemies[i];
                if (enemy == null || !enemy.IsAlive || enemy.Data == null) continue;
                Vector3 offset = enemy.transform.position - origin;
                offset.y = 0f;
                float along = Vector3.Dot(offset, forward);
                if (along < 0f || along > range) continue;
                float halfWidth = Mathf.Max(0f, shape.radius) * (along / range);
                float lateralSquared = offset.sqrMagnitude - along * along;
                if (lateralSquared <= halfWidth * halfWidth)
                    ApplyHit(enemy, multiplier, skill, true);
            }
            return true;
        }

        private bool TryFindDensePoint(SkillData skill, out Vector3 point)
        {
            point = default;
            EnemyManager enemyManager = _context != null ? _context.EnemyManager : null;
            if (enemyManager == null) return false;

            IReadOnlyList<EnemyController> enemies = enemyManager.ActiveEnemies;
            if (skill.kind == SkillKind.Zone)
            {
                point = _context.Position;
                float radiusSquared = skill.radius * skill.radius;
                for (int i = 0; i < enemies.Count; i++)
                {
                    EnemyController enemy = enemies[i];
                    if (enemy == null || !enemy.IsAlive || enemy.Data == null) continue;
                    Vector3 offset = enemy.transform.position - point;
                    offset.y = 0f;
                    if (offset.sqrMagnitude <= radiusSquared) return true;
                }
                return false;
            }

            float maxRange = _context.Range;
            float maxRangeSquared = maxRange * maxRange;
            float clusterRadius = skill.kind == SkillKind.Line ? Mathf.Max(skill.width, skill.radius) : skill.radius;
            float clusterRadiusSquared = clusterRadius * clusterRadius;
            int bestCount = 0;
            float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyController candidate = enemies[i];
                if (candidate == null || !candidate.IsAlive || candidate.Data == null) continue;
                Vector3 fromHero = candidate.transform.position - _context.Position;
                fromHero.y = 0f;
                if (fromHero.sqrMagnitude > maxRangeSquared) continue;

                int count = 0;
                for (int j = 0; j < enemies.Count; j++)
                {
                    EnemyController other = enemies[j];
                    if (other == null || !other.IsAlive || other.Data == null) continue;
                    Vector3 offset = other.transform.position - candidate.transform.position;
                    offset.y = 0f;
                    if (offset.sqrMagnitude <= clusterRadiusSquared) count++;
                }

                if (count < bestCount || (count == bestCount && fromHero.sqrMagnitude >= bestDistance)) continue;
                bestCount = count;
                bestDistance = fromHero.sqrMagnitude;
                point = candidate.transform.position;
            }
            return bestCount > 0;
        }

        private EnemyController FindTargetForSkill(Vector3 point)
        {
            if (_context == null || _context.EnemyManager == null) return null;
            IReadOnlyList<EnemyController> enemies = _context.EnemyManager.ActiveEnemies;
            float rangeSquared = _context.Range * _context.Range;
            EnemyController best = null;
            float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyController enemy = enemies[i];
                if (enemy == null || !enemy.IsAlive || enemy.Data == null) continue;
                Vector3 fromHero = enemy.transform.position - _context.Position;
                fromHero.y = 0f;
                if (fromHero.sqrMagnitude > rangeSquared) continue;
                Vector3 toPoint = enemy.transform.position - point;
                toPoint.y = 0f;
                if (toPoint.sqrMagnitude >= bestDistance) continue;
                bestDistance = toPoint.sqrMagnitude;
                best = enemy;
            }
            return best;
        }

        private bool IsValidSkill(SkillData skill)
        {
            if (float.IsNaN(skill.damageMultiplier) || float.IsInfinity(skill.damageMultiplier) || skill.damageMultiplier < 0f
                || float.IsNaN(skill.cooldown) || float.IsInfinity(skill.cooldown) || skill.cooldown < 0f
                || float.IsNaN(skill.castDelay) || float.IsInfinity(skill.castDelay) || skill.castDelay < 0f
                || float.IsNaN(skill.duration) || float.IsInfinity(skill.duration) || skill.duration < 0f
                || float.IsNaN(skill.radius) || float.IsInfinity(skill.radius)
                || float.IsNaN(skill.length) || float.IsInfinity(skill.length)
                || float.IsNaN(skill.width) || float.IsInfinity(skill.width))
            {
                ReportInvalidSkill("SkillData has invalid damage, shape, timing, or cooldown values.");
                return false;
            }

            if ((skill.kind == SkillKind.Line && (skill.length <= 0f || skill.width <= 0f))
                || (skill.kind == SkillKind.Zone && skill.radius <= 0f)
                || ((skill.kind == SkillKind.AreaAtPoint || skill.kind == SkillKind.Cone) && skill.radius < 0f)
                || (skill.kind == SkillKind.Projectile
                    && (skill.projectileSpeed <= 0f || float.IsNaN(skill.projectileSpeed) || float.IsInfinity(skill.projectileSpeed))))
            {
                ReportInvalidSkill($"SkillData for {skill.kind} has invalid shape dimensions.");
                return false;
            }
            return true;
        }

        private bool HasFieldHit(int fieldIndex, EnemyController enemy)
        {
            for (int i = 0; i < MaxEnemiesPerField; i++)
                if (_fieldHits[fieldIndex][i] == enemy
                    && _fieldHitGenerations[fieldIndex][i] == enemy.SpawnVersion)
                    return true;
            return false;
        }

        private void RememberFieldHit(int fieldIndex, EnemyController enemy)
        {
            for (int i = 0; i < MaxEnemiesPerField; i++)
            {
                if (_fieldHits[fieldIndex][i] != enemy) continue;
                _fieldHitGenerations[fieldIndex][i] = enemy.SpawnVersion;
                return;
            }
            for (int i = 0; i < MaxEnemiesPerField; i++)
            {
                if (_fieldHits[fieldIndex][i] != null && _fieldHits[fieldIndex][i].IsAlive) continue;
                _fieldHits[fieldIndex][i] = enemy;
                _fieldHitGenerations[fieldIndex][i] = enemy.SpawnVersion;
                return;
            }
        }

        private void PruneFieldHits(int fieldIndex)
        {
            for (int i = 0; i < MaxEnemiesPerField; i++)
            {
                EnemyController enemy = _fieldHits[fieldIndex][i];
                if (enemy != null && enemy.IsAlive) continue;
                _fieldHits[fieldIndex][i] = null;
                _fieldHitGenerations[fieldIndex][i] = 0;
            }
        }

        private int FindAvailableField()
        {
            for (int i = 0; i < MaxActiveFields; i++)
                if (_fieldSkills[i] == null) return i;
            return -1;
        }

        private void ClearActiveFields()
        {
            for (int i = 0; i < MaxActiveFields; i++) ClearField(i);
        }

        private void ClearField(int index)
        {
            _fieldSkills[index] = null;
            _fieldRemaining[index] = 0f;
            _fieldStatusRefreshRemaining[index] = 0f;
            for (int i = 0; i < MaxEnemiesPerField; i++)
            {
                _fieldHits[index][i] = null;
                _fieldHitGenerations[index][i] = 0;
            }
        }

        private bool LaunchProjectile(EnemyController target, float multiplier, SkillData shape, SkillData triggeredSkill)
        {
            if (target == null) return false;
            if (!CanLaunchProjectile()) return false;
            float speed = shape != null ? shape.projectileSpeed : 0f;
            if (speed <= 0f || float.IsNaN(speed) || float.IsInfinity(speed))
            {
                ReportInvalidSkill("projectile attacks require a positive finite projectileSpeed in passive SkillData.");
                return false;
            }

            Projectile projectile = _projectiles.Get();
            _activeProjectiles[_activeProjectileCount++] = projectile;
            Transform muzzleAnchor = _context.Muzzle;
            Vector3 muzzle = muzzleAnchor != null ? muzzleAnchor.position : transform.position;
            var damage = CreateDamage(multiplier, triggeredSkill, true, muzzle);
            projectile.Launch(muzzle, target, target.transform, damage, speed);
            _reportedPoolExhaustion = false;
            return true;
        }

        private bool CanLaunchProjectile()
        {
            if (_projectiles != null && _projectiles.CountInactive > 0) return true;
            if (_projectiles == null)
            {
                if (!_reportedMissingProjectile)
                {
                    Debug.LogError("Projectile skills require a Projectile prefab assigned to SkillRunner.", this);
                    _reportedMissingProjectile = true;
                }
                return false;
            }

            if (!_reportedPoolExhaustion)
            {
                Debug.LogWarning("SkillRunner projectile pool is exhausted; cast delayed until a projectile returns.", this);
                _reportedPoolExhaustion = true;
            }
            return false;
        }

        private bool ApplyAreaHit(Vector3 center, SkillData shape, SkillData skill, float multiplier)
        {
            EnemyManager enemyManager = _context.EnemyManager;
            if (enemyManager == null)
            {
                ReportInvalidSkill("AreaAtPoint requires an EnemyManager assigned to HeroController.");
                return false;
            }
            if (shape == null || shape.kind != SkillKind.AreaAtPoint)
            {
                ReportInvalidSkill("AreaAtPoint requires matching passive SkillData with a configured radius.");
                return false;
            }

            float radius = Mathf.Max(0f, shape.radius);
            float radiusSquared = radius * radius;
            IReadOnlyList<EnemyController> enemies = enemyManager.ActiveEnemies;

            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyController enemy = enemies[i];
                if (enemy == null || !enemy.IsAlive || enemy.Data == null) continue;

                Vector3 offset = enemy.transform.position - center;
                offset.y = 0f;
                if (offset.sqrMagnitude > radiusSquared) continue;

                ApplyHit(enemy, multiplier, skill, true);
            }

            return true;
        }

        private void ApplyHit(EnemyController target, float multiplier, SkillData triggeredSkill, bool canHitFlying)
        {
            if (target == null || !target.IsAlive) return;

            var damage = CreateDamage(multiplier, triggeredSkill, canHitFlying, target.transform.position);
            target.TakeDamage(damage);
        }

        private void RefreshFieldStatus(EnemyController target, SkillData skill)
        {
            if (skill.status == null || target.Status == null) return;
            target.Status.Apply(skill.status, _context.Damage * skill.damageMultiplier, skill.damageType);
        }

        private static float GetStatusRefreshInterval(SkillData skill)
        {
            if (skill.status == null) return 0f;
            float interval = skill.status.tickInterval;
            // Non-periodic statuses like slow still need refreshing while an enemy remains inside a field.
            if (interval <= 0f || float.IsNaN(interval) || float.IsInfinity(interval))
                interval = BuffRefreshInterval;

            float duration = skill.status.duration;
            return duration > 0f && !float.IsNaN(duration) && !float.IsInfinity(duration)
                ? Mathf.Min(interval, duration * 0.5f)
                : interval;
        }

        private DamageInfo CreateDamage(float multiplier, SkillData triggeredSkill, bool canHitFlying, Vector3 hitPoint)
        {
            DamageType damageType = triggeredSkill != null ? triggeredSkill.damageType : _context.DamageType;
            StatusEffectData status = triggeredSkill != null ? triggeredSkill.status : null;
            return new DamageInfo(_context.Damage * multiplier, damageType, _context.Element, gameObject,
                canHitFlying, status, hitPoint);
        }

        private void InitializeProjectilePool()
        {
            if (_projectilePrefab == null) return;
            if (_projectilePrewarmCount < 1)
            {
                Debug.LogError("SkillRunner projectile prewarm count must be at least one.", this);
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

        private void ReportInvalidSkill(string message)
        {
            if (_reportedInvalidSkill) return;
            Debug.LogError(message, this);
            _reportedInvalidSkill = true;
        }

        private static SkillKind KindFromAttackType(AttackType attackType)
        {
            switch (attackType)
            {
                case AttackType.Melee: return SkillKind.MeleeHit;
                case AttackType.Projectile: return SkillKind.Projectile;
                case AttackType.Area: return SkillKind.AreaAtPoint;
                case AttackType.Cone: return SkillKind.Cone;
                default: return SkillKind.Buff;
            }
        }

        private static bool IsSupportedAttackKind(SkillKind kind)
        {
            ISkillExecutor executor = GetExecutor(kind);
            return executor != null && executor.SupportsBasicAttack;
        }

        private static bool MatchesAttackType(SkillKind kind, AttackType attackType)
        {
            return (kind == SkillKind.MeleeHit && attackType == AttackType.Melee)
                || (kind == SkillKind.Projectile && attackType == AttackType.Projectile)
                || (kind == SkillKind.AreaAtPoint && attackType == AttackType.Area)
                || (kind == SkillKind.Cone && attackType == AttackType.Cone);
        }

        private static ISkillExecutor GetExecutor(SkillKind kind)
        {
            int index = (int)kind;
            if (index < 0 || index >= SkillExecutors.Length) return null;
            ISkillExecutor executor = SkillExecutors[index];
            return executor.Kind == kind ? executor : null;
        }

        private interface ISkillExecutor
        {
            SkillKind Kind { get; }
            bool SupportsBasicAttack { get; }
            bool ExecuteBasic(SkillRunner runner, EnemyController target, Vector3 aimPoint, Vector3 origin,
                SkillData shape, SkillData skill, float multiplier);
            bool ExecuteActive(SkillRunner runner, SkillData skill, EnemyController target,
                Vector3 aimPoint, Vector3 origin);
        }

        private sealed class MeleeHitExecutor : ISkillExecutor
        {
            public SkillKind Kind => SkillKind.MeleeHit;
            public bool SupportsBasicAttack => true;

            public bool ExecuteBasic(SkillRunner runner, EnemyController target, Vector3 aimPoint, Vector3 origin,
                SkillData shape, SkillData skill, float multiplier)
            {
                runner.ApplyHit(target, multiplier, skill, false);
                return true;
            }

            public bool ExecuteActive(SkillRunner runner, SkillData skill, EnemyController target,
                Vector3 aimPoint, Vector3 origin)
            {
                if (target == null || target.IsFlying) return false;
                runner.ApplyHit(target, skill.damageMultiplier, skill, false);
                return true;
            }
        }

        private sealed class ProjectileExecutor : ISkillExecutor
        {
            public SkillKind Kind => SkillKind.Projectile;
            public bool SupportsBasicAttack => true;

            public bool ExecuteBasic(SkillRunner runner, EnemyController target, Vector3 aimPoint, Vector3 origin,
                SkillData shape, SkillData skill, float multiplier)
            {
                return runner.LaunchProjectile(target, multiplier, shape, skill);
            }

            public bool ExecuteActive(SkillRunner runner, SkillData skill, EnemyController target,
                Vector3 aimPoint, Vector3 origin)
            {
                return runner.LaunchProjectile(target, skill.damageMultiplier, skill, skill);
            }
        }

        private sealed class AreaAtPointExecutor : ISkillExecutor
        {
            public SkillKind Kind => SkillKind.AreaAtPoint;
            public bool SupportsBasicAttack => true;

            public bool ExecuteBasic(SkillRunner runner, EnemyController target, Vector3 aimPoint, Vector3 origin,
                SkillData shape, SkillData skill, float multiplier)
            {
                return runner.ApplyAreaHit(aimPoint, shape, skill, multiplier);
            }

            public bool ExecuteActive(SkillRunner runner, SkillData skill, EnemyController target,
                Vector3 aimPoint, Vector3 origin)
            {
                return runner.ApplyAreaHit(aimPoint, skill, skill, skill.damageMultiplier);
            }
        }

        private sealed class ConeExecutor : ISkillExecutor
        {
            public SkillKind Kind => SkillKind.Cone;
            public bool SupportsBasicAttack => true;

            public bool ExecuteBasic(SkillRunner runner, EnemyController target, Vector3 aimPoint, Vector3 origin,
                SkillData shape, SkillData skill, float multiplier)
            {
                return runner.ApplyConeHit(aimPoint, origin, shape, skill, multiplier);
            }

            public bool ExecuteActive(SkillRunner runner, SkillData skill, EnemyController target,
                Vector3 aimPoint, Vector3 origin)
            {
                return runner.ApplyConeHit(aimPoint, origin, skill, skill, skill.damageMultiplier);
            }
        }

        private sealed class LineExecutor : ISkillExecutor
        {
            public SkillKind Kind => SkillKind.Line;
            public bool SupportsBasicAttack => false;

            public bool ExecuteBasic(SkillRunner runner, EnemyController target, Vector3 aimPoint, Vector3 origin,
                SkillData shape, SkillData skill, float multiplier) => false;

            public bool ExecuteActive(SkillRunner runner, SkillData skill, EnemyController target,
                Vector3 aimPoint, Vector3 origin)
            {
                return runner.StartField(skill, aimPoint, origin);
            }
        }

        private sealed class ZoneExecutor : ISkillExecutor
        {
            public SkillKind Kind => SkillKind.Zone;
            public bool SupportsBasicAttack => false;

            public bool ExecuteBasic(SkillRunner runner, EnemyController target, Vector3 aimPoint, Vector3 origin,
                SkillData shape, SkillData skill, float multiplier) => false;

            public bool ExecuteActive(SkillRunner runner, SkillData skill, EnemyController target,
                Vector3 aimPoint, Vector3 origin)
            {
                return runner.StartField(skill, aimPoint, origin);
            }
        }

        private sealed class BuffExecutor : ISkillExecutor
        {
            public SkillKind Kind => SkillKind.Buff;
            public bool SupportsBasicAttack => false;

            public bool ExecuteBasic(SkillRunner runner, EnemyController target, Vector3 aimPoint, Vector3 origin,
                SkillData shape, SkillData skill, float multiplier) => false;

            public bool ExecuteActive(SkillRunner runner, SkillData skill, EnemyController target,
                Vector3 aimPoint, Vector3 origin) => false;
        }

        private void OnDestroy()
        {
            ResetState();
            for (int i = 0; i < _ownedProjectiles.Count; i++)
                if (_ownedProjectiles[i] != null) _ownedProjectiles[i].Finished -= HandleProjectileFinished;
        }
    }
}
