using System;
using UnityEngine;

namespace FateBastion.Heroes
{
    /// <summary>Stores one hero's runtime stats and applies rarity and level multipliers (S6).</summary>
    [DisallowMultipleComponent]
    public sealed class HeroStats : MonoBehaviour
    {
        [SerializeField] private HeroStatsSettings _settings;

        private HeroData _data;
        private float _damageMultiplier = 1f;
        private float _attackSpeedMultiplier = 1f;
        private float _rangeMultiplier = 1f;

        public float Damage => GetBaseDamage() * _damageMultiplier;
        public float AttacksPerSecond => GetData().attacksPerSecond * _attackSpeedMultiplier;
        public float Range => GetData().range * _rangeMultiplier;

        public void Initialize(HeroData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (_settings == null) throw new InvalidOperationException($"{name}: HeroStatsSettings is not assigned.");

            _data = data;
            ResetLevelMultipliers();
        }

        /// <summary>Applies the level factors supplied by HeroLevel (TEAM_ASSIGNMENT 3.1 #4).</summary>
        public void SetLevelMultipliers(float damage, float attackSpeed, float range)
        {
            ValidateMultiplier(damage, nameof(damage));
            ValidateMultiplier(attackSpeed, nameof(attackSpeed));
            ValidateMultiplier(range, nameof(range));

            _damageMultiplier = damage;
            _attackSpeedMultiplier = attackSpeed;
            _rangeMultiplier = range;
        }

        public void ResetLevelMultipliers()
        {
            _damageMultiplier = 1f;
            _attackSpeedMultiplier = 1f;
            _rangeMultiplier = 1f;
        }

        public void Clear()
        {
            _data = null;
            ResetLevelMultipliers();
        }

        private float GetBaseDamage()
        {
            HeroData data = GetData();
            if (_settings == null)
                throw new InvalidOperationException("HeroStats must be initialized with HeroStatsSettings.");

            return data.damage * _settings.GetDamageMultiplier(data.rarity);
        }

        private HeroData GetData()
        {
            if (_data == null) throw new InvalidOperationException("HeroStats must be initialized with HeroData.");
            return _data;
        }

        private static void ValidateMultiplier(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
                throw new ArgumentOutOfRangeException(parameterName, value, "Stat multipliers must be positive finite values.");
        }
    }
}
