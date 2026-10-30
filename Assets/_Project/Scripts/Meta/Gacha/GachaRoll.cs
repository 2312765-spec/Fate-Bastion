using FateBastion.Core;

namespace FateBastion.Meta
{
    /// <summary>One result of a pull: which hero, its rarity and how it was obtained (S8).</summary>
    public readonly struct GachaRoll
    {
        public readonly string HeroId;
        public readonly Rarity Rarity;

        /// <summary>First time this hero is owned: it was added to ownedEver and gave the first-own gems.</summary>
        public readonly bool IsNew;

        /// <summary>Forced Legendary by the pity counter.</summary>
        public readonly bool FromPity;

        /// <summary>Last pull of a x10 that was re-rolled among Epic + Legendary.</summary>
        public readonly bool Guaranteed;

        public GachaRoll(string heroId, Rarity rarity, bool isNew, bool fromPity, bool guaranteed)
        {
            HeroId = heroId;
            Rarity = rarity;
            IsNew = isNew;
            FromPity = fromPity;
            Guaranteed = guaranteed;
        }

        public bool IsEpicOrBetter => Rarity >= Rarity.Epic;

        public GachaRoll AsGuaranteed() => new GachaRoll(HeroId, Rarity, IsNew, FromPity, true);
    }
}
