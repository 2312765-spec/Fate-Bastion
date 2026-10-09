using UnityEngine;

namespace FateBastion.Enemies
{
    /// <summary>One wave. Created by Tools > Import Balance CSV from waves.csv as 'L&lt;level&gt;_W&lt;wave&gt;' (S0, S2).</summary>
    [CreateAssetMenu(menuName = "Fate Bastion/Wave", fileName = "L1_W1")]
    public class WaveData : ScriptableObject
    {
        [Header("Written by the CSV importer")]
        [Tooltip("Level this wave belongs to, 1-based; kept for debugging and for the importer to find the asset.")]
        public int level;

        [Tooltip("Wave number inside the level, 1-based.")]
        public int wave;

        public SpawnGroup[] groups;

        [Tooltip("True when the wave contains at least one boss; drives the warning banner and the boss SFX.")]
        public bool isBossWave;
    }
}
