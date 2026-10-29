using System;
using System.Collections.Generic;
using FateBastion.Core;

namespace FateBastion.Meta
{
    /// <summary>
    /// Gacha pulls on a SaveData in memory (S8). A x10 is computed entirely in memory first (pity, Epic+ guarantee,
    /// first-own bonus) and only then applied to the data in one step, so a refused or replaced pull never leaves
    /// half-applied changes. The caller saves afterwards (see MetaSession).
    /// </summary>
    public sealed class GachaService
    {
        /// <summary>Pulls in a x10 (S8).</summary>
        public const int TenPullCount = 10;

        private readonly GachaTable _table;
        private readonly Random _rng;

        /// <param name="rng">Injected so tests can fix the seed; never UnityEngine.Random in Meta.</param>
        public GachaService(GachaTable table, Random rng)
        {
            _table = table != null ? table : throw new ArgumentNullException(nameof(table));
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));

            for (int i = 0; i < GachaTable.RarityCount; i++)
            {
                if (_table.HeroesOf((Rarity)i).Count == 0)
                {
                    throw new ArgumentException($"GachaTable has no hero of rarity {(Rarity)i}. Run Tools > Meta > Build Gacha Table.",
                        nameof(table));
                }
            }
        }

        public int CostOf(int pulls) => pulls == TenPullCount ? _table.TenCost : _table.SingleCost * pulls;

        public bool CanAfford(SaveData data, int pulls) => data != null && data.gems >= CostOf(pulls);

        public GachaPullResult PullOne(SaveData data) => Pull(data, 1);

        public GachaPullResult PullTen(SaveData data) => Pull(data, TenPullCount);

        private GachaPullResult Pull(SaveData data, int count)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            SaveDataGuard.EnsureCollections(data);
            int cost = CostOf(count);
            if (data.gems < cost)
            {
                return GachaPullResult.Failed(GachaError.NotEnoughGems, cost);
            }

            // 1. Compute every roll on copies of the state.
            var owned = new HashSet<string>(data.ownedEver ?? new List<string>());
            var rolls = new GachaRoll[count];
            int pity = data.pityCounter;
            int pityBeforeLast = pity;
            for (int i = 0; i < count; i++)
            {
                if (i == count - 1)
                {
                    pityBeforeLast = pity;
                }

                rolls[i] = Roll(ref pity, owned, false);
            }

            // 2. x10 without Epic+: undo the last roll (pity, first-own) and re-roll it among Epic + Legendary.
            if (count == TenPullCount && !HasEpicOrBetter(rolls))
            {
                GachaRoll last = rolls[count - 1];
                if (last.IsNew)
                {
                    owned.Remove(last.HeroId);
                }

                pity = pityBeforeLast;
                rolls[count - 1] = Roll(ref pity, owned, true).AsGuaranteed();
            }

            // 3. Apply once.
            data.gems -= cost;
            int awarded = 0;
            for (int i = 0; i < rolls.Length; i++)
            {
                GachaRoll roll = rolls[i];
                SaveDataGuard.AddCopy(data, roll.HeroId);
                if (roll.IsNew)
                {
                    data.ownedEver.Add(roll.HeroId);
                    awarded += _table.FirstOwnGems;
                }
            }

            data.gems += awarded;
            data.pityCounter = pity;
            return GachaPullResult.Succeeded(rolls, cost, awarded);
        }

        private GachaRoll Roll(ref int pity, HashSet<string> owned, bool epicOrBetterOnly)
        {
            Rarity rarity;
            bool fromPity = false;
            if (_table.PityThreshold > 0 && pity >= _table.PityThreshold - 1)
            {
                rarity = Rarity.Legendary;
                fromPity = true;
            }
            else
            {
                rarity = RollRarity(epicOrBetterOnly);
            }

            pity = rarity == Rarity.Legendary ? 0 : pity + 1;

            IReadOnlyList<string> pool = _table.HeroesOf(rarity);
            string heroId = pool[_rng.Next(pool.Count)];
            bool isNew = owned.Add(heroId);
            return new GachaRoll(heroId, rarity, isNew, fromPity, false);
        }

        /// <summary>Weighted rarity; with <paramref name="epicOrBetterOnly"/> the Epic and Legendary weights are renormalised.</summary>
        private Rarity RollRarity(bool epicOrBetterOnly)
        {
            int first = epicOrBetterOnly ? (int)Rarity.Epic : (int)Rarity.Common;
            double total = 0d;
            for (int i = first; i < GachaTable.RarityCount; i++)
            {
                total += _table.WeightOf((Rarity)i);
            }

            if (total <= 0d)
            {
                return (Rarity)first;
            }

            double pick = _rng.NextDouble() * total;
            for (int i = first; i < GachaTable.RarityCount; i++)
            {
                pick -= _table.WeightOf((Rarity)i);
                if (pick < 0d)
                {
                    return (Rarity)i;
                }
            }

            return Rarity.Legendary; // Floating-point rest lands on the last bucket.
        }

        private static bool HasEpicOrBetter(GachaRoll[] rolls)
        {
            for (int i = 0; i < rolls.Length; i++)
            {
                if (rolls[i].IsEpicOrBetter)
                {
                    return true;
                }
            }

            return false;
        }

    }
}
