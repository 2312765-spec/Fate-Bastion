using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FateBastion.Enemies;
using FateBastion.Game;
using FateBastion.Heroes;
using FateBastion.Meta;

namespace FateBastion.UI
{
    /// <summary>
    /// Screen_LevelSelect (S10, B-15): stars per level, enemy preview, Endless record and lock, the deck preview with a
    /// warning when it cannot start (S8), and "Vào trận" which hands the choice to MatchRequest and loads the match.
    /// </summary>
    public sealed class LevelSelectScreen : LobbyScreen
    {
        private const int EndlessIndex = -1;

        private readonly List<LevelCardView> _cards = new List<LevelCardView>(3);
        private readonly List<Image> _deckIcons = new List<Image>(DeckService.DeckSize);
        private LevelCardView _endlessCard;
        private TMP_Text _deckWarning;
        private Button _startButton;
        private int _selected;
        private bool _hasSelection;

        public override string Title => "Chọn màn";

        protected override void OnBind()
        {
            MatchCatalog levels = Context.Levels;
            int levelCount = levels != null ? levels.LevelCount : 0;
            for (int i = 0; i < levelCount; i++)
            {
                Transform node = UiFind.Node(transform, $"Levels/LevelCard_{i + 1}", this);
                if (node == null) break;
                var card = new LevelCardView(node);
                int index = i;
                if (card.Button != null) card.Button.onClick.AddListener(() => Select(index));
                _cards.Add(card);
            }

            // C's layout has three sample cards; hide any the catalog does not use.
            for (int n = _cards.Count + 1; ; n++)
            {
                Transform extra = transform.Find($"Levels/LevelCard_{n}");
                if (extra == null) break;
                extra.gameObject.SetActive(false);
            }

            Transform endless = UiFind.Node(transform, "Levels/LevelCard_Endless", this);
            if (endless != null)
            {
                _endlessCard = new LevelCardView(endless);
                if (_endlessCard.Button != null) _endlessCard.Button.onClick.AddListener(() => Select(EndlessIndex));
            }

            for (int i = 0; i < DeckService.DeckSize; i++)
            {
                _deckIcons.Add(UiFind.Optional<Image>(transform, $"Footer/DeckPreview/DeckIcon_{i + 1}"));
            }

            _deckWarning = UiFind.Get<TMP_Text>(transform, "Footer/Status/DeckWarningText", this);
            _startButton = UiFind.Get<Button>(transform, "Footer/StartButton", this);
            if (_startButton != null) _startButton.onClick.AddListener(StartMatch);

            Button editDeck = UiFind.Get<Button>(transform, "Footer/EditDeckButton", this);
            if (editDeck != null) editDeck.onClick.AddListener(() => Context.Open(Context.Collection));
        }

        protected override void Refresh()
        {
            SaveData data = Context.Data;
            MatchCatalog levels = Context.Levels;
            if (levels == null) return;

            int[] stars = LevelProgress.StarsByLevel(data, levels);
            for (int i = 0; i < _cards.Count; i++)
            {
                LevelData level = levels.LevelAt(i);
                LevelCardView card = _cards[i];
                card.SetTitle(level != null && !string.IsNullOrEmpty(level.displayName) ? level.displayName : $"Màn {i + 1}");
                card.SetStars(stars[i]);
                card.SetEnemies(level != null ? level.enemyTypesPreview : null);
                card.SetLocked(!LevelProgress.IsLevelUnlocked(i, stars), $"Qua màn {i} để mở");
            }

            if (_endlessCard != null)
            {
                LevelData endless = levels.EndlessLevel;
                _endlessCard.SetTitle("Vô tận");
                _endlessCard.SetRecord(data.endlessBest);
                _endlessCard.SetEnemies(endless != null ? endless.enemyTypesPreview : null);
                _endlessCard.SetLocked(!LevelProgress.IsEndlessUnlocked(levels.LevelsToUnlockEndless, stars),
                    $"Qua màn {Mathf.Max(1, levels.LevelsToUnlockEndless)} để mở");
            }

            if (!_hasSelection || !IsUnlocked(_selected, stars)) _selected = DefaultSelection(stars);
            _hasSelection = true;
            ShowSelection();
            RefreshDeck();
        }

        private int DefaultSelection(int[] stars)
        {
            if (MatchRequest.HasRequest)
            {
                int requested = MatchRequest.IsEndless ? EndlessIndex : Context.Levels.IndexOf(MatchRequest.Level);
                if (IsUnlocked(requested, stars)) return requested;
            }

            // The furthest unlocked level: where the player most likely continues.
            int best = 0;
            for (int i = 0; i < stars.Length; i++)
            {
                if (LevelProgress.IsLevelUnlocked(i, stars)) best = i;
            }

            return best;
        }

        private bool IsUnlocked(int index, int[] stars) =>
            index == EndlessIndex
                ? _endlessCard != null && LevelProgress.IsEndlessUnlocked(Context.Levels.LevelsToUnlockEndless, stars)
                : index >= 0 && index < _cards.Count && LevelProgress.IsLevelUnlocked(index, stars);

        private void Select(int index)
        {
            _selected = index;
            ShowSelection();
        }

        private void ShowSelection()
        {
            for (int i = 0; i < _cards.Count; i++) _cards[i].SetSelected(i == _selected);
            _endlessCard?.SetSelected(_selected == EndlessIndex);
        }

        private void RefreshDeck()
        {
            SaveData data = Context.Data;
            for (int i = 0; i < _deckIcons.Count; i++)
            {
                Image icon = _deckIcons[i];
                if (icon == null) continue;
                string id = data.deck != null && i < data.deck.Count ? data.deck[i] : null;
                HeroData hero = Context.HeroData(id);
                if (hero != null && hero.icon != null)
                {
                    icon.sprite = hero.icon;
                    icon.color = Color.white;
                }
                else
                {
                    icon.color = id != null ? UiText.RarityColor(Context.RarityOf(id)) : new Color(1f, 1f, 1f, 0.25f);
                }
            }

            bool canStart = Context.Session.Deck.CanStartMatch(data);
            UiFind.SetActive(_deckWarning, !canStart);
            if (_startButton != null) _startButton.interactable = canStart;
        }

        private void StartMatch()
        {
            if (!Context.Session.Deck.CanStartMatch(Context.Data))
            {
                Context.Message("Deck chưa hợp lệ", "Deck cần đủ 5 tướng khác nhau. Mở Túi & Deck để sửa.");
                return;
            }

            bool endless = _selected == EndlessIndex;
            LevelData level = endless ? Context.Levels.EndlessLevel : Context.Levels.LevelAt(_selected);
            if (level == null)
            {
                Context.Message("Chưa có màn chơi", "Màn này chưa có dữ liệu.");
                return;
            }

            MatchRequest.Launch(level, endless);
        }
    }
}
