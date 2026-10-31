using UnityEngine;

namespace FateBastion.Audio
{
    /// <summary>How long a one-shot effect needs before it can go back to the pool (C-09).</summary>
    public static class FxLifetime
    {
        /// <summary>
        /// Longest start delay + duration + particle lifetime over the root and every child system, in scaled seconds.
        /// Read once per prefab at load. A looping layer never ends: its duration is used and an error is logged.
        /// </summary>
        public static float Of(ParticleSystem root)
        {
            float longest = 0f;
            foreach (ParticleSystem system in root.GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.MainModule main = system.main;
                if (main.loop)
                    Debug.LogError($"FX '{root.name}': layer '{system.name}' loops; one-shot FX must not loop.", root);

                float end = main.startDelay.constantMax + main.duration + main.startLifetime.constantMax;
                if (end > longest) longest = end;
            }

            return longest;
        }
    }
}
