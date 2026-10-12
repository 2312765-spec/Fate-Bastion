using System;
using FateBastion.Core;

namespace FateBastion.Combat
{
    /// <summary>The single place damage is computed: floor(D x k x (1 - resist)), resist clamped [0; 0.8], min 1 (S1).</summary>
    public static class DamageCalculator
    {
        /// <summary>Upper bound of armor and magic resist (S1); a technical rule of the formula, not a balance number.</summary>
        public const float MaxResistance = 0.8f;

        // ASSUMPTION: values that are whole on paper (100 x (1 - 0.7) = 30) can land a hair below in float
        // (29.999998) and floor would lose 1 HP. A relative nudge of 1e-6 is far above float error
        // (~1e-7 per operation) and far below any real fraction from design data (2 decimals).
        private const double RelativeEpsilon = 1e-6;

        /// <summary>Effective resistance for a damage type: armor for Physical, magic resist for Magic, 0 for True.</summary>
        public static float GetResistance(DamageType type, float armor, float magicResist)
        {
            switch (type)
            {
                case DamageType.Physical: return Clamp(armor);
                case DamageType.Magic: return Clamp(magicResist);
                default: return 0f;
            }
        }

        /// <summary>Full formula. realDamage is D after rarity and level; skillMultiplier is k (basic attack = 1).</summary>
        public static int Calculate(float realDamage, float skillMultiplier, DamageType type,
            float armor, float magicResist)
        {
            double raw = (double)realDamage * skillMultiplier * (1.0 - GetResistance(type, armor, magicResist));
            double floored = Math.Floor(raw * (1.0 + RelativeEpsilon));

            // ASSUMPTION: minimum 1 applies to every call, even Amount <= 0; sources that deal no damage do not call this.
            if (!(floored >= 1.0)) return 1; // also catches NaN from bad data
            return floored >= int.MaxValue ? int.MaxValue : (int)floored;
        }

        /// <summary>For a hit: info.Amount is already D x k (S6 multiplies before building DamageInfo).</summary>
        public static int Calculate(in DamageInfo info, float armor, float magicResist)
        {
            return Calculate(info.Amount, 1f, info.Type, armor, magicResist);
        }

        private static float Clamp(float resistance)
        {
            if (resistance < 0f) return 0f;
            return resistance > MaxResistance ? MaxResistance : resistance;
        }
    }
}
