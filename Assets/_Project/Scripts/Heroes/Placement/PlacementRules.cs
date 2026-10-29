using UnityEngine;
using FateBastion.Core;

namespace FateBastion.Heroes
{
    /// <summary>
    /// In-match placement economy (S4): place cost and max copies on the field per rarity, army cap, occupancy
    /// radius and sell refund rate. Arrays are indexed by Rarity (Common, Rare, Epic, Legendary).
    /// </summary>
    [CreateAssetMenu(menuName = "Fate Bastion/Settings/Placement Rules", fileName = "PlacementRules")]
    public class PlacementRules : ScriptableObject
    {
        private const int RarityCount = (int)Rarity.Legendary + 1;

        [Tooltip("Gold to place one hero, by rarity: Common, Rare, Epic, Legendary.")]
        [SerializeField] private int[] _placeCost = { 100, 200, 350, 600 };

        [Tooltip("Max copies of one hero on the field, by rarity: Common, Rare, Epic, Legendary.")]
        [SerializeField] private int[] _maxPerHero = { 6, 5, 4, 2 };

        [Tooltip("Max heroes on the field in total.")]
        [SerializeField, Min(1)] private int _maxHeroes = 15;

        [Tooltip("Radius (m) checked around the drop point for heroes, path and obstacles.")]
        [SerializeField, Min(0f)] private float _occupancyRadius = 0.8f;

        [Tooltip("Share of goldSpent refunded on sell (rounded down).")]
        [SerializeField, Range(0f, 1f)] private float _refundRate = 0.5f;

        public int MaxHeroes => _maxHeroes;
        public float OccupancyRadius => _occupancyRadius;
        public float RefundRate => _refundRate;

        public int PlaceCostOf(Rarity rarity) => _placeCost[(int)rarity];

        public int MaxPerHeroOf(Rarity rarity) => _maxPerHero[(int)rarity];

        /// <summary>For tests and tools; in the game the values come from the asset.</summary>
        public void Configure(int[] placeCost, int[] maxPerHero, int maxHeroes, float occupancyRadius, float refundRate)
        {
            _placeCost = (int[])placeCost.Clone();
            _maxPerHero = (int[])maxPerHero.Clone();
            _maxHeroes = Mathf.Max(1, maxHeroes);
            _occupancyRadius = Mathf.Max(0f, occupancyRadius);
            _refundRate = Mathf.Clamp01(refundRate);
            OnValidate();
        }

        private void OnValidate()
        {
            if (_placeCost == null || _placeCost.Length != RarityCount)
            {
                System.Array.Resize(ref _placeCost, RarityCount);
                Debug.LogError($"{name}: place cost needs exactly {RarityCount} values (Common..Legendary).", this);
            }

            if (_maxPerHero == null || _maxPerHero.Length != RarityCount)
            {
                System.Array.Resize(ref _maxPerHero, RarityCount);
                Debug.LogError($"{name}: max per hero needs exactly {RarityCount} values (Common..Legendary).", this);
            }
        }
    }
}
