using System;
using System.Collections.Generic;
using UnityEngine;

namespace FateBastion.Heroes
{
    /// <summary>
    /// Hero levels 2..N (S4): upgrade price as a share of the place cost, and the stat bonuses of that level.
    /// Level 1 is the placed hero (no bonus); default data is level 2 and level 3 from the spec.
    /// </summary>
    [CreateAssetMenu(menuName = "Fate Bastion/Settings/Hero Level Table", fileName = "HeroLevelTable")]
    public class HeroLevelTable : ScriptableObject
    {
        /// <summary>One level above 1.</summary>
        [Serializable]
        public struct LevelStep
        {
            [Tooltip("Upgrade price = floor(place cost × this).")]
            public float costPercent;

            [Tooltip("Bonus over level 1, e.g. 0.4 = +40% damage.")]
            public float damageBonus;

            public float attackSpeedBonus;
            public float rangeBonus;

            public LevelStep(float costPercent, float damageBonus, float attackSpeedBonus, float rangeBonus)
            {
                this.costPercent = costPercent;
                this.damageBonus = damageBonus;
                this.attackSpeedBonus = attackSpeedBonus;
                this.rangeBonus = rangeBonus;
            }
        }

        [Tooltip("Element 0 = level 2, element 1 = level 3.")]
        [SerializeField] private List<LevelStep> _steps = new List<LevelStep>
        {
            new LevelStep(0.75f, 0.40f, 0.10f, 0.10f),
            new LevelStep(1.50f, 1.00f, 0.20f, 0.15f)
        };

        /// <summary>Highest level (3 with the spec data).</summary>
        public int MaxLevel => 1 + _steps.Count;

        /// <summary>Data of <paramref name="level"/> (2..MaxLevel).</summary>
        public LevelStep StepOf(int level)
        {
            if (level < 2 || level > MaxLevel)
            {
                throw new ArgumentOutOfRangeException(nameof(level), level, $"Levels with a step are 2..{MaxLevel}.");
            }

            return _steps[level - 2];
        }

        /// <summary>Multipliers for HeroStats; level 1 is ×1.</summary>
        public LevelMultipliers MultipliersOf(int level)
        {
            if (level <= 1)
            {
                return LevelMultipliers.One;
            }

            LevelStep step = StepOf(level);
            return new LevelMultipliers(1f + step.damageBonus, 1f + step.attackSpeedBonus, 1f + step.rangeBonus);
        }

        /// <summary>For tests and tools; in the game the values come from the asset.</summary>
        public void Configure(IEnumerable<LevelStep> steps)
        {
            _steps = new List<LevelStep>(steps);
        }
    }
}
