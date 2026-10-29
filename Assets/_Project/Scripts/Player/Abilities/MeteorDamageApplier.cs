using System;
using FateBastion.Combat;
using FateBastion.Core;
using FateBastion.Enemies;
using FateBastion.Skills;
using UnityEngine;

namespace FateBastion.Player
{
    /// <summary>Applies one meteor blast to the active enemy roster, including flying targets (S7).</summary>
    public sealed class MeteorDamageApplier
    {
        // Matches the gameplay cap in EnemySettings; the roster pass is a bounded fallback.
        private const int MaxActiveEnemies = 100;

        // Covers the active-enemy cap with several colliders per enemy and a margin for nearby map colliders.
        private const int MaxOverlapColliders = 512;

        private readonly MeteorSettings _settings;
        private readonly EnemyManager _enemyManager;
        private readonly Collider[] _overlapColliders = new Collider[MaxOverlapColliders];
        private readonly EnemyController[] _activeSnapshot = new EnemyController[MaxActiveEnemies];
        private readonly EnemyController[] _damagedEnemies = new EnemyController[MaxActiveEnemies];
        private bool _reportedColliderOverflow;
        private bool _reportedEnemyOverflow;

        public MeteorDamageApplier(MeteorSettings settings, EnemyManager enemyManager)
        {
            _settings = settings != null ? settings : throw new ArgumentNullException(nameof(settings));
            _enemyManager = enemyManager != null ? enemyManager : throw new ArgumentNullException(nameof(enemyManager));

            SkillData skill = _settings.Skill;
            if (skill == null || skill.kind != SkillKind.AreaAtPoint || skill.damageType != DamageType.True
                || skill.radius <= 0f || _settings.VerticalQueryPadding < 0f)
                throw new InvalidOperationException("MeteorSettings must reference a True AreaAtPoint skill with positive radius and non-negative vertical query padding.");
        }

        public int Apply(Vector3 point, int wave, LevelData level, UnityEngine.Object source)
        {
            if (level == null) throw new ArgumentNullException(nameof(level));

            SkillData skill = _settings.Skill;
            float radius = skill.radius;
            float damage = MeteorDamageCalculator.Calculate(_settings.NormalEnemy, level, skill, wave);

            int colliderCount = Physics.OverlapSphereNonAlloc(
                point,
                radius + _settings.VerticalQueryPadding,
                _overlapColliders,
                Physics.AllLayers,
                QueryTriggerInteraction.Collide);
            if (colliderCount == _overlapColliders.Length && !_reportedColliderOverflow)
            {
                Debug.LogError($"{nameof(MeteorDamageApplier)} collider buffer reached {_overlapColliders.Length}; the active roster fallback will still be checked.");
                _reportedColliderOverflow = true;
            }

            int activeCount = _enemyManager.ActiveEnemies.Count;
            if (activeCount > _activeSnapshot.Length && !_reportedEnemyOverflow)
            {
                Debug.LogError($"{nameof(MeteorDamageApplier)} supports at most {_activeSnapshot.Length} active enemies.");
                _reportedEnemyOverflow = true;
            }

            int snapshotCount = Mathf.Min(activeCount, _activeSnapshot.Length);
            for (int i = 0; i < snapshotCount; i++)
                _activeSnapshot[i] = _enemyManager.ActiveEnemies[i];

            int damagedCount = 0;
            try
            {
                for (int i = 0; i < colliderCount; i++)
                {
                    Collider collider = _overlapColliders[i];
                    if (collider == null) continue;

                    EnemyController enemy = collider.GetComponentInParent<EnemyController>();
                    TryDamage(enemy, point, radius, damage, source, ref damagedCount);
                }

                // The bounded roster pass catches flying enemies and colliders absent from the physics query.
                for (int i = 0; i < snapshotCount; i++)
                    TryDamage(_activeSnapshot[i], point, radius, damage, source, ref damagedCount);

                return damagedCount;
            }
            finally
            {
                for (int i = 0; i < colliderCount; i++)
                    _overlapColliders[i] = null;
                for (int i = 0; i < snapshotCount; i++)
                    _activeSnapshot[i] = null;
                for (int i = 0; i < damagedCount; i++)
                    _damagedEnemies[i] = null;
            }
        }

        private void TryDamage(EnemyController enemy, Vector3 point, float radius, float damage,
            UnityEngine.Object source, ref int damagedCount)
        {
            if (enemy == null || !enemy.IsAlive
                || !MeteorAimMath.IsWithinRangeXZ(point, enemy.transform.position, radius)
                || WasDamaged(enemy, damagedCount))
                return;

            if (damagedCount >= _damagedEnemies.Length)
            {
                if (!_reportedEnemyOverflow)
                {
                    Debug.LogError($"{nameof(MeteorDamageApplier)} hit buffer reached {_damagedEnemies.Length}.");
                    _reportedEnemyOverflow = true;
                }
                return;
            }

            _damagedEnemies[damagedCount++] = enemy;
            // ASSUMPTION: Fire is a presentation label only; True damage and a null status do not apply burn.
            var hit = new DamageInfo(damage, DamageType.True, Element.Fire, source, true, null, point);
            enemy.TakeDamage(in hit);
        }

        private bool WasDamaged(EnemyController enemy, int damagedCount)
        {
            for (int i = 0; i < damagedCount; i++)
                if (_damagedEnemies[i] == enemy) return true;
            return false;
        }
    }
}
