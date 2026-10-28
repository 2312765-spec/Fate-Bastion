using FateBastion.Core;

namespace FateBastion.Audio
{
    /// <summary>
    /// Picks the music track from match events (S11): match music while playing, boss music from the start of a
    /// boss wave until it is cleared, silence once the match ends. Plain C# so the rules are unit-tested.
    /// </summary>
    public class MusicDirector
    {
        private readonly int _bossWaveInterval;
        private bool _bossActive;
        private bool _matchOver;

        public MusicDirector(int bossWaveInterval)
        {
            _bossWaveInterval = bossWaveInterval < 1 ? 1 : bossWaveInterval;
        }

        /// <summary>Track wanted after the state change, or null to keep the current one.</summary>
        public MusicTrack? OnStateChanged(GameState state)
        {
            switch (state)
            {
                case GameState.Preparing:
                case GameState.InWave:
                case GameState.Resting:
                    if (_matchOver) return null;
                    return _bossActive ? MusicTrack.Boss : MusicTrack.Match;
                case GameState.Won:
                case GameState.Lost:
                    _matchOver = true;
                    _bossActive = false;
                    return MusicTrack.None;
                default:
                    return null;
            }
        }

        public MusicTrack? OnWaveStarted(int wave)
        {
            if (_matchOver || wave <= 0 || wave % _bossWaveInterval != 0) return null;
            _bossActive = true;
            return MusicTrack.Boss;
        }

        public MusicTrack? OnWaveCleared(int wave)
        {
            if (_matchOver || !_bossActive) return null;
            _bossActive = false;
            return MusicTrack.Match;
        }
    }
}
