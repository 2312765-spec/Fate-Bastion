using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using FateBastion.Core;
using FateBastion.Game;
using FateBastion.Heroes;
using FateBastion.Meta;

namespace FateBastion.UI.DebugTools
{
    /// <summary>
    /// F1 debug panel (S9, B-16): delete the save, add gems, grant any hero, and in a match jump to a wave, make the
    /// Castle invincible, speed ×5, add gold, win or lose now. Toggled by InputRouter.DebugPanel (never reads keys).
    /// Every member is compiled only in the Editor and Development builds; in a Release build only an empty shell is
    /// left (so scenes and prefabs keep a valid script reference) and it removes itself on Awake.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DebugPanel : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private const float DebugSpeed = 5f;
        private const int GemStep = 1000;
        private const int GoldStep = 1000;
        private const float ReferenceHeight = 1080f;

        [Tooltip("Heroes offered by \"Nhận tướng\". Empty = the ids of the save's GachaTable.")]
        [SerializeField] private HeroCatalog _heroCatalog;

        [SerializeField] private bool _visible;

        // F1 only exists in the Summoner action map; the lobby runs the UI map, so a small corner button opens the
        // panel there. STUB: bỏ khi C thêm action Debug vào Action Map UI (issue "[Cần C]").
        [Tooltip("Show a small \"Debug\" button in the corner while the panel is hidden.")]
        [SerializeField] private bool _showCornerButton = true;

        private readonly List<string> _heroIds = new List<string>();
        private Action _onToggle;
        private InputRouter _router;
        private Rect _window = new Rect(20f, 20f, 440f, 10f);
        private Vector2 _heroScroll;
        private string _waveText = "10";
        private string _status = string.Empty;

        public bool Visible
        {
            get => _visible;
            set => _visible = value;
        }

        private void Awake() => _onToggle = Toggle;

        private void OnEnable() => Subscribe();

        private void Start() => Subscribe();

        private void OnDisable()
        {
            if (_router != null) _router.DebugPanel -= _onToggle;
            _router = null;
        }

        public void Toggle() => _visible = !_visible;

        private void Subscribe()
        {
            InputRouter router = InputRouter.Instance;
            if (router == null || router == _router) return;
            if (_router != null) _router.DebugPanel -= _onToggle;
            _router = router;
            _router.DebugPanel += _onToggle;
        }

        private void OnGUI()
        {
            if (!_visible)
            {
                // F1 already works in the Summoner map (in a match), so the corner button only shows in the UI map;
                // in play the hidden panel draws nothing and adds no IMGUI controls to the frame being profiled.
                bool needsButton = _showCornerButton && (_router == null || _router.ActiveMap == ActionMapId.UI);
                if (needsButton && GUI.Button(new Rect(4f, Screen.height - 28f, 64f, 24f), "Debug")) _visible = true;
                return;
            }

            float scale = Mathf.Max(1f, Screen.height / ReferenceHeight);
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            _window = GUILayout.Window(GetInstanceID(), _window, DrawWindow, "Debug (F1)");
            GUI.matrix = previous;
        }

        private void DrawWindow(int id)
        {
            DrawSave();
            GameManager game = GameManager.Instance;
            if (game != null && game.Flow != null) DrawMatch(game);
            if (!string.IsNullOrEmpty(_status)) GUILayout.Label(_status);
            if (GUILayout.Button("Đóng")) _visible = false;
            GUI.DragWindow();
        }

        private void DrawSave()
        {
            GUILayout.Label("Save");
            MetaSession session = MetaRuntime.Session;
            if (session == null)
            {
                GUILayout.Label("Chưa có save (mở từ sảnh hoặc có GameManager).");
                return;
            }

            SaveData data = session.Data;
            GUILayout.Label($"Ngọc: {data.gems} · pity {data.pityCounter} · deck {string.Join(", ", data.deck ?? new List<string>())}");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button($"+{GemStep} Ngọc"))
            {
                DebugCommands.AddGems(data, GemStep);
                session.SaveNow();
                _status = $"+{GemStep} Ngọc";
            }

            if (GUILayout.Button("Xóa save"))
            {
                MetaRuntime.DeleteSaveAndRestart();
                _status = "Đã xóa save, tạo lại từ StarterSettings";
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }

            GUILayout.EndHorizontal();

            GUILayout.Label("Nhận tướng:");
            RefreshHeroIds(session);
            _heroScroll = GUILayout.BeginScrollView(_heroScroll, GUILayout.Height(150f));
            foreach (string heroId in _heroIds)
            {
                int copies = CollectionService.CopiesOf(data, heroId);
                if (GUILayout.Button($"{heroId}  (×{copies})"))
                {
                    DebugCommands.GrantHero(data, heroId);
                    session.SaveNow();
                    _status = $"Nhận {heroId}";
                }
            }

            GUILayout.EndScrollView();
        }

        private void DrawMatch(GameManager game)
        {
            GUILayout.Space(8f);
            MatchFlow flow = game.Flow;
            GUILayout.Label($"Trận: {flow.State} · wave {flow.CurrentWave}/{flow.WaveCount} · Vàng {game.Wallet?.Gold}");

            GUILayout.BeginHorizontal();
            GUILayout.Label("Nhảy tới wave", GUILayout.Width(110f));
            _waveText = GUILayout.TextField(_waveText, GUILayout.Width(60f));
            if (GUILayout.Button("Đi") && int.TryParse(_waveText, out int wave))
            {
                game.DebugJumpToWave(wave);
                _status = $"Wave {wave}";
            }

            GUILayout.EndHorizontal();

            if (game.Castle != null)
            {
                bool invincible = GUILayout.Toggle(game.Castle.Invincible, "Bất tử Thành");
                if (invincible != game.Castle.Invincible) game.Castle.Invincible = invincible;
            }

            TimeController time = game.TimeControl;
            if (time != null)
            {
                bool fast = GUILayout.Toggle(Mathf.Approximately(time.Speed, DebugSpeed), $"Tốc độ ×{DebugSpeed}");
                if (fast != Mathf.Approximately(time.Speed, DebugSpeed)) time.SetSpeed(fast ? DebugSpeed : TimeScaleState.NormalSpeed);
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button($"+{GoldStep} Vàng") && game.Wallet != null) game.Wallet.Add(GoldStep);
            // Goes through the real FinishMatch: gems, stars and unlocks are written to the save.
            if (GUILayout.Button("Thắng ngay (ghi thưởng)")) game.DebugEnd(true);
            if (GUILayout.Button("Thua ngay")) game.DebugEnd(false);
            GUILayout.EndHorizontal();
        }

        private void RefreshHeroIds(MetaSession session)
        {
            if (_heroIds.Count > 0) return;
            if (_heroCatalog != null)
            {
                foreach (HeroData hero in _heroCatalog.Heroes)
                {
                    if (hero != null && !string.IsNullOrEmpty(hero.id)) _heroIds.Add(hero.id);
                }
            }

            if (_heroIds.Count == 0 && session.Data.collection != null) _heroIds.AddRange(session.Data.collection.Keys);
        }
#else
        private void Awake() => Destroy(this);
#endif
    }
}
