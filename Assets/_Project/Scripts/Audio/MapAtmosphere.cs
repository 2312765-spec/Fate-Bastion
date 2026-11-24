using UnityEngine;
using UnityEngine.Rendering;

namespace FateBastion.Audio
{
    /// <summary>
    /// Scene lighting that travels with the map prefab (C-17): makes the map's Sun the main light and sets ambient and
    /// fog while the map is loaded, then restores what the scene had. Keeps Game.unity and the sandboxes in step without
    /// editing each scene's Lighting window.
    /// </summary>
    public sealed class MapAtmosphere : MonoBehaviour
    {
        [SerializeField] private Light _sun;

        [Header("Ambient (Trilight)")]
        [SerializeField] private Color _skyColor = new Color(0.62f, 0.72f, 0.86f);
        [SerializeField] private Color _equatorColor = new Color(0.56f, 0.62f, 0.56f);
        [SerializeField] private Color _groundColor = new Color(0.36f, 0.33f, 0.28f);

        [Header("Fog (linear, metres from the camera)")]
        [SerializeField] private bool _fog = true;
        [SerializeField] private Color _fogColor = new Color(0.72f, 0.81f, 0.88f);
        [SerializeField] private float _fogStart = 45f;
        [SerializeField] private float _fogEnd = 120f;

        private bool _applied;
        private Light _previousSun;
        private AmbientMode _previousAmbientMode;
        private Color _previousSky, _previousEquator, _previousGround;
        private bool _previousFog;
        private FogMode _previousFogMode;
        private Color _previousFogColor;
        private float _previousFogStart, _previousFogEnd;

        public Light Sun => _sun;

        private void Awake()
        {
            if (_sun == null)
                Debug.LogError($"{name}: MapAtmosphere has no Sun light; the scene's default light stays the main light.", this);
        }

        private void OnEnable() => Apply();

        private void OnDisable() => Restore();

        /// <summary>Applies the map's lighting; also called by the map preview tool in the Editor.</summary>
        public void Apply()
        {
            if (_applied) return;
            _previousSun = RenderSettings.sun;
            _previousAmbientMode = RenderSettings.ambientMode;
            _previousSky = RenderSettings.ambientSkyColor;
            _previousEquator = RenderSettings.ambientEquatorColor;
            _previousGround = RenderSettings.ambientGroundColor;
            _previousFog = RenderSettings.fog;
            _previousFogMode = RenderSettings.fogMode;
            _previousFogColor = RenderSettings.fogColor;
            _previousFogStart = RenderSettings.fogStartDistance;
            _previousFogEnd = RenderSettings.fogEndDistance;

            if (_sun != null) RenderSettings.sun = _sun;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = _skyColor;
            RenderSettings.ambientEquatorColor = _equatorColor;
            RenderSettings.ambientGroundColor = _groundColor;
            RenderSettings.fog = _fog;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = _fogColor;
            RenderSettings.fogStartDistance = _fogStart;
            RenderSettings.fogEndDistance = _fogEnd;
            _applied = true;
        }

        /// <summary>Puts back the scene's own lighting values.</summary>
        public void Restore()
        {
            if (!_applied) return;
            RenderSettings.sun = _previousSun;
            RenderSettings.ambientMode = _previousAmbientMode;
            RenderSettings.ambientSkyColor = _previousSky;
            RenderSettings.ambientEquatorColor = _previousEquator;
            RenderSettings.ambientGroundColor = _previousGround;
            RenderSettings.fog = _previousFog;
            RenderSettings.fogMode = _previousFogMode;
            RenderSettings.fogColor = _previousFogColor;
            RenderSettings.fogStartDistance = _previousFogStart;
            RenderSettings.fogEndDistance = _previousFogEnd;
            _applied = false;
        }
    }
}
