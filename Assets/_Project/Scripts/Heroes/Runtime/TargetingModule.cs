using System;
using System.Collections.Generic;
using UnityEngine;
using FateBastion.Core;
using FateBastion.Enemies;

namespace FateBastion.Heroes
{
    /// <summary>Selects the front-most valid enemy, prioritizing an in-range boss for SingleDps heroes.</summary>
    public sealed class TargetingModule
    {
        private HeroData _data;

        public void Initialize(HeroData data)
        {
            _data = data != null ? data : throw new ArgumentNullException(nameof(data));
        }

        public void Clear()
        {
            _data = null;
        }

        public EnemyController FindTarget(Vector3 heroPosition, IReadOnlyList<EnemyController> enemies)
        {
            return FindTarget(heroPosition, enemies, _data != null ? _data.range : 0f);
        }

        public EnemyController FindTarget(Vector3 heroPosition, IReadOnlyList<EnemyController> enemies, float effectiveRange)
        {
            if (_data == null || enemies == null) return null;

            float range = Mathf.Max(0f, effectiveRange);
            float rangeSquared = range * range;
            EnemyController firstInLine = null;
            EnemyController bossInRange = null;
            float firstDistance = float.NegativeInfinity;
            float bossDistance = float.NegativeInfinity;

            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyController enemy = enemies[i];
                if (enemy == null || !enemy.IsAlive || enemy.Data == null) continue;
                if (_data.attackType == AttackType.Melee && enemy.IsFlying) continue;

                Vector3 offset = enemy.transform.position - heroPosition;
                offset.y = 0f;
                if (offset.sqrMagnitude > rangeSquared) continue;

                if (enemy.Distance > firstDistance)
                {
                    firstInLine = enemy;
                    firstDistance = enemy.Distance;
                }

                if (_data.role == Role.SingleDps && enemy.IsBoss && enemy.Distance > bossDistance)
                {
                    bossInRange = enemy;
                    bossDistance = enemy.Distance;
                }
            }

            return bossInRange != null ? bossInRange : firstInLine;
        }
    }
}
