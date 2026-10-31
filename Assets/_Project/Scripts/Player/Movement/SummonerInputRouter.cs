using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using FateBastion.Core;
using FateBastion.Heroes;

namespace FateBastion.Player
{
    /// <summary>
    /// The real InputRouter (S3): reads FateBastion.inputactions, keeps exactly one action map enabled and
    /// republishes input as InputRouter events. Esc / right tap go through CancelStack (TEAM_ASSIGNMENT 3.1 #6).
    /// Also the pointer source for meteor aiming (S7) and hero placement (S4), so nothing else reads the mouse.
    /// </summary>
    // Awake before every gameplay script that reads InputRouter.Instance in its own Awake (MeteorAimIndicator,
    // PlacementSystem, CameraRig at -100) when they share a prefab or scene; still after GameManager (-1000).
    [DefaultExecutionOrder(-900)]
    [DisallowMultipleComponent]
    public class SummonerInputRouter : InputRouter, IMeteorAimRaySource, IPointerSource
    {
        // Spec S3: a right click is a cancel tap only if the mouse moved less than this while pressed.
        // The 0.2 s limit is the Tap interaction's duration in the .inputactions asset.
        private const float TapMaxMovePixels = 5f;

        [SerializeField] private InputActionAsset _actions;

        [Tooltip("Editor only. Clicking any other Editor window also loses focus, so Alt-Tab pause is off in the Editor " +
                 "unless ticked here. Builds always pause on focus loss.")]
        [SerializeField] private bool _pauseOnFocusLostInEditor;

        [Tooltip("Camera for the meteor aim ray (Main Camera in Player.prefab). Empty = Camera.main, looked up once in Awake.")]
        [SerializeField] private Camera _pointerCamera;

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

            if (_pointerCamera == null)
            {
                _pointerCamera = Camera.main;
                if (_pointerCamera != null)
                {
                    UnityEngine.Debug.LogWarning($"{nameof(SummonerInputRouter)} on '{name}': {nameof(_pointerCamera)} is not assigned; using Camera.main.", this);
                }
            }
        }

        /// <summary>Tests and scene setup: the camera used for <see cref="TryGetAimRay"/>.</summary>
        public void SetPointerCamera(Camera pointerCamera) => _pointerCamera = pointerCamera;

        /// <summary>
        /// Ray from the camera through the mouse (S7). False while the UI map is active, the pointer is over UI,
        /// or there is no mouse / camera. Call from Update or an InputRouter event, not from inside an input callback
        /// (EventSystem cannot be queried there).
        /// </summary>
        public bool TryGetAimRay(out Ray ray)
        {
            bool hasPointer = TryGetPointerPosition(out Vector2 position);
            return PointerRay.TryBuild(_initialized && ActiveMap == ActionMapId.Summoner, hasPointer, position,
                _pointerCamera, hasPointer && IsPointerOverUi(), out ray);
        }

        /// <summary>Mouse position in screen pixels for PlacementSystem (S4); false when no mouse is connected.</summary>
        public bool TryGetPointerPosition(out Vector2 screenPosition)
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                screenPosition = default;
                return false;
            }

            screenPosition = mouse.position.ReadValue();
            return true;
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
