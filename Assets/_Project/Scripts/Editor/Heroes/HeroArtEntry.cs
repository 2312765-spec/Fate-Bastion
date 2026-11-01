using System;
using UnityEngine;

namespace FateBastion.Editor.Heroes
{
    /// <summary>Art of one hero for Tools > Heroes > Build Hero Variants (C-08): model, clips, anchors and projectile.</summary>
    [Serializable]
    public class HeroArtEntry
    {
        [Tooltip("HeroData.id this art belongs to (heroes.csv).")]
        public string heroId;

        [Tooltip("Suffix of the Variant name: Hero_<variantName>.prefab in Prefabs/Heroes.")]
        public string variantName;

        [Tooltip("Imported model (FBX). Empty = hero skipped until the model exists.")]
        public GameObject model;

        [Tooltip("Animator Override Controller of AC_Hero_Base with this hero's Idle/Attack/Spawn/Ultimate clips.")]
        public AnimatorOverrideController animations;

        [Tooltip("Uniform scale of ModelRoot. Models are exported at final size (1.8 m, Legendary 2.34 m), so usually 1.")]
        [Min(0.01f)] public float modelScale = 1f;

        [Tooltip("Bone the Muzzle follows. LastBone = no bone: Muzzle sits in front of the model at the fallback height.")]
        public HumanBodyBones muzzleBone = HumanBodyBones.RightHand;

        [Tooltip("Offset from the bone (or fallback point) in hero space, metres; the hero faces +Z.")]
        public Vector3 muzzleOffset;

        [Tooltip("Projectile_* prefab for SkillRunner. Empty = keep the one on Hero_Base.")]
        public GameObject projectile;
    }
}
