using UnityEngine;

namespace FateBastion.UI.World
{
    /// <summary>
    /// Makes one enemy model glow white for a moment when it takes damage (S11), through a MaterialPropertyBlock on
    /// _EmissionColor (enemy materials need Emission enabled, black). Sits on the model root of C's enemy variants.
    /// Has no Update: HitFlashTracker ticks the flashing ones. The block is removed afterwards so the renderers
    /// go back to the SRP Batcher.
    /// </summary>
    [DisallowMultipleComponent]
    public class HitFlash : MonoBehaviour
    {
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private Renderer[] _renderers;
        private MaterialPropertyBlock _block;
        private Color _color;
        private float _duration;
        private float _remaining;

        public bool IsFlashing => _remaining > 0f;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (_renderers != null) return;
            // Load time (pooled enemies are created at prewarm), so the array is allocated once per enemy.
            _renderers = GetComponentsInChildren<Renderer>(true);
            _block = new MaterialPropertyBlock();
            if (_renderers.Length == 0) Debug.LogWarning($"{name}: HitFlash không tìm thấy Renderer nào bên dưới.", this);
        }

        /// <summary>Starts (or restarts) the flash at full brightness.</summary>
        public void Flash(Color color, float duration)
        {
            EnsureInitialized();
            _color = color;
            _duration = duration;
            _remaining = duration;
            Apply(1f);
        }

        /// <summary>Advances the flash; false once it has ended and the block is cleared.</summary>
        public bool Tick(float deltaTime)
        {
            if (_remaining <= 0f) return false;
            _remaining -= deltaTime;
            if (_remaining <= 0f)
            {
                Clear();
                return false;
            }

            Apply(HitFlashMath.Intensity(_remaining, _duration));
            return true;
        }

        /// <summary>Ends the flash at once (enemy despawned).</summary>
        public void Clear()
        {
            _remaining = 0f;
            if (_renderers == null) return;
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null) _renderers[i].SetPropertyBlock(null);
            }
        }

        private void Apply(float intensity)
        {
            _block.SetColor(EmissionColorId, _color * intensity);
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null) _renderers[i].SetPropertyBlock(_block);
            }
        }
    }
}
