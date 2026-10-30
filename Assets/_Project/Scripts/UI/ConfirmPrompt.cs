using System;

namespace FateBastion.UI
{
    /// <summary>
    /// Holds the callbacks of the question a <see cref="ConfirmDialog"/> is showing and hands each one out at most once,
    /// so a double click or Esc after a click never answers the same question twice (S10).
    /// </summary>
    public sealed class ConfirmPrompt
    {
        private Action<bool> _onConfirm;
        private Action _onCancel;

        /// <summary>True between <see cref="Begin"/> and the first Take call.</summary>
        public bool IsPending { get; private set; }

        /// <summary>
        /// Starts a new question. Returns the cancel callback of a question that was still pending (it is replaced and
        /// must be answered "cancel" by the caller), otherwise null.
        /// </summary>
        public Action Begin(Action<bool> onConfirm, Action onCancel)
        {
            if (onConfirm == null)
            {
                throw new ArgumentNullException(nameof(onConfirm));
            }

            Action replaced = IsPending ? _onCancel : null;
            _onConfirm = onConfirm;
            _onCancel = onCancel;
            IsPending = true;
            return replaced;
        }

        /// <summary>Ends the question as confirmed; returns its confirm callback, or null when nothing was pending.</summary>
        public Action<bool> TakeConfirm()
        {
            if (!IsPending)
            {
                return null;
            }

            Action<bool> callback = _onConfirm;
            Clear();
            return callback;
        }

        /// <summary>Ends the question as cancelled; returns its cancel callback (may be null), or null when nothing was pending.</summary>
        public Action TakeCancel()
        {
            if (!IsPending)
            {
                return null;
            }

            Action callback = _onCancel;
            Clear();
            return callback;
        }

        private void Clear()
        {
            _onConfirm = null;
            _onCancel = null;
            IsPending = false;
        }
    }
}
