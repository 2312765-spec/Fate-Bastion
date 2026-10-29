using System;
using UnityEngine;
using FateBastion.Core;
using FateBastion.Enemies;
using FateBastion.Skills;

namespace FateBastion.Player
{
    /// <summary>Calculates the meteor's True damage from the normal enemy's wave-scaled HP (S7).</summary>
    public static class MeteorDamageCalculator
    {
        public static float Calculate(EnemyData normalEnemy, LevelData level, SkillData skill, int wave)
        {
            if (normalEnemy == null) throw new ArgumentNullException(nameof(normalEnemy));
            if (level == null) throw new ArgumentNullException(nameof(level));
            if (skill == null) throw new ArgumentNullException(nameof(skill));
            if (wave < 1) throw new ArgumentOutOfRangeException(nameof(wave));
            if (skill.kind != SkillKind.AreaAtPoint || skill.damageType != DamageType.True)
                throw new ArgumentException("Meteor damage requires a True AreaAtPoint skill.", nameof(skill));
            if (normalEnemy.baseHP <= 0f || level.hpGrowth <= 0f || level.hpMultiplier <= 0f
                || skill.damageMultiplier <= 0f)
                throw new ArgumentOutOfRangeException(nameof(skill), "Meteor damage inputs must be positive.");

            float damage = normalEnemy.baseHP * Mathf.Pow(level.hpGrowth, wave - 1)
                * level.hpMultiplier * skill.damageMultiplier;
            if (float.IsNaN(damage) || float.IsInfinity(damage))
                throw new OverflowException($"Meteor damage is not finite for wave {wave}.");

            return damage;
        }
    }
}
