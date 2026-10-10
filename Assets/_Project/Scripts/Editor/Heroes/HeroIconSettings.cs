using UnityEngine;

namespace FateBastion.Editor.Heroes
{
    /// <summary>
    /// Camera and light setup shared by every icon of Tools > Render Hero Icons so the 9 icons look consistent (S0b).
    /// Editor-only: lives in Data/Settings but nothing at runtime references it.
    /// </summary>
    [CreateAssetMenu(menuName = "Fate Bastion/Settings/Hero Icon Settings", fileName = "HeroIconSettings")]
    public class HeroIconSettings : ScriptableObject
    {
        [Tooltip("Icon width and height in pixels (S0b: 256).")]
        public int size = 256;

        [Header("Camera")]
        [Tooltip("Rotation around the hero, degrees. 0 = camera in front of the hero looking at its face (hero faces +Z).")]
        public float yaw = 20f;

        [Tooltip("Camera tilt down, degrees.")]
        public float pitch = 12f;

        public float fieldOfView = 25f;

        [Tooltip("1 = model bounds just fit the frame; larger leaves more margin.")]
        public float distanceMultiplier = 1.15f;

        [Tooltip("Aim point as a fraction of the model height (0 = feet, 1 = top).")]
        [Range(0f, 1f)] public float lookAtHeight = 0.55f;

        [Header("Lights")]
        public Vector3 keyLightEuler = new Vector3(40f, -30f, 0f);
        public float keyLightIntensity = 1.2f;
        public Vector3 fillLightEuler = new Vector3(340f, 160f, 0f);
        public float fillLightIntensity = 0.5f;
    }
}
