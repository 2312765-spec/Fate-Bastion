using UnityEngine;

namespace FateBastion.Core
{
    /// <summary>Payload of <see cref="CombatEvents.OnEnemyKilled"/>; kept in Core so UI and Audio can read it.</summary>
    public readonly struct EnemyKilledInfo
    {
        public readonly string EnemyId;
        public readonly Vector3 Position;
        public readonly int GoldReward;
        public readonly bool IsBoss;

        public EnemyKilledInfo(string enemyId, Vector3 position, int goldReward, bool isBoss)
        {
            EnemyId = enemyId;
            Position = position;
            GoldReward = goldReward;
            IsBoss = isBoss;
        }
    }
}
