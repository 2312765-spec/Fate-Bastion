using UnityEngine;

namespace FateBastion.Game
{
    /// <summary>Gems of an Endless run (S5): GemsPerStep for every WaveStep waves reached (+10 per 5 waves).</summary>
    [CreateAssetMenu(menuName = "Fate Bastion/Settings/Endless Rewards", fileName = "EndlessRewardSettings")]
    public class EndlessRewardSettings : ScriptableObject
    {
        [SerializeField, Min(0)] private int _gemsPerStep = 10;
        [SerializeField, Min(1)] private int _waveStep = 5;

        public int GemsPerStep => _gemsPerStep;
        public int WaveStep => _waveStep;

        /// <summary>For tests and tools; in the game the values come from the asset.</summary>
        public void Configure(int gemsPerStep, int waveStep)
        {
            _gemsPerStep = Mathf.Max(0, gemsPerStep);
            _waveStep = Mathf.Max(1, waveStep);
        }
    }
}
