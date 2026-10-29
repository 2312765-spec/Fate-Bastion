using System;
using UnityEngine;

namespace FateBastion.Player
{
    /// <summary>Pure geometry helpers for the meteor ground-point aim (S7).</summary>
    public static class MeteorAimMath
    {
        public static bool IsWithinRangeXZ(Vector3 origin, Vector3 target, float range)
        {
            if (float.IsNaN(range) || float.IsInfinity(range) || range < 0f)
                throw new ArgumentOutOfRangeException(nameof(range), "Range must be finite and non-negative.");

            Vector3 offset = target - origin;
            offset.y = 0f;
            return offset.sqrMagnitude <= range * range;
        }
    }
}
