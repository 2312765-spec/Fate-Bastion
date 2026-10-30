using System;
using Unity.Cinemachine;
using UnityEngine;

namespace FateBastion.Audio
{
    /// <summary>How one kind of camera shake feels: Cinemachine impulse shape, strength and minimum spacing (S11).</summary>
    [Serializable]
    public class ShakeProfile
    {
        [Tooltip("Shape, duration and distance falloff. Uniform = same strength everywhere; Dissipating = weaker far from the camera.")]
        public CinemachineImpulseDefinition definition = new CinemachineImpulseDefinition();

        [Tooltip("Impulse strength (length of the impact velocity). 0 turns this shake off.")]
        [Min(0f)] public float force = 0.5f;

        [Tooltip("Seconds (real time) before the same kind can shake again; stops 10 enemies at the castle from shaking non-stop.")]
        [Min(0f)] public float minInterval;

        public ShakeProfile()
        {
        }

        public ShakeProfile(CinemachineImpulseDefinition.ImpulseShapes shape, float duration,
            CinemachineImpulseDefinition.ImpulseTypes type, float dissipationDistance, float force, float minInterval)
        {
            definition = new CinemachineImpulseDefinition
            {
                ImpulseChannel = 1,
                ImpulseShape = shape,
                ImpulseDuration = duration,
                ImpulseType = type,
                DissipationDistance = dissipationDistance,
                DissipationRate = 0.25f,
                PropagationSpeed = 343f,
            };
            this.force = force;
            this.minInterval = minInterval;
        }
    }
}
