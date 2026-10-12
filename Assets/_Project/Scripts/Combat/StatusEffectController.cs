using UnityEngine;
using FateBastion.Core;

namespace FateBastion.Combat
{
    /// <summary>
    /// Burn and slow state of one enemy (S1). Plain class, not a MonoBehaviour: EnemyController (S2) owns one,
    /// EnemyManager ticks it with scaled delta time. Fixed arrays, so Apply and Tick allocate nothing.
    /// Lightning is not a timed effect: chains are resolved on hit with <see cref="ChainLightning"/>.
    /// </summary>
    public sealed class StatusEffectController
    {
        /// <summary>Technical limit: distinct slow assets active on one enemy at once.</summary>
        public const int MaxSlowSources = 4;

        // Float timers drift by ~1e-6 per frame; this keeps "tick at exactly 1 s" from slipping a frame.
        private const float TimeEpsilon = 1e-4f;

        // ASSUMPTION: a burn asset with tickInterval <= 0 is a data error; tick once per second and warn.
        private const float FallbackTickInterval = 1f;

        private readonly float _slowCap;
        private readonly float _bossSlowEffectiveness;
        private bool _isBoss;

        private bool _burnActive;
        private float _burnDamagePerSecond;
        private float _burnRemaining;
        private float _burnTickInterval;
        private float _burnTickTimer;

        private readonly StatusEffectData[] _slowSources = new StatusEffectData[MaxSlowSources];
        private readonly float[] _slowValues = new float[MaxSlowSources];
        private readonly float[] _slowRemaining = new float[MaxSlowSources];
        private int _slowCount;

        public StatusEffectController(float slowCap, float bossSlowEffectiveness)
        {
            _slowCap = slowCap;
            _bossSlowEffectiveness = bossSlowEffectiveness;
        }

        public StatusEffectController(CombatSettings settings)
            : this(settings.SlowCap, settings.BossSlowEffectiveness)
        {
        }

        public bool IsBurning => _burnActive;
        public bool IsSlowed => _slowCount > 0;

        /// <summary>Damage type of burn ticks: same as the hit that applied the burn, so resist still applies.</summary>
        public DamageType BurnDamageType { get; private set; }

        public Element BurnElement { get; private set; }

        /// <summary>Total slow after the cap and boss effectiveness, as a fraction (0.3 = -30% speed).</summary>
        public float SlowAmount { get; private set; }

        /// <summary>Multiply move speed by this.</summary>
        public float SpeedMultiplier => 1f - SlowAmount;

        /// <summary>Call when the enemy is taken from the pool (and returned to it). Clears every effect.</summary>
        public void ResetState(bool isBoss)
        {
            _isBoss = isBoss;
            _burnActive = false;
            _burnDamagePerSecond = 0f;
            _burnRemaining = 0f;
            _burnTickTimer = 0f;

            for (int i = 0; i < MaxSlowSources; i++) _slowSources[i] = null;
            _slowCount = 0;
            SlowAmount = 0f;
        }

        /// <summary>
        /// Applies the status carried by a hit. hitAmount is the hit's DamageInfo.Amount (D x k, before resist);
        /// burn per second = value x hitAmount. Fire = burn, Ice = slow, anything else is ignored.
        /// </summary>
        public void Apply(StatusEffectData data, float hitAmount, DamageType hitType)
        {
            if (data == null) return;

            switch (data.element)
            {
                case Element.Fire:
                    ApplyBurn(data, hitAmount, hitType);
                    break;
                case Element.Ice:
                    ApplySlow(data);
                    break;
            }
        }

        /// <summary>
        /// Advances every effect by deltaTime (scaled). Returns the raw burn damage due this frame, before
        /// resist; the owner passes it through DamageCalculator with <see cref="BurnDamageType"/>.
        /// </summary>
        public float Tick(float deltaTime)
        {
            float burnDamage = 0f;

            if (_burnActive)
            {
                // Only the part of the frame still inside the duration can produce ticks.
                float step = Mathf.Min(deltaTime, _burnRemaining);
                _burnRemaining -= deltaTime;
                _burnTickTimer += step;

                while (_burnTickTimer >= _burnTickInterval - TimeEpsilon)
                {
                    _burnTickTimer -= _burnTickInterval;
                    burnDamage += _burnDamagePerSecond * _burnTickInterval;
                }

                if (_burnRemaining <= TimeEpsilon) _burnActive = false;
            }

            if (_slowCount > 0)
            {
                bool changed = false;
                for (int i = _slowCount - 1; i >= 0; i--)
                {
                    _slowRemaining[i] -= deltaTime;
                    if (_slowRemaining[i] > TimeEpsilon) continue;

                    RemoveSlowAt(i);
                    changed = true;
                }

                if (changed) RecalculateSlow();
            }

            return burnDamage;
        }

        private void ApplyBurn(StatusEffectData data, float hitAmount, DamageType hitType)
        {
            float damagePerSecond = data.value * hitAmount;
            float interval = data.tickInterval;
            if (interval <= 0f)
            {
                Debug.LogWarning($"StatusEffectData '{data.id}': burn needs tickInterval > 0, using {FallbackTickInterval} s.");
                interval = FallbackTickInterval;
            }

            if (!_burnActive)
            {
                _burnActive = true;
                _burnTickTimer = 0f;
                SetBurn(data, damagePerSecond, interval, hitType);
            }
            else if (data.stackRule == StackRule.StrongestWins && damagePerSecond > _burnDamagePerSecond)
            {
                // Keep the tick phase: a hero hitting faster than the tick interval must not delay the next tick.
                SetBurn(data, damagePerSecond, interval, hitType);
            }

            // Both stack rules restart the duration.
            _burnRemaining = data.duration;
        }

        private void SetBurn(StatusEffectData data, float damagePerSecond, float interval, DamageType hitType)
        {
            _burnDamagePerSecond = damagePerSecond;
            _burnTickInterval = interval;
            BurnDamageType = hitType;
            BurnElement = data.element;
        }

        private void ApplySlow(StatusEffectData data)
        {
            float value = Mathf.Clamp01(data.value);

            for (int i = 0; i < _slowCount; i++)
            {
                // Reference compare: same asset = same source, and it skips UnityEngine.Object's native null check.
                if (!ReferenceEquals(_slowSources[i], data)) continue;

                // Same source: strongest wins (or keep current for RefreshDuration), restart the duration.
                if (data.stackRule == StackRule.StrongestWins && value > _slowValues[i]) _slowValues[i] = value;
                _slowRemaining[i] = data.duration;
                RecalculateSlow();
                return;
            }

            int slot = _slowCount;
            if (slot >= MaxSlowSources)
            {
                // ASSUMPTION: all slots full (never in GDD data) -> a stronger slow replaces the weakest one.
                slot = WeakestSlowSlot();
                if (value <= _slowValues[slot]) return;
            }
            else
            {
                _slowCount++;
            }

            _slowSources[slot] = data;
            _slowValues[slot] = value;
            _slowRemaining[slot] = data.duration;
            RecalculateSlow();
        }

        private int WeakestSlowSlot()
        {
            int weakest = 0;
            for (int i = 1; i < _slowCount; i++)
            {
                if (_slowValues[i] < _slowValues[weakest]) weakest = i;
            }
            return weakest;
        }

        private void RemoveSlowAt(int index)
        {
            int last = _slowCount - 1;
            _slowSources[index] = _slowSources[last];
            _slowValues[index] = _slowValues[last];
            _slowRemaining[index] = _slowRemaining[last];
            _slowSources[last] = null;
            _slowCount = last;
        }

        // Different sources add, the total is capped, then a boss takes only part of it (S1).
        private void RecalculateSlow()
        {
            float total = 0f;
            for (int i = 0; i < _slowCount; i++) total += _slowValues[i];

            total = Mathf.Min(total, _slowCap);
            if (_isBoss) total *= _bossSlowEffectiveness;
            SlowAmount = total;
        }
    }
}
