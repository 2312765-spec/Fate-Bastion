using UnityEditor;
using UnityEngine;
using FateBastion.Meta;

namespace FateBastion.Editor.DebugTools
{
    /// <summary>Editor menu of the debug tools (S9, B-16): delete or open the save without entering Play mode.</summary>
    public static class DebugMenu
    {
        [MenuItem("Tools/Debug/Delete Save File")]
        public static void DeleteSave()
        {
            string folder = Application.persistentDataPath;
            if (!EditorUtility.DisplayDialog("Xóa save", $"Xóa save.json, save.bak, save.tmp trong\n{folder}?", "Xóa", "Hủy")) return;

            new SaveService(folder).Delete();
            MetaRuntime.ResetAll();
            Debug.Log($"[Debug] Deleted the save in {folder}.");
        }

        [MenuItem("Tools/Debug/Open Save Folder")]
        public static void OpenSaveFolder() => EditorUtility.RevealInFinder(Application.persistentDataPath);
    }
}
