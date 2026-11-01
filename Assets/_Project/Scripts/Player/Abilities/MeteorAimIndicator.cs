using FateBastion.Core;
using FateBastion.Enemies;
using FateBastion.Skills;
using UnityEngine;

namespace FateBastion.Player
{
    /// <summary>Drives ground-point aiming, range feedback, and meteor impact timing (S7).</summary>
    [DisallowMultipleComponent]
    public sealed class MeteorAimIndicator : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly Color InRangeColor = new Color(0.1f, 0.9f, 0.2f, 0.55f);
        private static readonly Color OutOfRangeColor = new Color(1f, 0.1f, 0.1f, 0.55f);

        [SerializeField] private MeteorSettings _settings;
        [SerializeField] private WaveManager _waveManager;
        [SerializeField] private EnemyManager _enemyManager;
        [SerializeField] private Transform _summoner;
        [SerializeField] private MonoBehaviour _aimRaySourceComponent;
        [SerializeField] private LayerMask _groundLayers;
        [SerializeField] private Transform _rangeIndicator;
        [SerializeField] private Renderer _blastPreview;
        [SerializeField] private Renderer _impactMarker;
        [Tooltip("Optional fall/telegraph effect. Created once during scene setup and shown during the scaled fall delay.")]
        [SerializeField] private GameObject _fallFxPrefab;

        // Created in Awake: Unity forbids MaterialPropertyBlock in a field initializer (CreateImpl exception, field stays null).
        // Takeover fix by C (TEAM_ASSIGNMENT 5.3), draft until A reviews.
        private MaterialPropertyBlock _colorProperties;
        private MeteorAbility _ability;
        private MeteorDamageApplier _damageApplier;
        private GameObject _fallFx;
        private ParticleSystem _fallFxParticles;
        private IMeteorAimRaySource _aimRaySource;
        private InputRouter _inputRouter;
        private Vector3 _aimPoint;
        private bool _hasGroundPoint;
        private bool _aimPointInRange;
        private bool _matchEnded;
        private bool _reportedInvalidSetup;
        private int _currentWave = 1;

        private void Awake()
        {
            _colorProperties = new MaterialPropertyBlock();
            if (!TryInitialize()) enabled = false;
        }

        private void OnDestroy()
        {
            if (_inputRouter != null) _inputRouter.Select -= OnSelect;
            if (_ability != null)
            {
                _ability.OnStateChanged -= OnStateChanged;
                _ability.OnImpactReady -= OnImpactReady;
                _ability.Dispose();
            }
            if (_fallFx != null) Destroy(_fallFx);

            GameEvents.OnWaveStarted -= OnWaveStarted;
            GameEvents.OnMatchEnded -= OnMatchEnded;
        }

        private void Update()
        {
            if (_ability == null) return;

            _ability.Tick(Time.deltaTime);
            if (_ability.CurrentState == MeteorAbility.State.Aiming)
                RefreshAimPoint();
        }

        private bool TryInitialize()
        {
            if (_settings == null || _waveManager == null || _enemyManager == null || _summoner == null
                || _rangeIndicator == null || _blastPreview == null || _impactMarker == null)
            {
                return ReportInvalidSetup("A required settings, wave, enemy, transform, or renderer reference is missing.");
            }

            if (!(_aimRaySourceComponent is IMeteorAimRaySource raySource))
                return ReportInvalidSetup($"{nameof(_aimRaySourceComponent)} must implement {nameof(IMeteorAimRaySource)}.");

            SkillData skill = _settings.Skill;
            if (skill == null || _settings.NormalEnemy == null
                || !IsPositiveFinite(_settings.NormalEnemy.baseHP)
                || skill.kind != SkillKind.AreaAtPoint
                || skill.aimMode != AimMode.GroundPoint || skill.damageType != DamageType.True
                || !IsPositiveFinite(skill.radius) || !IsPositiveFinite(skill.cooldown)
                || !IsFiniteNonNegative(skill.castDelay) || !IsPositiveFinite(skill.damageMultiplier)
                || !IsPositiveFinite(_settings.Range) || !IsFiniteNonNegative(_settings.VerticalQueryPadding)
                || _groundLayers.value == 0)
            {
                return ReportInvalidSetup("Meteor settings must reference a True AreaAtPoint/GroundPoint skill, normal enemy data, positive range/radius, and a ground layer.");
            }

            _inputRouter = InputRouter.Instance;
            if (_inputRouter == null)
                return ReportInvalidSetup($"No {nameof(InputRouter)} is available.");

            _aimRaySource = raySource;
            _ability = new MeteorAbility(_inputRouter, skill.cooldown, skill.castDelay);
            _damageApplier = new MeteorDamageApplier(_settings, _enemyManager);

            _inputRouter.Select += OnSelect;
            _ability.OnStateChanged += OnStateChanged;
            _ability.OnImpactReady += OnImpactReady;
            GameEvents.OnWaveStarted += OnWaveStarted;
            GameEvents.OnMatchEnded += OnMatchEnded;

            SetDiameter(_rangeIndicator, _settings.Range * 2f);
            SetDiameter(_blastPreview.transform, skill.radius * 2f);
            SetDiameter(_impactMarker.transform, skill.radius * 2f);
            SetRendererColor(_impactMarker, OutOfRangeColor);
            _rangeIndicator.gameObject.SetActive(false);
            _blastPreview.gameObject.SetActive(false);
            _impactMarker.gameObject.SetActive(false);
            PrepareFallFx();
            return true;
        }

        private bool ReportInvalidSetup(string message)
        {
            if (!_reportedInvalidSetup)
            {
                Debug.LogError($"{nameof(MeteorAimIndicator)} on '{name}': {message}", this);
                _reportedInvalidSetup = true;
            }
            return false;
        }

        private void RefreshAimPoint()
        {
            _rangeIndicator.position = _summoner.position;

            if (!_aimRaySource.TryGetAimRay(out Ray ray)
                || !Physics.Raycast(ray, out RaycastHit hit, float.PositiveInfinity, _groundLayers,
                    QueryTriggerInteraction.Ignore))
            {
                _hasGroundPoint = false;
                _blastPreview.gameObject.SetActive(false);
                return;
            }

            _aimPoint = hit.point;
            _hasGroundPoint = true;
            _aimPointInRange = MeteorAimMath.IsWithinRangeXZ(_summoner.position, _aimPoint, _settings.Range);
            _blastPreview.transform.position = _aimPoint;
            SetRendererColor(_blastPreview, _aimPointInRange ? InRangeColor : OutOfRangeColor);
            _blastPreview.gameObject.SetActive(true);
        }

        private void OnSelect()
        {
            if (_ability.CurrentState != MeteorAbility.State.Aiming) return;

            RefreshAimPoint();
            if (_hasGroundPoint && _aimPointInRange)
                _ability.ConfirmAim(_aimPoint);
        }

        private void OnStateChanged(MeteorAbility.State state)
        {
            bool aiming = state == MeteorAbility.State.Aiming;
            _rangeIndicator.gameObject.SetActive(aiming);
            if (!aiming)
            {
                _hasGroundPoint = false;
                _blastPreview.gameObject.SetActive(false);
            }

            bool falling = state == MeteorAbility.State.Falling;
            if (falling)
            {
                _impactMarker.transform.position = _ability.ImpactPoint;
                ShowFallFx();
            }
            else
            {
                HideFallFx();
            }
            _impactMarker.gameObject.SetActive(falling);
        }

        private void OnImpactReady(Vector3 point)
        {
            HideFallFx();
            if (_matchEnded) return;

            LevelData level = _waveManager.Level;
            if (level == null)
            {
                Debug.LogError($"{nameof(MeteorAimIndicator)} on '{name}': WaveManager has no active LevelData; meteor impact was skipped.", this);
                return;
            }

            _damageApplier.Apply(point, _currentWave, level, this);
            AbilityEvents.RaiseMeteorExploded(point, _settings.Skill.radius);
        }

        private void OnWaveStarted(int wave)
        {
            if (wave < 1)
            {
                Debug.LogError($"{nameof(MeteorAimIndicator)} on '{name}': received invalid wave {wave}.", this);
                return;
            }

            _currentWave = wave;
            _matchEnded = false;
        }

        private void OnMatchEnded(MatchResult result)
        {
            _matchEnded = true;
            HideFallFx();
            _impactMarker.gameObject.SetActive(false);
            if (_ability.CurrentState == MeteorAbility.State.Aiming)
                _ability.OnCancel();
        }

        private void PrepareFallFx()
        {
            if (_fallFxPrefab == null) return;

            _fallFx = Instantiate(_fallFxPrefab);
            _fallFxParticles = _fallFx.GetComponent<ParticleSystem>();
            if (_fallFxParticles == null)
            {
                Debug.LogError($"{nameof(MeteorAimIndicator)}: fall VFX prefab needs a root ParticleSystem.", _fallFxPrefab);
                Destroy(_fallFx);
                _fallFx = null;
                return;
            }
            if (_fallFxParticles.main.loop)
            {
                Debug.LogError($"{nameof(MeteorAimIndicator)}: fall VFX must be a one-shot effect.", _fallFxPrefab);
                Destroy(_fallFx);
                _fallFx = null;
                _fallFxParticles = null;
                return;
            }
            _fallFx.SetActive(false);
        }

        private void ShowFallFx()
        {
            if (_fallFx == null || _fallFxParticles == null) return;

            _fallFx.transform.SetPositionAndRotation(_ability.ImpactPoint, Quaternion.identity);
            _fallFx.transform.localScale = Vector3.one * _settings.Skill.radius;
            _fallFx.SetActive(true);
            _fallFxParticles.Clear(true);
            _fallFxParticles.Play(true);
        }

        private void HideFallFx()
        {
            if (_fallFx == null || _fallFxParticles == null || !_fallFx.activeSelf) return;

            _fallFxParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _fallFx.SetActive(false);
        }

        private void SetRendererColor(Renderer target, Color color)
        {
            target.GetPropertyBlock(_colorProperties);
            _colorProperties.SetColor(BaseColorId, color);
            _colorProperties.SetColor(ColorId, color);
            target.SetPropertyBlock(_colorProperties);
        }

        private static void SetDiameter(Transform target, float diameter)
        {
            Vector3 scale = target.localScale;
            target.localScale = new Vector3(diameter, scale.y, diameter);
        }

        private static bool IsPositiveFinite(float value) =>
            value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);

        private static bool IsFiniteNonNegative(float value) =>
            value >= 0f && !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
