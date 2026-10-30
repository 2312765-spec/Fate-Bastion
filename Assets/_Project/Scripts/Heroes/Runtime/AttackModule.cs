using UnityEngine;
using FateBastion.Core;
using FateBastion.Enemies;
using FateBastion.Skills;

namespace FateBastion.Heroes
{
    /// <summary>Runs basic attacks on a scaled-time timer and delegates their execution to SkillRunner (S6).</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SkillRunner))]
    public sealed class AttackModule : MonoBehaviour
    {
        private readonly AttackTimer _attackTimer = new AttackTimer();
        private HeroController _hero;
        private SkillRunner _skillRunner;
        private bool _reportedUnsupportedType;

        private void Awake()
        {
            if (!TryGetComponent(out _hero))
            {
                Debug.LogError($"{name}: AttackModule requires a HeroController on the same GameObject.", this);
                enabled = false;
                return;
            }
            if (!TryGetComponent(out _skillRunner))
            {
                Debug.LogError($"{name}: AttackModule requires a SkillRunner on the same GameObject.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            if (_hero == null || _hero.Data == null || _hero.Stats == null)
            {
                _attackTimer.Reset();
                return;
            }

            HeroData data = _hero.Data;
            if (data.attackType != AttackType.Melee
                && data.attackType != AttackType.Projectile
                && data.attackType != AttackType.Area
                && data.attackType != AttackType.Cone)
            {
                if (!_reportedUnsupportedType)
                {
                    Debug.LogWarning(
                        $"{name}: basic {data.attackType} attacks are not supported by the current SkillRunner kinds.",
                        this);
                    _reportedUnsupportedType = true;
                }
                _attackTimer.Reset();
                return;
            }

            EnemyController target = _hero.CurrentTarget;
            bool canAttack = IsTargetInRange(target, data.attackType);
            canAttack &= _skillRunner.CanExecuteBasicAttack();

            if (!_attackTimer.Tick(Time.deltaTime, _hero.Stats.AttacksPerSecond, canAttack)) return;
            _skillRunner.ExecuteBasicAttack(target);
        }

        /// <summary>Clears the attack cadence when the hero enters its pool.</summary>
        public void ResetState()
        {
            _attackTimer.Reset();
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

    }
}
