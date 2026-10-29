using UnityEngine;

namespace FateBastion.Game
{
    /// <summary>
    /// Match gold paid by GameManager (S5, SRS FR-PLC-07): end of wave n pays base + perWave × n, skipping the rest
    /// pays skipGold. Kill gold is on EnemyData and added by Wallet.
    /// </summary>
    [CreateAssetMenu(menuName = "Fate Bastion/Settings/Match Economy", fileName = "MatchEconomySettings")]
    public class MatchEconomySettings : ScriptableObject
    {
        [Tooltip("Gold at the end of every wave, before the per-wave part.")]
        [SerializeField, Min(0)] private int _waveClearBaseGold = 50;

        [Tooltip("Extra gold × wave number at the end of a wave (wave 3 → 50 + 30).")]
        [SerializeField, Min(0)] private int _waveClearGoldPerWave = 10;

        [Tooltip("Gold for skipping the rest between waves.")]
        [SerializeField, Min(0)] private int _skipGold = 50;

        public int WaveClearBaseGold => _waveClearBaseGold;
        public int WaveClearGoldPerWave => _waveClearGoldPerWave;
        public int SkipGold => _skipGold;

        /// <summary>For tests and tools; in the game the values come from the asset.</summary>
        public void Configure(int waveClearBaseGold, int waveClearGoldPerWave, int skipGold)
        {
            _waveClearBaseGold = Mathf.Max(0, waveClearBaseGold);
            _waveClearGoldPerWave = Mathf.Max(0, waveClearGoldPerWave);
            _skipGold = Mathf.Max(0, skipGold);
        }
    }

    /// <summary>Pure match gold formulas (S5).</summary>
    public static class MatchEconomy
    {
        /// <summary>Gold for clearing wave <paramref name="wave"/> (1-based): base + perWave × wave.</summary>
        public static int WaveClearGold(int baseGold, int perWave, int wave) => wave <= 0 ? 0 : baseGold + perWave * wave;

        public static int WaveClearGold(MatchEconomySettings settings, int wave) =>
            WaveClearGold(settings.WaveClearBaseGold, settings.WaveClearGoldPerWave, wave);
    }
}
