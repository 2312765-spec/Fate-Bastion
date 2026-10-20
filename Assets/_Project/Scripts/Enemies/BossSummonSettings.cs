using UnityEngine;

namespace FateBastion.Enemies
{
    /// <summary>
    /// Boss summoning (S2/A-05): a boss calls Count extra enemies every IntervalSeconds while it is alive on the
    /// field. One asset at Data/Settings/BossSummonSettings, owned by A; the summons keep the boss's wave so the
    /// wave only finishes when they die too.
    /// </summary>
    [CreateAssetMenu(menuName = "Fate Bastion/Settings/Boss Summon", fileName = "BossSummonSettings")]
    public class BossSummonSettings : ScriptableObject
    {
        [Tooltip("Enemy type the boss calls (enemy_nhanh / Fast).")]
        [SerializeField] private EnemyData _summonEnemy;

        [Tooltip("How many enemies each wave of summons adds.")]
        [SerializeField, Min(0)] private int _count = 3;

        [Tooltip("Seconds between two summon batches (scaled time).")]
        [SerializeField, Min(0.01f)] private float _intervalSeconds = 12f;

        public EnemyData SummonEnemy => _summonEnemy;
        public int Count => _count;
        public float IntervalSeconds => _intervalSeconds;
        public bool IsValid => _summonEnemy != null && _count > 0;

        /// <summary>For tests and tools; in the game the values are set in the Inspector.</summary>
        public void Configure(EnemyData summonEnemy, int count, float intervalSeconds)
        {
            _summonEnemy = summonEnemy;
            _count = Mathf.Max(0, count);
            _intervalSeconds = Mathf.Max(0.01f, intervalSeconds);
        }
    }
}
