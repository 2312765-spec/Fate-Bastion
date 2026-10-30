using UnityEngine;
using UnityEngine.UI;

namespace FateBastion.UI.Juice
{
    /// <summary>
    /// Gives this Image the generated soft glow dot at runtime. The sprite only exists in memory, so it cannot be
    /// saved in a scene or prefab; this component assigns it when the object wakes up.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    [AddComponentMenu("Fate Bastion/UI/Soft Glow Sprite")]
    public class UiProceduralSpriteBinder : MonoBehaviour
    {
        private void Awake()
        {
            var image = GetComponent<Image>();
            image.sprite = UiProceduralSprites.SoftDot;
            image.type = Image.Type.Simple;
        }
    }
}
