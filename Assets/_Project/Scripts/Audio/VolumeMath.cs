using UnityEngine;

namespace FateBastion.Audio
{
    /// <summary>Converts the 0–1 volume sliders to AudioMixer decibels (S11).</summary>
    public static class VolumeMath
    {
        /// <summary>The AudioMixer floor; treated as silence.</summary>
        public const float SilentDecibels = -80f;

        // log10(0) is -infinity, so tiny slider values are clamped to the mixer floor.
        private const float MinLinear = 0.0001f;

        /// <summary>20·log10(v), clamped to [-80, 0]. Linear dB keeps the slider feeling even across its range.</summary>
        public static float ToDecibels(float linear)
        {
            linear = Mathf.Clamp01(linear);
            if (linear <= MinLinear) return SilentDecibels;
            return Mathf.Max(SilentDecibels, 20f * Mathf.Log10(linear));
        }
    }
}
