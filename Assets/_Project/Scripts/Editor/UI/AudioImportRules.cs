using UnityEditor;
using UnityEngine;

namespace FateBastion.Editor.UI
{
    /// <summary>
    /// Import settings for game audio (C-13): music streams, short SFX decompress on load, 3D SFX are mono.
    /// Applied automatically the first time a clip is imported under Audio/; settings C changes by hand afterwards
    /// are kept. Tools > Audio > Reapply Import Settings forces the rules on every clip again.
    /// </summary>
    public class AudioImportRules : AssetPostprocessor
    {
        private const string MusicFolder = "Assets/_Project/Audio/Music/";
        private const string SfxFolder = "Assets/_Project/Audio/SFX/";

        // Folders whose sounds play in 3D (SoundData.spatialBlend 1): stereo is wasted there, so force mono.
        private static readonly string[] SpatialSfxFolders = { "Heroes/", "Summoner/", "Enemies/" };
        // Short UI and impact sounds stay uncompressed in memory; jingles (Match, Gacha) are longer and compressed.
        private static readonly string[] JingleSfxFolders = { "Match/", "Gacha/" };

        private const float MusicQuality = 0.6f;
        private const float JingleQuality = 0.7f;

        private static bool _forceAll;

        private void OnPreprocessAudio()
        {
            bool isMusic = assetPath.StartsWith(MusicFolder);
            bool isSfx = assetPath.StartsWith(SfxFolder);
            if (!isMusic && !isSfx) return;
            if (!_forceAll && !assetImporter.importSettingsMissing) return;

            var importer = (AudioImporter)assetImporter;
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;

            if (isMusic)
            {
                importer.forceToMono = false;
                importer.loadInBackground = true;
                settings.loadType = AudioClipLoadType.Streaming;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = MusicQuality;
                settings.preloadAudioData = false;
            }
            else
            {
                string sub = assetPath.Substring(SfxFolder.Length);
                importer.forceToMono = StartsWithAny(sub, SpatialSfxFolders);
                importer.loadInBackground = false;
                settings.preloadAudioData = true;
                if (StartsWithAny(sub, JingleSfxFolders))
                {
                    settings.loadType = AudioClipLoadType.CompressedInMemory;
                    settings.compressionFormat = AudioCompressionFormat.Vorbis;
                    settings.quality = JingleQuality;
                }
                else
                {
                    settings.loadType = AudioClipLoadType.DecompressOnLoad;
                    settings.compressionFormat = AudioCompressionFormat.ADPCM;
                }
            }

            importer.defaultSampleSettings = settings;
        }

        [MenuItem("Tools/Audio/Reapply Import Settings")]
        private static void ReapplyMenu()
        {
            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/_Project/Audio" });
            _forceAll = true;
            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (string guid in guids)
                {
                    AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                _forceAll = false;
            }

            Debug.Log($"[Audio Import] Reapplied import settings to {guids.Length} clip(s).");
        }

        private static bool StartsWithAny(string path, string[] prefixes)
        {
            foreach (string prefix in prefixes)
            {
                if (path.StartsWith(prefix)) return true;
            }

            return false;
        }
    }
}
