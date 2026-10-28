using UnityEngine;
using UnityEngine.Audio;

namespace FateBastion.Audio
{
    /// <summary>
    /// Audio setup (S11): mixer and its exposed volume parameters, pool size, music clips and crossfade.
    /// One asset at Data/Settings/AudioConfig, owned by C. Named AudioConfig because Unity already has AudioSettings.
    /// </summary>
    [CreateAssetMenu(menuName = "Fate Bastion/Audio/Audio Config", fileName = "AudioConfig")]
    public class AudioConfig : ScriptableObject
    {
        [Header("Mixer")]
        public AudioMixer mixer;
        public AudioMixerGroup musicGroup;
        public AudioMixerGroup sfxGroup;

        [Tooltip("Exposed parameter names in the mixer (right-click Volume → Expose, then rename).")]
        public string masterParameter = "MasterVolume";
        public string musicParameter = "MusicVolume";
        public string sfxParameter = "SfxVolume";

        [Header("Default volumes (0–1, first launch; same as the S9 save defaults)")]
        [Range(0f, 1f)] public float defaultMaster = 0.8f;
        [Range(0f, 1f)] public float defaultMusic = 0.6f;
        [Range(0f, 1f)] public float defaultSfx = 0.8f;

        [Header("Pool")]
        [Tooltip("AudioSources created at scene load; when all are busy new sounds are dropped.")]
        [Min(1)] public int poolSize = 24;

        // Technical size, not balance: distinct SoundData assets expected, so the voice counter never grows at runtime.
        [Min(1)] public int voiceCounterCapacity = 64;

        [Header("Music")]
        public AudioClip lobbyMusic;
        public AudioClip matchMusic;
        public AudioClip bossMusic;
        [Range(0f, 1f)] public float musicVolume = 1f;

        [Tooltip("Seconds to fade from one track to the next (real time, also while paused).")]
        [Min(0f)] public float crossfadeSeconds = 1.5f;

        [Tooltip("Every Nth wave is a boss wave (S2: 5/10/15, Endless every 5). Drives boss music and the boss alert.")]
        [Min(1)] public int bossWaveInterval = 5;

        [Header("Sounds")]
        public SoundLibrary sounds;

        public AudioClip GetMusic(MusicTrack track)
        {
            switch (track)
            {
                case MusicTrack.Lobby: return lobbyMusic;
                case MusicTrack.Match: return matchMusic;
                case MusicTrack.Boss: return bossMusic;
                default: return null;
            }
        }

        public bool IsBossWave(int wave) => wave > 0 && wave % bossWaveInterval == 0;
    }
}
