using System;
using System.Collections.Generic;

namespace FateBastion.Meta
{
    /// <summary>
    /// Fixes a loaded SaveData that no longer matches the game (S9 "Trường hợp đặc biệt"): drops hero ids that do
    /// not exist any more and repairs the deck. Pure and roster-driven, so S8 (B) can call it after Load with the
    /// real HeroData list and show the warnings to the player.
    /// </summary>
    public static class SaveSanitizer
    {
        /// <summary>Deck size required by S8.</summary>
        public const int DeckSize = 5;

        /// <summary>
        /// Cleans collection / ownedEver / deck in place. Returns human-readable warnings (empty when nothing changed).
        /// When <paramref name="validHeroIds"/> is empty nothing is validated, so the caller must pass the real roster.
        /// </summary>
        public static List<string> Sanitize(SaveData data, IReadOnlyCollection<string> validHeroIds,
            IReadOnlyList<string> starterDeck = null)
        {
            var warnings = new List<string>();
            if (data == null || validHeroIds == null || validHeroIds.Count == 0) return warnings;

            var valid = new HashSet<string>(validHeroIds);

            RemoveUnknown(data.collection, valid, warnings);
            RemoveZeroCopies(data.collection, warnings);
            RemoveUnknownList(data.ownedEver, valid, warnings);
            RepairDeck(data, valid, starterDeck, warnings);
            return warnings;
        }

        private static void RemoveUnknown(Dictionary<string, int> collection, HashSet<string> valid,
            List<string> warnings)
        {
            if (collection == null) return;

            // Copy the keys first: removing while enumerating a Dictionary is not allowed.
            var toRemove = new List<string>();
            foreach (KeyValuePair<string, int> pair in collection)
                if (!valid.Contains(pair.Key)) toRemove.Add(pair.Key);

            for (int i = 0; i < toRemove.Count; i++)
            {
                collection.Remove(toRemove[i]);
                warnings.Add($"Bỏ qua tướng không còn tồn tại trong game: {toRemove[i]}.");
            }
        }

        private static void RemoveZeroCopies(Dictionary<string, int> collection, List<string> warnings)
        {
            if (collection == null) return;

            var toRemove = new List<string>();
            foreach (KeyValuePair<string, int> pair in collection)
                if (pair.Value <= 0) toRemove.Add(pair.Key);

            for (int i = 0; i < toRemove.Count; i++)
            {
                collection.Remove(toRemove[i]);
                warnings.Add($"Bỏ qua tướng không còn bản nào: {toRemove[i]}.");
            }
        }

        private static void RemoveUnknownList(List<string> ids, HashSet<string> valid, List<string> warnings)
        {
            if (ids == null) return;

            for (int i = ids.Count - 1; i >= 0; i--)
            {
                if (string.IsNullOrEmpty(ids[i]) || !valid.Contains(ids[i]))
                {
                    warnings.Add($"Bỏ qua tướng không còn tồn tại trong game: {ids[i]}.");
                    ids.RemoveAt(i);
                }
            }
        }

        private static void RepairDeck(SaveData data, HashSet<string> valid, IReadOnlyList<string> starterDeck,
            List<string> warnings)
        {
            var owned = new HashSet<string>();
            if (data.collection != null)
                foreach (KeyValuePair<string, int> pair in data.collection)
                    if (pair.Value > 0) owned.Add(pair.Key);

            var deck = new List<string>(DeckSize);
            var seen = new HashSet<string>();
            bool changed = false;

            // Keep the existing entries that are still valid and owned, without duplicates.
            if (data.deck != null)
            {
                foreach (string id in data.deck)
                {
                    if (string.IsNullOrEmpty(id)) { changed = true; continue; }
                    if (!valid.Contains(id) || !owned.Contains(id) || !seen.Add(id))
                    {
                        changed = true;
                        warnings.Add($"Deck chứa tướng không hợp lệ: {id}.");
                        continue;
                    }
                    if (deck.Count < DeckSize) deck.Add(id);
                }
            }

            // Fill the free slots from the starter deck, then from every owned hero (sorted for determinism).
            FillFrom(deck, seen, starterDeck, valid, owned, ref changed);
            FillFrom(deck, seen, SortedOwned(owned), valid, owned, ref changed);

            if (changed || data.deck == null || data.deck.Count != deck.Count) data.deck = deck;
            if (deck.Count < DeckSize)
                warnings.Add($"Deck chỉ có {deck.Count}/{DeckSize} tướng hợp lệ; cần kiểm tra lại deck.");
        }

        private static void FillFrom(List<string> deck, HashSet<string> seen, IReadOnlyList<string> source,
            HashSet<string> valid, HashSet<string> owned, ref bool changed)
        {
            if (source == null) return;
            for (int i = 0; i < source.Count && deck.Count < DeckSize; i++)
            {
                string id = source[i];
                if (string.IsNullOrEmpty(id) || !valid.Contains(id) || !owned.Contains(id) || !seen.Add(id)) continue;
                deck.Add(id);
                changed = true;
            }
        }

        private static List<string> SortedOwned(HashSet<string> owned)
        {
            var list = new List<string>(owned);
            list.Sort(StringComparer.Ordinal);
            return list;
        }
    }
}
