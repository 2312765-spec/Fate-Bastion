using UnityEngine;
using FateBastion.Enemies;

namespace FateBastion.Tests.EditMode.S2
{
    /// <summary>Straight path along +X from the origin, so tests do not need a SplineContainer.</summary>
    public sealed class StraightTestPath : IEnemyPath
    {
        public StraightTestPath(float length)
        {
            Length = length;
        }

        public float Length { get; }

        public void Evaluate(float distance, out Vector3 position, out Vector3 forward)
        {
            position = new Vector3(Mathf.Clamp(distance, 0f, Length), 0f, 0f);
            forward = Vector3.right;
        }
    }
}
