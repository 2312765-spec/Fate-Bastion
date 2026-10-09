using UnityEngine;
using FateBastion.Core;
using FateBastion.Combat;

namespace FateBastion.Skills
{
    /// <summary>
    /// Data of one skill: hero passives, Legendary ultimates and the Summoner's meteor all use this asset (S6).
    /// Fields written by the CSV importer are marked; the rest is assigned by hand in the Inspector.
    /// </summary>
    [CreateAssetMenu(menuName = "Fate Bastion/Skill", fileName = "skill_")]
    public class SkillData : ScriptableObject
    {
        [Header("Identity (importer: id, displayName)")]
        public string id;
        public string displayName;

        [Tooltip("Assigned by hand; the importer never overwrites it.")]
        public Sprite icon;

        [Header("Execution")]
        public SkillKind kind = SkillKind.Projectile;
        public AimMode aimMode = AimMode.Auto;

        [Tooltip("Multiplier on the hero's damage; a basic attack is 1.")]
        public float damageMultiplier = 1f;

        public DamageType damageType = DamageType.Physical;

        [Tooltip("Status effect applied on hit; null when none. Assigned by hand.")]
        public StatusEffectData status;

        [Header("Shape (importer: radius from heroes.csv areaRadius)")]
        [Tooltip("Radius for AreaAtPoint, Zone and Cone skills.")]
        public float radius;

        [Tooltip("Length of a Line skill.")]
        public float length;

        [Tooltip("Width of a Line skill.")]
        public float width;

        [Header("Timing")]
        public float projectileSpeed = 20f;

        [Tooltip("How long a Zone or Line skill stays alive, in scaled seconds.")]
        public float duration;

        [Tooltip("Cooldown in scaled seconds; 0 for passives driven by the attack timer.")]
        public float cooldown;

        [Tooltip("Delay between the cast starting and damage being applied; driven by a logic timer, not an animation event.")]
        public float castDelay;

        [Header("Buff (importer: buffType, buffValue, buffRadius)")]
        public BuffType buffType = BuffType.None;

        [Tooltip("Buff strength as a fraction, for example 0.3 for +30%.")]
        public float buffValue;

        public float buffRadius;

        [Header("Passive trigger (importer: derived from the hero row)")]
        public PassiveTrigger trigger = PassiveTrigger.None;

        [Tooltip("Used only when trigger is OnNthHit.")]
        public int nthHit = 1;

        [Header("Presentation (assigned by hand)")]
        public string animTrigger;
        public GameObject vfx;
        public AudioClip sfx;

        [Tooltip("Cinemachine Impulse amplitude; 0 means no shake.")]
        public float cameraShake;
    }
}
