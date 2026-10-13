#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using FateBastion.Core;

namespace FateBastion.Player
{
    /// <summary>
    /// Sandbox helper: logs InputRouter events so S3 can be checked by hand (Esc order, right tap, Select over UI).
    /// Can also push fake cancelable modes with the buttons in its context menu. Not used in the Game scene.
    /// </summary>
    public class InputEventLogger : MonoBehaviour
    {
        private sealed class FakeMode : ICancelable
        {
            private readonly string _name;

            public FakeMode(string name) => _name = name;

            public void OnCancel() => UnityEngine.Debug.Log($"[S3] CancelStack cancelled: {_name}");
        }

        [Tooltip("Sandbox only: cap the frame rate to check movement is the same at 60 and 30 FPS; -1 = no cap. Turn Game view VSync off.")]
        [SerializeField] private int _targetFrameRate = -1;

        private readonly FakeMode _heroPanel = new FakeMode("hero info panel");
        private readonly FakeMode _meteorAim = new FakeMode("meteor aim");
        private InputRouter _router;

        private void Start()
        {
            CancelStack.ResetAll();
            Application.targetFrameRate = _targetFrameRate;
            _router = InputRouter.Instance;
            if (_router == null)
            {
                UnityEngine.Debug.LogError($"{nameof(InputEventLogger)}: no InputRouter in the scene.", this);
                return;
            }

            _router.Select += OnSelect;
            _router.Cancel += OnCancel;
            _router.PauseRequested += OnPause;
            _router.DeckSlot += OnDeck;
            _router.Meteor += OnMeteor;
            _router.ActionMapChanged += OnMapChanged;
        }

        private void OnDestroy()
        {
            if (_router == null) return;
            _router.Select -= OnSelect;
            _router.Cancel -= OnCancel;
            _router.PauseRequested -= OnPause;
            _router.DeckSlot -= OnDeck;
            _router.Meteor -= OnMeteor;
            _router.ActionMapChanged -= OnMapChanged;
        }

        private void OnSelect() => UnityEngine.Debug.Log("[S3] Select (world)");
        private void OnCancel() => UnityEngine.Debug.Log("[S3] Cancel (raw)");
        private void OnDeck(int slot) => UnityEngine.Debug.Log($"[S3] DeckSlot {slot}");
        private void OnMapChanged(ActionMapId map) => UnityEngine.Debug.Log($"[S3] Action map → {map}");

        // Q pushes "meteor aim", like MeteorAbility (A) will.
        private void OnMeteor()
        {
            CancelStack.Push(_meteorAim);
            UnityEngine.Debug.Log("[S3] Meteor → pushed meteor aim");
        }

        // Pause screen stand-in: switch to the UI map; Esc there switches back (UI Cancel).
        private void OnPause()
        {
            UnityEngine.Debug.Log("[S3] PauseRequested → UI map (Summoner input is off; press Esc to return)");
            _router.SetActionMap(ActionMapId.UI);
            _router.Cancel += BackToSummoner;
        }

        private void BackToSummoner()
        {
            _router.Cancel -= BackToSummoner;
            _router.SetActionMap(ActionMapId.Summoner);
        }

        [ContextMenu("Push hero info panel")]
        private void PushHeroPanel()
        {
            CancelStack.Push(_heroPanel);
            UnityEngine.Debug.Log("[S3] pushed hero info panel");
        }
    }
}
#endif
