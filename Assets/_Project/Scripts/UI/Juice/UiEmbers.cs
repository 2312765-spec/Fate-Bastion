using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace FateBastion.UI.Juice
{
    /// <summary>
    /// Ambient embers drifting up the screen behind menus. A fixed set of glow dots is created once in Awake and
    /// relaunched when each finishes (no allocation afterwards). Place on a full-screen RectTransform under the
    /// background; purely cosmetic, so UnityEngine.Random is fine here.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Fate Bastion/UI/Embers")]
    public class UiEmbers : MonoBehaviour
    {
        [SerializeField, Range(1, 80)] private int _count = 30;
        [SerializeField] private Color _colorA = new Color(1f, 0.62f, 0.2f, 0.9f);
        [SerializeField] private Color _colorB = new Color(1f, 0.85f, 0.45f, 0.75f);
        [SerializeField] private Vector2 _sizeRange = new Vector2(6f, 16f);
        [Tooltip("Real seconds to cross the screen, min/max.")]
        [SerializeField] private Vector2 _riseSeconds = new Vector2(7f, 13f);
        [SerializeField] private float _sway = 40f;

        private RectTransform _area;
        private Ember[] _embers;

        /// <summary>One particle; also the tween target so callbacks need no closure.</summary>
        private sealed class Ember
        {
            public UiEmbers Owner;
            public RectTransform Rect;
            public Image Image;
        }

        private void Awake()
        {
            _area = (RectTransform)transform;
            _embers = new Ember[_count];
            Sprite dot = UiProceduralSprites.SoftDot;
            for (int i = 0; i < _count; i++)
            {
                var go = new GameObject("Ember_" + i, typeof(RectTransform), typeof(Image));
                var rect = (RectTransform)go.transform;
                rect.SetParent(_area, false);
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 0f);
                var image = go.GetComponent<Image>();
                image.sprite = dot;
                image.raycastTarget = false;
                _embers[i] = new Ember { Owner = this, Rect = rect, Image = image };
            }
        }

        private void OnEnable()
        {
            if (_embers == null)
            {
                return;
            }

            // Spread the first wave over a whole rise so the screen does not start empty and then fill up at once.
            for (int i = 0; i < _embers.Length; i++)
            {
                Launch(_embers[i], Random.Range(0f, _riseSeconds.y));
            }
        }

        private void OnDisable()
        {
            if (_embers == null)
            {
                return;
            }

            for (int i = 0; i < _embers.Length; i++)
            {
                Tween.StopAll(_embers[i].Rect);
                Tween.StopAll(_embers[i].Image);
            }
        }

        private void Launch(Ember ember, float delay)
        {
            Rect area = _area.rect;
            float size = Random.Range(_sizeRange.x, _sizeRange.y);
            float x = Random.Range(0f, area.width);
            float duration = Random.Range(_riseSeconds.x, _riseSeconds.y);

            ember.Rect.sizeDelta = new Vector2(size, size);
            ember.Rect.anchoredPosition = new Vector2(x, -size);
            Color color = Color.Lerp(_colorA, _colorB, Random.value);
            float peakAlpha = color.a;
            color.a = 0f;
            ember.Image.color = color;

            Tween.UIAnchoredPositionY(ember.Rect, area.height * Random.Range(0.55f, 1.05f), duration, Ease.OutSine,
                    startDelay: delay, useUnscaledTime: true)
                .OnComplete(ember, e => e.Owner.Relaunch(e));
            Tween.UIAnchoredPositionX(ember.Rect, x + Random.Range(-_sway, _sway), duration * 0.5f, Ease.InOutSine,
                cycles: 2, cycleMode: CycleMode.Yoyo, startDelay: delay, useUnscaledTime: true);
            // Glow in, then burn out before reaching the top.
            Tween.Alpha(ember.Image, peakAlpha, duration * 0.5f, Ease.InOutSine,
                cycles: 2, cycleMode: CycleMode.Yoyo, startDelay: delay, useUnscaledTime: true);
        }

        private void Relaunch(Ember ember)
        {
            if (isActiveAndEnabled)
            {
                Launch(ember, Random.Range(0f, 1.5f));
            }
        }
    }
}
