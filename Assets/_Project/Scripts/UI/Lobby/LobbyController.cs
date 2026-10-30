using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FateBastion.Game;
using FateBastion.Heroes;
using FateBastion.Meta;

namespace FateBastion.UI
{
    /// <summary>
    /// Lobby root on Lobby_Canvas (S10, B-15): opens the save session (MetaRuntime), binds every lobby screen, and runs the
    /// top bar – back button, page title, gems from <see cref="MetaSession.Changed"/>. Screens and the dialog are found
    /// under this object when not assigned, so the component can simply be added to C's prefab.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIManager))]
    public sealed class LobbyController : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private GachaTable _gachaTable;
        [SerializeField] private StarterSettings _starterSettings;
        [SerializeField] private HeroCatalog _heroCatalog;
        [SerializeField] private MatchCatalog _matchCatalog;

        [Header("Optional: found under this object when empty")]
        [SerializeField] private ConfirmDialog _confirm;

        private UIManager _manager;
        private LobbyContext _context;
        private Button _back;
        private TMP_Text _title;
        private TMP_Text _gems;
        private MetaSession _session;

        public LobbyContext Context => _context;

        private void Awake()
        {
            _manager = GetComponent<UIManager>();
            if (_confirm == null) _confirm = GetComponentInChildren<ConfirmDialog>(true);
            if (_gachaTable == null || _starterSettings == null)
            {
                Debug.LogError($"{name}: LobbyController needs GachaTable and StarterSettings to open the save.", this);
                return;
            }

            _session = MetaRuntime.Ensure(_gachaTable, _starterSettings);
            _context = new LobbyContext(_session, _heroCatalog, _matchCatalog, _gachaTable, _manager, _confirm);

            foreach (LobbyScreen screen in GetComponentsInChildren<LobbyScreen>(true))
            {
                switch (screen)
                {
                    case LevelSelectScreen levelSelect: _context.LevelSelect = levelSelect; break;
                    case GachaScreen gacha: _context.GachaScreen = gacha; break;
                    case CollectionScreen collection: _context.Collection = collection; break;
                }
            }

            _context.Settings = GetComponentInChildren<SettingsScreen>(true);
            if (_context.Settings != null) _context.Settings.Bind(_session);
            foreach (LobbyScreen screen in GetComponentsInChildren<LobbyScreen>(true)) screen.Bind(_context);

            _back = UiFind.Optional<Button>(transform, "TopBar/BackButton");
            _title = UiFind.Optional<TMP_Text>(transform, "TopBar/TitleText");
            _gems = UiFind.Optional<TMP_Text>(transform, "TopBar/GemPanel/GemText");
            if (_back != null) _back.onClick.AddListener(() => _manager.Pop());

            _manager.TopChanged += HandleTopChanged;
            _session.Changed += HandleSaveChanged;
            HandleSaveChanged(_session.Data);
        }

        private void Start()
        {
            HandleTopChanged(_manager.Top);
            if (_context == null || MetaRuntime.LoadWarnings.Count == 0) return;

            // S9: tell the player once when the save was recovered or the deck repaired.
            _context.Message("Dữ liệu lưu đã được sửa", string.Join("\n", MetaRuntime.LoadWarnings));
            MetaRuntime.ClearLoadWarnings();
        }

        private void OnDestroy()
        {
            if (_manager != null) _manager.TopChanged -= HandleTopChanged;
            if (_session != null) _session.Changed -= HandleSaveChanged;
        }

        private void HandleSaveChanged(SaveData data)
        {
            if (_gems != null) _gems.text = UiText.Gems(data.gems);
        }

        private void HandleTopChanged(UIScreen top)
        {
            // Overlays (dialogs, gacha result) keep the title of the page under them.
            if (top == null || top.IsOverlay) return;

            if (_title != null && top is LobbyScreen page) _title.text = page.Title;
            if (_back != null) _back.gameObject.SetActive(top != _manager.RootScreen);
        }
    }
}
