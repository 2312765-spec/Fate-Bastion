namespace FateBastion.Enemies
{
    /// <summary>Gives the path for a SpawnGroup.pathIndex: 0 = ground path, 1 = flying path (S2).</summary>
    public interface IEnemyPathProvider
    {
        /// <summary>Returns null (and logs an error) when the map has no path for this index.</summary>
        IEnemyPath GetPath(int pathIndex);
    }
}
