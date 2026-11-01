using UnityEngine;

namespace FateBastion.Editor.Heroes
{
    /// <summary>
    /// Pure placement of the three HeroVisual anchors from the model bounds, in hero space (hero root at the origin,
    /// facing +Z). Used only by the Editor builder; gameplay still reads the anchors through HeroVisual (S0b).
    /// </summary>
    public static class HeroAnchorLayout
    {
        /// <summary>Above the top of the model, centred on the bounds.</summary>
        public static Vector3 Overhead(Bounds model, float margin) =>
            new Vector3(model.center.x, model.max.y + margin, model.center.z);

        /// <summary>On the ground under the hero root: aura rings and the level 3 effect centre on the gameplay position.</summary>
        public static Vector3 AuraAnchor() => Vector3.zero;

        /// <summary>
        /// The bone position plus offset when there is a bone; otherwise the front face of the bounds at
        /// <paramref name="fallbackHeight"/> of the model height, plus offset.
        /// </summary>
        public static Vector3 Muzzle(Bounds model, Vector3? bone, Vector3 offset, float fallbackHeight)
        {
            if (bone.HasValue) return bone.Value + offset;
            return new Vector3(model.center.x, model.min.y + model.size.y * fallbackHeight, model.max.z) + offset;
        }
    }
}
