using System;
using UnityEngine;

namespace FateBastion.Heroes
{
    /// <summary>Balance settings for Legendary hero rage and Ultimate activation.</summary>
    [CreateAssetMenu(menuName = "Fate Bastion/Settings/Hero Rage", fileName = "HeroRageSettings")]
    public sealed class HeroRageSettings : ScriptableObject
    {
        [SerializeField, Min(0.01f)] private float _ragePerAttack = 5f;
        [SerializeField, Min(0.01f)] private float _maximumRage = 100f;

        /// <summary>Rage gained for each successful basic attack.</summary>
        public float RagePerAttack => GetValid(_ragePerAttack, nameof(_ragePerAttack));

        /// <summary>Rage required to activate an Ultimate.</summary>
        public float MaximumRage => GetValid(_maximumRage, nameof(_maximumRage));

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!IsValid(_ragePerAttack) || !IsValid(_maximumRage))
                Debug.LogError($"{name}: rage settings must be positive finite values.", this);
        }
#endif

        private float GetValid(float value, string fieldName)
        {
            if (!IsValid(value))
                throw new InvalidOperationException($"{name}: {fieldName} must be a positive finite value.");
            return value;
        }

        private static bool IsValid(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f;
    }
}
