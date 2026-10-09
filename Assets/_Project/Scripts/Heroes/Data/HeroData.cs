using UnityEngine;
using FateBastion.Core;
using FateBastion.Skills;

namespace FateBastion.Heroes
{
    /// <summary>
    /// Static data of one hero. Fields id..range are written by Tools > Import Balance CSV from heroes.csv
    /// and keep the CSV column names so the importer maps them one to one. Do not edit them by hand (S0).
    /// </summary>
    [CreateAssetMenu(menuName = "Fate Bastion/Hero", fileName = "hero_")]
    public class HeroData : ScriptableObject
    {
        [Header("Written by the CSV importer")]
        public string id;
        public string displayName;
        public Rarity rarity;
        public Element element;
        public Role role;
        public DamageType damageType;
        public AttackType attackType;

        [Tooltip("Base damage per hit at level 1, before the rarity multiplier.")]
        public float damage;

        public float attacksPerSecond;

        [Tooltip("Attack range in metres, measured on the horizontal XZ plane.")]
        public float range;

        [Header("Assigned by hand; the importer never overwrites these")]
        public Sprite icon;
        public GameObject prefab;

        [Tooltip("Created and linked by the importer as '<id>_passive'.")]
        public SkillData passiveSkill;

        [Tooltip("Legendary heroes only; not part of the CSV.")]
        public SkillData ultimateSkill;
    }
}
