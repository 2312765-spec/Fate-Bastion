using System;
using System.IO;
using UnityEditor;

namespace FateBastion.Editor.Heroes
{
    /// <summary>
    /// Pure rules behind <see cref="ThirdPartyModelPostprocessor"/>: maps an asset path to an import profile and the
    /// importer values of that profile. No AssetDatabase access, so it is unit tested with plain strings (C-07).
    /// </summary>
    public static class ThirdPartyImportRules
    {
        // Folder convention of Assets/ThirdParty (C-07). Technical constants, not balance numbers.
        public const string QuaterniusCharactersFolder = "Assets/ThirdParty/Quaternius/Characters/";
        public const string QuaterniusCreaturesFolder = "Assets/ThirdParty/Quaternius/Creatures/";
        public const string QuaterniusPropsFolder = "Assets/ThirdParty/Quaternius/Props/";
        public const string MixamoFolder = "Assets/ThirdParty/Mixamo/";
        public const string KenneyFolder = "Assets/ThirdParty/Kenney/";

        // Mixamo clips whose file name contains one of these loop; Attack/Cast/Spawn play once.
        private static readonly string[] LoopKeywords = { "idle", "walk", "run" };

        /// <summary>Profile for an FBX path; <see cref="ThirdPartyModelKind.None"/> for anything else.</summary>
        public static ThirdPartyModelKind Classify(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath) ||
                !assetPath.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
            {
                return ThirdPartyModelKind.None;
            }

            // AssetDatabase paths always use '/', but normalise in case a caller passes a Windows path.
            string path = assetPath.Replace('\\', '/');

            if (StartsWith(path, QuaterniusCharactersFolder)) return ThirdPartyModelKind.HumanoidCharacter;
            if (StartsWith(path, QuaterniusCreaturesFolder)) return ThirdPartyModelKind.GenericCreature;
            if (StartsWith(path, QuaterniusPropsFolder)) return ThirdPartyModelKind.StaticProp;
            if (StartsWith(path, MixamoFolder)) return ThirdPartyModelKind.HumanoidAnimation;
            if (StartsWith(path, KenneyFolder)) return ThirdPartyModelKind.StaticProp;
            return ThirdPartyModelKind.None;
        }

        /// <summary>Importer rig type for a profile.</summary>
        public static ModelImporterAnimationType GetAnimationType(ThirdPartyModelKind kind)
        {
            switch (kind)
            {
                case ThirdPartyModelKind.HumanoidCharacter:
                case ThirdPartyModelKind.HumanoidAnimation:
                    return ModelImporterAnimationType.Human;
                case ThirdPartyModelKind.GenericCreature:
                    return ModelImporterAnimationType.Generic;
                default:
                    return ModelImporterAnimationType.None;
            }
        }

        /// <summary>Whether clips are imported. Quaternius characters ship CC0 clips (Idle, Walk, Attack…), so they keep them.</summary>
        public static bool ImportsAnimation(ThirdPartyModelKind kind)
        {
            return kind != ThirdPartyModelKind.None && kind != ThirdPartyModelKind.StaticProp;
        }

        /// <summary>Animation-only FBX carry no useful material; everything else keeps its embedded colours.</summary>
        public static ModelImporterMaterialImportMode GetMaterialImportMode(ThirdPartyModelKind kind)
        {
            return kind == ThirdPartyModelKind.HumanoidAnimation
                ? ModelImporterMaterialImportMode.None
                : ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        }

        /// <summary>True when a Mixamo file name looks like a looping clip (Idle, Walk, Run).</summary>
        public static bool ShouldLoop(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return false;
            }

            string fileName = Path.GetFileNameWithoutExtension(assetPath);
            for (int i = 0; i < LoopKeywords.Length; i++)
            {
                if (fileName.IndexOf(LoopKeywords[i], StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool StartsWith(string path, string folder)
        {
            return path.StartsWith(folder, StringComparison.OrdinalIgnoreCase);
        }
    }
}
