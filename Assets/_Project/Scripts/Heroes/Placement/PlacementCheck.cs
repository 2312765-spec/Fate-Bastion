using FateBastion.Core;

namespace FateBastion.Heroes
{
    /// <summary>
    /// Everything PlacementValidator needs for one placement attempt. PlacementSystem (B-02) fills the physics
    /// results (raycast on Placeable, CheckSphere) each frame; tests fill it by hand.
    /// </summary>
    public readonly struct PlacementCheck
    {
        public readonly int Gold;
        public readonly Rarity Rarity;

        /// <summary>Copies of this deck hero already on the field (DeckSlot.placedCount).</summary>
        public readonly int PlacedOfThisHero;

        /// <summary>All heroes on the field.</summary>
        public readonly int TotalPlaced;

        /// <summary>The mouse ray hit the Placeable layer.</summary>
        public readonly bool HitPlaceable;

        /// <summary>CheckSphere(occupancy radius) touched no Hero, Path or Obstacle.</summary>
        public readonly bool AreaFree;

        /// <summary>Meteor aim mode is active (S7).</summary>
        public readonly bool AimingMeteor;

        public PlacementCheck(int gold, Rarity rarity, int placedOfThisHero, int totalPlaced, bool hitPlaceable,
            bool areaFree, bool aimingMeteor)
        {
            Gold = gold;
            Rarity = rarity;
            PlacedOfThisHero = placedOfThisHero;
            TotalPlaced = totalPlaced;
            HitPlaceable = hitPlaceable;
            AreaFree = areaFree;
            AimingMeteor = aimingMeteor;
        }
    }
}
