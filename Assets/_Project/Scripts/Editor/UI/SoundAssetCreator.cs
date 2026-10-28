using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using FateBastion.Audio;

namespace FateBastion.Editor.UI
{
    /// <summary>
    /// Tools > Audio > Create Sound Assets (C-13): creates every SoundData of the S11 list with the suggested
    /// values, plus SoundLibrary and AudioConfig, and wires them together. Safe to run again: existing assets keep
    /// the values C tuned; only empty clip lists and empty slots are filled.
    /// </summary>
    public static class SoundAssetCreator
    {
        public const string SoundFolder = "Assets/_Project/Data/Audio";
        public const string LibraryPath = "Assets/_Project/Data/Settings/SoundLibrary.asset";
        public const string ConfigPath = "Assets/_Project/Data/Settings/AudioConfig.asset";
        public const string MixerPath = "Assets/_Project/Audio/Mixer/FB_Mixer.mixer";
        public const string SfxClipFolder = "Assets/_Project/Audio/SFX";
        public const string MusicClipFolder = "Assets/_Project/Audio/Music";

        // Clip naming convention: a clip named "<asset name>" or "<asset name>_<anything>" (SFX_EnemyDeath_01.wav)
        // is put into that SoundData; music files are MUS_Lobby, MUS_Match, MUS_Boss.
        private const string MusicPrefix = "MUS_";

        private readonly struct SoundSpec
        {
            public readonly string Name;
            public readonly float SpatialBlend;
            public readonly int MaxConcurrent;
            public readonly float Volume;
            public readonly float PitchVariance;
            public readonly float MinDistance;
            public readonly float MaxDistance;

            public SoundSpec(string name, float spatialBlend, int maxConcurrent, float volume,
                float pitchVariance = 0.1f, float minDistance = 5f, float maxDistance = 45f)
            {
                Name = name;
                SpatialBlend = spatialBlend;
                MaxConcurrent = maxConcurrent;
                Volume = volume;
                PitchVariance = pitchVariance;
                MinDistance = minDistance;
                MaxDistance = maxDistance;
            }
        }

        // Starting values (presentation, not balance); C tunes them in each asset afterwards.
        private static readonly SoundSpec[] Specs =
        {
            // Heroes
            new SoundSpec("SFX_HeroPlace", 1f, 2, 0.8f),
            new SoundSpec("SFX_HeroUpgrade", 1f, 2, 0.8f),
            new SoundSpec("SFX_HeroLevel3", 1f, 1, 1f),
            new SoundSpec("SFX_HeroSell", 1f, 2, 0.8f),
            new SoundSpec("SFX_Attack_Melee", 1f, 4, 0.5f),
            new SoundSpec("SFX_Attack_Bow", 1f, 4, 0.5f),
            new SoundSpec("SFX_Attack_Magic", 1f, 4, 0.5f),
            new SoundSpec("SFX_Ultimate", 1f, 2, 1f),
            // Summoner
            new SoundSpec("SFX_Footstep", 1f, 2, 0.4f, pitchVariance: 0.15f, minDistance: 2f),
            new SoundSpec("SFX_MeteorFall", 0f, 1, 0.9f),
            new SoundSpec("SFX_MeteorExplode", 1f, 2, 1f, maxDistance: 60f),
            new SoundSpec("SFX_MeteorReady", 0f, 1, 0.7f),
            // Enemies
            new SoundSpec("SFX_EnemyHit", 1f, 4, 0.4f),
            new SoundSpec("SFX_EnemyDeath", 1f, 4, 0.6f),
            new SoundSpec("SFX_BossDeath", 1f, 1, 1f),
            new SoundSpec("SFX_CastleHit", 0f, 2, 0.9f),
            new SoundSpec("SFX_BossAppear", 0f, 1, 1f),
            // Match (jingles keep their key: no pitch variance)
            new SoundSpec("SFX_WaveStart", 0f, 1, 0.8f, pitchVariance: 0f),
            new SoundSpec("SFX_WaveClear", 0f, 1, 0.8f, pitchVariance: 0f),
            new SoundSpec("SFX_Victory", 0f, 1, 1f, pitchVariance: 0f),
            new SoundSpec("SFX_Defeat", 0f, 1, 1f, pitchVariance: 0f),
            // Lobby
            new SoundSpec("SFX_Gacha_Common", 0f, 1, 0.9f, pitchVariance: 0f),
            new SoundSpec("SFX_Gacha_Rare", 0f, 1, 0.9f, pitchVariance: 0f),
            new SoundSpec("SFX_Gacha_Epic", 0f, 1, 0.9f, pitchVariance: 0f),
            new SoundSpec("SFX_Gacha_Legendary", 0f, 1, 0.9f, pitchVariance: 0f),
            // UI
            new SoundSpec("SFX_UiClick", 0f, 3, 0.6f, pitchVariance: 0.05f),
            new SoundSpec("SFX_PanelOpen", 0f, 2, 0.6f, pitchVariance: 0.05f),
            new SoundSpec("SFX_PanelClose", 0f, 2, 0.6f, pitchVariance: 0.05f),
            new SoundSpec("SFX_Error", 0f, 1, 0.8f, pitchVariance: 0f),
            new SoundSpec("SFX_Merge", 0f, 1, 0.9f, pitchVariance: 0f),
        };

        [MenuItem("Tools/Audio/Create Sound Assets")]
        public static void CreateAll()
        {
            Directory.CreateDirectory(SoundFolder);
            List<AudioClip> sfxClips = LoadClips(SfxClipFolder);
            var sounds = new Dictionary<string, SoundData>(Specs.Length);
            int created = 0;
            int filled = 0;

            foreach (SoundSpec spec in Specs)
            {
                string path = SoundFolder + "/" + spec.Name + ".asset";
                var sound = AssetDatabase.LoadAssetAtPath<SoundData>(path);
                if (sound == null)
                {
                    sound = ScriptableObject.CreateInstance<SoundData>();
                    sound.spatialBlend = spec.SpatialBlend;
                    sound.maxConcurrent = spec.MaxConcurrent;
                    sound.volume = spec.Volume;
                    sound.pitchVariance = spec.PitchVariance;
                    sound.minDistance = spec.MinDistance;
                    sound.maxDistance = spec.MaxDistance;
                    sound.group = AudioGroup.Sfx;
                    AssetDatabase.CreateAsset(sound, path);
                    created++;
                }

                if (!sound.HasClips)
                {
                    AudioClip[] clips = MatchClips(sfxClips, spec.Name);
                    if (clips.Length > 0)
                    {
                        sound.clips = clips;
                        EditorUtility.SetDirty(sound);
                        filled++;
                    }
                }

                sounds[spec.Name] = sound;
            }

            SoundLibrary library = CreateLibrary(sounds);
            CreateConfig(library);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Create Sound Assets] {created} SoundData created, {filled} got clips, in {SoundFolder}. " +
                      $"Library: {LibraryPath}, config: {ConfigPath}.");
        }

        private static SoundLibrary CreateLibrary(Dictionary<string, SoundData> s)
        {
            var library = AssetDatabase.LoadAssetAtPath<SoundLibrary>(LibraryPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<SoundLibrary>();
                AssetDatabase.CreateAsset(library, LibraryPath);
            }

            library.heroPlace = Keep(library.heroPlace, s["SFX_HeroPlace"]);
            library.heroUpgrade = Keep(library.heroUpgrade, s["SFX_HeroUpgrade"]);
            library.heroLevel3 = Keep(library.heroLevel3, s["SFX_HeroLevel3"]);
            library.heroSell = Keep(library.heroSell, s["SFX_HeroSell"]);
            library.defaultAttack = Keep(library.defaultAttack, s["SFX_Attack_Melee"]);
            library.defaultUltimate = Keep(library.defaultUltimate, s["SFX_Ultimate"]);
            library.footstep = Keep(library.footstep, s["SFX_Footstep"]);
            library.meteorFall = Keep(library.meteorFall, s["SFX_MeteorFall"]);
            library.meteorExplode = Keep(library.meteorExplode, s["SFX_MeteorExplode"]);
            library.meteorReady = Keep(library.meteorReady, s["SFX_MeteorReady"]);
            library.enemyHit = Keep(library.enemyHit, s["SFX_EnemyHit"]);
            library.enemyDeath = Keep(library.enemyDeath, s["SFX_EnemyDeath"]);
            library.bossDeath = Keep(library.bossDeath, s["SFX_BossDeath"]);
            library.castleHit = Keep(library.castleHit, s["SFX_CastleHit"]);
            library.bossAppear = Keep(library.bossAppear, s["SFX_BossAppear"]);
            library.waveStart = Keep(library.waveStart, s["SFX_WaveStart"]);
            library.waveClear = Keep(library.waveClear, s["SFX_WaveClear"]);
            library.victory = Keep(library.victory, s["SFX_Victory"]);
            library.defeat = Keep(library.defeat, s["SFX_Defeat"]);
            library.uiClick = Keep(library.uiClick, s["SFX_UiClick"]);
            library.panelOpen = Keep(library.panelOpen, s["SFX_PanelOpen"]);
            library.panelClose = Keep(library.panelClose, s["SFX_PanelClose"]);
            library.error = Keep(library.error, s["SFX_Error"]);
            library.merge = Keep(library.merge, s["SFX_Merge"]);

            // Indexed by Rarity: Common, Rare, Epic, Legendary.
            string[] gachaNames = { "SFX_Gacha_Common", "SFX_Gacha_Rare", "SFX_Gacha_Epic", "SFX_Gacha_Legendary" };
            if (library.gachaReveal == null || library.gachaReveal.Length != gachaNames.Length)
            {
                var resized = new SoundData[gachaNames.Length];
                for (int i = 0; library.gachaReveal != null && i < Mathf.Min(resized.Length, library.gachaReveal.Length); i++)
                {
                    resized[i] = library.gachaReveal[i];
                }

                library.gachaReveal = resized;
            }

            for (int i = 0; i < gachaNames.Length; i++)
            {
                library.gachaReveal[i] = Keep(library.gachaReveal[i], s[gachaNames[i]]);
            }

            EditorUtility.SetDirty(library);
            return library;
        }

        private static void CreateConfig(SoundLibrary library)
        {
            var config = AssetDatabase.LoadAssetAtPath<AudioConfig>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<AudioConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }

            config.sounds = Keep(config.sounds, library);

            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            if (mixer == null)
            {
                Debug.LogWarning($"[Create Sound Assets] No mixer at {MixerPath}; assign it in {ConfigPath} by hand.");
            }
            else
            {
                config.mixer = Keep(config.mixer, mixer);
                config.musicGroup = Keep(config.musicGroup, FindGroup(mixer, "Music"));
                config.sfxGroup = Keep(config.sfxGroup, FindGroup(mixer, "SFX"));
                WarnMissingParameter(mixer, config.masterParameter);
                WarnMissingParameter(mixer, config.musicParameter);
                WarnMissingParameter(mixer, config.sfxParameter);
            }

            List<AudioClip> music = LoadClips(MusicClipFolder);
            config.lobbyMusic = Keep(config.lobbyMusic, FirstMatch(music, MusicPrefix + "Lobby"));
            config.matchMusic = Keep(config.matchMusic, FirstMatch(music, MusicPrefix + "Match"));
            config.bossMusic = Keep(config.bossMusic, FirstMatch(music, MusicPrefix + "Boss"));

            EditorUtility.SetDirty(config);
        }

        /// <summary>Keeps what C already assigned; fills only an empty slot.</summary>
        private static T Keep<T>(T current, T fallback) where T : Object => current != null ? current : fallback;

        private static AudioMixerGroup FindGroup(AudioMixer mixer, string groupName)
        {
            foreach (AudioMixerGroup group in mixer.FindMatchingGroups(groupName))
            {
                if (group.name == groupName) return group;
            }

            Debug.LogWarning($"[Create Sound Assets] Mixer has no group '{groupName}'.");
            return null;
        }

        private static void WarnMissingParameter(AudioMixer mixer, string parameter)
        {
            if (!mixer.GetFloat(parameter, out _))
            {
                Debug.LogWarning($"[Create Sound Assets] Mixer parameter '{parameter}' is not exposed yet " +
                                 "(select the group, right-click Volume → Expose, then rename it under Exposed Parameters).");
            }
        }

        private static List<AudioClip> LoadClips(string folder)
        {
            var clips = new List<AudioClip>();
            if (!AssetDatabase.IsValidFolder(folder)) return clips;
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { folder }))
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guid));
                if (clip != null) clips.Add(clip);
            }

            clips.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return clips;
        }

        private static AudioClip[] MatchClips(List<AudioClip> clips, string soundName)
        {
            var matches = new List<AudioClip>();
            foreach (AudioClip clip in clips)
            {
                if (IsMatch(clip.name, soundName)) matches.Add(clip);
            }

            return matches.ToArray();
        }

        private static AudioClip FirstMatch(List<AudioClip> clips, string name)
        {
            foreach (AudioClip clip in clips)
            {
                if (IsMatch(clip.name, name)) return clip;
            }

            return null;
        }

        // "SFX_EnemyHit" matches SFX_EnemyHit and SFX_EnemyHit_02, but not SFX_EnemyHitBig.
        private static bool IsMatch(string clipName, string soundName) =>
            clipName == soundName || clipName.StartsWith(soundName + "_");
    }
}
