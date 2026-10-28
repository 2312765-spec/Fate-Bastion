using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using FateBastion.Audio;
using FateBastion.Core;

namespace FateBastion.Tests.PlayMode.S11
{
    /// <summary>Music switches between lobby, match and boss and keeps fading while paused (S11, C-13).</summary>
    public class MusicPlayerPlayModeTests
    {
        private const float FadeSeconds = 0.2f;
        private const float VolumeTolerance = 1e-3f;

        private GameObject _go;
        private MusicPlayer _player;
        private AudioConfig _config;
        private AudioClip _lobby;
        private AudioClip _match;
        private AudioClip _boss;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            GameEvents.ResetAll();

            _lobby = AudioClip.Create("lobby", 44100, 1, 44100, false);
            _match = AudioClip.Create("match", 44100, 1, 44100, false);
            _boss = AudioClip.Create("boss", 44100, 1, 44100, false);
            _config = ScriptableObject.CreateInstance<AudioConfig>();
            _config.lobbyMusic = _lobby;
            _config.matchMusic = _match;
            _config.bossMusic = _boss;
            _config.crossfadeSeconds = FadeSeconds;
            _config.musicVolume = 1f;

            _go = new GameObject("_Audio");
            _go.SetActive(false);
            _player = _go.AddComponent<MusicPlayer>();
            _player.Configure(_config, MusicTrack.Lobby);
            _go.SetActive(true);

            yield return null; // Start: subscribe and play the lobby track.
        }

        [TearDown]
        public void TearDown()
        {
            // The pause test sets the time scale; only TimeController does this in the game.
            Time.timeScale = 1f;
            GameEvents.ResetAll();
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_config);
            Object.DestroyImmediate(_lobby);
            Object.DestroyImmediate(_match);
            Object.DestroyImmediate(_boss);
        }

        [Test]
        public void Start_LobbyStartTrack_PlaysLobby() =>
            Assert.AreEqual(MusicTrack.Lobby, _player.CurrentTrack);

        [UnityTest]
        public IEnumerator Play_WhilePaused_StillCrossfades()
        {
            Time.timeScale = 0f;

            _player.Play(MusicTrack.Match);
            yield return new WaitForSecondsRealtime(FadeSeconds * 2f);

            Assert.IsFalse(_player.IsFading);
            Assert.AreEqual(MusicTrack.Match, _player.CurrentTrack);
            Assert.AreEqual(_config.musicVolume, _player.CurrentVolume, VolumeTolerance);
        }

        [Test]
        public void GameEvents_BossWaveStartedThenCleared_BossThenMatch()
        {
            GameEvents.RaiseStateChanged(GameState.InWave);
            Assert.AreEqual(MusicTrack.Match, _player.CurrentTrack);

            GameEvents.RaiseWaveStarted(5);
            Assert.AreEqual(MusicTrack.Boss, _player.CurrentTrack);

            GameEvents.RaiseWaveCleared(5);
            Assert.AreEqual(MusicTrack.Match, _player.CurrentTrack);
        }

        [Test]
        public void GameEvents_MatchWon_FadesOut()
        {
            GameEvents.RaiseStateChanged(GameState.InWave);

            GameEvents.RaiseStateChanged(GameState.Won);

            Assert.AreEqual(MusicTrack.None, _player.CurrentTrack);
        }
    }
}
