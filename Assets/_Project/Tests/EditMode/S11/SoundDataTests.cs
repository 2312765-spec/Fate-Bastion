using NUnit.Framework;
using UnityEngine;
using FateBastion.Audio;

namespace FateBastion.Tests.EditMode.S11
{
    /// <summary>Random clip and ±pitch picks of a SoundData (S11).</summary>
    public class SoundDataTests
    {
        private SoundData _sound;
        private AudioClip[] _clips;

        [SetUp]
        public void SetUp()
        {
            _clips = new[]
            {
                AudioClip.Create("a", 441, 1, 44100, false),
                AudioClip.Create("b", 441, 1, 44100, false),
                AudioClip.Create("c", 441, 1, 44100, false),
            };
            _sound = ScriptableObject.CreateInstance<SoundData>();
            _sound.clips = _clips;
            _sound.pitchVariance = 0.1f;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_sound);
            foreach (AudioClip clip in _clips) Object.DestroyImmediate(clip);
        }

        [Test]
        public void PickClip_FixedSeed_Deterministic()
        {
            var first = new System.Random(42);
            var second = new System.Random(42);

            for (int i = 0; i < 20; i++) Assert.AreSame(_sound.PickClip(first), _sound.PickClip(second));
        }

        [Test]
        public void PickClip_ManyPicks_UsesEveryClip()
        {
            var random = new System.Random(1);
            var seen = new System.Collections.Generic.HashSet<AudioClip>();

            for (int i = 0; i < 100; i++) seen.Add(_sound.PickClip(random));

            Assert.AreEqual(_clips.Length, seen.Count);
        }

        [Test]
        public void PickClip_EmptyList_ReturnsNull()
        {
            _sound.clips = new AudioClip[0];

            Assert.IsNull(_sound.PickClip(new System.Random(1)));
        }

        [Test]
        public void PickPitch_Variance10Percent_StaysWithinRange()
        {
            var random = new System.Random(7);

            for (int i = 0; i < 1000; i++)
            {
                float pitch = _sound.PickPitch(random);
                Assert.That(pitch, Is.InRange(0.9f, 1.1f));
            }
        }
    }
}
