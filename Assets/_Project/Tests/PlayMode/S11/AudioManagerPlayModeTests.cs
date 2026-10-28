using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using FateBastion.Audio;
using FateBastion.Core;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace FateBastion.Tests.PlayMode.S11
{
    /// <summary>100 enemies dying in one frame play at most 4 death sounds, without GC allocation (S11, C-13).</summary>
    public class AudioManagerPlayModeTests
    {
        private const int EnemyCount = 100;
        private const int MaxDeathVoices = 4;

        private GameObject _go;
        private AudioManager _manager;
        private AudioConfig _config;
        private SoundLibrary _library;
        private SoundData _death;
        private AudioClip _clip;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            CombatEvents.ResetAll();

            _clip = AudioClip.Create("death", 44100, 1, 44100, false); // 1 s, long enough to still be playing.
            _death = ScriptableObject.CreateInstance<SoundData>();
            _death.clips = new[] { _clip };
            _death.maxConcurrent = MaxDeathVoices;
            _library = ScriptableObject.CreateInstance<SoundLibrary>();
            _library.enemyDeath = _death;
            _config = ScriptableObject.CreateInstance<AudioConfig>();
            _config.sounds = _library;

            // Inactive while configuring, so Awake sees the config.
            _go = new GameObject("_Audio");
            _go.SetActive(false);
            _manager = _go.AddComponent<AudioManager>();
            _manager.Configure(_config, randomSeed: 1);
            _go.AddComponent<GameAudioBridge>();
            _go.SetActive(true);

            yield return null; // Start: subscribe to CombatEvents.
        }

        [TearDown]
        public void TearDown()
        {
            CombatEvents.ResetAll();
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_config);
            Object.DestroyImmediate(_library);
            Object.DestroyImmediate(_death);
            Object.DestroyImmediate(_clip);
        }

        [Test]
        public void EnemyKilled_100SameFrame_AtMost4DeathVoices()
        {
            for (int i = 0; i < EnemyCount; i++)
            {
                CombatEvents.RaiseEnemyKilled(new EnemyKilledInfo("enemy_normal", new Vector3(i, 0f, 0f), 5, false));
            }

            Assert.AreEqual(MaxDeathVoices, _manager.CountPlaying(_death));
        }

        [Test]
        public void EnemyKilled_100SameFrame_NoGCAlloc()
        {
            var info = new EnemyKilledInfo("enemy_normal", Vector3.zero, 5, false);
            CombatEvents.RaiseEnemyKilled(info); // Warm-up: first call JITs the handlers.

            Assert.That(() =>
            {
                for (int i = 0; i < EnemyCount; i++) CombatEvents.RaiseEnemyKilled(info);
            }, Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void Play_NoClips_ReturnsFalse()
        {
            SoundData empty = ScriptableObject.CreateInstance<SoundData>();

            Assert.IsFalse(_manager.Play(empty, Vector3.zero));

            Object.DestroyImmediate(empty);
        }

        [Test]
        public void SetVolume_OutOfRange_ClampedAndKept()
        {
            float before = _manager.GetVolume(AudioGroup.Sfx);

            _manager.SetVolume(AudioGroup.Sfx, 1.5f);

            Assert.AreEqual(1f, _manager.GetVolume(AudioGroup.Sfx));
            _manager.SetVolume(AudioGroup.Sfx, before); // Volumes are static and carry into the next test.
        }
    }
}
