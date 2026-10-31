using System.Collections.Generic;
using FateBastion.Core;
using UnityEngine;
using UnityEngine.Pool;

namespace FateBastion.Audio
{
    /// <summary>
    /// Pooled one-shot effects driven by Core events (C-09): hit spark by element, enemy and boss death, meteor
    /// explosion. Pools are filled in Awake (scene load), so playing never instantiates or allocates. Effects run in
    /// scaled time: twice as fast at ×2, frozen while paused. Lives in the _Audio prefab, one per scene.
    /// </summary>
    // After default scripts, so a CombatEvents.ResetAll in GameManager.Start does not wipe these subscriptions.
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public class FxService : MonoBehaviour
    {
        private struct ActiveFx
        {
            public int Kind;
            public ParticleSystem Instance;
            public float Remaining;
        }

        [SerializeField] private FxSettings _settings;

        private static readonly int KindCount = System.Enum.GetValues(typeof(FxKind)).Length;

        private readonly FxBudget _budget = new FxBudget(KindCount);
        private readonly ObjectPool<ParticleSystem>[] _pools = new ObjectPool<ParticleSystem>[KindCount];
        private readonly int[] _caps = new int[KindCount];
        private readonly float[] _lifetimes = new float[KindCount];
        private List<ActiveFx> _active;
        private bool _started;
        private bool _subscribed;

        private void Awake()
        {
            if (_settings == null)
            {
                Debug.LogError($"{nameof(FxService)} on '{name}': {nameof(_settings)} is not assigned; no FX will play.", this);
                return;
            }

            int total = 0;
            for (int kind = 0; kind < KindCount; kind++) total += CreatePool(kind);
            _active = new List<ActiveFx>(total);
        }

        /// <summary>Sets the asset from code (tests: add to an inactive object, configure, then activate).</summary>
        public void Configure(FxSettings settings) => _settings = settings;

        private void Start()
        {
            _started = true;
            Subscribe();
        }

        private void OnEnable()
        {
            if (_started) Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            ReleaseAll();
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            _subscribed = true;
            CombatEvents.OnDamageDealt += HandleDamageDealt;
            CombatEvents.OnEnemyKilled += HandleEnemyKilled;
            AbilityEvents.OnMeteorExploded += HandleMeteorExploded;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;
            CombatEvents.OnDamageDealt -= HandleDamageDealt;
            CombatEvents.OnEnemyKilled -= HandleEnemyKilled;
            AbilityEvents.OnMeteorExploded -= HandleMeteorExploded;
        }

        private void HandleDamageDealt(DamageDealtInfo info)
        {
            if (FxKindMap.TryGetHit(info.Type, info.Element, out FxKind kind)) Play(kind, info.HitPoint);
        }

        private void HandleEnemyKilled(EnemyKilledInfo info) => Play(FxKindMap.ForDeath(info.IsBoss), info.Position);

        // Explosion prefabs are built for a 1 m radius, so the blast radius is the scale.
        private void HandleMeteorExploded(Vector3 point, float radius) => Play(FxKind.MeteorExplosion, point, radius);

        /// <summary>
        /// Plays <paramref name="kind"/> at <paramref name="position"/>, uniformly scaled. False when the kind has no
        /// prefab, all its copies are alive, or it is still inside its minimum interval.
        /// </summary>
        public bool Play(FxKind kind, Vector3 position, float scale = 1f)
        {
            int k = (int)kind;
            if (_pools[k] == null) return false;
            if (!_budget.TryStart(k, Time.time, _caps[k], _settings.Get(kind).minInterval)) return false;

            ParticleSystem fx = _pools[k].Get();
            Transform t = fx.transform;
            t.SetPositionAndRotation(position, Quaternion.identity);
            t.localScale = new Vector3(scale, scale, scale);
            fx.gameObject.SetActive(true);
            // A pooled copy may still hold particles from its last use; start clean even if Play On Awake is off.
            fx.Clear(true);
            fx.Play(true);

            _active.Add(new ActiveFx { Kind = k, Instance = fx, Remaining = _lifetimes[k] });
            return true;
        }

#if UNITY_EDITOR
        // Right-click the component in Play mode to see each effect 8 m in front of the camera while tuning prefabs.
        [ContextMenu("Test FX/Hit Fire")]
        private void TestHitFire() => Play(FxKind.HitFire, TestPoint());

        [ContextMenu("Test FX/Hit Ice")]
        private void TestHitIce() => Play(FxKind.HitIce, TestPoint());

        [ContextMenu("Test FX/Hit Lightning")]
        private void TestHitLightning() => Play(FxKind.HitLightning, TestPoint());

        [ContextMenu("Test FX/Enemy Death")]
        private void TestEnemyDeath() => Play(FxKind.EnemyDeath, TestPoint());

        [ContextMenu("Test FX/Boss Death")]
        private void TestBossDeath() => Play(FxKind.BossDeath, TestPoint());

        [ContextMenu("Test FX/Meteor Explosion (4 m)")]
        private void TestMeteor() => Play(FxKind.MeteorExplosion, TestPoint(), 4f);

        private Vector3 TestPoint()
        {
            Camera camera = Camera.main;
            if (camera == null) return transform.position;
            Vector3 forward = camera.transform.forward;
            forward.y = 0f;
            Vector3 point = camera.transform.position + forward.normalized * 8f;
            point.y = 0f;
            return point;
        }
#endif

        /// <summary>Copies of <paramref name="kind"/> playing now; for tests and the Profiler pass (C-15).</summary>
        public int CountPlaying(FxKind kind) => _budget.CountPlaying((int)kind);

        private void Update()
        {
            if (_active == null) return;

            float deltaTime = Time.deltaTime;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                ActiveFx fx = _active[i];
                fx.Remaining -= deltaTime;
                if (fx.Remaining > 0f)
                {
                    _active[i] = fx;
                    continue;
                }

                Release(i);
            }
        }

        private void Release(int index)
        {
            ActiveFx fx = _active[index];
            int last = _active.Count - 1;
            _active[index] = _active[last];
            _active.RemoveAt(last);
            _pools[fx.Kind].Release(fx.Instance);
            _budget.Finish(fx.Kind);
        }

        private void ReleaseAll()
        {
            if (_active == null) return;
            for (int i = _active.Count - 1; i >= 0; i--) Release(i);
        }

        /// <summary>Builds and fills the pool of one kind; returns its size (0 when the kind has no prefab).</summary>
        private int CreatePool(int kind)
        {
            FxEntry entry = _settings.Get((FxKind)kind);
            if (entry == null || entry.prefab == null)
            {
                Debug.LogWarning($"{nameof(FxService)}: no prefab for {(FxKind)kind}; that effect is skipped.", this);
                return 0;
            }

            ParticleSystem prefab = entry.prefab;
            int size = Mathf.Max(1, entry.maxConcurrent);
            _caps[kind] = size;
            _lifetimes[kind] = FxLifetime.Of(prefab);

            Transform parent = transform;
            var pool = new ObjectPool<ParticleSystem>(
                () =>
                {
                    ParticleSystem fx = Instantiate(prefab, parent);
                    fx.gameObject.SetActive(false);
                    return fx;
                },
                null,
                fx => fx.gameObject.SetActive(false),
                fx =>
                {
                    if (fx != null) Destroy(fx.gameObject);
                },
                false,
                size,
                size);

            var prewarmed = new ParticleSystem[size];
            for (int i = 0; i < size; i++) prewarmed[i] = pool.Get();
            for (int i = 0; i < size; i++) pool.Release(prewarmed[i]);

            _pools[kind] = pool;
            return size;
        }
    }
}
