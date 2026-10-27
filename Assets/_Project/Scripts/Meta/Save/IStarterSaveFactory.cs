namespace FateBastion.Meta
{
    /// <summary>
    /// Builds the SaveData of a brand-new player: the five starter heroes, 1000 gems and the default deck (S8/S9).
    /// STUB: the real values live in StarterSettings (S8, B). A-15 reads them once B-11 is done; until then this
    /// interface lets SaveService create a new save without depending on S8.
    /// </summary>
    public interface IStarterSaveFactory
    {
        SaveData CreateNew();
    }
}
