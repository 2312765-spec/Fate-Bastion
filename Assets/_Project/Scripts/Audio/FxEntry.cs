using System;
using UnityEngine;

namespace FateBastion.Audio
{
    /// <summary>One kind of pooled effect: prefab, how many may be alive at once, minimum spacing (C-09).</summary>
    [Serializable]
    public class FxEntry
    {
        [Tooltip("Prefab under Prefabs/FX; its root must have a non-looping ParticleSystem.")]
        public ParticleSystem prefab;

        [Tooltip("Copies alive at once. Also the pool size, created when the scene loads (no Instantiate in a match).")]
        [Min(1)] public int maxConcurrent = 8;

        [Tooltip("Game seconds before the same kind may start again; 0 = no spacing.")]
        [Min(0f)] public float minInterval;

        public FxEntry()
        {
        }

        public FxEntry(int maxConcurrent, float minInterval)
        {
            this.maxConcurrent = maxConcurrent;
            this.minInterval = minInterval;
        }
    }
}
