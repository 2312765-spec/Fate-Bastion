using NUnit.Framework;
using FateBastion.Audio;

namespace FateBastion.Tests.EditMode.S11
{
    /// <summary>Crossfade volume curve between two music tracks (S11).</summary>
    public class MusicCrossfadeTests
    {
        private const float Tolerance = 1e-4f;

        private MusicCrossfade _fade;

        [SetUp]
        public void SetUp() => _fade = new MusicCrossfade();

        [Test]
        public void Begin_StartsWithIncomingSilentAndOutgoingFull()
        {
            _fade.Begin(duration: 2f, outgoingVolume: 1f);

            Assert.IsTrue(_fade.IsActive);
            Assert.AreEqual(0f, _fade.IncomingVolume, Tolerance);
            Assert.AreEqual(1f, _fade.OutgoingVolume, Tolerance);
        }

        [Test]
        public void Tick_HalfDuration_BothAtHalf()
        {
            _fade.Begin(duration: 2f, outgoingVolume: 1f);

            _fade.Tick(1f);

            Assert.AreEqual(0.5f, _fade.IncomingVolume, Tolerance);
            Assert.AreEqual(0.5f, _fade.OutgoingVolume, Tolerance);
        }

        [Test]
        public void Tick_PastDuration_EndsAtFullAndSilent()
        {
            _fade.Begin(duration: 2f, outgoingVolume: 1f);

            _fade.Tick(5f);

            Assert.IsFalse(_fade.IsActive);
            Assert.AreEqual(1f, _fade.IncomingVolume, Tolerance);
            Assert.AreEqual(0f, _fade.OutgoingVolume, Tolerance);
        }

        [Test]
        public void Begin_CutMidFade_OutgoingStartsFromItsCurrentVolume()
        {
            _fade.Begin(duration: 2f, outgoingVolume: 0.4f);

            _fade.Tick(1f);

            Assert.AreEqual(0.2f, _fade.OutgoingVolume, Tolerance);
        }

        [Test]
        public void Begin_ZeroDuration_SwitchesAtOnce()
        {
            _fade.Begin(duration: 0f, outgoingVolume: 1f);

            Assert.IsFalse(_fade.IsActive);
            Assert.AreEqual(1f, _fade.IncomingVolume, Tolerance);
        }
    }
}
