using UnityEngine;

namespace FateBastion.Audio
{
    /// <summary>
    /// Click sound of one UI button (S11). Tools → Audio → Add UI Sounds adds it and wires <see cref="Play"/> into the
    /// Button's OnClick as a persistent listener, so the Audio assembly needs no reference to UnityEngine.UI.
    /// </summary>
    [DisallowMultipleComponent]
    public class UIButtonSound : MonoBehaviour
    {
        [Tooltip("Leave empty to use the click sound from the SoundLibrary.")]
        [SerializeField] private SoundData _override;

        /// <summary>Called by the Button's OnClick.</summary>
        public void Play()
        {
            AudioManager manager = AudioManager.Instance;
            if (manager == null) return;
            if (_override != null) manager.Play2D(_override);
            else manager.PlayUi(UiSound.Click);
        }
    }
}
