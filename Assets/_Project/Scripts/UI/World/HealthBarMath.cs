using UnityEngine;

namespace FateBastion.UI.World
{
    /// <summary>Pure rules of the enemy health bar: fill ratio and "hidden at full HP" (S10).</summary>
    public static class HealthBarMath
    {
        public static float FillRatio(float current, float max)
        {
            return max > 0f ? Mathf.Clamp01(current / max) : 0f;
        }

        /// <summary>Shown only once the enemy has lost health, and hidden again when it dies.</summary>
        public static bool ShouldShow(float current, float max)
        {
            return max > 0f && current > 0f && current < max;
        }
    }
}
