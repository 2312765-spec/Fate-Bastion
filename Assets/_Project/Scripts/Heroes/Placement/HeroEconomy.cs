using System;
using FateBastion.Core;

namespace FateBastion.Heroes
{
    /// <summary>
    /// In-match prices (S4): place, upgrade and sell. Pure so the numbers are unit-tested; every result is rounded
    /// down, e.g. an Epic at level 2 cost 350 + 262 = 612 and sells for 306.
    /// </summary>
    public static class HeroEconomy
    {
        // Guards floor() against float noise such as 199.99998 for 200.
        private const double FloorEpsilon = 1e-4;

        public static int PlaceCost(PlacementRules rules, Rarity rarity) => rules.PlaceCostOf(rarity);

        /// <summary>True when a hero at <paramref name="currentLevel"/> can still be upgraded.</summary>
        public static bool CanUpgrade(HeroLevelTable table, int currentLevel) => currentLevel >= 1 && currentLevel < table.MaxLevel;

        /// <summary>Price to go from <paramref name="currentLevel"/> to the next one: floor(place cost × % of the next level).</summary>
        public static int UpgradeCost(PlacementRules rules, HeroLevelTable table, Rarity rarity, int currentLevel)
        {
            if (!CanUpgrade(table, currentLevel))
            {
                throw new ArgumentOutOfRangeException(nameof(currentLevel), currentLevel, "Hero is already at max level.");
            }

            return FloorTimes(rules.PlaceCostOf(rarity), table.StepOf(currentLevel + 1).costPercent);
        }

        /// <summary>Total gold put into a hero placed and upgraded up to <paramref name="level"/>.</summary>
        public static int GoldSpentAt(PlacementRules rules, HeroLevelTable table, Rarity rarity, int level)
        {
            int spent = rules.PlaceCostOf(rarity);
            for (int current = 1; current < level; current++)
            {
                spent += UpgradeCost(rules, table, rarity, current);
            }

            return spent;
        }

        /// <summary>Sell refund: floor(goldSpent × refund rate).</summary>
        public static int SellRefund(PlacementRules rules, int goldSpent) => FloorTimes(goldSpent, rules.RefundRate);

        private static int FloorTimes(int value, float factor) => (int)Math.Floor(value * (double)factor + FloorEpsilon);
    }
}
