using TMPro;
using UnityEngine;

namespace FateBastion.UI.World
{
    /// <summary>
    /// One pooled floating damage number on the world UI canvas (S10, S11). Has no Update: DamageNumberSpawner
    /// moves and fades every active number from one LateUpdate.
    /// </summary>
    [DisallowMultipleComponent]
    public class DamageNumber : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;

        private RectTransform _rect;
        private Vector3 _worldPoint;
        private float _age;
        private bool _shown = true;

        public bool IsShown => _shown;

        /// <summary>For the layout builder and tests.</summary>
        public void Configure(TMP_Text text)
        {
            _text = text;
        }

        /// <summary>Called once right after Instantiate (load time): caches components and hides the number.</summary>
        public void InitializeHidden()
        {
            _rect = (RectTransform)transform;
            if (_text == null) _text = GetComponentInChildren<TMP_Text>(true);
            if (_text == null) Debug.LogError($"{name}: DamageNumber chưa gắn TMP Text.", this);
            _shown = gameObject.activeSelf;
            SetShown(false);
        }

        public void Show(int amount, Color color, float fontSize, Vector3 worldPoint)
        {
            _worldPoint = worldPoint;
            _age = 0f;
            if (_text != null)
            {
                // SetText with a numeric argument formats without allocating a string.
                _text.SetText("{0}", amount);
                _text.color = color;
                _text.fontSize = fontSize;
            }
            SetShown(true);
        }

        /// <summary>Advances one frame; false when the number has finished and should go back to the pool.</summary>
        public bool Tick(Camera camera, float deltaTime, float lifetime, float rise)
        {
            _age += deltaTime;
            float t = _age / lifetime;
            if (t >= 1f) return false;

            Vector3 screenPoint = camera.WorldToScreenPoint(_worldPoint);
            if (screenPoint.z <= 0f)
            {
                // Behind the camera: keep ageing but draw nothing.
                if (_text != null) _text.alpha = 0f;
                return true;
            }

            _rect.position = new Vector3(screenPoint.x, screenPoint.y + rise * t, 0f);
            // Hold full opacity for the first half, then fade.
            if (_text != null) _text.alpha = t < 0.5f ? 1f : 1f - (t - 0.5f) * 2f;
            return true;
        }

        public void Hide() => SetShown(false);

        private void SetShown(bool shown)
        {
            if (shown == _shown) return;
            _shown = shown;
            gameObject.SetActive(shown);
        }
    }
}
