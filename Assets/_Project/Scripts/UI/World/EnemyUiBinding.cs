using System;
using FateBastion.Enemies;

namespace FateBastion.UI.World
{
    /// <summary>
    /// Links one enemy to its health bar. The OnHealthChanged handler is created once per binding (bindings are
    /// pooled), so subscribing on every spawn allocates nothing.
    /// </summary>
    internal sealed class EnemyUiBinding
    {
        private readonly Action<float, float> _onHealthChanged;

        public EnemyUiBinding()
        {
            _onHealthChanged = HandleHealthChanged;
        }

        public EnemyController Enemy { get; private set; }
        public WorldHealthBar Bar { get; private set; }

        /// <summary>Slot in the presenter's bound list (swap-remove); -1 while free.</summary>
        public int Index { get; set; } = -1;

        public void Bind(EnemyController enemy, WorldHealthBar bar)
        {
            Enemy = enemy;
            Bar = bar;
            enemy.OnHealthChanged += _onHealthChanged;
        }

        public void Unbind()
        {
            if (Enemy != null) Enemy.OnHealthChanged -= _onHealthChanged;
            Enemy = null;
            Bar = null;
            Index = -1;
        }

        private void HandleHealthChanged(float current, float max)
        {
            if (Bar != null) Bar.SetHealth(current, max);
        }
    }
}
