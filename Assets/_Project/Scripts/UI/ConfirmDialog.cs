using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FateBastion.UI
{
    /// <summary>
    /// Yes/no question shown on top of the current screen (S10): title, message, Confirm / Cancel and an optional
    /// "Không hỏi lại" toggle. Used for skip wave, merge and selling a level-3 hero. Esc counts as Cancel.
    /// </summary>
    public class ConfirmDialog : UIScreen
    {
        [Tooltip("Manager of the canvas this dialog lives on. Empty = the UIManager found in the parents.")]
        [SerializeField] private UIManager _manager;

        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _messageText;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _cancelButton;

        [Tooltip("Optional \"Không hỏi lại\" toggle (DontAskToggle); hidden unless Show asks for it.")]
        [SerializeField] private Toggle _dontAskToggle;

        private readonly ConfirmPrompt _prompt = new ConfirmPrompt();
        private bool _setUp;

        /// <summary>Always an overlay: the screen that asked stays visible behind the dim blocker.</summary>
        public override bool IsOverlay => true;

        /// <summary>True while a question is waiting for an answer.</summary>
        public bool IsPending => _prompt.IsPending;

        /// <summary>For tests and dialogs built in code. Call before the first <see cref="Show"/>.</summary>
        public void ConfigureReferences(UIManager manager, TMP_Text titleText, TMP_Text messageText, Button confirmButton,
            Button cancelButton, Toggle dontAskToggle)
        {
            _manager = manager;
            _titleText = titleText;
            _messageText = messageText;
            _confirmButton = confirmButton;
            _cancelButton = cancelButton;
            _dontAskToggle = dontAskToggle;
        }

        private void Awake()
        {
            EnsureSetUp();
        }

        /// <summary>
        /// Opens the dialog. <paramref name="onConfirm"/> receives whether "Không hỏi lại" was ticked; the caller stores
        /// that choice (ASSUMPTION: the dialog does not save it, saving belongs to S9). A question still pending is
        /// answered "cancel" first.
        /// </summary>
        public void Show(string title, string message, Action<bool> onConfirm, Action onCancel = null,
            bool showDontAskAgain = false)
        {
            if (onConfirm == null)
            {
                Debug.LogError($"{nameof(ConfirmDialog)} on '{name}': Show needs an onConfirm callback.", this);
                return;
            }

            EnsureSetUp();
            UIManager manager = ResolveManager();
            if (manager == null)
            {
                Debug.LogError($"{nameof(ConfirmDialog)} on '{name}': no UIManager assigned or found in the parents.", this);
                return;
            }

            if (_titleText != null)
            {
                _titleText.text = title;
            }

            if (_messageText != null)
            {
                _messageText.text = message;
            }

            if (_dontAskToggle != null)
            {
                _dontAskToggle.SetIsOnWithoutNotify(false);
                _dontAskToggle.gameObject.SetActive(showDontAskAgain);
            }

            if (_cancelButton != null && !_cancelButton.gameObject.activeSelf)
            {
                _cancelButton.gameObject.SetActive(true); // A previous ShowMessage hid it.
            }

            Action replaced = _prompt.Begin(onConfirm, onCancel);
            manager.Push(this);
            replaced?.Invoke();
        }

        /// <summary>
        /// Information only (gacha "not enough gems", merge result, save warnings): the Cancel button is hidden and
        /// Confirm, Esc or closing all call <paramref name="onClose"/>.
        /// </summary>
        public void ShowMessage(string title, string message, Action onClose = null)
        {
            Show(title, message, _ => onClose?.Invoke(), onClose);
            if (_cancelButton != null && IsPending)
            {
                _cancelButton.gameObject.SetActive(false);
            }
        }

        /// <summary>Answers "confirm" (Confirm button). Does nothing when no question is pending.</summary>
        public void Confirm()
        {
            if (!_prompt.IsPending)
            {
                return;
            }

            bool dontAskAgain = _dontAskToggle != null && _dontAskToggle.gameObject.activeSelf && _dontAskToggle.isOn;
            Action<bool> callback = _prompt.TakeConfirm();

            // Closed before the callback so the callback may open this dialog again with a new question.
            Close();
            callback?.Invoke(dontAskAgain);
        }

        /// <summary>Answers "cancel" (Cancel button, Esc). Does nothing when no question is pending.</summary>
        public void Cancel()
        {
            if (!_prompt.IsPending)
            {
                return;
            }

            Action callback = _prompt.TakeCancel();
            Close();
            callback?.Invoke();
        }

        public override void OnCancelPressed()
        {
            Cancel();
        }

        protected override void OnClosed()
        {
            // Closed from outside (PopToRoot, scene change) while still asking: treat as Cancel so the caller is not left waiting.
            if (_prompt.IsPending)
            {
                _prompt.TakeCancel()?.Invoke();
            }
        }

        protected override void OnDestroy()
        {
            if (_confirmButton != null)
            {
                _confirmButton.onClick.RemoveListener(Confirm);
            }

            if (_cancelButton != null)
            {
                _cancelButton.onClick.RemoveListener(Cancel);
            }

            base.OnDestroy();
        }

        // Runs from Awake or from the first Show, whichever comes first: the dialog starts inactive in the prefab.
        private void EnsureSetUp()
        {
            if (_setUp)
            {
                return;
            }

            _setUp = true;
            BindByHierarchy();
            LogMissingReferences();

            if (_confirmButton != null)
            {
                _confirmButton.onClick.AddListener(Confirm);
            }

            if (_cancelButton != null)
            {
                _cancelButton.onClick.AddListener(Cancel);
            }
        }

        // Empty fields are found by the node names of C's ConfirmDialog prefab (C-12), so the component can simply be
        // added to the prefab without dragging every reference.
        private void BindByHierarchy()
        {
            if (_titleText == null) _titleText = FindChild<TMP_Text>("Panel/TitleText");
            if (_messageText == null) _messageText = FindChild<TMP_Text>("Panel/MessageText");
            if (_confirmButton == null) _confirmButton = FindChild<Button>("Panel/Buttons/ConfirmButton");
            if (_cancelButton == null) _cancelButton = FindChild<Button>("Panel/Buttons/CancelButton");
            if (_dontAskToggle == null) _dontAskToggle = FindChild<Toggle>("Panel/DontAskToggle");
        }

        private T FindChild<T>(string path) where T : Component
        {
            Transform node = transform.Find(path);
            return node != null ? node.GetComponent<T>() : null;
        }

        private void LogMissingReferences()
        {
            StringBuilder missing = null;
            Append(ref missing, _titleText == null, nameof(_titleText));
            Append(ref missing, _messageText == null, nameof(_messageText));
            Append(ref missing, _confirmButton == null, nameof(_confirmButton));
            Append(ref missing, _cancelButton == null, nameof(_cancelButton));

            if (missing != null)
            {
                Debug.LogError($"{nameof(ConfirmDialog)} on '{name}': missing references {missing}. Drag them in the Inspector.", this);
            }
        }

        private static void Append(ref StringBuilder builder, bool isMissing, string field)
        {
            if (!isMissing)
            {
                return;
            }

            if (builder == null)
            {
                builder = new StringBuilder(field);
            }
            else
            {
                builder.Append(", ").Append(field);
            }
        }

        private UIManager ResolveManager()
        {
            if (_manager == null)
            {
                _manager = GetComponentInParent<UIManager>(true);
            }

            return _manager;
        }
    }
}
