using UnityEditor;

namespace FateBastion.Editor.Heroes
{
    /// <summary>
    /// Sets rig, animation and material import options for FBX files dropped into Assets/ThirdParty (C-07), so the
    /// ~15 models of C-08 do not need the same Inspector clicks one by one. Rules live in <see cref="ThirdPartyImportRules"/>.
    /// </summary>
    public class ThirdPartyModelPostprocessor : AssetPostprocessor
    {
        private void OnPreprocessModel()
        {
            // Only on the first import: a later manual fix in the Inspector (e.g. Generic for a rig Humanoid
            // cannot map) must survive reimports.
            if (!assetImporter.importSettingsMissing)
            {
                return;
            }

            ThirdPartyModelKind kind = ThirdPartyImportRules.Classify(assetPath);
            if (kind == ThirdPartyModelKind.None)
            {
                return;
            }

            var importer = (ModelImporter)assetImporter;
            importer.animationType = ThirdPartyImportRules.GetAnimationType(kind);
            importer.importAnimation = ThirdPartyImportRules.ImportsAnimation(kind);
            importer.materialImportMode = ThirdPartyImportRules.GetMaterialImportMode(kind);
            importer.importCameras = false;
            importer.importLights = false;
            importer.importVisibility = false;
            // No gameplay code reads mesh data at runtime; keeping it CPU-side would double the memory.
            importer.isReadable = false;
        }

        private void OnPreprocessAnimation()
        {
            if (!assetImporter.importSettingsMissing ||
                ThirdPartyImportRules.Classify(assetPath) != ThirdPartyModelKind.HumanoidAnimation)
            {
                return;
            }

            var importer = (ModelImporter)assetImporter;
            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            bool loop = ThirdPartyImportRules.ShouldLoop(assetPath);
            for (int i = 0; i < clips.Length; i++)
            {
                clips[i].loopTime = loop;
                // Mixamo In Place clips: keep the body on the spot, rotation and height baked into the pose.
                clips[i].lockRootRotation = true;
                clips[i].lockRootHeightY = true;
                clips[i].lockRootPositionXZ = true;
            }

            importer.clipAnimations = clips;
        }
    }
}
