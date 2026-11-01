using System;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FateBastion.Core;
using FateBastion.Game;
using FateBastion.Meta;

namespace FateBastion.UI
{
    /// <summary>
    /// Level-1 tutorial (S10, B-17): four hint boxes driven by events – place a hero (HeroEvents.OnHeroPlaced), upgrade it
    /// (OnHeroUpgraded), the Command Aura (CommandAura.OnHeroEnteredAura relayed by GameManager, 3.1 #10) and the meteor
    /// (AbilityEvents.OnMeteorExploded). Shown only on the first campaign level while SaveData.tutorialDone is false;
    /// finishing or "Bỏ qua" sets it and saves. Put it on the HUD canvas: the hint box is built once at start, in the
    /// HUD's own font.
    /// </summary>
    [DefaultExecutionOrder(110)]
    [DisallowMultipleComponent]
    public sealed class TutorialController : MonoBehaviour
    {
        [Tooltip("Show the tutorial even when the save says it was done (testing).")]
        [SerializeField] private bool _forceShow;

        [Tooltip("Real seconds the final \"done\" box stays before fading out.")]
        [SerializeField, Min(0f)] private float _doneSeconds = 2.5f;

        private static readonly Color PanelColor = new Color(0.17f, 0.12f, 0.08f, 0.92f);
        private static readonly Color AccentColor = new Color(0.96f, 0.77f, 0.35f, 1f);

        private readonly TutorialSequence _sequence = new TutorialSequence();
        private GameManager _game;
        private RectTransform _box;
        private CanvasGroup _group;
        private TMP_Text _progress;
        private TMP_Text _hint;
        private bool _active;

        private Action<HeroEventInfo> _onPlaced;
        private Action<HeroEventInfo> _onUpgraded;
        private Action<GameObject> _onAura;
        private Action<Vector3, float> _onMeteor;
        private Action<MatchResult> _onEnded;

        public TutorialSequence Sequence => _sequence;
        public bool IsActive => _active;

        private void Awake()
        {
            _onPlaced = _ => _sequence.Notify(TutorialTrigger.HeroPlaced);
            _onUpgraded = _ => _sequence.Notify(TutorialTrigger.HeroUpgraded);
            _onAura = _ => _sequence.Notify(TutorialTrigger.HeroEnteredAura);
            _onMeteor = (_, __) => _sequence.Notify(TutorialTrigger.MeteorExploded);
            _onEnded = _ => Hide(false);
            _sequence.StepChanged += ShowStep;
        }

        private void Start()
        {
            _game = GameManager.Instance;
            if (!ShouldRun()) return;

            BuildBox();
            _active = true;
            HeroEvents.OnHeroPlaced += _onPlaced;
            HeroEvents.OnHeroUpgraded += _onUpgraded;
            AbilityEvents.OnMeteorExploded += _onMeteor;
            GameEvents.OnMatchEnded += _onEnded;
            if (_game != null) _game.HeroEnteredAura += _onAura;
            ShowStep(_sequence.Current);
        }

        private void OnDestroy() => Unsubscribe();

        /// <summary>First campaign level and the tutorial not done yet (S10: "chỉ hiện lần đầu").</summary>
        private bool ShouldRun()
        {
            if (_forceShow) return true;
            if (_game == null || _game.Level == null || _game.IsEndless) return false;
            if (_game.Level.level != 1) return false;

            MetaSession session = MetaRuntime.Session;
            return session != null && !session.Data.tutorialDone;
        }

        private void ShowStep(TutorialStep step)
        {
            if (_box == null) return;
            _progress.text = TutorialSequence.Progress(step);
            _hint.text = TutorialSequence.Hint(step);

            Tween.StopAll(_box);
            _box.localScale = Vector3.one * 0.92f;
            Tween.Scale(_box, 1f, 0.3f, Ease.OutBack, useUnscaledTime: true);

            if (step == TutorialStep.Done) Finish();
        }

        private void Skip() => _sequence.Skip();

        private void Finish()
        {
            MarkDone();
            Hide(true);
        }

        private void MarkDone()
        {
            Unsubscribe();
            MetaSession session = MetaRuntime.Session;
            if (session == null || session.Data.tutorialDone) return;
            session.Data.tutorialDone = true;
            session.SaveNow();
        }

        private void Hide(bool afterDelay)
        {
            Unsubscribe();
            if (_group == null) return;
            Tween.StopAll(_group);
            Tween.Alpha(_group, 0f, 0.4f, startDelay: afterDelay ? _doneSeconds : 0f, useUnscaledTime: true)
                .OnComplete(this, t => { if (t._box != null) t._box.gameObject.SetActive(false); });
        }

        private void Unsubscribe()
        {
            if (!_active) return;
            _active = false;
            HeroEvents.OnHeroPlaced -= _onPlaced;
            HeroEvents.OnHeroUpgraded -= _onUpgraded;
            AbilityEvents.OnMeteorExploded -= _onMeteor;
            GameEvents.OnMatchEnded -= _onEnded;
            if (_game != null) _game.HeroEnteredAura -= _onAura;
        }

        // ---------- Hint box (built once) ----------

        private void BuildBox()
        {
            TMP_Text fontSource = GetComponentInChildren<TMP_Text>(true);

            _box = NewRect("TutorialBox", transform);
            _box.anchorMin = _box.anchorMax = new Vector2(0f, 1f);
            _box.pivot = new Vector2(0f, 1f);
            _box.anchoredPosition = new Vector2(40f, -40f);
            _box.sizeDelta = new Vector2(640f, 0f);
            _box.SetAsLastSibling();

            _box.gameObject.AddComponent<Image>().color = PanelColor;
            _group = _box.gameObject.AddComponent<CanvasGroup>();
            var layout = _box.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 18, 18);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            _box.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _progress = NewText("ProgressText", _box, fontSource, 22f, AccentColor);
            _hint = NewText("HintText", _box, fontSource, 26f, Color.white);

            RectTransform skip = NewRect("SkipButton", _box);
            var skipLayout = skip.gameObject.AddComponent<LayoutElement>();
            skipLayout.preferredHeight = 40f;
            var skipImage = skip.gameObject.AddComponent<Image>();
            skipImage.color = new Color(1f, 1f, 1f, 0.08f);
            var button = skip.gameObject.AddComponent<Button>();
            button.targetGraphic = skipImage;
            button.onClick.AddListener(Skip);
            TMP_Text skipLabel = NewText("Label", skip, fontSource, 20f, AccentColor);
            skipLabel.text = "Bỏ qua hướng dẫn";
            skipLabel.alignment = TextAlignmentOptions.Center;
            var labelRect = skipLabel.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static TMP_Text NewText(string name, Transform parent, TMP_Text fontSource, float size, Color color)
        {
            var text = NewRect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            if (fontSource != null) text.font = fontSource.font;
            text.fontSize = size;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }
    }
}
