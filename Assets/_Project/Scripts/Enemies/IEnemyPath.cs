using UnityEngine;

namespace FateBastion.Enemies
{
    /// <summary>A path enemies walk by distance in metres; positions are in world space (S2).</summary>
    public interface IEnemyPath
    {
        /// <summary>Total length in metres; an enemy has reached the Castle when its distance is at least this.</summary>
        float Length { get; }

        /// <summary>World position and walking direction at a distance from the start, clamped to [0, Length].</summary>
        void Evaluate(float distance, out Vector3 position, out Vector3 forward);
    }
}
