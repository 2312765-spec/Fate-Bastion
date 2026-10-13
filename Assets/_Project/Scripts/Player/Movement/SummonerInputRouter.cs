using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using FateBastion.Core;

namespace FateBastion.Player
{
    /// <summary>
    /// The real InputRouter (S3): reads FateBastion.inputactions, keeps exactly one action map enabled and
    /// republishes input as InputRouter events. Esc / right tap go through CancelStack (TEAM_ASSIGNMENT 3.1 #6).
    /// </summary>
    [DisallowMultipleComponent]
    public class SummonerInputRouter : InputRouter
    {
        // Spec S3: a right click is a cancel tap only if the mouse moved less than this while pressed.
        // The 0.2 s limit is the Tap interaction's duration in the .inputactions asset.
        private const float TapMaxMovePixels = 5f;

        [SerializeField] private InputActionAsset _actions;

        [Tooltip("Editor only. Clicking any other Editor window also loses focus, so Alt-Tab pause is off in the Editor " +
                 "unless ticked here. Builds always pause on focus loss.")]
        [SerializeField] private bool _pauseOnFocusLostInEditor;

        private readonly InputActionMap[] _maps = new InputActionMap[2];
        private readonly InputAction[] _deckActions = new InputAction[5];
        private readonly TapMovementGuard _tapGuard = new TapMovementGuard();

        private InputAction _move;
        private InputAction _sprint;
        private InputAction _look;
        private InputAction _lookDelta;
        private InputAction _zoom;
        private InputAction _select;
        private InputAction _cancel;
        private InputAction _meteor;
        private InputAction _upgrade;
        private InputAction _sell;
        private InputAction _ultimate;
        private InputAction _skipWave;
        private InputAction _debug;
        private InputAction _uiCancel;

        private bool _initialized;
        private bool _selectPending;

        /// <summary>True while the right mouse button is held long enough to rotate the camera.</summary>
        public bool IsLookHeld { get; private set; }

        /// <summary>Right mouse hold started / ended; CursorController hides the cursor while true. Player-only event.</summary>
        public event Action<bool> LookHeldChanged;

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this)
            {
                return;
            }

            if (_actions != null)
            {
                Initialize(_actions);
            }
        }

        private void Start()
        {
            if (Instance == this && !_initialized)
            {
                UnityEngine.Debug.LogError($"{nameof(SummonerInputRouter)} on '{name}': Input Actions asset is not assigned or invalid.", this);
            }
        }

        /// <summary>
        /// Binds to <paramref name="actions"/> and enables the current map. Called from Awake with the Inspector
        /// asset; tests call it right after AddComponent with an asset built in code.
        /// </summary>
        public void Initialize(InputActionAsset actions)
        {
            if (_initialized)
            {
                Unbind();
            }

            _actions = actions;
            if (actions == null)
            {
                return;
            }

            InputActionMap summoner = actions.FindActionMap(InputActionNames.SummonerMap);
            InputActionMap ui = actions.FindActionMap(InputActionNames.UIMap);
            if (summoner == null || ui == null)
            {
                UnityEngine.Debug.LogError(
                    $"{nameof(SummonerInputRouter)}: '{actions.name}' must contain action maps '{InputActionNames.SummonerMap}' and '{InputActionNames.UIMap}'.",
                    this);
                return;
            }

            _maps[(int)ActionMapId.Summoner] = summoner;
            _maps[(int)ActionMapId.UI] = ui;

            _move = Find(summoner, InputActionNames.Move);
            _sprint = Find(summoner, InputActionNames.Sprint);
            _look = Find(summoner, InputActionNames.Look);
            _lookDelta = Find(summoner, InputActionNames.LookDelta);
            _zoom = Find(summoner, InputActionNames.Zoom);
            _select = Find(summoner, InputActionNames.Select);
            _cancel = Find(summoner, InputActionNames.Cancel);
            _meteor = Find(summoner, InputActionNames.Meteor);
            _upgrade = Find(summoner, InputActionNames.Upgrade);
            _sell = Find(summoner, InputActionNames.Sell);
            _ultimate = Find(summoner, InputActionNames.Ultimate);
            _skipWave = Find(summoner, InputActionNames.SkipWave);
            _debug = Find(summoner, InputActionNames.Debug);
            for (int i = 0; i < _deckActions.Length; i++)
            {
                _deckActions[i] = Find(summoner, InputActionNames.DeckSlots[i]);
            }

            _uiCancel = Find(ui, InputActionNames.Cancel);

            Bind();
            _initialized = true;
            ApplyMap(ActiveMap);
        }

        private InputAction Find(InputActionMap map, string actionName)
        {
            InputAction action = map.FindAction(actionName);
            if (action == null)
            {
                UnityEngine.Debug.LogError($"{nameof(SummonerInputRouter)}: action '{map.name}/{actionName}' is missing.", this);
            }

            return action;
        }

        public override void SetActionMap(ActionMapId map)
        {
            if (_initialized)
            {
                if (ActiveMap == ActionMapId.Summoner && map != ActionMapId.Summoner)
                {
                    ReleaseHeldInput();
                }

                ApplyMap(map);
            }

            base.SetActionMap(map);
        }

        private void ApplyMap(ActionMapId map)
        {
            // Disable first so two maps are never enabled together, even for one call.
            for (int i = 0; i < _maps.Length; i++)
            {
                if (i != (int)map)
                {
                    _maps[i].Disable();
                }
            }

            _maps[(int)map].Enable();
        }

        /// <summary>Stops everything that is "held" so the Summoner does not keep walking while the map is off.</summary>
        private void ReleaseHeldInput()
        {
            RaiseMove(Vector2.zero);
            RaiseSprintChanged(false);
            SetLookHeld(false);
            _tapGuard.End();
            _selectPending = false;
        }

        private void Update()
        {
            // EventSystem cannot be queried from inside an input callback, so Select is checked here.
            if (!_selectPending)
            {
                return;
            }

            _selectPending = false;
            if (!IsPointerOverUi())
            {
                RaiseSelect();
            }
        }

        private static bool IsPointerOverUi()
        {
            EventSystem eventSystem = EventSystem.current;
            return eventSystem != null && eventSystem.IsPointerOverGameObject();
        }

        // ASSUMPTION: Alt-Tab during play asks for pause like Esc (S3 "Alt-Tab: tự tạm dừng (S5)");
        // GameManager (B) opens the pause screen from PauseRequested.
        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus || !_initialized || ActiveMap != ActionMapId.Summoner)
            {
                return;
            }

            // Keys released outside the window never send "canceled", so stop held movement in every case.
            ReleaseHeldInput();

#if UNITY_EDITOR
            if (!_pauseOnFocusLostInEditor)
            {
                return;
            }
#endif
            RaisePauseRequested();
        }

        protected override void OnDestroy()
        {
            if (_initialized)
            {
                Unbind();
                for (int i = 0; i < _maps.Length; i++)
                {
                    _maps[i]?.Disable();
                }
            }

            base.OnDestroy();
        }

        private void SetLookHeld(bool held)
        {
            if (IsLookHeld == held)
            {
                return;
            }

            IsLookHeld = held;
            LookHeldChanged?.Invoke(held);
        }

        // --- Binding --------------------------------------------------------------------------------------------

        private void Bind()
        {
            Hook(_move, OnMove, OnMove);
            Hook(_sprint, OnSprintPerformed, OnSprintCanceled);
            Hook(_look, OnLookPerformed, OnLookCanceled);
            Hook(_lookDelta, OnLookDelta, null);
            Hook(_zoom, OnZoom, null);
            Hook(_select, OnSelect, null);
            Hook(_cancel, OnCancelPerformed, OnCancelCanceled);
            if (_cancel != null) _cancel.started += OnCancelStarted;
            Hook(_meteor, OnMeteor, null);
            Hook(_upgrade, OnUpgrade, null);
            Hook(_sell, OnSell, null);
            Hook(_ultimate, OnUltimate, null);
            Hook(_skipWave, OnSkipWave, null);
            Hook(_debug, OnDebug, null);
            for (int i = 0; i < _deckActions.Length; i++)
            {
                Hook(_deckActions[i], OnDeck, null);
            }

            Hook(_uiCancel, OnUiCancel, null);
        }

        private void Unbind()
        {
            Unhook(_move, OnMove, OnMove);
            Unhook(_sprint, OnSprintPerformed, OnSprintCanceled);
            Unhook(_look, OnLookPerformed, OnLookCanceled);
            Unhook(_lookDelta, OnLookDelta, null);
            Unhook(_zoom, OnZoom, null);
            Unhook(_select, OnSelect, null);
            Unhook(_cancel, OnCancelPerformed, OnCancelCanceled);
            if (_cancel != null) _cancel.started -= OnCancelStarted;
            Unhook(_meteor, OnMeteor, null);
            Unhook(_upgrade, OnUpgrade, null);
            Unhook(_sell, OnSell, null);
            Unhook(_ultimate, OnUltimate, null);
            Unhook(_skipWave, OnSkipWave, null);
            Unhook(_debug, OnDebug, null);
            for (int i = 0; i < _deckActions.Length; i++)
            {
                Unhook(_deckActions[i], OnDeck, null);
            }

            Unhook(_uiCancel, OnUiCancel, null);
            _initialized = false;
        }

        private static void Hook(InputAction action, Action<InputAction.CallbackContext> performed,
            Action<InputAction.CallbackContext> canceled)
        {
            if (action == null) return;
            action.performed += performed;
            if (canceled != null) action.canceled += canceled;
        }

        private static void Unhook(InputAction action, Action<InputAction.CallbackContext> performed,
            Action<InputAction.CallbackContext> canceled)
        {
            if (action == null) return;
            action.performed -= performed;
            if (canceled != null) action.canceled -= canceled;
        }

        // --- Callbacks ------------------------------------------------------------------------------------------

        private void OnMove(InputAction.CallbackContext ctx) => RaiseMove(ctx.ReadValue<Vector2>());

        private void OnSprintPerformed(InputAction.CallbackContext ctx) => RaiseSprintChanged(true);

        private void OnSprintCanceled(InputAction.CallbackContext ctx) => RaiseSprintChanged(false);

        // Look = right mouse with Hold: performed once held long enough, canceled on release.
        private void OnLookPerformed(InputAction.CallbackContext ctx) => SetLookHeld(true);

        private void OnLookCanceled(InputAction.CallbackContext ctx) => SetLookHeld(false);

        private void OnLookDelta(InputAction.CallbackContext ctx)
        {
            Vector2 delta = ctx.ReadValue<Vector2>();
            _tapGuard.AddDelta(delta);
            if (IsLookHeld)
            {
                RaiseLook(delta);
            }
        }

        private void OnZoom(InputAction.CallbackContext ctx)
        {
            float value = ctx.ReadValue<float>();
            if (value != 0f)
            {
                RaiseZoom(value);
            }
        }

        private void OnSelect(InputAction.CallbackContext ctx) => _selectPending = true;

        private static bool IsMouse(InputAction.CallbackContext ctx) => ctx.control != null && ctx.control.device is Mouse;

        private void OnCancelStarted(InputAction.CallbackContext ctx)
        {
            if (IsMouse(ctx))
            {
                _tapGuard.Begin();
            }
        }

        private void OnCancelPerformed(InputAction.CallbackContext ctx)
        {
            bool isEscape = !IsMouse(ctx);
            if (!isEscape)
            {
                // Right mouse Tap: released within the Tap duration; also require little movement.
                bool isTap = _tapGuard.IsTap(TapMaxMovePixels);
                _tapGuard.End();
                if (!isTap)
                {
                    return;
                }
            }

            // Raw Cancel first: if it came after PauseRequested, a listener closing "the top screen" on Cancel
            // would close the pause screen opened by this same press.
            RaiseCancel();

            // One step per press; Esc with nothing to cancel opens the pause screen, a right tap does nothing.
            if (CancelPressRouter.Route(isEscape) == CancelPressResult.PauseRequested)
            {
                RaisePauseRequested();
            }
        }

        // Held past the Tap duration: it was a camera drag, not a cancel.
        private void OnCancelCanceled(InputAction.CallbackContext ctx)
        {
            if (IsMouse(ctx))
            {
                _tapGuard.End();
            }
        }

        private void OnDeck(InputAction.CallbackContext ctx)
        {
            for (int i = 0; i < _deckActions.Length; i++)
            {
                if (_deckActions[i] == ctx.action)
                {
                    RaiseDeckSlot(i);
                    return;
                }
            }
        }

        private void OnMeteor(InputAction.CallbackContext ctx) => RaiseMeteor();

        private void OnUpgrade(InputAction.CallbackContext ctx) => RaiseUpgrade();

        private void OnSell(InputAction.CallbackContext ctx) => RaiseSell();

        private void OnUltimate(InputAction.CallbackContext ctx) => RaiseUltimate();

        private void OnSkipWave(InputAction.CallbackContext ctx) => RaiseSkipWave();

        private void OnDebug(InputAction.CallbackContext ctx)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            RaiseDebugPanel();
#endif
        }

        // UI map: Esc only closes the top screen (UIManager listens to Cancel); no CancelStack, no pause.
        private void OnUiCancel(InputAction.CallbackContext ctx) => RaiseCancel();
    }
}
