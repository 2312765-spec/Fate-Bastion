using UnityEngine;

namespace FateBastion.Enemies
{
    /// <summary>Technical limits of the enemy system (S2). One asset at Data/Settings/EnemySettings, owned by A.</summary>
    [CreateAssetMenu(menuName = "Fate Bastion/Settings/Enemy", fileName = "EnemySettings")]
    public class EnemySettings : ScriptableObject
    {
        [Tooltip("Max enemies alive at once; extra spawns wait in a queue that still counts toward the wave.")]
        [SerializeField, Min(1)] private int _maxActiveEnemies = 100;

        [Tooltip("Pool size per enemy type = largest count of that type in one wave of the level + this.")]
        [SerializeField, Min(0)] private int _extraPrewarmPerType = 5;

        [Tooltip("Initial capacity of the active list and the spawn queue, so they do not grow during a match.")]
        [SerializeField, Min(1)] private int _listCapacity = 128;

        public int MaxActiveEnemies => _maxActiveEnemies;
        public int ExtraPrewarmPerType => _extraPrewarmPerType;
        public int ListCapacity => _listCapacity;
    }
}
