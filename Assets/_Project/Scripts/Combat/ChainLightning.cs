using UnityEngine;

namespace FateBastion.Combat
{
    /// <summary>
    /// Lightning chain target selection (S1): from the first hit, jump to the nearest live enemy within
    /// radius (XZ) of the last one hit, never hitting the same enemy twice. Works on caller-owned arrays
    /// so a chain allocates nothing; the caller maps indices back to its enemies.
    /// </summary>
    public static class ChainLightning
    {
        /// <summary>
        /// Writes up to maxBounces enemy indices into results, in hit order, and returns how many were written.
        /// Only the first count entries of positions and alive are read; firstIndex is the enemy already hit.
        /// </summary>
        public static int SelectTargets(int firstIndex, Vector3[] positions, bool[] alive, int count,
            int maxBounces, float radius, int[] results)
        {
            int limit = Mathf.Min(maxBounces, results.Length);
            if (firstIndex < 0 || firstIndex >= count) return 0;

            float sqrRadius = radius * radius;
            int current = firstIndex;
            int found = 0;

            while (found < limit)
            {
                int best = -1;
                float bestSqr = float.MaxValue;

                for (int i = 0; i < count; i++)
                {
                    if (!alive[i] || AlreadyHit(i, firstIndex, results, found)) continue;

                    float sqr = CombatMath.SqrDistanceXZ(positions[current], positions[i]);
                    if (sqr <= sqrRadius && sqr < bestSqr)
                    {
                        best = i;
                        bestSqr = sqr;
                    }
                }

                if (best < 0) break;

                results[found++] = best;
                current = best;
            }

            return found;
        }

        /// <summary>Damage before resist for a bounce: baseDamage x (1 - falloff)^bounce; bounce 0 is the first hit.</summary>
        public static float BounceDamage(float baseDamage, float falloff, int bounce)
        {
            return baseDamage * Mathf.Pow(1f - falloff, bounce);
        }

        // Linear scan is fine: at most 1 + maxBounces (3) entries.
        private static bool AlreadyHit(int index, int firstIndex, int[] results, int found)
        {
            if (index == firstIndex) return true;
            for (int i = 0; i < found; i++)
            {
                if (results[i] == index) return true;
            }
            return false;
        }
    }
}
