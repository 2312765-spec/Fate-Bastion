using UnityEngine;

namespace FateBastion.Audio
{
    /// <summary>
    /// Plays the open sound when its panel is shown and the close sound when it is hidden (S11). Panels already
    /// visible when the scene loads stay quiet, as do panels torn down with the scene.
    /// </summary>
    [DisallowMultipleComponent]
    public class UIPanelSound : MonoBehaviour
    {
        [SerializeField] private bool _playOnOpen = true;
        [SerializeField] private bool _playOnClose = true;

        private void OnEnable()
        {
            if (_playOnOpen) Play(UiSound.PanelOpen);
        }

        private void OnDisable()
        {
            if (_playOnClose && gameObject.scene.isLoaded) Play(UiSound.PanelClose);
        }

        private static void Play(UiSound cue)
        {
            AudioManager manager = AudioManager.Instance;
            // Not ready = the scene is still loading and this is just the panel's initial state.
            if (manager != null && manager.IsReady) manager.PlayUi(cue);
        }
    }
}
