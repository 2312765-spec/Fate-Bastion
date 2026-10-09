using System;
using System.IO;
using UnityEditor;

namespace FateBastion.Editor.Import
{
    /// <summary>
    /// Tools &gt; Import Balance CSV: reads heroes.csv, enemies.csv and waves.csv and creates or updates the
    /// matching ScriptableObject assets. Running it twice must not create a second copy of anything (S0).
    /// </summary>
    public static class BalanceImporter
    {
        public static BalanceImportReport Import(BalanceImportRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var report = new BalanceImportReport();
            AssetFolderUtility.EnsureFolder(request.OutputRoot);

            try
            {
                // Enemies first: the wave importer needs the EnemyData assets to build its spawn groups.
                ImportOne(request.EnemiesCsvPath, "enemies.csv", report,
                    table => EnemyCsvImporter.Import(table, request, report));

                ImportOne(request.HeroesCsvPath, "heroes.csv", report,
                    table => HeroCsvImporter.Import(table, request, report));

                ImportOne(request.WavesCsvPath, "waves.csv", report,
                    table => WaveCsvImporter.Import(table, request, report));
            }
            finally
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            return report;
        }

        private static void ImportOne(string path, string fileName, BalanceImportReport report, Action<CsvTable> import)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            if (!File.Exists(path))
            {
                report.AddError($"{fileName}: file not found at '{path}'; skipped.");
                return;
            }

            CsvTable table;
            try
            {
                table = CsvReader.ParseFile(path);
            }
            catch (Exception e)
            {
                report.AddError($"{fileName}: cannot be parsed ({e.Message}); skipped.");
                return;
            }

            import(table);
        }
    }
}
