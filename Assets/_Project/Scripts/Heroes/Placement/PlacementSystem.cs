using System;
using UnityEngine;
using FateBastion.Core;

namespace FateBastion.Heroes
{
    /// <summary>
    /// In-match placement, upgrade and sell (S4, B-02). Idle or Previewing: a deck slot (keys 1–5 or a HUD button) shows
    /// the ghost, which follows the mouse and is re-validated every frame; a left click places when valid and the
    /// preview stays while another copy could still be placed. Esc or a right tap cancels through <see cref="CancelStack"/>;
    /// Q (meteor) leaves the preview by itself (TEAM_ASSIGNMENT 3.1 #6, #7). In Idle a click selects a placed hero, which
    /// the Upgrade and Sell keys then act on. The HUD only listens to the events below and to <see cref="HeroEvents"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlacementSystem : MonoBehaviour, ICancelable
    {
        [SerializeField] private PlacementRules _rules;
        [SerializeField] private HeroLevelTable _levelTable;
        [SerializeField] private HeroPool _pool;
        [SerializeField] private GhostPreview _ghost;

        [Tooltip("Camera for the mouse ray. Empty = Camera.main when the match starts.")]
        [SerializeField] private Camera _camera;

        [Tooltip("Optional IPointerSource. Empty = InputRouter when it implements it, otherwise InputSystemPointerStub.")]
        [SerializeField] private MonoBehaviour _pointerSource;

        private Action<int> _onDeckSlot;
        private Action _onSelect;
        private Action _onMeteor;
        private Action _onUpgrade;
        private Action _onSell;

        private MatchDeck _deck;
        private Wallet _wallet;
        private IPointerSource _pointer;
        private InputRouter _router;
        private PlacementMasks _masks;
        private int _heroMask;
        private bool _matchActive;
        private int _previewSlot = -1;
        private HeroLevel _selected;
        private PlacementError _previewError;
        private bool _hasPreviewError;

        private void Awake()
        {
            // Cached once so subscribing never allocates.
            _onDeckSlot = HandleDeckSlot;
            _onSelect = HandleSelect;
            _onMeteor = HandleMeteor;
            _onUpgrade = HandleUpgrade;
            _onSell = HandleSell;
        }

        /// <summary>Preview entered (slot 0..4) or left (-1).</summary>
        public event Action<int> PreviewChanged;

        /// <summary>The live validation result of the preview changed (HUD shows the reason, deck slot turns red).</summary>
        public event Action<PlacementError> PreviewErrorChanged;

        /// <summary>A click tried to place and failed; the HUD shows this reason.</summary>
        public event Action<PlacementError> PlaceFailed;

        public event Action<HeroLevel, UpgradeError> UpgradeFailed;

        /// <summary>
        /// Selling a top-level hero asks first (S10 ConfirmDialog). When nobody listens, the sale goes through directly;
        /// the listener calls <see cref="TrySell"/> after the player confirms.
        /// </summary>
        public event Action<HeroLevel> SellConfirmRequested;

        /// <summary>STUB: thay bằng trạng thái ngắm của MeteorAbility (S7, A). Đặt tướng ↔ ngắm đã loại trừ nhau qua phím Q.</summary>
        public Func<bool> IsAimingMeteor { get; set; }

        public bool IsMatchActive => _matchActive;
        public bool IsPreviewing => _previewSlot >= 0;
        public int PreviewSlot => _previewSlot;
        public HeroLevel Selected => _selected;
        public MatchDeck Deck => _deck;
        public Wallet Wallet => _wallet;
        public PlacementRules Rules => _rules;
        public HeroLevelTable LevelTable => _levelTable;
        public HeroPool Pool => _pool;

        /// <summary>For tests and scenes built in code.</summary>
        public void Configure(PlacementRules rules, HeroLevelTable levelTable, HeroPool pool, GhostPreview ghost, Camera camera,
            IPointerSource pointer = null)
        {
            _rules = rules;
            _levelTable = levelTable;
            _pool = pool;
            _ghost = ghost;
            _camera = camera;
            _pointer = pointer;
        }

        private void OnEnable() => Subscribe();

        private void Start() => Subscribe(); // InputRouter may wake up after this component.

        private void OnDisable()
        {
            Unsubscribe();
            if (IsPreviewing) ExitPreview();
        }

        /// <summary>Match loaded: the pool and ghosts are prewarmed by the caller (GameManager).</summary>
        public void BeginMatch(MatchDeck deck, Wallet wallet)
        {
            if (_rules == null || _levelTable == null || _pool == null)
            {
                Debug.LogError($"{name}: PlacementSystem needs PlacementRules, HeroLevelTable and HeroPool.", this);
                return;
            }

            _deck = deck ?? throw new ArgumentNullException(nameof(deck));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _masks = PlacementMasks.FromProjectLayers();
            _heroMask = LayerMask.GetMask("Hero");
            if (_camera == null) _camera = Camera.main;
            ResolvePointer();
            _matchActive = true;
            Subscribe();
        }

        /// <summary>End of the match (S4): leave the preview, every hero back to the pool, deck counts to zero.</summary>
        public void EndMatch()
        {
            if (IsPreviewing) ExitPreview();
            Deselect();
            if (_pool != null) _pool.ReleaseAll();
            _deck?.Reset();
            _matchActive = false;
        }

        // ---------- Preview ----------

        /// <summary>Enters the preview of a deck slot (key 1–5 or a HUD deck button).</summary>
        public void EnterPreview(int slot)
        {
            if (!_matchActive || _deck == null || !_deck.IsValidSlot(slot)) return;

            Deselect();
            _previewSlot = slot;
            _hasPreviewError = false;
            if (_ghost != null) _ghost.Show(_deck[slot].HeroData);
            CancelStack.Push(this);
            PreviewChanged?.Invoke(slot);
            RefreshPreview();
        }

        /// <summary>Leaves the preview by itself (no more gold, cap reached, Q, same slot again).</summary>
        public void ExitPreview()
        {
            if (!IsPreviewing) return;
            CancelStack.Remove(this);
            LeavePreview();
        }

        /// <summary>CancelStack already removed this entry (Esc or right tap).</summary>
        public void OnCancel() => LeavePreview();

        private void LeavePreview()
        {
            if (!IsPreviewing) return;
            _previewSlot = -1;
            if (_ghost != null) _ghost.Hide();
            PreviewChanged?.Invoke(-1);
        }

        private void Update()
        {
            if (IsPreviewing) RefreshPreview();
        }

        private void RefreshPreview()
        {
            DeckSlot slot = _deck[_previewSlot];
            if (!TryGetPointerRay(out Ray ray))
            {
                if (_ghost != null) _ghost.HideVisuals();
                SetPreviewError(PlacementError.NotPlaceableGround);
                return;
            }

            PlacementProbeResult probe = PlacementProbe.Probe(ray, _masks, _rules.OccupancyRadius);
            PlacementError error = PlacementValidator.Validate(_rules, BuildCheck(slot, probe));
            if (_ghost != null)
            {
                if (probe.HitSurface) _ghost.UpdatePose(probe.Point, slot.HeroData.range, error == PlacementError.None);
                else _ghost.HideVisuals();
            }

            SetPreviewError(error);
        }

        private void SetPreviewError(PlacementError error)
        {
            if (_hasPreviewError && error == _previewError) return;
            _previewError = error;
            _hasPreviewError = true;
            PreviewErrorChanged?.Invoke(error);
        }

        private PlacementCheck BuildCheck(DeckSlot slot, in PlacementProbeResult probe) =>
            new PlacementCheck(_wallet.Gold, slot.HeroData.rarity, slot.PlacedCount, _deck.TotalPlaced, probe.HitPlaceable,
                probe.AreaFree, IsAimingMeteor != null && IsAimingMeteor());

        /// <summary>
        /// Places the previewed hero under the mouse. Every check runs again here (never the last frame's result),
        /// so fast repeated clicks cannot overspend or stack heroes.
        /// </summary>
        public bool TryPlaceAtPointer()
        {
            if (!IsPreviewing) return false;
            if (!TryGetPointerRay(out Ray ray)) return Fail(PlacementError.NotPlaceableGround);

            int slotIndex = _previewSlot;
            DeckSlot slot = _deck[slotIndex];
            PlacementProbeResult probe = PlacementProbe.Probe(ray, _masks, _rules.OccupancyRadius);
            PlacementError error = PlacementValidator.Validate(_rules, BuildCheck(slot, probe));
            if (error != PlacementError.None) return Fail(error);

            HeroData data = slot.HeroData;
            int cost = HeroEconomy.PlaceCost(_rules, data.rarity);
            if (!_wallet.TrySpend(cost)) return Fail(PlacementError.NotEnoughGold);

            HeroLevel hero = _pool.Get(data, probe.Point, Quaternion.identity);
            if (hero == null)
            {
                _wallet.Add(cost); // Refund: nothing was placed.
                return Fail(PlacementError.AreaBlocked);
            }

            hero.BeginPlaced(slotIndex, cost);
            _deck.RecordPlaced(slotIndex);
            HeroEvents.RaiseHeroPlaced(InfoOf(hero, cost));

            if (!PlacementValidator.CanPlaceAnother(_rules, _wallet.Gold, data.rarity, slot.PlacedCount, _deck.TotalPlaced))
            {
                ExitPreview();
            }

            return true;
        }

        private bool Fail(PlacementError error)
        {
            PlaceFailed?.Invoke(error);
            return false;
        }

        // ---------- Selection, upgrade, sell ----------

        /// <summary>Selects a placed hero (opens the hero info panel through <see cref="HeroEvents.OnHeroSelected"/>).</summary>
        public void Select(HeroLevel hero)
        {
            if (hero == null || !hero.IsPlaced)
            {
                Deselect();
                return;
            }

            if (_selected == hero) return;
            _selected = hero;
            HeroEvents.RaiseHeroSelected(InfoOf(hero, 0));
        }

        public void Deselect()
        {
            if (_selected == null) return;
            _selected = null;
            HeroEvents.RaiseHeroSelected(new HeroEventInfo(null, null, default, 0, Vector3.zero, 0));
        }

        /// <summary>Upgrade button or key: pays the next level (S4 step 5).</summary>
        public UpgradeError TryUpgrade(HeroLevel hero)
        {
            if (!_matchActive || hero == null || !hero.IsPlaced) return UpgradeError.NotPlaced;

            UpgradeError error = hero.TryUpgrade(_rules, _wallet, out int cost);
            if (error == UpgradeError.None) HeroEvents.RaiseHeroUpgraded(InfoOf(hero, cost));
            else UpgradeFailed?.Invoke(hero, error);
            return error;
        }

        /// <summary>A top-level hero asks before selling (S10).</summary>
        public bool NeedsSellConfirm(HeroLevel hero) => hero != null && hero.IsTopLevel;

        /// <summary>Sell key: confirms first for a top-level hero when a dialog listens, otherwise sells.</summary>
        public void RequestSell(HeroLevel hero)
        {
            if (hero == null || !hero.IsPlaced) return;
            if (NeedsSellConfirm(hero) && SellConfirmRequested != null) SellConfirmRequested.Invoke(hero);
            else TrySell(hero);
        }

        /// <summary>Sells now (S4 step 6): refund floor(goldSpent × 0.5), back to the pool, placedCount--.</summary>
        public bool TrySell(HeroLevel hero)
        {
            if (!_matchActive || hero == null || !hero.IsPlaced) return false;

            int refund = hero.State.SellRefund(_rules);
            int slot = hero.DeckSlot;
            HeroEventInfo info = InfoOf(hero, refund);
            if (_selected == hero) Deselect();

            _pool.Release(hero);
            if (_deck.IsValidSlot(slot)) _deck.RecordRemoved(slot);
            _wallet.Add(refund);
            HeroEvents.RaiseHeroSold(info);
            return true;
        }

        private static HeroEventInfo InfoOf(HeroLevel hero, int gold) =>
            new HeroEventInfo(hero.gameObject, hero.State.HeroId, hero.State.Rarity, hero.Level, hero.transform.position, gold);

        // ---------- Input ----------

        private void HandleDeckSlot(int slot)
        {
            if (!_matchActive) return;
            if (slot == _previewSlot) ExitPreview(); // ASSUMPTION: pressing the same slot again leaves the preview.
            else EnterPreview(slot);
        }

        private void HandleSelect()
        {
            if (!_matchActive) return;
            if (IsPreviewing)
            {
                TryPlaceAtPointer();
                return;
            }

            Select(TryGetPointerRay(out Ray ray) ? PlacementProbe.PickHero(ray, _heroMask) : null);
        }

        private void HandleMeteor()
        {
            if (IsPreviewing) ExitPreview(); // 3.1 #7: never call MeteorAbility directly.
        }

        private void HandleUpgrade()
        {
            if (_selected != null) TryUpgrade(_selected);
        }

        private void HandleSell()
        {
            if (_selected != null) RequestSell(_selected);
        }

        private bool TryGetPointerRay(out Ray ray)
        {
            ray = default;
            if (_camera == null || _pointer == null || !_pointer.TryGetPointerPosition(out Vector2 position)) return false;
            ray = _camera.ScreenPointToRay(position);
            return true;
        }

        private void ResolvePointer()
        {
            if (_pointer != null) return;
            _pointer = _pointerSource as IPointerSource;
            if (_pointer == null) _pointer = InputRouter.Instance as IPointerSource;
            if (_pointer != null) return;

            // STUB: thay bằng SummonerInputRouter : IPointerSource (C).
            if (!TryGetComponent(out InputSystemPointerStub stub)) stub = gameObject.AddComponent<InputSystemPointerStub>();
            _pointer = stub;
        }

        private void Subscribe()
        {
            InputRouter router = InputRouter.Instance;
            if (router == null || router == _router) return;

            Unsubscribe();
            _router = router;
            _router.DeckSlot += _onDeckSlot;
            _router.Select += _onSelect;
            _router.Meteor += _onMeteor;
            _router.Upgrade += _onUpgrade;
            _router.Sell += _onSell;
        }

        private void Unsubscribe()
        {
            if (_router == null) return;
            _router.DeckSlot -= _onDeckSlot;
            _router.Select -= _onSelect;
            _router.Meteor -= _onMeteor;
            _router.Upgrade -= _onUpgrade;
            _router.Sell -= _onSell;
            _router = null;
        }
    }
}
