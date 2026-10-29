using System.Collections.Generic;

namespace FateBastion.Meta
{
    /// <summary>Small helpers shared by the S8 services for the collections inside SaveData.</summary>
    internal static class SaveDataGuard
    {
        /// <summary>A save written by hand or by an older build may have null lists; S8 code never trusts them.</summary>
        public static void EnsureCollections(SaveData data)
        {
            if (data.collection == null) data.collection = new Dictionary<string, int>();
            if (data.ownedEver == null) data.ownedEver = new List<string>();
            if (data.deck == null) data.deck = new List<string>();
        }

        public static int CopiesOf(SaveData data, string heroId)
        {
            return heroId != null && data.collection != null && data.collection.TryGetValue(heroId, out int copies) ? copies : 0;
        }

        public static void AddCopy(SaveData data, string heroId)
        {
            data.collection.TryGetValue(heroId, out int copies);
            data.collection[heroId] = copies + 1;
        }
    }
}
