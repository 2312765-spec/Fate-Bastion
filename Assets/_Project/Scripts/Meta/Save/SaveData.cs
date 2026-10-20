using System.Collections.Generic;
using Newtonsoft.Json;

namespace FateBastion.Meta
{
    /// <summary>
    /// Every persistent out-of-match value (S9): gems, gacha pity, hero copies, deck, unlocked heroes,
    /// level stars, endless best, tutorial flag and settings. Kept as plain data so other services (S8)
    /// work on it in memory and only call SaveService to persist it.
    /// </summary>
    public class SaveData
    {
        /// <summary>Schema version; the one-month build only ever writes 1 (no SaveMigrator yet).</summary>
        public const int CurrentVersion = 1;

        [JsonProperty("version")] public int version = CurrentVersion;

        [JsonProperty("gems")] public int gems;

        [JsonProperty("pityCounter")] public int pityCounter;

        /// <summary>heroId to number of copies owned; never below 1 for an owned hero.</summary>
        [JsonProperty("collection")] public Dictionary<string, int> collection = new Dictionary<string, int>();

        /// <summary>The five heroIds of the battle deck.</summary>
        [JsonProperty("deck")] public List<string> deck = new List<string>();

        /// <summary>Heroes owned at least once, for the one-time +50 gems reward.</summary>
        [JsonProperty("ownedEver")] public List<string> ownedEver = new List<string>();

        /// <summary>levelId to stars earned (0..3).</summary>
        [JsonProperty("levelProgress")] public Dictionary<string, int> levelProgress = new Dictionary<string, int>();

        [JsonProperty("endlessBest")] public int endlessBest;

        [JsonProperty("tutorialDone")] public bool tutorialDone;

        [JsonProperty("settings")] public SaveSettings settings = new SaveSettings();
    }
}
