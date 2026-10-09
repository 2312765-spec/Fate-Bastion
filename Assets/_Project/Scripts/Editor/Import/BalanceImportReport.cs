using System.Collections.Generic;
using System.Text;

namespace FateBastion.Editor.Import
{
    /// <summary>
    /// Result of one run of Tools &gt; Import Balance CSV: how many assets were created or updated, plus the
    /// rows that were skipped. A bad row never aborts the import, it only lands in <see cref="Errors"/> (S0).
    /// </summary>
    public sealed class BalanceImportReport
    {
        public int HeroesCreated { get; internal set; }
        public int HeroesUpdated { get; internal set; }
        public int SkillsCreated { get; internal set; }
        public int SkillsUpdated { get; internal set; }
        public int EnemiesCreated { get; internal set; }
        public int EnemiesUpdated { get; internal set; }
        public int WavesCreated { get; internal set; }
        public int WavesUpdated { get; internal set; }
        public int LevelsCreated { get; internal set; }
        public int LevelsUpdated { get; internal set; }

        public List<string> Errors { get; } = new List<string>();
        public List<string> Warnings { get; } = new List<string>();

        public int HeroTotal => HeroesCreated + HeroesUpdated;
        public int SkillTotal => SkillsCreated + SkillsUpdated;
        public int EnemyTotal => EnemiesCreated + EnemiesUpdated;
        public int WaveTotal => WavesCreated + WavesUpdated;
        public int LevelTotal => LevelsCreated + LevelsUpdated;

        public bool HasErrors => Errors.Count > 0;

        /// <summary>Records a skipped row with its file name and 1-based line number, as required by S0.</summary>
        internal void AddError(string fileName, int lineNumber, string message)
        {
            Errors.Add($"{fileName} line {lineNumber}: {message}");
        }

        internal void AddError(string message)
        {
            Errors.Add(message);
        }

        internal void AddWarning(string message)
        {
            Warnings.Add(message);
        }

        public string ToSummary()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"HeroData:   {HeroTotal} ({HeroesCreated} new, {HeroesUpdated} updated)");
            sb.AppendLine($"SkillData:  {SkillTotal} ({SkillsCreated} new, {SkillsUpdated} updated)");
            sb.AppendLine($"EnemyData:  {EnemyTotal} ({EnemiesCreated} new, {EnemiesUpdated} updated)");
            sb.AppendLine($"WaveData:   {WaveTotal} ({WavesCreated} new, {WavesUpdated} updated)");
            sb.AppendLine($"LevelData:  {LevelTotal} ({LevelsCreated} new, {LevelsUpdated} updated)");
            sb.AppendLine($"Warnings:   {Warnings.Count}");
            sb.AppendLine($"Errors:     {Errors.Count}");

            for (int i = 0; i < Warnings.Count; i++)
            {
                sb.AppendLine("  WARN  " + Warnings[i]);
            }

            for (int i = 0; i < Errors.Count; i++)
            {
                sb.AppendLine("  ERROR " + Errors[i]);
            }

            return sb.ToString();
        }
    }
}
