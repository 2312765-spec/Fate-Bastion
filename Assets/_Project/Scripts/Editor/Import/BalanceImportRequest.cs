using System.IO;
using UnityEngine;

namespace FateBastion.Editor.Import
{
    /// <summary>
    /// Input of one import run. The output root is a parameter so unit tests can write into a throwaway
    /// folder instead of the real Data folder (S0).
    /// </summary>
    public sealed class BalanceImportRequest
    {
        public const string DefaultOutputRoot = "Assets/_Project/Data";

        public string HeroesCsvPath;
        public string EnemiesCsvPath;
        public string WavesCsvPath;

        /// <summary>Project-relative folder that holds Heroes, Skills, Enemies, Waves and Levels.</summary>
        public string OutputRoot = DefaultOutputRoot;

        /// <summary>
        /// Default CSV folder: Data/Balance sits next to Assets in the repository, so it is one level above
        /// Application.dataPath.
        /// </summary>
        public static string DefaultCsvFolder =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Data", "Balance"));

        public static BalanceImportRequest CreateDefault()
        {
            string folder = DefaultCsvFolder;
            return new BalanceImportRequest
            {
                HeroesCsvPath = Path.Combine(folder, "heroes.csv"),
                EnemiesCsvPath = Path.Combine(folder, "enemies.csv"),
                WavesCsvPath = Path.Combine(folder, "waves.csv"),
                OutputRoot = DefaultOutputRoot
            };
        }

        public string HeroesFolder => OutputRoot + "/Heroes";
        public string SkillsFolder => OutputRoot + "/Skills";
        public string EnemiesFolder => OutputRoot + "/Enemies";
        public string WavesFolder => OutputRoot + "/Waves";
        public string LevelsFolder => OutputRoot + "/Levels";
    }
}
