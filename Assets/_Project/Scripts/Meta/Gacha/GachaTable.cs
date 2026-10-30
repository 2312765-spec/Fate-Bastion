using System;
using System.Collections.Generic;
using UnityEngine;
using FateBastion.Core;

namespace FateBastion.Meta
{
    /// <summary>
    /// Gacha numbers (S8): rarity weights, pull prices, pity, first-own bonus, merge cost and the hero ids of each
    /// rarity. Hero ids (not HeroData) because Meta may only reference Core; Tools > Meta > Build Gacha Table fills
    /// the list from every HeroData.
    /// </summary>
    [CreateAssetMenu(menuName = "Fate Bastion/Settings/Gacha Table", fileName = "GachaTable")]
    public class GachaTable : ScriptableObject
    {
        /// <summary>Number of Rarity values (Common..Legendary).</summary>
        public const int RarityCount = (int)Rarity.Legendary + 1;

        /// <summary>One hero of the pool and its rarity.</summary>
        [Serializable]
        public struct HeroEntry
        {
            public string heroId;
            public Rarity rarity;

            public HeroEntry(string heroId, Rarity rarity)
            {
                this.heroId = heroId;
                this.rarity = rarity;
            }
        }

        [Header("Rarity weights (relative, the spec uses %)")]
        [SerializeField, Min(0f)] private float _commonWeight = 65f;
        [SerializeField, Min(0f)] private float _rareWeight = 25f;
        [SerializeField, Min(0f)] private float _epicWeight = 8.5f;
        [SerializeField, Min(0f)] private float _legendaryWeight = 1.5f;

        [Header("Prices (gems)")]
        [SerializeField, Min(0)] private int _singleCost = 100;
        [SerializeField, Min(0)] private int _tenCost = 900;

        [Header("Pity")]
        [Tooltip("This pull number is always Legendary (25 = the 25th pull without Legendary). 0 disables pity.")]
        [SerializeField, Min(0)] private int _pityThreshold = 25;

        [Header("Rewards and merge")]
        [Tooltip("Gems for owning a hero for the first time.")]
        [SerializeField, Min(0)] private int _firstOwnGems = 50;

        [Tooltip("Spare copies of one rarity needed for one merge.")]
        [SerializeField, Min(1)] private int _mergeCost = 3;

        [Header("Heroes (filled by Tools > Meta > Build Gacha Table)")]
        [SerializeField] private List<HeroEntry> _heroes = new List<HeroEntry>();

        [NonSerialized] private List<string>[] _byRarity;
        [NonSerialized] private Dictionary<string, Rarity> _rarityById;

        public int SingleCost => _singleCost;
        public int TenCost => _tenCost;
        public int PityThreshold => _pityThreshold;
        public int FirstOwnGems => _firstOwnGems;
        public int MergeCost => _mergeCost;
        public IReadOnlyList<HeroEntry> Heroes => _heroes;

        public float WeightOf(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Common: return _commonWeight;
                case Rarity.Rare: return _rareWeight;
                case Rarity.Epic: return _epicWeight;
                default: return _legendaryWeight;
            }
        }

        /// <summary>Hero ids of <paramref name="rarity"/>, in table order (never null).</summary>
        public IReadOnlyList<string> HeroesOf(Rarity rarity)
        {
            EnsureCache();
            return _byRarity[(int)rarity];
        }

        public bool TryGetRarity(string heroId, out Rarity rarity)
        {
            EnsureCache();
            if (heroId != null && _rarityById.TryGetValue(heroId, out rarity))
            {
                return true;
            }

            rarity = default;
            return false;
        }

        /// <summary>For tests and tools; in the game the values come from the asset.</summary>
        public void Configure(float common, float rare, float epic, float legendary, int singleCost, int tenCost,
            int pityThreshold, int firstOwnGems, int mergeCost)
        {
            _commonWeight = Mathf.Max(0f, common);
            _rareWeight = Mathf.Max(0f, rare);
            _epicWeight = Mathf.Max(0f, epic);
            _legendaryWeight = Mathf.Max(0f, legendary);
            _singleCost = Mathf.Max(0, singleCost);
            _tenCost = Mathf.Max(0, tenCost);
            _pityThreshold = Mathf.Max(0, pityThreshold);
            _firstOwnGems = Mathf.Max(0, firstOwnGems);
            _mergeCost = Mathf.Max(1, mergeCost);
        }

        /// <summary>Replaces the hero pool (Build Gacha Table tool, tests).</summary>
        public void SetHeroes(IEnumerable<HeroEntry> heroes)
        {
            _heroes = heroes != null ? new List<HeroEntry>(heroes) : new List<HeroEntry>();
            _byRarity = null;
        }

        private void OnValidate()
        {
            _byRarity = null;
        }

        private void EnsureCache()
        {
            if (_byRarity != null)
            {
                return;
            }

            _byRarity = new List<string>[RarityCount];
            for (int i = 0; i < RarityCount; i++)
            {
                _byRarity[i] = new List<string>();
            }

            _rarityById = new Dictionary<string, Rarity>();
            for (int i = 0; i < _heroes.Count; i++)
            {
                HeroEntry entry = _heroes[i];
                if (string.IsNullOrEmpty(entry.heroId) || _rarityById.ContainsKey(entry.heroId))
                {
                    continue;
                }

                _rarityById.Add(entry.heroId, entry.rarity);
                _byRarity[(int)entry.rarity].Add(entry.heroId);
            }
        }
    }
}
