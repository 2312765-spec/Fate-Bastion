using System;
using UnityEngine;
using FateBastion.Core;

namespace FateBastion.Heroes
{
    /// <summary>Stores one hero's runtime stats and applies rarity and level multipliers (S6).</summary>
    [DisallowMultipleComponent]
    public sealed class HeroStats : MonoBehaviour
    {
        // Technical capacity: 15 placed heroes plus the Summoner's aura.
        private const int MaxBuffSources = 16;
        [SerializeField] private HeroStatsSettings _settings;

        private HeroData _data;
        private float _damageMultiplier = 1f;
        private float _attackSpeedMultiplier = 1f;
        private float _rangeMultiplier = 1f;
        private readonly UnityEngine.Object[] _buffSources = new UnityEngine.Object[MaxBuffSources];
        private readonly UnityEngine.Object[] _buffTypes = new UnityEngine.Object[MaxBuffSources];
        private readonly BuffType[] _buffKinds = new BuffType[MaxBuffSources];
        private readonly float[] _buffValues = new float[MaxBuffSources];
        private int _buffCount;
        private float _damageBuff;
        private float _attackSpeedBuff;

        public float Damage => GetBaseDamage() * _damageMultiplier * (1f + _damageBuff);
        public float AttacksPerSecond => GetData().attacksPerSecond * _attackSpeedMultiplier * (1f + _attackSpeedBuff);
        public float Range => GetData().range * _rangeMultiplier;

        public void Initialize(HeroData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (_settings == null) throw new InvalidOperationException($"{name}: HeroStatsSettings is not assigned.");

            _settings.GetDamageMultiplier(data.rarity);
            _data = data;
            ClearBuffs();
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

        /// <summary>Sets or replaces one source's buff contribution.</summary>
        public void SetBuff(UnityEngine.Object source, UnityEngine.Object sourceType, BuffType type, float value)
        {
            if (_data == null || _settings == null)
                throw new InvalidOperationException($"{name}: initialize HeroStats before applying buffs.");
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (sourceType == null) throw new ArgumentNullException(nameof(sourceType));
            if (type == BuffType.None) throw new ArgumentOutOfRangeException(nameof(type));
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
                throw new ArgumentOutOfRangeException(nameof(value), value, "Buff values must be finite and non-negative.");

            for (int i = 0; i < _buffCount; i++)
            {
                if (!ReferenceEquals(_buffSources[i], source)) continue;
                if (ReferenceEquals(_buffTypes[i], sourceType) && _buffKinds[i] == type && _buffValues[i] == value)
                    return;

                _buffTypes[i] = sourceType;
                _buffKinds[i] = type;
                _buffValues[i] = value;
                RecalculateBuffs();
                return;
            }

            if (_buffCount == MaxBuffSources)
                throw new InvalidOperationException($"{name}: no buff slots remain; expected at most 15 heroes plus the Summoner aura.");

            _buffSources[_buffCount] = source;
            _buffTypes[_buffCount] = sourceType;
            _buffKinds[_buffCount] = type;
            _buffValues[_buffCount] = value;
            _buffCount++;
            RecalculateBuffs();
        }

        /// <summary>Removes all buff contributions registered by one source.</summary>
        public void RemoveBuff(UnityEngine.Object source)
        {
            if (ReferenceEquals(source, null)) return;

            bool changed = false;
            for (int i = _buffCount - 1; i >= 0; i--)
            {
                if (!ReferenceEquals(_buffSources[i], source)) continue;
                RemoveBuffAt(i);
                changed = true;
            }

            if (changed) RecalculateBuffs();
        }

        public void Clear()
        {
            _data = null;
            ClearBuffs();
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

        private void ClearBuffs()
        {
            for (int i = 0; i < _buffCount; i++)
            {
                _buffSources[i] = null;
                _buffTypes[i] = null;
                _buffKinds[i] = BuffType.None;
                _buffValues[i] = 0f;
            }
            _buffCount = 0;
            _damageBuff = 0f;
            _attackSpeedBuff = 0f;
        }

        private void RecalculateBuffs()
        {
            _damageBuff = 0f;
            _attackSpeedBuff = 0f;

            for (int i = 0; i < _buffCount; i++)
            {
                if (_buffKinds[i] == BuffType.Damage)
                    _damageBuff += _buffValues[i];
                else if (_buffKinds[i] == BuffType.AttackSpeed)
                    _attackSpeedBuff += _buffValues[i];
            }
        }

        private void RemoveBuffAt(int index)
        {
            int last = --_buffCount;
            _buffSources[index] = _buffSources[last];
            _buffTypes[index] = _buffTypes[last];
            _buffKinds[index] = _buffKinds[last];
            _buffValues[index] = _buffValues[last];
            _buffSources[last] = null;
            _buffTypes[last] = null;
            _buffKinds[last] = BuffType.None;
            _buffValues[last] = 0f;
        }

        private static void ValidateMultiplier(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
                throw new ArgumentOutOfRangeException(parameterName, value, "Stat multipliers must be positive finite values.");
        }
    }
}
