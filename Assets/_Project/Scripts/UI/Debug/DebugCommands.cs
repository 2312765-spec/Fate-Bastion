#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using FateBastion.Meta;

namespace FateBastion.UI.DebugTools
{
    /// <summary>Save edits of the F1 debug panel (S9, B-16), pure so they are unit-tested. Never in a Release build.</summary>
    public static class DebugCommands
    {
        public static void AddGems(SaveData data, int amount)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            data.gems = Math.Max(0, data.gems + amount);
        }

        /// <summary>One more copy of <paramref name="heroId"/>; marks it owned (no first-own gems: it is a debug gift).</summary>
        public static void GrantHero(SaveData data, string heroId)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (string.IsNullOrEmpty(heroId)) throw new ArgumentException("heroId is required", nameof(heroId));

            if (data.collection == null) data.collection = new Dictionary<string, int>();
            if (data.ownedEver == null) data.ownedEver = new List<string>();

            data.collection.TryGetValue(heroId, out int copies);
            data.collection[heroId] = copies + 1;
            if (!data.ownedEver.Contains(heroId)) data.ownedEver.Add(heroId);
        }
    }
}
#endif
