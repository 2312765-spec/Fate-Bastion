using System;
using FateBastion.Core;

namespace FateBastion.Heroes
{
    /// <summary>
    /// The match gold (S4, B-03). The only place that adds gold for kills: it listens to
    /// CombatEvents.OnEnemyKilled and adds GoldReward (TEAM_ASSIGNMENT 3.1 #8). GameManager (B-06) adds wave-end
    /// and skip-wave gold through <see cref="Add"/>. The HUD shows the value from <see cref="OnGoldChanged"/>.
    /// </summary>
    public sealed class Wallet
    {
        private readonly Action<EnemyKilledInfo> _onEnemyKilled;
        private bool _listening;

        public Wallet(int startingGold = 0)
        {
            Gold = Math.Max(0, startingGold);
            _onEnemyKilled = HandleEnemyKilled;
        }

        /// <summary>Raised with the new amount, only when it actually changes.</summary>
        public event Action<int> OnGoldChanged;

        public int Gold { get; private set; }

        public bool CanAfford(int amount) => amount >= 0 && Gold >= amount;

        /// <summary>Pays <paramref name="amount"/>; false and no change when there is not enough gold.</summary>
        public bool TrySpend(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), amount, "Use Add for income.");
            if (Gold < amount) return false;
            Set(Gold - amount);
            return true;
        }

        public void Add(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), amount, "Use TrySpend for costs.");
            Set(Gold + amount);
        }

        /// <summary>Start of a match (LevelData.startingGold) or end of a match (0, S4).</summary>
        public void Reset(int gold) => Set(Math.Max(0, gold));

        /// <summary>Starts adding kill rewards. Call once when the match starts.</summary>
        public void ListenToKills()
        {
            if (_listening) return;
            CombatEvents.OnEnemyKilled += _onEnemyKilled;
            _listening = true;
        }

        /// <summary>Stops adding kill rewards (match end, scene unload). Safe to call twice.</summary>
        public void StopListeningToKills()
        {
            if (!_listening) return;
            CombatEvents.OnEnemyKilled -= _onEnemyKilled;
            _listening = false;
        }

        private void HandleEnemyKilled(EnemyKilledInfo info)
        {
            if (info.GoldReward > 0) Add(info.GoldReward);
        }

        private void Set(int gold)
        {
            if (gold == Gold) return;
            Gold = gold;
            OnGoldChanged?.Invoke(Gold);
        }
    }
}
