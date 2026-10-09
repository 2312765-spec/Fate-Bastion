using System;

namespace FateBastion.Core
{
    /// <summary>Combat event hub (S1). Payloads are Core-only structs so UI and Audio stay decoupled from Combat.</summary>
    public static class CombatEvents
    {
        public static event Action<DamageDealtInfo> OnDamageDealt;

        /// <summary>Raised exactly once per enemy, guarded by the enemy's IsAlive flag (S1, S2).</summary>
        public static event Action<EnemyKilledInfo> OnEnemyKilled;

        public static void RaiseDamageDealt(in DamageDealtInfo info) => OnDamageDealt?.Invoke(info);
        public static void RaiseEnemyKilled(in EnemyKilledInfo info) => OnEnemyKilled?.Invoke(info);

        /// <summary>See <see cref="GameEvents.ResetAll"/>.</summary>
        public static void ResetAll()
        {
            OnDamageDealt = null;
            OnEnemyKilled = null;
        }
    }
}
