using System.Collections.Generic;
using UnityEngine;

namespace FateBastion.Editor.Heroes
{
    /// <summary>
    /// Art table read by Tools > Heroes > Build Hero Variants (C-08). Editor-only: lives in Data/Settings but nothing at
    /// runtime references it. Tweak anchors here, not in the Variant, because a rebuild replaces the model subtree.
    /// </summary>
    [CreateAssetMenu(menuName = "Fate Bastion/Settings/Hero Art Catalog", fileName = "HeroArtCatalog")]
    public class HeroArtCatalog : ScriptableObject
    {
        [Tooltip("Gap between the top of the model bounds and Overhead, metres.")]
        [Min(0f)] public float overheadMargin = 0.3f;

        [Tooltip("Muzzle height as a fraction of the model height when the model has no Humanoid bone to follow.")]
        [Range(0f, 1f)] public float fallbackMuzzleHeight = 0.6f;

        public List<HeroArtEntry> heroes = new List<HeroArtEntry>();

        /// <summary>Entry for <paramref name="heroId"/>, or null.</summary>
        public HeroArtEntry Find(string heroId)
        {
            for (int i = 0; i < heroes.Count; i++)
            {
                if (heroes[i] != null && heroes[i].heroId == heroId) return heroes[i];
            }

            return null;
        }
    }
}
