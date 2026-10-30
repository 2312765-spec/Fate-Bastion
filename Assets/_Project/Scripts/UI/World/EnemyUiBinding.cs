using System;
using FateBastion.Enemies;

namespace FateBastion.UI.World
{
    /// <summary>
    /// Links one enemy to its health bar and hit flash. The OnHealthChanged handler is created once per binding
    /// (bindings are pooled), so subscribing on every spawn allocates nothing. EnemyController drops its
    /// subscribers on despawn, which is why the flash goes through here instead of subscribing on the enemy prefab.
    /// </summary>
    internal sealed class EnemyUiBinding
    {
        private readonly Action<float, float> _onHealthChanged;
        private HitFlashTracker _flashes;
        private WorldUiSettings _settings;
        private float _lastHP;

        public EnemyUiBinding()
        {
            _onHealthChanged = HandleHealthChanged;
        }

        public EnemyController Enemy { get; private set; }
        public WorldHealthBar Bar { get; private set; }

        /// <summary>Hit flash on the enemy model; null when the variant has none.</summary>
        public HitFlash Flash { get; private set; }

        /// <summary>Slot in the presenter's bound list (swap-remove); -1 while free.</summary>
        public int Index { get; set; } = -1;

        public void Bind(EnemyController enemy, WorldHealthBar bar, HitFlash flash, HitFlashTracker flashes, WorldUiSettings settings)
        {
            Enemy = enemy;
            Bar = bar;
            Flash = flash;
            _flashes = flashes;
            _settings = settings;
            _lastHP = enemy.CurrentHP;
            enemy.OnHealthChanged += _onHealthChanged;
        }

        public void Unbind()
        {
            if (Enemy != null) Enemy.OnHealthChanged -= _onHealthChanged;
            if (Flash != null) _flashes.Stop(Flash);
            Enemy = null;
            Bar = null;
            Flash = null;
            _flashes = null;
            _settings = null;
            Index = -1;
        }

        private void HandleHealthChanged(float current, float max)
        {
            if (Bar != null) Bar.SetHealth(current, max);

            // Flash only on damage, not on a reset to full health.
            if (current < _lastHP && Flash != null) _flashes.Start(Flash, _settings.HitFlashColor, _settings.HitFlashDuration);
            _lastHP = current;
        }
    }
}
