using System.Collections.Generic;
using FateBastion.Enemies;
using FateBastion.Meta;

namespace FateBastion.Game
{
    /// <summary>
    /// Stars and unlocks for the level select screen (S9 levelProgress, GDD 9): level 1 is always open, level n opens
    /// once level n-1 has a star, Endless opens after the first <see cref="MatchCatalog.LevelsToUnlockEndless"/> levels.
    /// The save key of a level is its asset name (level_1…), the same key MatchRewards writes.
    /// </summary>
    public static class LevelProgress
    {
        public static string KeyOf(LevelData level) => level != null ? level.name : null;

        public static int StarsOf(SaveData data, LevelData level)
        {
            string key = KeyOf(level);
            if (data?.levelProgress == null || key == null) return 0;
            return data.levelProgress.TryGetValue(key, out int stars) ? stars : 0;
        }

        /// <summary>Pure rule: <paramref name="starsByLevel"/>[i] = best stars of level i.</summary>
        public static bool IsLevelUnlocked(int index, IReadOnlyList<int> starsByLevel)
        {
            if (index <= 0) return index == 0;
            return starsByLevel != null && index - 1 < starsByLevel.Count && starsByLevel[index - 1] > 0;
        }

        /// <summary>Pure rule: the first <paramref name="levelsNeeded"/> levels all have a star.</summary>
        public static bool IsEndlessUnlocked(int levelsNeeded, IReadOnlyList<int> starsByLevel)
        {
            if (levelsNeeded <= 0) return true;
            if (starsByLevel == null || starsByLevel.Count < levelsNeeded) return false;
            for (int i = 0; i < levelsNeeded; i++)
            {
                if (starsByLevel[i] <= 0) return false;
            }

            return true;
        }

        public static int[] StarsByLevel(SaveData data, MatchCatalog catalog)
        {
            var stars = new int[catalog.LevelCount];
            for (int i = 0; i < stars.Length; i++) stars[i] = StarsOf(data, catalog.LevelAt(i));
            return stars;
        }
    }
}
