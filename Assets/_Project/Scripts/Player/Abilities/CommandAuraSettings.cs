using System;
using UnityEngine;

namespace FateBastion.Player
{
    /// <summary>Balance settings for the Summoner's passive Command Aura.</summary>
    [CreateAssetMenu(menuName = "Fate Bastion/Settings/Command Aura", fileName = "CommandAuraSettings")]
    public sealed class CommandAuraSettings : ScriptableObject
    {
        [SerializeField, Min(0.01f)] private float _radius = 6f;
        [SerializeField, Min(0f)] private float _attackSpeedBonus = 0.25f;
        [SerializeField, Min(0.01f)] private float _checkInterval = 0.2f;

        /// <summary>Maximum horizontal distance at which a hero receives the aura.</summary>
        public float Radius => GetValid(_radius, nameof(_radius), allowZero: false);

        /// <summary>AttackSpeed bonus as a fraction.</summary>
        public float AttackSpeedBonus => GetValid(_attackSpeedBonus, nameof(_attackSpeedBonus), allowZero: true);

        /// <summary>Scaled-time interval between aura membership checks.</summary>
        public float CheckInterval => GetValid(_checkInterval, nameof(_checkInterval), allowZero: false);

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!IsValid(_radius, allowZero: false) || !IsValid(_attackSpeedBonus, allowZero: true)
                || !IsValid(_checkInterval, allowZero: false))
                Debug.LogError($"{name}: CommandAuraSettings values are invalid.", this);
        }
#endif

        private float GetValid(float value, string fieldName, bool allowZero)
        {
            if (!IsValid(value, allowZero))
                throw new InvalidOperationException($"{name}: {fieldName} has an invalid value.");
            return value;
        }

        private static bool IsValid(float value, bool allowZero) =>
            !float.IsNaN(value) && !float.IsInfinity(value) && (allowZero ? value >= 0f : value > 0f);
    }
}
