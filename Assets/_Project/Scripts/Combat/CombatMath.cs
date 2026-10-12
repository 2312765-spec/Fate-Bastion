using UnityEngine;

namespace FateBastion.Combat
{
    /// <summary>Distance helpers on the XZ plane: range and blast checks ignore height because flyers are 2 m up (S1).</summary>
    public static class CombatMath
    {
        public static float SqrDistanceXZ(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return dx * dx + dz * dz;
        }

        public static float DistanceXZ(Vector3 a, Vector3 b)
        {
            return Mathf.Sqrt(SqrDistanceXZ(a, b));
        }

        /// <summary>True when b is within range of a on the XZ plane; the boundary counts as in range.</summary>
        public static bool InRangeXZ(Vector3 a, Vector3 b, float range)
        {
            return SqrDistanceXZ(a, b) <= range * range;
        }
    }
}
