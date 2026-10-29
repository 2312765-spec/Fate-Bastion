using System;
using FateBastion.Enemies;
using FateBastion.Meta;

namespace FateBastion.Game
{
    /// <summary>
    /// Gems after a match (S5, B-07). Campaign: first clear +firstClearGems, then +gemsPerNewStar for each star
    /// above the best already saved, so a replay only pays the stars it improves. Endless: +GemsPerStep for every
    /// WaveStep waves reached, and the record is updated. The caller saves afterwards (S9).
    /// </summary>
    public static class MatchRewards
    {
        /// <summary>Gems for finishing a level with <paramref name="newStars"/> when the best so far was <paramref name="previousStars"/>.</summary>
        public static int CampaignGems(int previousStars, int newStars, int firstClearGems, int gemsPerNewStar)
        {
            if (newStars <= 0)
            {
                return 0; // Lost: no reward.
            }

            int gems = previousStars <= 0 ? firstClearGems : 0;
            return gems + Math.Max(0, newStars - Math.Max(0, previousStars)) * gemsPerNewStar;
        }

        /// <summary>
        /// Applies a campaign result to the save: adds the gems, keeps the best stars of <paramref name="levelKey"/>.
        /// Returns the gems awarded (for MatchResult and the result screen).
        /// </summary>
        public static int ApplyCampaign(SaveData data, string levelKey, int stars, int firstClearGems, int gemsPerNewStar)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (string.IsNullOrEmpty(levelKey)) throw new ArgumentException("levelKey is required", nameof(levelKey));

            stars = Math.Min(StarRating.MaxStars, Math.Max(0, stars));
            if (data.levelProgress == null) data.levelProgress = new System.Collections.Generic.Dictionary<string, int>();
            data.levelProgress.TryGetValue(levelKey, out int previous);

            int gems = CampaignGems(previous, stars, firstClearGems, gemsPerNewStar);
            data.gems += gems;
            if (stars > previous)
            {
                data.levelProgress[levelKey] = stars;
            }

            return gems;
        }

        /// <summary>Same as above with the numbers of <paramref name="level"/>; the key is the LevelData asset name (level_1…).</summary>
        public static int ApplyCampaign(SaveData data, LevelData level, int stars) =>
            ApplyCampaign(data, level.name, stars, level.firstClearGems, level.gemsPerNewStar);

        // ASSUMPTION: S5 says "Vô tận +10 mỗi 5 wave" without limiting it to waves beyond the record, so every run
        // pays for all the waves it reached; only the record itself is kept as the best.
        public static int EndlessGems(int wavesReached, int gemsPerStep, int waveStep) =>
            waveStep <= 0 || wavesReached <= 0 ? 0 : wavesReached / waveStep * gemsPerStep;

        /// <summary>Adds the Endless gems and updates endlessBest. Returns the gems awarded.</summary>
        public static int ApplyEndless(SaveData data, int wavesReached, EndlessRewardSettings settings)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            int gems = EndlessGems(wavesReached, settings.GemsPerStep, settings.WaveStep);
            data.gems += gems;
            data.endlessBest = Math.Max(data.endlessBest, wavesReached);
            return gems;
        }
    }
}
