using System.Collections.Generic;
using UnityEngine;

namespace FateBastion.Heroes
{
    /// <summary>
    /// Visual contract of a hero model: the only way gameplay code reaches into a model (S0b).
    /// Holds references only – no gameplay logic, no Update. Put it on the root of the model under ModelRoot.
    /// </summary>
    [DisallowMultipleComponent]
    public class HeroVisual : MonoBehaviour
    {
        // Names reported by validation; they match the property names in Spec S0b and the Inspector labels.
        public const string MuzzleField = "Muzzle";
        public const string OverheadField = "Overhead";
        public const string AuraAnchorField = "AuraAnchor";
        public const string AnimatorField = "Animator";

        [Tooltip("Projectile spawn point and start of cast effects (bow tip, hand, dragon mouth).")]
        [SerializeField] private Transform _muzzle;

        [Tooltip("Anchor above the head for health bar, name label and damage numbers.")]
        [SerializeField] private Transform _overhead;

        [Tooltip("Anchor at the feet, on the ground, for the rarity aura and level 3 effect.")]
        [SerializeField] private Transform _auraAnchor;

        [Tooltip("Animator of the model; HeroController scales clip speed by attack speed. May sit on the prefab root.")]
        [SerializeField] private Animator _animator;

        public Transform Muzzle => _muzzle;
        public Transform Overhead => _overhead;
        public Transform AuraAnchor => _auraAnchor;
        public Animator Animator => _animator;

        /// <summary>Appends the Spec name of every unassigned field to <paramref name="into"/> and returns how many were missing.</summary>
        public int CollectMissingFields(List<string> into)
        {
            int before = into.Count;
            if (_muzzle == null) into.Add(MuzzleField);
            if (_overhead == null) into.Add(OverheadField);
            if (_auraAnchor == null) into.Add(AuraAnchorField);
            if (_animator == null) into.Add(AnimatorField);
            return into.Count - before;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            var missing = new List<string>(4);
            if (CollectMissingFields(missing) > 0)
            {
                Debug.LogWarning($"[HeroVisual] '{name}' is missing: {string.Join(", ", missing)}. " +
                                 "Run Tools > Validate Hero Prefabs (S0b).", this);
            }
        }
#endif
    }
}
