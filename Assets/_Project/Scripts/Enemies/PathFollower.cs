using UnityEngine;

namespace FateBastion.Enemies
{
    /// <summary>
    /// Distance of one enemy along its path (S2). Reaching the Castle is checked as distance &gt;= length,
    /// never by collision, so large steps at x2 cannot skip it.
    /// </summary>
    public sealed class PathFollower
    {
        public IEnemyPath Path { get; private set; }
        public float Distance { get; private set; }

        public bool HasReachedEnd => Path == null || Distance >= Path.Length;

        /// <summary>0 at the Gate, 1 at the Castle; used for "first in line" targeting and the offscreen warning.</summary>
        public float Progress
        {
            get
            {
                if (Path == null || Path.Length <= 0f) return 1f;
                return Mathf.Clamp01(Distance / Path.Length);
            }
        }

        public void Reset(IEnemyPath path, float startDistance)
        {
            Path = path;
            Distance = Mathf.Max(0f, startDistance);
        }

        /// <summary>Moves by speed x speedMultiplier x deltaTime. Returns true once the end is reached.</summary>
        public bool Step(float speed, float speedMultiplier, float deltaTime)
        {
            if (deltaTime > 0f) Distance += speed * speedMultiplier * deltaTime;
            return HasReachedEnd;
        }
    }
}
