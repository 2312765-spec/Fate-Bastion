using FateBastion.Core;

namespace FateBastion.Meta
{
    /// <summary>Outcome of a merge: the hero received, or why it was refused (S8).</summary>
    public readonly struct MergeResult
    {
        public readonly MergeError Error;

        /// <summary>Spare copies still missing when Error is NotEnoughSpare, else 0.</summary>
        public readonly int Missing;

        public readonly string HeroId;
        public readonly Rarity Rarity;
        public readonly bool IsNew;
        public readonly int GemsAwarded;

        private MergeResult(MergeError error, int missing, string heroId, Rarity rarity, bool isNew, int gemsAwarded)
        {
            Error = error;
            Missing = missing;
            HeroId = heroId;
            Rarity = rarity;
            IsNew = isNew;
            GemsAwarded = gemsAwarded;
        }

        public bool Success => Error == MergeError.None;

        public static MergeResult Failed(MergeError error, int missing = 0) =>
            new MergeResult(error, missing, null, default, false, 0);

        public static MergeResult Succeeded(string heroId, Rarity rarity, bool isNew, int gemsAwarded) =>
            new MergeResult(MergeError.None, 0, heroId, rarity, isNew, gemsAwarded);
    }
}
