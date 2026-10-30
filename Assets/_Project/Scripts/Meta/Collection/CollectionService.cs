using System;
using System.Collections.Generic;
using FateBastion.Core;

namespace FateBastion.Meta
{
    /// <summary>
    /// The bag (S8): spare copies and merging. A spare copy is any copy beyond the first; merging spends MergeCost
    /// spares of one rarity (from one or several heroes) for one random hero of the next rarity. Every hero keeps at
    /// least one copy, so a merge never touches the deck. Legendary cannot be merged.
    /// </summary>
    public sealed class CollectionService
    {
        private readonly GachaTable _table;
        private readonly Random _rng;

        public CollectionService(GachaTable table, Random rng)
        {
            _table = table != null ? table : throw new ArgumentNullException(nameof(table));
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
        }

        public int MergeCost => _table.MergeCost;

        /// <summary>Copies beyond the first one; 0 for a hero not owned.</summary>
        public static int SpareOf(SaveData data, string heroId) => Math.Max(0, SaveDataGuard.CopiesOf(data, heroId) - 1);

        public static int CopiesOf(SaveData data, string heroId) => SaveDataGuard.CopiesOf(data, heroId);

        /// <summary>Total spare copies of every owned hero of <paramref name="rarity"/>.</summary>
        public int SpareCopies(SaveData data, Rarity rarity)
        {
            if (data?.collection == null)
            {
                return 0;
            }

            int total = 0;
            foreach (KeyValuePair<string, int> pair in data.collection)
            {
                if (_table.TryGetRarity(pair.Key, out Rarity heroRarity) && heroRarity == rarity)
                {
                    total += Math.Max(0, pair.Value - 1);
                }
            }

            return total;
        }

        /// <summary>Spares still needed for one merge of <paramref name="rarity"/> (for the "còn thiếu" label).</summary>
        public int MissingFor(SaveData data, Rarity rarity) => Math.Max(0, MergeCost - SpareCopies(data, rarity));

        public bool CanMerge(SaveData data, Rarity rarity) => rarity != Rarity.Legendary && MissingFor(data, rarity) == 0;

        /// <summary>
        /// "Tự chọn": takes MergeCost spares starting with the hero that has the most spares (ties by id), using all
        /// of its spares before moving to the next one. Empty when there are not enough spares.
        /// </summary>
        public List<string> AutoSelect(SaveData data, Rarity rarity)
        {
            var selection = new List<string>(MergeCost);
            if (!CanMerge(data, rarity))
            {
                return selection;
            }

            var candidates = new List<KeyValuePair<string, int>>();
            foreach (KeyValuePair<string, int> pair in data.collection)
            {
                if (pair.Value > 1 && _table.TryGetRarity(pair.Key, out Rarity heroRarity) && heroRarity == rarity)
                {
                    candidates.Add(new KeyValuePair<string, int>(pair.Key, pair.Value - 1));
                }
            }

            candidates.Sort((a, b) => a.Value != b.Value ? b.Value.CompareTo(a.Value) : string.CompareOrdinal(a.Key, b.Key));
            for (int i = 0; i < candidates.Count && selection.Count < MergeCost; i++)
            {
                for (int copy = 0; copy < candidates[i].Value && selection.Count < MergeCost; copy++)
                {
                    selection.Add(candidates[i].Key);
                }
            }

            return selection;
        }

        public MergeResult MergeAuto(SaveData data, Rarity rarity)
        {
            if (rarity == Rarity.Legendary)
            {
                return MergeResult.Failed(MergeError.LegendaryCannotMerge);
            }

            int missing = MissingFor(data, rarity);
            return missing > 0 ? MergeResult.Failed(MergeError.NotEnoughSpare, missing) : Merge(data, rarity, AutoSelect(data, rarity));
        }

        /// <summary>
        /// Merges the selected spare copies (one entry per copy, a hero may appear several times). Validates
        /// everything before changing the data.
        /// </summary>
        public MergeResult Merge(SaveData data, Rarity rarity, IReadOnlyList<string> selection)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            SaveDataGuard.EnsureCollections(data);
            if (rarity == Rarity.Legendary)
            {
                return MergeResult.Failed(MergeError.LegendaryCannotMerge);
            }

            int missing = MissingFor(data, rarity);
            if (missing > 0)
            {
                return MergeResult.Failed(MergeError.NotEnoughSpare, missing);
            }

            if (selection == null || selection.Count != MergeCost)
            {
                return MergeResult.Failed(MergeError.WrongSelectionSize);
            }

            var taken = new Dictionary<string, int>();
            for (int i = 0; i < selection.Count; i++)
            {
                string heroId = selection[i];
                if (!_table.TryGetRarity(heroId, out Rarity heroRarity) || heroRarity != rarity)
                {
                    return MergeResult.Failed(MergeError.WrongRarity);
                }

                taken.TryGetValue(heroId, out int count);
                taken[heroId] = count + 1;
                if (taken[heroId] > SpareOf(data, heroId))
                {
                    return MergeResult.Failed(MergeError.HeroHasNoSpare);
                }
            }

            foreach (KeyValuePair<string, int> pair in taken)
            {
                data.collection[pair.Key] -= pair.Value; // Stays >= 1: only spares were taken.
            }

            Rarity next = rarity + 1;
            IReadOnlyList<string> pool = _table.HeroesOf(next);
            if (pool.Count == 0)
            {
                throw new InvalidOperationException($"GachaTable has no hero of rarity {next}.");
            }

            string result = pool[_rng.Next(pool.Count)];
            SaveDataGuard.AddCopy(data, result);

            // ASSUMPTION: the +50 "first time owned" reward (GDD, CLAUDE.md §9) also applies to a hero obtained by
            // merging, not only by gacha; S8 only describes it for pulls.
            bool isNew = !data.ownedEver.Contains(result);
            int gems = 0;
            if (isNew)
            {
                data.ownedEver.Add(result);
                gems = _table.FirstOwnGems;
                data.gems += gems;
            }

            return MergeResult.Succeeded(result, next, isNew, gems);
        }
    }
}
