using System;
using FateBastion.Core;

namespace FateBastion.Heroes
{
    /// <summary>Why an upgrade did not happen (S4).</summary>
    public enum UpgradeError
    {
        None,
        NotPlaced,
        MaxLevel,
        NotEnoughGold
    }

    /// <summary>
    /// In-match state of one hero on the field (S4 "PlacedHero { heroData, level, goldSpent }"): level 1..max and the
    /// gold put into it, which sets the sell refund. Pure so upgrade and sell are unit-tested; HeroLevel (B-04) wraps it.
    /// </summary>
    public sealed class PlacedHero
    {
        public string HeroId { get; private set; }
        public Rarity Rarity { get; private set; }

        /// <summary>Deck slot (0..4) the hero was placed from, so selling gives the copy back to that slot.</summary>
        public int DeckSlot { get; private set; } = -1;

        /// <summary>1..max while on the field, 0 when not placed.</summary>
        public int Level { get; private set; }

        public int GoldSpent { get; private set; }

        public bool IsPlaced => Level > 0;

        /// <summary>The hero was just placed at level 1 for <paramref name="placeCost"/> gold.</summary>
        public void Begin(string heroId, Rarity rarity, int deckSlot, int placeCost)
        {
            if (placeCost < 0) throw new ArgumentOutOfRangeException(nameof(placeCost), placeCost, "Cost cannot be negative.");

            HeroId = heroId;
            Rarity = rarity;
            DeckSlot = deckSlot;
            Level = 1;
            GoldSpent = placeCost;
        }

        /// <summary>Back to the pool: not placed any more.</summary>
        public void Clear()
        {
            HeroId = null;
            DeckSlot = -1;
            Level = 0;
            GoldSpent = 0;
        }

        public bool CanUpgrade(HeroLevelTable table) => IsPlaced && HeroEconomy.CanUpgrade(table, Level);

        /// <summary>Price of the next level, or -1 at max level (the HUD hides the button).</summary>
        public int NextUpgradeCost(PlacementRules rules, HeroLevelTable table) =>
            CanUpgrade(table) ? HeroEconomy.UpgradeCost(rules, table, Rarity, Level) : -1;

        /// <summary>
        /// Pays the next level from <paramref name="wallet"/> and raises the level (S4 step 5). Nothing changes on failure;
        /// the max-level check comes before the gold check so a full hero never reports "not enough gold".
        /// </summary>
        public UpgradeError TryUpgrade(PlacementRules rules, HeroLevelTable table, Wallet wallet, out int cost)
        {
            cost = 0;
            if (!IsPlaced) return UpgradeError.NotPlaced;
            if (!HeroEconomy.CanUpgrade(table, Level)) return UpgradeError.MaxLevel;

            cost = HeroEconomy.UpgradeCost(rules, table, Rarity, Level);
            if (!wallet.TrySpend(cost)) return UpgradeError.NotEnoughGold;

            Level++;
            GoldSpent += cost;
            return UpgradeError.None;
        }

        /// <summary>Gold returned when sold: floor(goldSpent × refund rate), e.g. Epic level 2 = 306.</summary>
        public int SellRefund(PlacementRules rules) => IsPlaced ? HeroEconomy.SellRefund(rules, GoldSpent) : 0;
    }
}
