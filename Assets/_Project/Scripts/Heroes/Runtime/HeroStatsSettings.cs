using System;
using UnityEngine;
using FateBastion.Core;

namespace FateBastion.Heroes
{
    /// <summary>Balance multipliers for hero rarity; values are configured in one project asset.</summary>
    [CreateAssetMenu(menuName = "Fate Bastion/Settings/Hero Stats", fileName = "HeroStatsSettings")]
    public sealed class HeroStatsSettings : ScriptableObject
    {
        [SerializeField, Min(0.01f)] private float _commonDamageMultiplier;
        [SerializeField, Min(0.01f)] private float _rareDamageMultiplier;
        [SerializeField, Min(0.01f)] private float _epicDamageMultiplier;
        [SerializeField, Min(0.01f)] private float _legendaryDamageMultiplier;
        [Tooltip("Maximum combined AttackSpeed buff as a fraction (0.6 = +60%).")]
        [SerializeField, Range(0f, 1f)] private float _attackSpeedBuffCap = 0.6f;

        /// <summary>Maximum combined AttackSpeed buff as a fraction.</summary>
        public float AttackSpeedBuffCap
        {
            get
            {
                if (float.IsNaN(_attackSpeedBuffCap) || float.IsInfinity(_attackSpeedBuffCap)
                    || _attackSpeedBuffCap < 0f || _attackSpeedBuffCap > 1f)
                    throw new InvalidOperationException("HeroStatsSettings attack-speed buff cap must be between 0 and 1.");
                return _attackSpeedBuffCap;
            }
        }

        public float GetDamageMultiplier(Rarity rarity)
        {
            float multiplier;
            switch (rarity)
            {
                case Rarity.Common:
                    multiplier = _commonDamageMultiplier;
                    break;
                case Rarity.Rare:
                    multiplier = _rareDamageMultiplier;
                    break;
                case Rarity.Epic:
                    multiplier = _epicDamageMultiplier;
                    break;
                case Rarity.Legendary:
                    multiplier = _legendaryDamageMultiplier;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(rarity), rarity, "Unknown hero rarity.");
            }

            if (float.IsNaN(multiplier) || float.IsInfinity(multiplier) || multiplier <= 0f)
                throw new InvalidOperationException($"HeroStatsSettings has no valid damage multiplier for {rarity}.");

            return multiplier;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!IsValid(_commonDamageMultiplier) || !IsValid(_rareDamageMultiplier) ||
                !IsValid(_epicDamageMultiplier) || !IsValid(_legendaryDamageMultiplier))
            {
                Debug.LogError($"{name}: set a positive damage multiplier for every rarity in HeroStatsSettings.", this);
            }
            if (float.IsNaN(_attackSpeedBuffCap) || float.IsInfinity(_attackSpeedBuffCap)
                || _attackSpeedBuffCap < 0f || _attackSpeedBuffCap > 1f)
            {
                Debug.LogError($"{name}: attack-speed buff cap must be between 0 and 1.", this);
            }
        }
#endif

        private static bool IsValid(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f;
    }
}
