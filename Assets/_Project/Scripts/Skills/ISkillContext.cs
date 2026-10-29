using UnityEngine;
using FateBastion.Combat;
using FateBastion.Core;
using FateBastion.Enemies;

namespace FateBastion.Skills
{
    /// <summary>Provides the combat data a skill needs without coupling Skills back to Heroes.</summary>
    public interface ISkillContext
    {
        float Damage { get; }
        float Range { get; }
        AttackType AttackType { get; }
        DamageType DamageType { get; }
        Element Element { get; }
        SkillData PassiveSkill { get; }
        EnemyController CurrentTarget { get; }
        Transform Muzzle { get; }
        EnemyManager EnemyManager { get; }
        Vector3 Position { get; }
        bool IsActive { get; }
        int ActiveHeroCount { get; }
        ISkillContext GetActiveHero(int index);
        void SetBuff(UnityEngine.Object source, UnityEngine.Object sourceType, BuffType type, float value);
        void RemoveBuff(UnityEngine.Object source);
    }
}
