namespace FateBastion.Meta
{
    /// <summary>Why a deck is not valid for a match (S8: exactly 5 different owned heroes).</summary>
    public enum DeckError
    {
        None,
        WrongSize,
        Duplicate,
        NotOwned,
        SlotOutOfRange
    }
}
