using FateBastion.Core;
using UnityEngine;
using UnityEngine.Audio;

namespace FateBastion.Audio
{
    /// <summary>
    /// Plays sound effects from a fixed pool of AudioSources (S11) and owns the mixer volumes. One instance per scene
    /// (inside the _Audio prefab); volumes live in static memory so they carry over when the next scene loads.
    /// </summary>
    [DisallowMultipleComponent]
    public class AudioManager : MonoBehaviour
    {
        [SerializeField] private AudioConfig _config;

        public static AudioManager Instance { get; private set; }

        // Shared by every scene's instance: Settings (B) applies the saved volumes once and later scenes keep them.
        private static readonly float[] _volumes = new float[3];
        private static bool _volumesInitialized;

        private AudioSource[] _sources;
        private SoundData[] _owners;
        private VoiceLimiter<SoundData> _limiter;
        // System.Random rather than UnityEngine.Random so tests can fix the clip and pitch picks with a seed.
        private System.Random _random = new System.Random();
        private bool _initialized;

        public AudioConfig Config => _config;
        public SoundLibrary Library => _config != null ? _config.sounds : null;

        /// <summary>False until Start: sounds requested while the scene is still loading (panels enabling) are ignored.</summary>
        public bool IsReady { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError($"{nameof(AudioManager)}: a second instance on '{name}' was disabled; keep one _Audio per scene.", this);
                enabled = false;
                return;
            }

            Instance = this;
            if (_config != null)
            {
                Initialize();
            }
            else
            {
                Debug.LogError($"{nameof(AudioManager)} on '{name}': {nameof(_config)} is not assigned.", this);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Sets the config from code and builds the pool (tests: add to an inactive object, configure, then activate).</summary>
        public void Configure(AudioConfig config, int randomSeed)
        {
            _config = config;
            _random = new System.Random(randomSeed);
            Initialize();
        }

        private void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            if (!_volumesInitialized)
            {
                _volumes[(int)AudioGroup.Master] = _config.defaultMaster;
                _volumes[(int)AudioGroup.Music] = _config.defaultMusic;
                _volumes[(int)AudioGroup.Sfx] = _config.defaultSfx;
                _volumesInitialized = true;
            }

            if (_config.mixer == null)
            {
                Debug.LogWarning($"{nameof(AudioManager)}: {nameof(AudioConfig)} has no mixer; volume sliders will do nothing.", this);
            }

            // Built once at scene load (prewarm), never during the match.
            _limiter = new VoiceLimiter<SoundData>(_config.voiceCounterCapacity);
            _sources = new AudioSource[_config.poolSize];
            _owners = new SoundData[_config.poolSize];
            for (int i = 0; i < _sources.Length; i++)
            {
                var child = new GameObject("SfxSource_" + i);
                child.transform.SetParent(transform, false);
                AudioSource source = child.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.rolloffMode = AudioRolloffMode.Linear;
                source.dopplerLevel = 0f;
                _sources[i] = source;
            }
        }

        private void Start()
        {
            // AudioMixer.SetFloat is ignored when called in Awake, so the volumes are pushed here.
            ApplyAllVolumes();
            IsReady = true;
        }

        private void Update()
        {
            if (_sources == null) return;

            for (int i = 0; i < _sources.Length; i++)
            {
                if (_owners[i] != null && !_sources[i].isPlaying)
                {
                    _limiter.Release(_owners[i]);
                    _owners[i] = null;
                }
            }
        }

        // ---------- Playback ----------

        /// <summary>Plays <paramref name="sound"/> at a world position. False when skipped (cap reached, pool full, no clip).</summary>
        public bool Play(SoundData sound, Vector3 position) => PlayInternal(sound, position, false);

        /// <summary>Plays <paramref name="sound"/> without 3D positioning (UI, alerts).</summary>
        public bool Play2D(SoundData sound) => PlayInternal(sound, Vector3.zero, true);

        /// <summary>UI cue for buttons and panels (B calls this for Error and Merge).</summary>
        public bool PlayUi(UiSound cue)
        {
            SoundLibrary library = Library;
            return library != null && Play2D(library.Get(cue));
        }

        /// <summary>Gacha reveal sound for the rarity of the pulled hero (B, S8).</summary>
        public bool PlayGachaReveal(Rarity rarity)
        {
            SoundLibrary library = Library;
            return library != null && Play2D(library.GetGachaReveal(rarity));
        }

        /// <summary>Copies of <paramref name="sound"/> playing now (counted until the pool notices they finished).</summary>
        public int CountPlaying(SoundData sound) => sound != null && _limiter != null ? _limiter.CountPlaying(sound) : 0;

        private bool PlayInternal(SoundData sound, Vector3 position, bool force2D)
        {
            if (sound == null || !sound.HasClips || _sources == null) return false;

            // Cap first, so the 5th death in a frame costs one dictionary lookup and nothing else.
            if (!_limiter.TryAcquire(sound, sound.maxConcurrent)) return false;

            int index = FindFreeSource();
            if (index < 0)
            {
                // ASSUMPTION: when all pooled sources are busy the new sound is dropped rather than stealing one.
                _limiter.Release(sound);
                return false;
            }

            AudioSource source = _sources[index];
            source.clip = sound.PickClip(_random);
            source.volume = sound.volume;
            source.pitch = sound.PickPitch(_random);
            source.outputAudioMixerGroup = sound.group == AudioGroup.Music ? _config.musicGroup : _config.sfxGroup;
            source.spatialBlend = force2D ? 0f : sound.spatialBlend;
            source.minDistance = sound.minDistance;
            source.maxDistance = sound.maxDistance;
            source.transform.position = position;
            source.Play();
            _owners[index] = sound;
            return true;
        }

        private int FindFreeSource()
        {
            for (int i = 0; i < _owners.Length; i++)
            {
                if (_owners[i] == null) return i;
            }

            return -1;
        }

        // ---------- Volume ----------

        /// <summary>Slider value 0–1 for a group. Settings (B) calls this, then saves via SaveService (S9).</summary>
        public void SetVolume(AudioGroup group, float linear)
        {
            _volumes[(int)group] = Mathf.Clamp01(linear);
            ApplyVolume(group);
        }

        public float GetVolume(AudioGroup group) => _volumes[(int)group];

        private void ApplyAllVolumes()
        {
            ApplyVolume(AudioGroup.Master);
            ApplyVolume(AudioGroup.Music);
            ApplyVolume(AudioGroup.Sfx);
        }

        private void ApplyVolume(AudioGroup group)
        {
            AudioMixer mixer = _config != null ? _config.mixer : null;
            if (mixer == null) return;

            string parameter = group == AudioGroup.Master ? _config.masterParameter
                : group == AudioGroup.Music ? _config.musicParameter
                : _config.sfxParameter;
            if (!mixer.SetFloat(parameter, VolumeMath.ToDecibels(_volumes[(int)group])))
            {
                Debug.LogWarning($"{nameof(AudioManager)}: mixer has no exposed parameter '{parameter}'.", this);
            }
        }
    }
}
