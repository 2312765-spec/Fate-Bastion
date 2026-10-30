using UnityEngine;

namespace FateBastion.UI.World
{
    /// <summary>Hit flash brightness over time (S11): full at the hit, linear fade to 0.</summary>
    public static class HitFlashMath
    {
        /// <summary>0–1 brightness with <paramref name="remaining"/> of <paramref name="duration"/> seconds left.</summary>
        public static float Intensity(float remaining, float duration)
        {
            if (duration <= 0f || remaining <= 0f) return 0f;
            return Mathf.Clamp01(remaining / duration);
        }
    }
}
