using UnityEngine;

namespace FateBastion.Editor.Import
{
    /// <summary>
    /// Batch-mode entry point for the balance import, so it can run from the command line:
    /// Unity.exe -batchmode -projectPath . -executeMethod FateBastion.Editor.Import.BalanceImportCli.ImportDefault -quit
    /// </summary>
    public static class BalanceImportCli
    {
        public static void ImportDefault()
        {
            BalanceImportReport report = BalanceImporter.Import(BalanceImportRequest.CreateDefault());
            if (report.HasErrors)
            {
                Debug.LogError("Import Balance CSV finished with errors:\n" + report.ToSummary());
            }
            else
            {
                Debug.Log("Import Balance CSV finished:\n" + report.ToSummary());
            }
        }
    }
}
