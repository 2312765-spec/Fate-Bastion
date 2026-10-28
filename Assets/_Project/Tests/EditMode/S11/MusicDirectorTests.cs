using NUnit.Framework;
using FateBastion.Audio;
using FateBastion.Core;

namespace FateBastion.Tests.EditMode.S11
{
    /// <summary>Which track plays when: match, boss wave, end of match (S11).</summary>
    public class MusicDirectorTests
    {
        private MusicDirector _director;

        [SetUp]
        public void SetUp() => _director = new MusicDirector(bossWaveInterval: 5);

        [Test]
        public void OnStateChanged_Preparing_PlaysMatchTrack() =>
            Assert.AreEqual(MusicTrack.Match, _director.OnStateChanged(GameState.Preparing));

        [Test]
        public void OnWaveStarted_NormalWave_KeepsTrack() =>
            Assert.IsNull(_director.OnWaveStarted(4));

        [Test]
        public void OnWaveStarted_Wave10_SwitchesToBossTrack() =>
            Assert.AreEqual(MusicTrack.Boss, _director.OnWaveStarted(10));

        [Test]
        public void OnWaveStarted_EndlessWave20_SwitchesToBossTrack() =>
            Assert.AreEqual(MusicTrack.Boss, _director.OnWaveStarted(20));

        [Test]
        public void OnStateChanged_InWaveDuringBossWave_StaysOnBoss()
        {
            _director.OnWaveStarted(5);

            Assert.AreEqual(MusicTrack.Boss, _director.OnStateChanged(GameState.InWave));
        }

        [Test]
        public void OnWaveCleared_BossWave_BackToMatchTrack()
        {
            _director.OnWaveStarted(5);

            Assert.AreEqual(MusicTrack.Match, _director.OnWaveCleared(5));
            Assert.AreEqual(MusicTrack.Match, _director.OnStateChanged(GameState.Resting));
        }

        [Test]
        public void OnWaveCleared_NormalWave_KeepsTrack()
        {
            _director.OnWaveStarted(3);

            Assert.IsNull(_director.OnWaveCleared(3));
        }

        [Test]
        public void OnStateChanged_Lost_FadesOutAndIgnoresLaterEvents()
        {
            _director.OnWaveStarted(15);

            Assert.AreEqual(MusicTrack.None, _director.OnStateChanged(GameState.Lost));
            Assert.IsNull(_director.OnWaveCleared(15));
            Assert.IsNull(_director.OnStateChanged(GameState.Resting));
        }
    }
}
