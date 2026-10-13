using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace FateBastion.Enemies
{
    /// <summary>
    /// IEnemyPath over a SplineContainer (S2). Length is cached at creation, so move or edit the spline before
    /// the match starts. Positions include the container's transform.
    /// </summary>
    public sealed class SplineEnemyPath : IEnemyPath
    {
        private readonly SplineContainer _container;

        public SplineEnemyPath(SplineContainer container)
        {
            _container = container;
            Length = container != null ? container.CalculateLength() : 0f;
        }

        public float Length { get; }

        public void Evaluate(float distance, out Vector3 position, out Vector3 forward)
        {
            if (_container == null || Length <= 0f)
            {
                position = _container != null ? _container.transform.position : Vector3.zero;
                forward = Vector3.forward;
                return;
            }

            // Normalised t of Unity Splines is length-based (distance lookup table), so t = distance / length.
            float t = Mathf.Clamp01(distance / Length);
            position = _container.EvaluatePosition(t);
            float3 tangent = _container.EvaluateTangent(t);
            forward = math.lengthsq(tangent) > 1e-8f ? (Vector3)math.normalize(tangent) : Vector3.forward;
        }
    }
}
