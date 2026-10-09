using UnityEngine;
using FateBastion.Core;

namespace FateBastion.Combat
{
    /// <summary>
    /// Data of one status effect (burn, slow, ...). STUB: S1 adds StatusEffectController and the tick logic;
    /// S0 only declares the asset so SkillData can reference it.
    /// </summary>
    [CreateAssetMenu(menuName = "Fate Bastion/Status Effect", fileName = "status_")]
    public class StatusEffectData : ScriptableObject
    {
        public string id;
        public Element element;

        [Tooltip("Total duration in scaled seconds.")]
        public float duration;

        [Tooltip("Seconds between ticks; 0 means the effect is not periodic (for example slow).")]
        public float tickInterval;

        [Tooltip("Meaning depends on the effect: burn = fraction of hit damage per second, slow = speed reduction.")]
        public float value;

        public StackRule stackRule = StackRule.StrongestWins;

        [Tooltip("Looping VFX played on the affected enemy. Assigned by hand, never by the CSV importer.")]
        public GameObject vfx;
    }
}
