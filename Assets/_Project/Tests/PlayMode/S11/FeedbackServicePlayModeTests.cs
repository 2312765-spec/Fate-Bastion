using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using FateBastion.Audio;
using FateBastion.Core;

namespace FateBastion.Tests.PlayMode.S11
{
    /// <summary>Camera shake on meteor, boss wave and castle damage; the Settings toggle stops it at once (S11, C-18).</summary>
    public class FeedbackServicePlayModeTests
    {
        // Short real-time wait so the impulse is under way when sampled.
        private const float SampleDelay = 0.05f;

        private GameObject _go;
        private FeedbackService _service;
        private FeedbackSettings _settings;
        private AudioConfig _audioConfig;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            GameEvents.ResetAll();
            AbilityEvents.ResetAll();
            FeedbackService.SetShakeEnabled(false); // Also clears impulses left by another test.
            FeedbackService.SetShakeEnabled(true);

            _settings = ScriptableObject.CreateInstance<FeedbackSettings>();
            _audioConfig = ScriptableObject.CreateInstance<AudioConfig>();
            _audioConfig.bossWaveInterval = 5;

            _go = new GameObject("_Audio");
            _go.SetActive(false);
            _service = _go.AddComponent<FeedbackService>();
            _service.Configure(_settings, _audioConfig);
            _go.SetActive(true);

            yield return null; // Start: subscribe.
        }

        [TearDown]
        public void TearDown()
        {
            FeedbackService.SetShakeEnabled(false);
            FeedbackService.SetShakeEnabled(true); // Static: leave the default for the next test.
            GameEvents.ResetAll();
            AbilityEvents.ResetAll();
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_settings);
            Object.DestroyImmediate(_audioConfig);
        }

        [UnityTest]
        public IEnumerator MeteorExploded_ShakeOn_ShakesListener()
        {
            AbilityEvents.RaiseMeteorExploded(Vector3.zero, 4f);
            yield return new WaitForSecondsRealtime(SampleDelay);

            Assert.IsTrue(FeedbackService.IsShakingAt(Vector3.zero));
        }

        [UnityTest]
        public IEnumerator MeteorExploded_ShakeOff_NoShake()
        {
            FeedbackService.SetShakeEnabled(false);

            AbilityEvents.RaiseMeteorExploded(Vector3.zero, 4f);
            yield return new WaitForSecondsRealtime(SampleDelay);

            Assert.IsFalse(FeedbackService.IsShakingAt(Vector3.zero));
        }

        [UnityTest]
        public IEnumerator SetShakeEnabled_FalseWhileShaking_StopsImmediately()
        {
            GameEvents.RaiseWaveStarted(5); // Boss rumble lasts 1.5 s.
            yield return new WaitForSecondsRealtime(SampleDelay);
            Assert.IsTrue(FeedbackService.IsShakingAt(Vector3.zero));

            FeedbackService.SetShakeEnabled(false);

            Assert.IsFalse(FeedbackService.IsShakingAt(Vector3.zero));
        }

        [UnityTest]
        public IEnumerator WaveStarted_BossWave_Shakes()
        {
            GameEvents.RaiseWaveStarted(10);
            yield return new WaitForSecondsRealtime(SampleDelay);

            Assert.IsTrue(FeedbackService.IsShakingAt(Vector3.zero));
        }

        [UnityTest]
        public IEnumerator WaveStarted_NormalWave_NoShake()
        {
            GameEvents.RaiseWaveStarted(4);
            yield return new WaitForSecondsRealtime(SampleDelay);

            Assert.IsFalse(FeedbackService.IsShakingAt(Vector3.zero));
        }

        [Test]
        public void Shake_CastleHitTwiceAtOnce_SecondBlocked()
        {
            Assert.IsTrue(_service.Shake(ShakeKind.CastleHit, Vector3.zero));
            Assert.IsFalse(_service.Shake(ShakeKind.CastleHit, Vector3.zero));
        }

        [UnityTest]
        public IEnumerator Shake_WhilePaused_StillShakes()
        {
            // Only TimeController changes the time scale in the game; the test pauses to check real-time impulses.
            Time.timeScale = 0f;
            try
            {
                _service.Shake(ShakeKind.MeteorExplosion, Vector3.zero);
                yield return new WaitForSecondsRealtime(SampleDelay);

                Assert.IsTrue(FeedbackService.IsShakingAt(Vector3.zero));
            }
            finally
            {
                Time.timeScale = 1f;
            }
        }
    }
}
