using UnityEngine;

namespace FateBastion.Enemies
{
    /// <summary>
    /// Endless mode (S2): uses the level 3 LevelData, and after wave 15 repeats waves loopFromWave..loopToWave
    /// while hpGrowth keeps applying. Wave mapping is done in A-06.
    /// </summary>
    [CreateAssetMenu(menuName = "Fate Bastion/Settings/Endless", fileName = "EndlessSettings")]
    public class EndlessSettings : ScriptableObject
    {
        [Tooltip("First wave of the level repeated after the last wave (1-based).")]
        [SerializeField, Min(1)] private int _loopFromWave = 6;

        [Tooltip("Last wave of the repeated block (1-based, inclusive).")]
        [SerializeField, Min(1)] private int _loopToWave = 15;

        public int LoopFromWave => _loopFromWave;
        public int LoopToWave => _loopToWave;

        /// <summary>For tests and tools; in the game the values are set in the Inspector.</summary>
        public void Configure(int loopFromWave, int loopToWave)
        {
            _loopFromWave = Mathf.Max(1, loopFromWave);
            _loopToWave = Mathf.Max(_loopFromWave, loopToWave);
        }
    }
}
