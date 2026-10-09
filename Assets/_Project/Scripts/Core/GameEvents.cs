using System;

namespace FateBastion.Core
{
    /// <summary>Match-flow event hub (S5). Publishers raise, UI and Audio only subscribe.</summary>
    public static class GameEvents
    {
        public static event Action<GameState> OnStateChanged;
        public static event Action<int> OnWaveStarted;
        public static event Action<int> OnWaveCleared;

        /// <summary>Castle took damage: current HP and max HP, so the HUD never has to read the Castle.</summary>
        public static event Action<int, int> OnCastleDamaged;
        public static event Action<MatchResult> OnMatchEnded;

        public static void RaiseStateChanged(GameState state) => OnStateChanged?.Invoke(state);
        public static void RaiseWaveStarted(int wave) => OnWaveStarted?.Invoke(wave);
        public static void RaiseWaveCleared(int wave) => OnWaveCleared?.Invoke(wave);
        public static void RaiseCastleDamaged(int currentHP, int maxHP) => OnCastleDamaged?.Invoke(currentHP, maxHP);
        public static void RaiseMatchEnded(in MatchResult result) => OnMatchEnded?.Invoke(result);

        /// <summary>
        /// Drops every subscriber. Static events survive scene loads and test cases, so the match
        /// bootstrap and test TearDown must call this to avoid leaking handlers into the next run.
        /// </summary>
        public static void ResetAll()
        {
            OnStateChanged = null;
            OnWaveStarted = null;
            OnWaveCleared = null;
            OnCastleDamaged = null;
            OnMatchEnded = null;
        }
    }
}
