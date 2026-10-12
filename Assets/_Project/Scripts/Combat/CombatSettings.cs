using UnityEngine;

namespace FateBastion.Combat
{
    /// <summary>
    /// Element rules that StatusEffectData does not carry: slow cap, boss slow effectiveness and the
    /// Lightning chain (S1). One asset at Data/Settings/CombatSettings, owned by A.
    /// </summary>
    [CreateAssetMenu(menuName = "Fate Bastion/Settings/Combat", fileName = "CombatSettings")]
    public class CombatSettings : ScriptableObject
    {
        [Header("Ice – slow")]
        [Tooltip("Maximum total slow from all sources, as a fraction (0.6 = -60% speed).")]
        [SerializeField, Range(0f, 1f)] private float _slowCap = 0.6f;

        [Tooltip("Fraction of the slow a boss actually takes (0.5 = half effect). Applied after the cap.")]
        [SerializeField, Range(0f, 1f)] private float _bossSlowEffectiveness = 0.5f;

        [Header("Lightning – chain")]
        [Tooltip("How many extra enemies the lightning jumps to after the first hit.")]
        [SerializeField, Min(0)] private int _chainTargets = 2;

        [Tooltip("Max XZ distance in metres between two consecutive enemies in the chain.")]
        [SerializeField, Min(0f)] private float _chainRadius = 3f;

        [Tooltip("Damage lost per jump, multiplicative (0.3 = 100 -> 70 -> 49).")]
        [SerializeField, Range(0f, 1f)] private float _chainFalloff = 0.3f;

        public float SlowCap => _slowCap;
        public float BossSlowEffectiveness => _bossSlowEffectiveness;
        public int ChainTargets => _chainTargets;
        public float ChainRadius => _chainRadius;
        public float ChainFalloff => _chainFalloff;
    }
}
