using UnityEngine;

namespace FateBastion.Enemies
{
    /// <summary>
    /// Static data of one enemy type. All ten CSV columns are written by Tools > Import Balance CSV
    /// from enemies.csv and keep the CSV column names (S0, S2).
    /// </summary>
    [CreateAssetMenu(menuName = "Fate Bastion/Enemy", fileName = "enemy_")]
    public class EnemyData : ScriptableObject
    {
        [Header("Written by the CSV importer")]
        public string id;
        public string displayName;

        [Tooltip("HP_0; the spawn HP is baseHP * hpGrowth^(wave-1) * hpMultiplier.")]
        public float baseHP;

        public float moveSpeed;

        [Tooltip("Physical damage reduction as a fraction; clamped to [0, 0.8] by DamageCalculator.")]
        public float armor;

        [Tooltip("Magic damage reduction as a fraction; clamped to [0, 0.8] by DamageCalculator.")]
        public float magicResist;

        [Tooltip("Flying enemies use their own path and cannot be hit by melee heroes.")]
        public bool flying;

        public bool boss;

        public int goldReward;

        [Tooltip("HP removed from the Castle when this enemy reaches the end of its path.")]
        public int castleDamage;

        [Header("Assigned by hand; the importer never overwrites these")]
        public GameObject prefab;
        public Sprite icon;
    }
}
