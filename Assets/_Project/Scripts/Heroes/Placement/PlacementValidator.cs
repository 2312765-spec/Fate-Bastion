using FateBastion.Core;

namespace FateBastion.Heroes
{
    /// <summary>
    /// Placement checks of S4 in the spec order, stopping at the first failure: gold, per-hero cap, army cap,
    /// Placeable ground, free area, not aiming the meteor. Pure, re-run on every click (never trusts last frame).
    /// </summary>
    public static class PlacementValidator
    {
        public static PlacementError Validate(PlacementRules rules, in PlacementCheck check)
        {
            PlacementError economy = ValidateEconomy(rules, check.Gold, check.Rarity, check.PlacedOfThisHero, check.TotalPlaced);
            if (economy != PlacementError.None) return economy;
            if (!check.HitPlaceable) return PlacementError.NotPlaceableGround;
            if (!check.AreaFree) return PlacementError.AreaBlocked;
            if (check.AimingMeteor) return PlacementError.AimingMeteor;
            return PlacementError.None;
        }

        /// <summary>Steps 1–3 only (gold, per-hero cap, army cap); no physics needed.</summary>
        public static PlacementError ValidateEconomy(PlacementRules rules, int gold, Rarity rarity, int placedOfThisHero,
            int totalPlaced)
        {
            if (gold < rules.PlaceCostOf(rarity)) return PlacementError.NotEnoughGold;
            if (placedOfThisHero >= rules.MaxPerHeroOf(rarity)) return PlacementError.HeroLimitReached;
            if (totalPlaced >= rules.MaxHeroes) return PlacementError.ArmyFull;
            return PlacementError.None;
        }

        /// <summary>
        /// After a successful placement: stay in preview mode only if another copy could still be placed
        /// (enough gold, per-hero cap and army cap), otherwise the preview exits by itself (S4 step 3).
        /// </summary>
        public static bool CanPlaceAnother(PlacementRules rules, int gold, Rarity rarity, int placedOfThisHero,
            int totalPlaced) =>
            ValidateEconomy(rules, gold, rarity, placedOfThisHero, totalPlaced) == PlacementError.None;
    }
}
