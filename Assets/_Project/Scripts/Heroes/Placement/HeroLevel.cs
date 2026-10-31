using UnityEngine;

namespace FateBastion.Heroes
{
    /// <summary>
    /// Level 1..3 of a hero on the field (S4, B-04). On every level change it reads <see cref="HeroLevelTable"/> and calls
    /// <see cref="HeroStats.SetLevelMultipliers"/> (TEAM_ASSIGNMENT 3.1 #4); at the top level it turns on a light aura under
    /// <see cref="HeroVisual.AuraAnchor"/>. Upgrade and sell prices live in <see cref="PlacedHero"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HeroLevel : MonoBehaviour
    {
        [Tooltip("Optional FX shown at the top level (C). Created once with the pool, then only toggled.")]
        [SerializeField] private GameObject _topLevelAuraPrefab;

        private readonly PlacedHero _state = new PlacedHero();
        private HeroLevelTable _table;
        private HeroController _controller;
        private HeroStats _stats;
        private GameObject _aura;

        public PlacedHero State => _state;
        public int Level => _state.Level;
        public int GoldSpent => _state.GoldSpent;
        public int DeckSlot => _state.DeckSlot;
        public bool IsPlaced => _state.IsPlaced;
        public HeroController Controller => _controller;
        public HeroData Data => _controller != null ? _controller.Data : null;
        public bool IsTopLevel => _table != null && _state.Level >= _table.MaxLevel;

        private void Awake()
        {
            CacheComponents();
        }

        /// <summary>
        /// Called once by <see cref="HeroPool"/> when the instance is created (load time, never in a match).
        /// <paramref name="topLevelAuraPrefab"/> overrides the field when not null.
        /// </summary>
        public void Setup(HeroLevelTable table, GameObject topLevelAuraPrefab)
        {
            CacheComponents();
            _table = table;
            if (topLevelAuraPrefab != null) _topLevelAuraPrefab = topLevelAuraPrefab;

            if (_aura == null && _topLevelAuraPrefab != null)
            {
                HeroVisual visual = GetComponentInChildren<HeroVisual>(true);
                Transform anchor = visual != null && visual.AuraAnchor != null ? visual.AuraAnchor : transform;
                _aura = Instantiate(_topLevelAuraPrefab, anchor, false);
                _aura.SetActive(false);
            }
        }

        /// <summary>The hero was just taken from the pool and initialized: level 1, <paramref name="placeCost"/> spent.</summary>
        public void BeginPlaced(int deckSlot, int placeCost)
        {
            HeroData data = Data;
            if (data == null)
            {
                Debug.LogError($"{name}: HeroLevel.BeginPlaced before HeroController.Init.", this);
                return;
            }

            _state.Begin(data.id, data.rarity, deckSlot, placeCost);
            ApplyLevel();
        }

        /// <summary>Pays and applies the next level; nothing changes on failure.</summary>
        public UpgradeError TryUpgrade(PlacementRules rules, Wallet wallet, out int cost)
        {
            if (_table == null)
            {
                cost = 0;
                Debug.LogError($"{name}: HeroLevel has no HeroLevelTable; HeroPool.Prewarm sets it.", this);
                return UpgradeError.MaxLevel;
            }

            UpgradeError error = _state.TryUpgrade(rules, _table, wallet, out cost);
            if (error == UpgradeError.None) ApplyLevel();
            return error;
        }

        public int NextUpgradeCost(PlacementRules rules) => _table != null ? _state.NextUpgradeCost(rules, _table) : -1;

        /// <summary>Before the hero returns to its pool.</summary>
        public void Clear()
        {
            _state.Clear();
            SetAura(false);
        }

        private void ApplyLevel()
        {
            if (_table == null || _stats == null) return;

            LevelMultipliers multipliers = _table.MultipliersOf(_state.Level);
            _stats.SetLevelMultipliers(multipliers.Damage, multipliers.AttackSpeed, multipliers.Range);
            SetAura(IsTopLevel);
        }

        private void SetAura(bool on)
        {
            if (_aura != null && _aura.activeSelf != on) _aura.SetActive(on);
        }

        private void CacheComponents()
        {
            if (_controller == null) _controller = GetComponent<HeroController>();
            if (_stats == null) _stats = GetComponent<HeroStats>();
        }
    }
}
