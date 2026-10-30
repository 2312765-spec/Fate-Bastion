using UnityEngine;

namespace FateBastion.UI.Juice
{
    /// <summary>
    /// Soft shapes generated once in memory (no art asset needed): a radial glow dot for embers and highlights,
    /// and a dark vignette. Cached for the whole session.
    /// </summary>
    public static class UiProceduralSprites
    {
        private const int GlowSize = 64;
        private const int VignetteSize = 256;

        private static Sprite _softDot;
        private static Texture2D _vignette;

        /// <summary>White dot, opaque in the middle and fading to transparent at the edge (tint it with Image.color).</summary>
        public static Sprite SoftDot
        {
            get
            {
                if (_softDot == null)
                {
                    Texture2D texture = CreateRadial(GlowSize, (d) => Mathf.Pow(Mathf.Clamp01(1f - d), 2.2f), Color.white);
                    _softDot = Sprite.Create(texture, new Rect(0, 0, GlowSize, GlowSize), new Vector2(0.5f, 0.5f), 100f);
                    _softDot.name = "UiSoftDot (generated)";
                }

                return _softDot;
            }
        }

        /// <summary>Black, transparent in the middle and darker toward the corners (use on a stretched RawImage).</summary>
        public static Texture2D Vignette
        {
            get
            {
                if (_vignette == null)
                {
                    _vignette = CreateRadial(VignetteSize, (d) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1.15f, d)), Color.black);
                    _vignette.name = "UiVignette (generated)";
                }

                return _vignette;
            }
        }

        // d = distance from the centre, 0 in the middle and 1 at the edge midpoints (corners are ~1.41).
        private static Texture2D CreateRadial(int size, System.Func<float, float> alphaAt, Color color)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave
            };

            var pixels = new Color32[size * size];
            float half = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - half) / half;
                    float dy = (y - half) / half;
                    float a = Mathf.Clamp01(alphaAt(Mathf.Sqrt(dx * dx + dy * dy)));
                    pixels[y * size + x] = new Color(color.r, color.g, color.b, a);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }
    }
}
