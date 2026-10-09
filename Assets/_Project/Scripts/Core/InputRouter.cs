using System;
using UnityEngine;

namespace FateBastion.Core
{
    /// <summary>
    /// Single place that reads input and republishes it as events; no other system may read keys (S3).
    /// S0 only declares the surface: the Input System wiring is added in S3.
    /// </summary>
    public class InputRouter : MonoBehaviour
    {
        public static InputRouter Instance { get; private set; }

        /// <summary>Exactly one action map is active at a time (S3).</summary>
        public ActionMapId ActiveMap { get; protected set; } = ActionMapId.Summoner;

        // Movement and camera (consumed by PlayerController and CameraRig, unscaled time).
        public event Action<Vector2> Move;
        public event Action<bool> SprintChanged;
        public event Action<Vector2> Look;
        public event Action<float> Zoom;

        // Pointer actions.
        public event Action Select;

        /// <summary>Right mouse tap or Esc; handled by priority order in S3.</summary>
        public event Action Cancel;

        // Hero and ability actions.
        /// <summary>Deck slot index 0..4 (keys 1-5).</summary>
        public event Action<int> DeckSlot;
        public event Action Meteor;
        public event Action Upgrade;
        public event Action Sell;
        public event Action Ultimate;
        public event Action SkipWave;
        public event Action DebugPanel;

        public event Action<ActionMapId> ActionMapChanged;

        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                UnityEngine.Debug.LogError($"{nameof(InputRouter)}: a second instance on '{name}'; destroying it.", this);
                Destroy(this);
                return;
            }

            Instance = this;
        }

        protected virtual void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Switches the active action map. STUB: S3 enables and disables the real Input System maps here.
        /// </summary>
        public virtual void SetActionMap(ActionMapId map)
        {
            if (ActiveMap == map)
            {
                return;
            }

            ActiveMap = map;
            ActionMapChanged?.Invoke(map);
        }

        protected void RaiseMove(Vector2 value) => Move?.Invoke(value);
        protected void RaiseSprintChanged(bool value) => SprintChanged?.Invoke(value);
        protected void RaiseLook(Vector2 value) => Look?.Invoke(value);
        protected void RaiseZoom(float value) => Zoom?.Invoke(value);
        protected void RaiseSelect() => Select?.Invoke();
        protected void RaiseCancel() => Cancel?.Invoke();
        protected void RaiseDeckSlot(int index) => DeckSlot?.Invoke(index);
        protected void RaiseMeteor() => Meteor?.Invoke();
        protected void RaiseUpgrade() => Upgrade?.Invoke();
        protected void RaiseSell() => Sell?.Invoke();
        protected void RaiseUltimate() => Ultimate?.Invoke();
        protected void RaiseSkipWave() => SkipWave?.Invoke();
        protected void RaiseDebugPanel() => DebugPanel?.Invoke();
    }
}
