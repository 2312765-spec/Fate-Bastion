using UnityEngine;

namespace FateBastion.Enemies
{
    /// <summary>Spawn HP of an enemy: baseHP x hpGrowth^(wave-1) x hpMultiplier (S2). Float so Endless cannot overflow.</summary>
    public static class EnemyHealthFormula
    {
        /// <param name="wave">1-based wave number; values below 1 count as wave 1.</param>
        public static float Calculate(float baseHP, float hpGrowth, int wave, float hpMultiplier)
        {
            int exponent = Mathf.Max(0, wave - 1);
            return baseHP * Mathf.Pow(hpGrowth, exponent) * hpMultiplier;
        }
    }
}
