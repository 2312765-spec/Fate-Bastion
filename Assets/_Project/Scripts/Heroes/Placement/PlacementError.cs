namespace FateBastion.Heroes
{
    /// <summary>First failed placement check, in the S4 order; the HUD shows it as the reason.</summary>
    public enum PlacementError
    {
        None,
        NotEnoughGold,
        HeroLimitReached,
        ArmyFull,
        NotPlaceableGround,
        AreaBlocked,
        AimingMeteor
    }
}
