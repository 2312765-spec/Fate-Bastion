using TMPro;
using UnityEngine;

namespace FateBastion.UI.World
{
    /// <summary>
    /// One pooled floating damage number on the world UI canvas (S10, S11). Has no Update: DamageNumberSpawner
    /// moves and fades every active number from one LateUpdate.
    /// </summary>
    /// <remarks>
    /// Kept allocation-free per hit by never toggling the GameObject or the TMP font size/colour: hiding is
    /// CanvasRenderer culling, colour and fade are the CanvasRenderer colour (a draw-time multiplier, no mesh
    /// rebuild), and the meteor's bigger size is the transform scale. Only SetText changes the mesh.
    /// </remarks>
    [DisallowMultipleComponent]
    public class DamageNumber : MonoBehaviour
    {
        // TMP sizes its character and mesh buffers on the first text it lays out. Laying out the widest number
        // expected at load moves that allocation out of the match.
        private const int WidestNumber = 99999999;

        [SerializeField] private TMP_Text _text;

        private RectTransform _rect;
        private CanvasRenderer _renderer;
        private Color _color;
        private Vector3 _worldPoint;
        private float _age;
        private bool _shown = true;

        public bool IsShown => _shown;

        /// <summary>For the layout builder and tests.</summary>
        public void Configure(TMP_Text text)
        {
            _text = text;
        }

        /// <summary>
        /// Called once right after Instantiate (load time): caches components, fixes the font size, prepares
        /// TMP's buffers and hides the number. The GameObject stays active from here on.
        /// </summary>
        public void InitializeHidden(float fontSize)
        {
            _rect = (RectTransform)transform;
            if (_text == null) _text = GetComponentInChildren<TMP_Text>(true);
            if (_text == null)
            {
                Debug.LogError($"{name}: DamageNumber chưa gắn TMP Text.", this);
                return;
            }

            if (!gameObject.activeSelf) gameObject.SetActive(true);
            _renderer = _text.canvasRenderer;
            // Colour comes from the CanvasRenderer, so the vertex colour stays white.
            _text.color = Color.white;
            _text.fontSize = fontSize;
            _text.SetText("{0}", WidestNumber);
            _text.ForceMeshUpdate();
            Hide();
        }

        /// <param name="scale">1 for a normal hit; above 1 for the meteor.</param>
        public void Show(int amount, Color color, float scale, Vector3 worldPoint)
        {
            if (_text == null) return;
            _worldPoint = worldPoint;
            _age = 0f;
            _color = color;
            // SetText with a numeric argument formats without allocating a string.
            _text.SetText("{0}", amount);
            _rect.localScale = new Vector3(scale, scale, 1f);
            _renderer.SetColor(color);
            _renderer.cull = false;
            _shown = true;
        }

        /// <summary>Advances one frame; false when the number has finished and should go back to the pool.</summary>
        public bool Tick(Camera camera, float deltaTime, float lifetime, float rise)
        {
            _age += deltaTime;
            float t = _age / lifetime;
            if (t >= 1f) return false;

            Vector3 screenPoint = camera.WorldToScreenPoint(_worldPoint);
            // Behind the camera: keep ageing but draw nothing.
            bool inFront = screenPoint.z > 0f;
            _renderer.cull = !inFront;
            if (!inFront) return true;

            _rect.position = new Vector3(screenPoint.x, screenPoint.y + rise * t, 0f);
            // Hold full opacity for the first half, then fade.
            Color color = _color;
            color.a *= t < 0.5f ? 1f : 1f - (t - 0.5f) * 2f;
            _renderer.SetColor(color);
            return true;
        }

        public void Hide()
        {
            _shown = false;
            if (_renderer != null) _renderer.cull = true;
        }
    }
}
