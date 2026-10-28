using FateBastion.Core;
using UnityEngine;

namespace FateBastion.Audio
{
    /// <summary>
    /// Background music (S11): lobby, match and boss tracks with a crossfade between two AudioSources. Listens to
    /// GameEvents in the match; the lobby instance just sets its start track. Runs in real time so it keeps
    /// playing and fading while the game is paused.
    /// </summary>
    // After default scripts, so a GameEvents.ResetAll in GameManager.Start does not wipe this subscription.
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public class MusicPlayer : MonoBehaviour
    {
        [SerializeField] private AudioConfig _config;

        [Tooltip("Track played when the scene starts: Lobby in Lobby.unity, None in Game.unity (match events pick it).")]
        [SerializeField] private MusicTrack _startTrack = MusicTrack.None;

        private AudioSource _incoming;
        private AudioSource _outgoing;
        private readonly MusicCrossfade _fade = new MusicCrossfade();
        private MusicDirector _director;
        private bool _started;
        private bool _subscribed;

        public MusicTrack CurrentTrack { get; private set; } = MusicTrack.None;
        public bool IsFading => _fade.IsActive;

        /// <summary>Volume factor (0–1) of the track now playing, before the mixer; for tests and debugging.</summary>
        public float CurrentVolume => _incoming != null ? _incoming.volume : 0f;

        private void Awake()
        {
            if (_config != null) ApplyConfig();
            else Debug.LogError($"{nameof(MusicPlayer)} on '{name}': {nameof(_config)} is not assigned.", this);
        }

        /// <summary>Sets the config from code (tests: add the component to an inactive object, configure, then activate).</summary>
        public void Configure(AudioConfig config, MusicTrack startTrack)
        {
            _config = config;
            _startTrack = startTrack;
            ApplyConfig();
        }

        private void ApplyConfig()
        {
            if (_incoming == null)
            {
                _incoming = CreateSource("MusicSource_A");
                _outgoing = CreateSource("MusicSource_B");
            }

            _director = new MusicDirector(_config.bossWaveInterval);
            _incoming.outputAudioMixerGroup = _config.musicGroup;
            _outgoing.outputAudioMixerGroup = _config.musicGroup;
        }

        private AudioSource CreateSource(string sourceName)
        {
            var child = new GameObject(sourceName);
            child.transform.SetParent(transform, false);
            AudioSource source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.ignoreListenerPause = true;
            source.volume = 0f;
            return source;
        }

        private void Start()
        {
            _started = true;
            Subscribe();
            if (_startTrack != MusicTrack.None) Play(_startTrack);
        }

        private void OnEnable()
        {
            if (_started) Subscribe();
        }

        private void OnDisable() => Unsubscribe();

        private void Subscribe()
        {
            if (_subscribed || _config == null) return;
            _subscribed = true;
            GameEvents.OnStateChanged += HandleStateChanged;
            GameEvents.OnWaveStarted += HandleWaveStarted;
            GameEvents.OnWaveCleared += HandleWaveCleared;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;
            GameEvents.OnStateChanged -= HandleStateChanged;
            GameEvents.OnWaveStarted -= HandleWaveStarted;
            GameEvents.OnWaveCleared -= HandleWaveCleared;
        }

        private void HandleStateChanged(GameState state) => PlayIfSet(_director.OnStateChanged(state));
        private void HandleWaveStarted(int wave) => PlayIfSet(_director.OnWaveStarted(wave));
        private void HandleWaveCleared(int wave) => PlayIfSet(_director.OnWaveCleared(wave));

        private void PlayIfSet(MusicTrack? track)
        {
            if (track.HasValue) Play(track.Value);
        }

        /// <summary>Crossfades to <paramref name="track"/>; None fades the music out. Same track again does nothing.</summary>
        public void Play(MusicTrack track)
        {
            if (_config == null || track == CurrentTrack) return;
            CurrentTrack = track;

            // The track playing now becomes the outgoing one; a track still fading out from before is cut.
            float outgoingFactor = _fade.IsActive ? _fade.IncomingVolume : 1f;
            _outgoing.Stop();
            (_incoming, _outgoing) = (_outgoing, _incoming);

            AudioClip clip = _config.GetMusic(track);
            if (clip != null)
            {
                _incoming.clip = clip;
                _incoming.volume = 0f;
                _incoming.Play();
            }
            else
            {
                if (track != MusicTrack.None)
                {
                    Debug.LogWarning($"{nameof(MusicPlayer)}: no clip assigned for {track} in {nameof(AudioConfig)}.", this);
                }

                _incoming.clip = null;
            }

            _fade.Begin(_config.crossfadeSeconds, _outgoing.isPlaying ? outgoingFactor : 0f);
            ApplyFade();
        }

        private void Update()
        {
            if (!_fade.IsActive) return;
            _fade.Tick(Time.unscaledDeltaTime);
            ApplyFade();
        }

        private void ApplyFade()
        {
            float volume = _config.musicVolume;
            _incoming.volume = _incoming.clip != null ? _fade.IncomingVolume * volume : 0f;
            _outgoing.volume = _fade.OutgoingVolume * volume;
            if (!_fade.IsActive) _outgoing.Stop();
        }
    }
}
