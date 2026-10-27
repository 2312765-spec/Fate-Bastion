using UnityEngine;
using UnityEngine.UI;

namespace FateBastion.UI.World
{
    /// <summary>
    /// One pooled health bar on the world UI canvas (S10). Has no Update: WorldHealthBarPresenter sets health from
    /// OnHealthChanged and the screen position each frame. Shown only while damaged and in front of the camera.
    /// </summary>
    [DisallowMultipleComponent]
    public class WorldHealthBar : MonoBehaviour
    {
        [Tooltip("Image of type Filled (Horizontal); fillAmount follows the enemy's HP.")]
        [SerializeField] private Image _fill;

        private RectTransform _rect;
        private float _fillAmount = 1f;
        private bool _damaged;
        private bool _onScreen = true;
        private bool _shown = true;

        /// <summary>The GameObject is active (damaged and in front of the camera).</summary>
        public bool IsShown => _shown;

        /// <summary>Damaged and alive; the presenter positions only these bars.</summary>
        public bool IsDamaged => _damaged;

        public bool IsBoss { get; private set; }
        public float Fill => _fillAmount;

        /// <summary>For tests and the layout builder.</summary>
        public void Configure(Image fill)
        {
            _fill = fill;
        }

        /// <summary>Called once right after Instantiate (load time): caches components and hides the bar.</summary>
        public void InitializeHidden()
        {
            _rect = (RectTransform)transform;
            if (_fill == null) Debug.LogError($"{name}: WorldHealthBar chưa gắn Image Fill.", this);
            _shown = gameObject.activeSelf;
            _damaged = false;
            _fillAmount = -1f; // Forces the first SetFill to write, whatever the prefab's sample fill is.
            Apply();
        }

        /// <summary>Taken from the pool for an enemy at full health.</summary>
        public void ResetState(bool isBoss, float bossScale)
        {
            IsBoss = isBoss;
            float scale = isBoss ? bossScale : 1f;
            transform.localScale = new Vector3(scale, scale, 1f);
            _damaged = false;
            _onScreen = true;
            SetFill(1f);
            Apply();
        }

        public void SetHealth(float current, float max)
        {
            SetFill(HealthBarMath.FillRatio(current, max));
            _damaged = HealthBarMath.ShouldShow(current, max);
            Apply();
        }

        public void SetOnScreen(bool onScreen)
        {
            _onScreen = onScreen;
            Apply();
        }

        /// <summary>Screen pixels; on a Screen Space Overlay canvas world position equals screen position.</summary>
        public void SetScreenPosition(Vector3 screenPoint)
        {
            _rect.position = new Vector3(screenPoint.x, screenPoint.y, 0f);
        }

        public void Hide()
        {
            _damaged = false;
            Apply();
        }

        private void SetFill(float amount)
        {
            // Exact compare on purpose: skip only true no-ops, so the Image is not marked dirty for nothing.
            if (amount == _fillAmount) return;
            _fillAmount = amount;
            if (_fill != null) _fill.fillAmount = amount;
        }

        private void Apply()
        {
            bool show = _damaged && _onScreen;
            if (show == _shown) return;
            _shown = show;
            gameObject.SetActive(show);
        }
    }
}
