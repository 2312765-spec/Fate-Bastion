using System;

namespace FateBastion.Core
{
    /// <summary>
    /// In-match hero event hub (S4). Raised by the placement system (B); listened to by the Command Aura (A),
    /// the HUD (B) and audio and FX (C). See Docs/TEAM_ASSIGNMENT.md section 3.
    /// </summary>
    public static class HeroEvents
    {
        public static event Action<HeroEventInfo> OnHeroPlaced;
        public static event Action<HeroEventInfo> OnHeroSold;
        public static event Action<HeroEventInfo> OnHeroUpgraded;

        /// <summary>Hero info panel opened; <see cref="HeroEventInfo.Hero"/> is null when the selection was cleared.</summary>
        public static event Action<HeroEventInfo> OnHeroSelected;

        public static void RaiseHeroPlaced(in HeroEventInfo info) => OnHeroPlaced?.Invoke(info);
        public static void RaiseHeroSold(in HeroEventInfo info) => OnHeroSold?.Invoke(info);
        public static void RaiseHeroUpgraded(in HeroEventInfo info) => OnHeroUpgraded?.Invoke(info);
        public static void RaiseHeroSelected(in HeroEventInfo info) => OnHeroSelected?.Invoke(info);

        /// <summary>See <see cref="GameEvents.ResetAll"/>.</summary>
        public static void ResetAll()
        {
            OnHeroPlaced = null;
            OnHeroSold = null;
            OnHeroUpgraded = null;
            OnHeroSelected = null;
        }
    }
}
