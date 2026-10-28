using FateBastion.Core;
using UnityEngine;

namespace FateBastion.Audio
{
    /// <summary>
    /// Turns the shared Core events (heroes, combat, match flow, meteor) into sound effects (S11). Only listens;
    /// never changes game state. Lives next to the AudioManager in the _Audio prefab.
    /// </summary>
    // After default scripts, so a GameEvents.ResetAll in GameManager.Start does not wipe these subscriptions.
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioManager))]
    public class GameAudioBridge : MonoBehaviour
    {
        private AudioManager _manager;
        private bool _started;
        private bool _subscribed;
        private float _lastMeteorRemaining;

        private void Awake()
        {
            _manager = GetComponent<AudioManager>();
        }

        private void Start()
        {
            _started = true;
            Subscribe();
        }

        private void OnEnable()
        {
            if (_started) Subscribe();
        }

        private void OnDisable() => Unsubscribe();

        private void Subscribe()
        {
            if (_subscribed) return;
            _subscribed = true;
            HeroEvents.OnHeroPlaced += HandleHeroPlaced;
            HeroEvents.OnHeroUpgraded += HandleHeroUpgraded;
            HeroEvents.OnHeroSold += HandleHeroSold;
            CombatEvents.OnDamageDealt += HandleDamageDealt;
            CombatEvents.OnEnemyKilled += HandleEnemyKilled;
            GameEvents.OnWaveStarted += HandleWaveStarted;
            GameEvents.OnWaveCleared += HandleWaveCleared;
            GameEvents.OnCastleDamaged += HandleCastleDamaged;
            GameEvents.OnMatchEnded += HandleMatchEnded;
            AbilityEvents.OnMeteorCooldownChanged += HandleMeteorCooldownChanged;
            AbilityEvents.OnMeteorExploded += HandleMeteorExploded;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;
            HeroEvents.OnHeroPlaced -= HandleHeroPlaced;
            HeroEvents.OnHeroUpgraded -= HandleHeroUpgraded;
            HeroEvents.OnHeroSold -= HandleHeroSold;
            CombatEvents.OnDamageDealt -= HandleDamageDealt;
            CombatEvents.OnEnemyKilled -= HandleEnemyKilled;
            GameEvents.OnWaveStarted -= HandleWaveStarted;
            GameEvents.OnWaveCleared -= HandleWaveCleared;
            GameEvents.OnCastleDamaged -= HandleCastleDamaged;
            GameEvents.OnMatchEnded -= HandleMatchEnded;
            AbilityEvents.OnMeteorCooldownChanged -= HandleMeteorCooldownChanged;
            AbilityEvents.OnMeteorExploded -= HandleMeteorExploded;
        }

        private SoundLibrary Library => _manager.Library;

        // ---------- Heroes ----------

        private void HandleHeroPlaced(HeroEventInfo info)
        {
            SoundLibrary library = Library;
            if (library != null) Play(library.heroPlace, info.Position);
        }

        private void HandleHeroUpgraded(HeroEventInfo info)
        {
            SoundLibrary library = Library;
            if (library == null) return;
            Play(info.Level >= 3 ? library.heroLevel3 : library.heroUpgrade, info.Position);
        }

        private void HandleHeroSold(HeroEventInfo info)
        {
            SoundLibrary library = Library;
            if (library != null) Play(library.heroSell, info.Position);
        }

        // ---------- Combat ----------

        private void HandleDamageDealt(DamageDealtInfo info)
        {
            // The killing blow is covered by the death sound.
            if (info.TargetWasKilled) return;
            SoundLibrary library = Library;
            if (library != null) Play(library.enemyHit, info.HitPoint);
        }

        private void HandleEnemyKilled(EnemyKilledInfo info)
        {
            SoundLibrary library = Library;
            if (library == null) return;
            Play(info.IsBoss && library.bossDeath != null ? library.bossDeath : library.enemyDeath, info.Position);
        }

        // ---------- Match ----------

        private void HandleWaveStarted(int wave)
        {
            SoundLibrary library = Library;
            if (library == null) return;
            _manager.Play2D(library.waveStart);
            // Boss waves come from the wave number (AudioConfig.bossWaveInterval); Core has no "boss spawned" event.
            if (_manager.Config.IsBossWave(wave)) _manager.Play2D(library.bossAppear);
        }

        private void HandleWaveCleared(int wave)
        {
            SoundLibrary library = Library;
            if (library != null) Play2D(library.waveClear);
        }

        private void HandleCastleDamaged(int currentHP, int maxHP)
        {
            SoundLibrary library = Library;
            if (library != null) Play2D(library.castleHit);
        }

        private void HandleMatchEnded(MatchResult result)
        {
            SoundLibrary library = Library;
            if (library == null) return;
            _manager.Play2D(result.Won ? library.victory : library.defeat);
        }

        // ---------- Meteor ----------

        private void HandleMeteorCooldownChanged(float remaining, float total)
        {
            SoundLibrary library = Library;
            if (library != null)
            {
                // Cooldown restarting at full = a meteor was just cast. Reaching 0 = ready again.
                if (total > 0f && remaining >= total && _lastMeteorRemaining < total) _manager.Play2D(library.meteorFall);
                else if (remaining <= 0f && _lastMeteorRemaining > 0f) _manager.Play2D(library.meteorReady);
            }

            _lastMeteorRemaining = remaining;
        }

        private void HandleMeteorExploded(Vector3 point, float radius)
        {
            SoundLibrary library = Library;
            if (library != null) Play(library.meteorExplode, point);
        }

        private void Play(SoundData sound, Vector3 position)
        {
            if (sound != null) _manager.Play(sound, position);
        }

        private void Play2D(SoundData sound)
        {
            if (sound != null) _manager.Play2D(sound);
        }
    }
}
