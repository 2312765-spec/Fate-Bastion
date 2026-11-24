using UnityEngine;

namespace FateBastion.Editor.UI
{
    /// <summary>
    /// Art settings of Tools > Map > Dress Map_Level1 (C-17): which Kenney models go where, how dense and how big.
    /// Editor-only: lives in Data/Settings but nothing at runtime references it. Same seed = same layout.
    /// </summary>
    [CreateAssetMenu(menuName = "Fate Bastion/Settings/Map Dressing Settings", fileName = "MapDressingSettings")]
    public class MapDressingSettings : ScriptableObject
    {
        [Tooltip("Seed of every scatter; change it for a different but still repeatable layout.")]
        public int seed = 17;

        [Header("Play area (GDD 3: ~60 x 60 m, walls at ±30.5 m)")]
        [Tooltip("Half size of the playable square; small decor stays inside, trees stay outside.")]
        public float playHalfSize = 30f;
        [Tooltip("Half size of the outer scenery square (forest, big rocks); fog hides what is beyond.")]
        public float sceneryHalfSize = 62f;
        [Tooltip("Decor keeps this far from the road edge (road is 3 m wide, so centre-line distance = 1.5 + this).")]
        public float roadClearance = 0.8f;
        [Tooltip("Margin around obstacles, the Gate and the Castle with no decor.")]
        public float blockerMargin = 1f;

        [Header("Forest outside the walls")]
        public GameObject[] trees;
        public int forestAttempts = 1200;
        public float forestSpacing = 4.5f;
        public Vector2 treeScale = new Vector2(3.2f, 5f);
        public GameObject[] outerRocks;
        public int outerRockAttempts = 160;
        public float outerRockSpacing = 7f;
        public Vector2 outerRockScale = new Vector2(3.5f, 7f);

        [Header("Small decor inside the play area (no colliders)")]
        public GameObject[] decor;
        public int decorAttempts = 1500;
        public float decorSpacing = 2f;
        public Vector2 decorScale = new Vector2(2f, 3f);

        [Header("Pebbles along the road edge")]
        public GameObject[] pebbles;
        public int pebbleAttempts = 900;
        public float pebbleSpacing = 1.6f;
        public Vector2 pebbleScale = new Vector2(1.5f, 2.5f);

        [Header("Landmarks and obstacles")]
        [Tooltip("Rock models dressed over the graybox Rock_* obstacle boxes.")]
        public GameObject[] obstacleRocks;
        public GameObject[] waterEdge;

        [Header("Materials")]
        public Color grassColor = new Color(0.42f, 0.62f, 0.30f);
        public Color dirtColor = new Color(0.62f, 0.48f, 0.33f);
        public Color waterColor = new Color(0.25f, 0.52f, 0.70f);

        [Header("Sun")]
        public Vector3 sunEuler = new Vector3(50f, -40f, 0f);
        public Color sunColor = new Color(1f, 0.95f, 0.85f);
        public float sunIntensity = 1.4f;
        [Range(0f, 1f)] public float sunShadowStrength = 0.75f;
    }
}
