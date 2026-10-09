using System;

namespace FateBastion.Enemies
{
    /// <summary>One batch of identical enemies inside a wave; groups of a wave run in parallel (S2).</summary>
    [Serializable]
    public class SpawnGroup
    {
        public EnemyData enemy;
        public int count;

        /// <summary>Seconds between two spawns inside this group (scaled time).</summary>
        public float interval;

        /// <summary>Seconds after the wave starts before this group spawns its first enemy.</summary>
        public float startDelay;

        /// <summary>Path to walk: 0 is the ground path, 1 is the flying path (S2).</summary>
        public int pathIndex;
    }
}
