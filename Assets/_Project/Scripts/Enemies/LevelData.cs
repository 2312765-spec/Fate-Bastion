using UnityEngine;

namespace FateBastion.Enemies
{
    /// <summary>
    /// One level: its 15 waves plus the economy, Castle and reward numbers (S2, S5).
    /// Created by Tools > Import Balance CSV as 'level_&lt;level&gt;'; the importer only writes 'level' and 'waves'.
    /// Paths are not here: a ScriptableObject cannot reference scene objects, so the SplineContainers live
    /// on a LevelPaths component in the Game scene (S2).
    /// </summary>
    [CreateAssetMenu(menuName = "Fate Bastion/Level", fileName = "level_1")]
    public class LevelData : ScriptableObject
    {
        [Header("Written by the CSV importer")]
        public int level;

        [Tooltip("The 15 waves of this level, in wave order.")]
        public WaveData[] waves;

        [Header("Assigned by hand")]
        public string displayName;

        [Tooltip("Enemy types shown on the level select screen.")]
        public EnemyData[] enemyTypesPreview;

        [Header("Enemy HP scaling")]
        [Tooltip("Per-wave HP growth; 1.12 for every level.")]
        public float hpGrowth = 1.12f;

        [Tooltip("Level HP multiplier: 1.0 / 1.15 / 1.3 for levels 1 / 2 / 3. Endless reuses the level 3 value.")]
        public float hpMultiplier = 1f;

        [Header("Economy and Castle")]
        public int startingGold = 300;
        public int castleHP = 20;

        [Header("Timing (seconds)")]
        public float prepareSeconds = 15f;
        public float restSeconds = 8f;

        [Header("Stars")]
        [Tooltip("Castle HP needed for 3 stars.")]
        public int star3CastleHP = 15;

        [Tooltip("Castle HP needed for 2 stars; surviving with less still gives 1 star.")]
        public int star2CastleHP = 8;

        [Header("Gem rewards")]
        public int firstClearGems = 300;
        public int gemsPerNewStar = 50;
    }
}
