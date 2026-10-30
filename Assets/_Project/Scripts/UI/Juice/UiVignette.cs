using UnityEngine;
using UnityEngine.UI;

namespace FateBastion.UI.Juice
{
    /// <summary>Darkens the screen edges with a generated vignette so the eye goes to the centre.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RawImage))]
    [AddComponentMenu("Fate Bastion/UI/Vignette")]
    public class UiVignette : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float _strength = 0.85f;

        private void Awake()
        {
            var image = GetComponent<RawImage>();
            image.texture = UiProceduralSprites.Vignette;
            image.color = new Color(1f, 1f, 1f, _strength);
            image.raycastTarget = false;
        }
    }
}
