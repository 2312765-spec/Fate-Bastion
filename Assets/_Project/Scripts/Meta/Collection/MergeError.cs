namespace FateBastion.Meta
{
    /// <summary>Why a merge was refused; nothing in SaveData changes when it is not None (S8).</summary>
    public enum MergeError
    {
        None,

        /// <summary>Legendary is the top rarity and cannot be merged.</summary>
        LegendaryCannotMerge,

        /// <summary>Fewer spare copies of that rarity than the merge cost (see MergeResult.Missing).</summary>
        NotEnoughSpare,

        /// <summary>The selection does not contain exactly MergeCost copies.</summary>
        WrongSelectionSize,

        /// <summary>A selected hero is unknown or of another rarity.</summary>
        WrongRarity,

        /// <summary>A hero was selected more times than it has spare copies (it must keep 1).</summary>
        HeroHasNoSpare
    }
}
