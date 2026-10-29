using System;
using UnityEngine;
using FateBastion.Core;
using FateBastion.Heroes;

namespace FateBastion.Player
{
    /// <summary>Applies the Summoner's AttackSpeed aura to nearby active heroes (S7).</summary>
    [DisallowMultipleComponent]
    public sealed class CommandAura : MonoBehaviour
    {
        private const int MaxHeroCount = 15;
        [SerializeField] private CommandAuraSettings _settings;

        private readonly HeroController[] _trackedHeroes = new HeroController[MaxHeroCount];
        private readonly uint[] _trackedVersions = new uint[MaxHeroCount];
        private int _trackedCount;
        private float _scanTimer;
        private bool _reportedCapacityExceeded;

        /// <summary>Raised once when an active hero enters the aura radius.</summary>
        public event Action<GameObject> OnHeroEnteredAura;

        private void Awake()
        {
            if (_settings == null)
            {
                Debug.LogError($"{name}: CommandAuraSettings is not assigned.", this);
                enabled = false;
                return;
            }

            _ = _settings.Radius;
            _ = _settings.AttackSpeedBonus;
            _ = _settings.CheckInterval;
        }

        private void OnEnable()
        {
            _scanTimer = 0f;
        }

        private void OnDisable()
        {
            ClearTrackedHeroes();
        }

        private void Update()
        {
            if (_settings == null) return;
            _scanTimer -= Time.deltaTime;
            if (_scanTimer > 0f) return;

            _scanTimer = _settings.CheckInterval;
            RefreshMembership();
        }

        private void RefreshMembership()
        {
            for (int trackedIndex = _trackedCount - 1; trackedIndex >= 0; trackedIndex--)
            {
                HeroController hero = _trackedHeroes[trackedIndex];
                if (hero != null && IsRegistered(hero)
                    && hero.InitializationVersion == _trackedVersions[trackedIndex] && IsWithinRange(hero))
                    continue;

                if (hero != null) hero.Stats?.RemoveBuff(this);
                RemoveTrackedAt(trackedIndex);
            }

            float radiusSquared = _settings.Radius * _settings.Radius;
            for (int activeIndex = 0; activeIndex < HeroController.ActiveHeroCount; activeIndex++)
            {
                HeroController hero = HeroController.GetActiveHero(activeIndex);
                if (hero == null || hero.Data == null || hero.Stats == null || IsTracked(hero)
                    || !IsWithinRange(hero, radiusSquared))
                    continue;

                if (_trackedCount == MaxHeroCount)
                {
                    if (!_reportedCapacityExceeded)
                    {
                        Debug.LogError($"{name}: CommandAura supports at most {MaxHeroCount} active heroes.", this);
                        _reportedCapacityExceeded = true;
                    }
                    continue;
                }

                hero.Stats.SetBuff(this, _settings, BuffType.AttackSpeed, _settings.AttackSpeedBonus);
                _trackedHeroes[_trackedCount] = hero;
                _trackedVersions[_trackedCount] = hero.InitializationVersion;
                _trackedCount++;
                OnHeroEnteredAura?.Invoke(hero.gameObject);
            }
        }

        private bool IsRegistered(HeroController hero)
        {
            for (int i = 0; i < HeroController.ActiveHeroCount; i++)
                if (HeroController.GetActiveHero(i) == hero) return true;
            return false;
        }

        private bool IsWithinRange(HeroController hero)
        {
            float radius = _settings.Radius;
            return IsWithinRange(hero, radius * radius);
        }

        private bool IsWithinRange(HeroController hero, float radiusSquared)
        {
            Vector3 offset = hero.transform.position - transform.position;
            offset.y = 0f;
            return offset.sqrMagnitude <= radiusSquared;
        }

        private bool IsTracked(HeroController hero)
        {
            for (int i = 0; i < _trackedCount; i++)
                if (_trackedHeroes[i] == hero) return true;
            return false;
        }

        private void RemoveTrackedAt(int index)
        {
            int last = --_trackedCount;
            _trackedHeroes[index] = _trackedHeroes[last];
            _trackedVersions[index] = _trackedVersions[last];
            _trackedHeroes[last] = null;
            _trackedVersions[last] = 0;
        }

        private void ClearTrackedHeroes()
        {
            for (int i = 0; i < _trackedCount; i++)
            {
                HeroController hero = _trackedHeroes[i];
                if (hero != null) hero.Stats?.RemoveBuff(this);
                _trackedHeroes[i] = null;
                _trackedVersions[i] = 0;
            }
            _trackedCount = 0;
        }
    }
}
