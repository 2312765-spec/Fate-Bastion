using System;
using System.Collections.Generic;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FateBastion.Core;
using FateBastion.Game;
using FateBastion.Heroes;

namespace FateBastion.UI
{
    /// <summary>
    /// Match HUD on C's HUD_Match (S10, B-14): wave and countdown, Castle bar, skip, speed, settings, gold, army size, the
    /// five deck slots, the meteor slot and the hero info panel. Everything is driven by events (GameEvents, HeroEvents,
    /// AbilityEvents, Wallet.OnGoldChanged, MatchDeck, PlacementSystem); the only per-frame work is the countdown, whose
    /// text changes once a second. Nodes are found by the names of C's layout (HudLayoutBuilder, C-11).
    /// </summary>
    // After GameManager.Start, which creates the wallet and the deck.
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class HudController : MonoBehaviour
    {
        private const float ToastSeconds = 1.6f;

        [Tooltip("Optional: opens the settings screen from the HUD button. Empty = found in the scene.")]
        [SerializeField] private MatchMenusController _menus;

        private readonly List<DeckSlotView> _slots = new List<DeckSlotView>(MatchDeck.Size);
        private GameManager _game;
        private PlacementSystem _placement;
        private Wallet _wallet;
        private MatchDeck _deck;
        private InputRouter _router;
        private HeroInfoPanelView _heroPanel;

        private TMP_Text _waveText;
        private Image _castleFill;
        private TMP_Text _castleText;
        private GameObject _skipButton;
        private TMP_Text _speedLabel;
        private TMP_Text _goldText;
        private TMP_Text _unitText;
        private TMP_Text _toast;
        private Tween _toastTween;

        private GameState _state = GameState.Loading;
        private int _wave;
        private int _shownSeconds = -1;

        private Action<GameState> _onState;
        private Action<int> _onWaveStarted;
        private Action<int, int> _onCastle;
        private Action<MatchResult> _onEnded;
        private Action<HeroEventInfo> _onSelected;
        private Action<HeroEventInfo> _onHeroChanged;
        private Action<int> _onGold;
        private Action<int> _onSlot;
        private Action<int> _onTotal;
        private Action<int> _onPreview;
        private Action<PlacementError> _onPlaceFailed;
        private Action<HeroLevel, UpgradeError> _onUpgradeFailed;
        private Action<float> _onSpeed;
        private Action _onUltimate;

        private void Awake()
        {
            _onState = HandleState;
            _onWaveStarted = HandleWaveStarted;
            _onCastle = HandleCastle;
            _onEnded = _ => HandleMatchEnded();
            _onSelected = info => _heroPanel?.Select(info.Hero);
            _onHeroChanged = _ => RefreshHeroPanel();
            _onGold = HandleGold;
            _onSlot = _ => RefreshSlots();
            _onTotal = HandleTotal;
            _onPreview = _ => RefreshSlots();
            _onPlaceFailed = e => ShowToast(HudFormat.Placement(e));
            _onUpgradeFailed = (_, e) => ShowToast(HudFormat.Upgrade(e));
            _onSpeed = s => SetText(_speedLabel, HudFormat.Speed(s));
            _onUltimate = () => _heroPanel?.ActivateUltimate();

            BindNodes();
        }

        private void Start()
        {
            _game = GameManager.Instance;
            if (_game == null || _game.Flow == null)
            {
                Debug.LogError($"{name}: HUD needs a GameManager that has started a match.", this);
                enabled = false;
                return;
            }

            _placement = _game.Placement;
            _wallet = _game.Wallet;
            _deck = _game.Deck;
            if (_menus == null) _menus = FindAnyObjectByType<MatchMenusController>();

            _heroPanel?.Bind(_placement);
            SetupSlots();
            Subscribe();

            HandleGold(_wallet.Gold);
            HandleTotal(_deck.TotalPlaced);
            if (_game.TimeControl != null) SetText(_speedLabel, HudFormat.Speed(_game.TimeControl.Speed));
            HandleState(_game.Flow.State);
        }

        private void OnDestroy()
        {
            Unsubscribe();
            _heroPanel?.Close();
        }

        private void Update()
        {
            // Countdown of Preparing / Resting: the text changes once a second, never every frame.
            MatchFlow flow = _game != null ? _game.Flow : null;
            if (flow == null || !flow.CanSkip) return;

            int seconds = HudFormat.ClockSeconds(flow.TimeLeft);
            if (seconds == _shownSeconds) return;
            _shownSeconds = seconds;
            RefreshWaveText();
        }

        // ---------- Nodes ----------

        private void BindNodes()
        {
            Transform t = transform;
            _waveText = UiFind.Get<TMP_Text>(t, "Top/WavePanel/WaveText", this);
            _castleFill = UiFind.Get<Image>(t, "Top/WavePanel/CastleBar/Fill", this);
            _castleText = UiFind.Optional<TMP_Text>(t, "Top/WavePanel/CastleBar/CastleText");
            _goldText = UiFind.Get<TMP_Text>(t, "Bottom/Economy/GoldText", this);
            _unitText = UiFind.Optional<TMP_Text>(t, "Bottom/Economy/UnitCountText");
            _speedLabel = UiFind.Optional<TMP_Text>(t, "TopRight/SpeedButton/Label");

            Transform skip = UiFind.Node(t, "Top/SkipWaveButton", this);
            if (skip != null) _skipButton = skip.gameObject;
            Hook(t, "Top/SkipWaveButton", () => { if (_game != null) _game.RequestSkip(); });
            Hook(t, "TopRight/SpeedButton", () => { if (_game != null && _game.TimeControl != null) _game.TimeControl.ToggleFast(); });
            Hook(t, "TopRight/SettingsButton", OpenSettings);
            Hook(t, "Bottom/MeteorSlot", () => ShowToast("Nhấn Q để ngắm Thiên thạch"));

            Transform meteor = t.Find("Bottom/MeteorSlot");
            if (meteor != null && meteor.GetComponent<CooldownSlot>() == null) meteor.gameObject.AddComponent<CooldownSlot>();

            for (int i = 0; i < MatchDeck.Size; i++)
            {
                Transform node = UiFind.Node(t, $"Bottom/DeckBar/DeckSlot_{i + 1}", this);
                if (node == null) continue;
                var view = new DeckSlotView(node);
                int slot = i;
                if (view.Button != null) view.Button.onClick.AddListener(() => ClickSlot(slot));
                _slots.Add(view);
            }

            Transform panel = UiFind.Node(t, "HeroInfoPanel", this);
            if (panel != null) _heroPanel = new HeroInfoPanelView(panel, ShowToast);

            _toast = CreateToast();
        }

        // A reason line under the wave panel, made once from the wave text so it keeps C's font and style.
        private TMP_Text CreateToast()
        {
            if (_waveText == null) return null;
            Transform top = transform.Find("Top");
            if (top == null) return null;

            var go = new GameObject("ToastText", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(top, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -230f);
            rect.sizeDelta = new Vector2(900f, 48f);

            var text = go.AddComponent<TextMeshProUGUI>();
            text.font = _waveText.font;
            text.fontSize = 28f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(1f, 0.85f, 0.45f, 1f);
            text.outlineWidth = 0.2f;
            text.raycastTarget = false;
            text.alpha = 0f;
            return text;
        }

        // ---------- Events ----------

        private void Subscribe()
        {
            GameEvents.OnStateChanged += _onState;
            GameEvents.OnWaveStarted += _onWaveStarted;
            GameEvents.OnCastleDamaged += _onCastle;
            GameEvents.OnMatchEnded += _onEnded;
            HeroEvents.OnHeroSelected += _onSelected;
            HeroEvents.OnHeroUpgraded += _onHeroChanged;
            HeroEvents.OnHeroSold += _onHeroChanged;
            _wallet.OnGoldChanged += _onGold;
            _deck.SlotChanged += _onSlot;
            _deck.TotalChanged += _onTotal;
            if (_placement != null)
            {
                _placement.PreviewChanged += _onPreview;
                _placement.PlaceFailed += _onPlaceFailed;
                _placement.UpgradeFailed += _onUpgradeFailed;
            }

            if (_game.TimeControl != null) _game.TimeControl.SpeedChanged += _onSpeed;
            _router = InputRouter.Instance;
            if (_router != null) _router.Ultimate += _onUltimate;
        }

        private void Unsubscribe()
        {
            GameEvents.OnStateChanged -= _onState;
            GameEvents.OnWaveStarted -= _onWaveStarted;
            GameEvents.OnCastleDamaged -= _onCastle;
            GameEvents.OnMatchEnded -= _onEnded;
            HeroEvents.OnHeroSelected -= _onSelected;
            HeroEvents.OnHeroUpgraded -= _onHeroChanged;
            HeroEvents.OnHeroSold -= _onHeroChanged;
            if (_wallet != null) _wallet.OnGoldChanged -= _onGold;
            if (_deck != null)
            {
                _deck.SlotChanged -= _onSlot;
                _deck.TotalChanged -= _onTotal;
            }

            if (_placement != null)
            {
                _placement.PreviewChanged -= _onPreview;
                _placement.PlaceFailed -= _onPlaceFailed;
                _placement.UpgradeFailed -= _onUpgradeFailed;
            }

            if (_game != null && _game.TimeControl != null) _game.TimeControl.SpeedChanged -= _onSpeed;
            if (_router != null) _router.Ultimate -= _onUltimate;
        }

        private void HandleState(GameState state)
        {
            _state = state;
            if (_skipButton != null)
            {
                bool canSkip = state == GameState.Preparing || state == GameState.Resting;
                if (_skipButton.activeSelf != canSkip) _skipButton.SetActive(canSkip);
            }

            _shownSeconds = -1;
            RefreshWaveText();
        }

        private void HandleWaveStarted(int wave)
        {
            _wave = wave;
            RefreshWaveText();
        }

        private void RefreshWaveText()
        {
            if (_waveText == null || _game == null || _game.Flow == null) return;
            MatchFlow flow = _game.Flow;
            _waveText.text = HudFormat.Wave(_state, Math.Max(_wave, flow.CurrentWave), flow.WaveCount, flow.IsEndless, flow.TimeLeft);
        }

        private void HandleCastle(int current, int max)
        {
            if (_castleFill != null) _castleFill.fillAmount = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
            SetText(_castleText, HudFormat.Castle(current, max));
        }

        private void HandleGold(int gold)
        {
            SetText(_goldText, HudFormat.Gold(gold));
            RefreshSlots();
            RefreshHeroPanel();
        }

        private void HandleTotal(int total)
        {
            if (_placement != null && _placement.Rules != null) SetText(_unitText, HudFormat.UnitCount(total, _placement.Rules.MaxHeroes));
            RefreshSlots();
        }

        private void HandleMatchEnded()
        {
            _heroPanel?.Close();
            if (_skipButton != null) _skipButton.SetActive(false);
        }

        private void RefreshHeroPanel()
        {
            if (_heroPanel != null && _heroPanel.IsOpen) _heroPanel.Refresh();
        }

        // ---------- Deck slots ----------

        private void SetupSlots()
        {
            PlacementRules rules = _placement != null ? _placement.Rules : null;
            for (int i = 0; i < _slots.Count; i++)
            {
                HeroData hero = i < _deck.Count ? _deck[i].HeroData : null;
                _slots[i].SetHero(hero, hero != null && rules != null ? rules.PlaceCostOf(hero.rarity) : 0);
            }

            RefreshSlots();
        }

        private void RefreshSlots()
        {
            if (_deck == null || _placement == null || _placement.Rules == null || _wallet == null) return;

            PlacementRules rules = _placement.Rules;
            for (int i = 0; i < _slots.Count && i < _deck.Count; i++)
            {
                if (!_deck.IsValidSlot(i)) continue;
                DeckSlot slot = _deck[i];
                _slots[i].SetCount(slot.PlacedCount, rules.MaxPerHeroOf(slot.HeroData.rarity));
                _slots[i].SetAvailable(_deck.CheckEconomy(rules, i, _wallet.Gold) == PlacementError.None);
                _slots[i].SetSelected(_placement.PreviewSlot == i);
            }
        }

        private void ClickSlot(int slot)
        {
            if (_placement == null) return;
            if (_placement.PreviewSlot == slot) _placement.ExitPreview();
            else _placement.EnterPreview(slot);
        }

        // ---------- Misc ----------

        private void OpenSettings()
        {
            if (_menus != null) _menus.OpenSettingsFromHud();
            else if (_game != null) _game.Pause();
        }

        /// <summary>Short reason under the wave panel (why a placement or an upgrade failed).</summary>
        public void ShowToast(string message)
        {
            if (_toast == null || string.IsNullOrEmpty(message)) return;
            _toast.text = message;
            _toastTween.Stop();
            _toast.alpha = 1f;
            _toastTween = Tween.Alpha(_toast, 0f, 0.4f, startDelay: ToastSeconds, useUnscaledTime: true);
        }

        private void Hook(Transform root, string path, UnityEngine.Events.UnityAction action)
        {
            Button button = UiFind.Optional<Button>(root, path);
            if (button != null) button.onClick.AddListener(action);
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null && text.text != value) text.text = value;
        }
    }
}
