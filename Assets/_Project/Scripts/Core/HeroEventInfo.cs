using UnityEngine;

namespace FateBastion.Core
{
    /// <summary>
    /// Payload of the <see cref="HeroEvents"/> hub. It stays in Core, so the hero's data asset cannot be
    /// referenced here: listeners that need the full HeroData read it off <see cref="Hero"/> instead.
    /// </summary>
    public readonly struct HeroEventInfo
    {
        /// <summary>The placed hero instance. Null on <see cref="HeroEvents.OnHeroSelected"/> when the selection was cleared.</summary>
        public readonly GameObject Hero;

        public readonly string HeroId;
        public readonly Rarity Rarity;

        /// <summary>Hero level 1..3 after the event.</summary>
        public readonly int Level;

        public readonly Vector3 Position;

        /// <summary>Gold spent when placing or upgrading, gold refunded when selling, 0 when selecting.</summary>
        public readonly int Gold;

        public HeroEventInfo(GameObject hero, string heroId, Rarity rarity, int level, Vector3 position, int gold)
        {
            Hero = hero;
            HeroId = heroId;
            Rarity = rarity;
            Level = level;
            Position = position;
            Gold = gold;
        }
    }
}
