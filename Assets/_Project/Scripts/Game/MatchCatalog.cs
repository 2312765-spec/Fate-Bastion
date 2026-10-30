using UnityEngine;
using FateBastion.Enemies;

namespace FateBastion.Game
{
    /// <summary>
    /// The playable levels in lobby order and the Endless setup (S2, S5), read by the level select screen and
    /// GameManager. Filled by Tools > Meta > Build Match Catalog.
    /// </summary>
    [CreateAssetMenu(menuName = "Fate Bastion/Settings/Match Catalog", fileName = "MatchCatalog")]
    public class MatchCatalog : ScriptableObject
    {
        [Tooltip("Campaign levels in order (level_1, level_2, level_3).")]
        [SerializeField] private LevelData[] _levels = new LevelData[0];

        [Tooltip("Level whose waves, HP scaling and timings Endless uses. Empty = the last campaign level.")]
        [SerializeField] private LevelData _endlessLevel;

        [SerializeField] private EndlessSettings _endlessSettings;

        [Tooltip("Endless unlocks after this many campaign levels have at least one star (GDD 9: after level 1).")]
        [SerializeField, Min(0)] private int _levelsToUnlockEndless = 1;

        public int LevelCount => _levels != null ? _levels.Length : 0;
        public EndlessSettings EndlessSettings => _endlessSettings;
        public int LevelsToUnlockEndless => _levelsToUnlockEndless;

        // ASSUMPTION: Endless reuses the last campaign level (LevelData tooltip: "Endless reuses the level 3 value").
        public LevelData EndlessLevel => _endlessLevel != null ? _endlessLevel : LevelCount > 0 ? _levels[LevelCount - 1] : null;

        public LevelData LevelAt(int index) => index >= 0 && index < LevelCount ? _levels[index] : null;

        public int IndexOf(LevelData level)
        {
            for (int i = 0; i < LevelCount; i++)
            {
                if (_levels[i] == level) return i;
            }

            return -1;
        }

        /// <summary>For tests and tools.</summary>
        public void Configure(LevelData[] levels, LevelData endlessLevel, EndlessSettings endlessSettings, int levelsToUnlockEndless = 1)
        {
            _levels = levels ?? new LevelData[0];
            _endlessLevel = endlessLevel;
            _endlessSettings = endlessSettings;
            _levelsToUnlockEndless = Mathf.Max(0, levelsToUnlockEndless);
        }
    }
}
