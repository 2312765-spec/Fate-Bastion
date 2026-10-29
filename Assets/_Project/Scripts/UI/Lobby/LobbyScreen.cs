using FateBastion.Meta;

namespace FateBastion.UI
{
    /// <summary>
    /// Base of the lobby pages (S10, B-15). <see cref="Bind"/> is called once by LobbyController, even while the page is
    /// inactive; the page then refreshes when it opens and whenever the save changes while it is open.
    /// </summary>
    public abstract class LobbyScreen : UIScreen
    {
        private bool _listening;

        protected LobbyContext Context { get; private set; }

        /// <summary>Text of the top bar while this page is on top.</summary>
        public abstract string Title { get; }

        public void Bind(LobbyContext context)
        {
            if (Context != null) return; // Bound once: OnBind hooks buttons.
            Context = context;
            OnBind();
        }

        /// <summary>Find nodes and hook buttons here (the page may be inactive).</summary>
        protected abstract void OnBind();

        /// <summary>Redraw from <see cref="LobbyContext.Data"/>.</summary>
        protected abstract void Refresh();

        protected override void OnOpened()
        {
            if (Context == null) return;
            if (!_listening)
            {
                Context.Session.Changed += HandleChanged;
                _listening = true;
            }

            Refresh();
        }

        protected override void OnClosed()
        {
            StopListening();
        }

        protected override void OnDestroy()
        {
            StopListening();
            base.OnDestroy();
        }

        private void StopListening()
        {
            if (!_listening || Context == null) return;
            Context.Session.Changed -= HandleChanged;
            _listening = false;
        }

        private void HandleChanged(SaveData data) => Refresh();
    }
}
