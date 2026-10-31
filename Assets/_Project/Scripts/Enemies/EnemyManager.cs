using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using FateBastion.Combat;

namespace FateBastion.Enemies
{
    /// <summary>
    /// Owns every enemy of the match (S2): one pool per EnemyData, one Update for all enemies, at most
    /// EnemySettings.MaxActiveEnemies alive (the rest queue), and per-wave counting for WaveManager.
    /// Castle (S5) listens to OnEnemyReachedCastle (3.1 #1); world UI (C) listens to OnEnemySpawned/Despawned (3.1 #9).
    /// </summary>
    public class EnemyManager : MonoBehaviour
    {
        [SerializeField] private EnemySettings _settings;
        [SerializeField] private CombatSettings _combatSettings;

        [Tooltip("Boss summoning config. Can also be set at runtime with SetBossSummonSettings.")]
        [SerializeField] private BossSummonSettings _bossSummonSettings;

        [Tooltip("LevelPaths on the map prefab. Can also be set at runtime with SetPaths.")]
        [SerializeField] private LevelPaths _levelPaths;

        /// <summary>An enemy reached the Castle; the argument is its castleDamage. Castle (B) subtracts HP. No gold.</summary>
        public event Action<int> OnEnemyReachedCastle;

        /// <summary>An enemy left the pool and is on the field (attach health bars here, no per-frame scans).</summary>
        public event Action<EnemyController> OnEnemySpawned;

        /// <summary>An enemy is about to return to the pool (killed, reached the Castle, or cleared).</summary>
        public event Action<EnemyController> OnEnemyDespawned;

        /// <summary>For WaveManager: the wave has spawned everything and none of its enemies remain.</summary>
        public event Action<int> OnWaveEnemiesFinished;

        private readonly Dictionary<EnemyData, ObjectPool<EnemyController>> _pools =
            new Dictionary<EnemyData, ObjectPool<EnemyController>>();
        private readonly HashSet<EnemyData> _warnedPoolGrowth = new HashSet<EnemyData>();
        private readonly List<StatusEffectData> _statusVfxData = new List<StatusEffectData>(StatusEffectController.MaxSlowSources + 1);
        private readonly WaveEnemyCounter _counter = new WaveEnemyCounter();

        private IEnemyPathProvider _paths;
        private List<EnemyController> _active;
        private List<EnemyController> _killedDuringUpdate;
        private List<EnemyController> _reachedDuringUpdate;
        private List<SpawnRequest> _summonBuffer;
        private SpawnQueue _queue;

        private bool _initialized;
        private bool _isUpdating;
        private bool _prewarming;
        private float _hpGrowth = 1f;
        private float _hpMultiplier = 1f;

        private static readonly List<EnemyController> EmptyList = new List<EnemyController>(0);

        /// <summary>Enemies on the field (read only; never modify). Queued enemies are not in this list.</summary>
        public IReadOnlyList<EnemyController> ActiveEnemies => _active ?? EmptyList;

        /// <summary>Enemies waiting for a free slot because the cap was reached.</summary>
        public int QueuedCount => _queue != null ? _queue.Count : 0;

        /// <summary>For tests and Sandbox scenes built in code. Call before the manager is used.</summary>
        public void Configure(EnemySettings settings, CombatSettings combatSettings, IEnemyPathProvider paths)
        {
            _settings = settings;
            _combatSettings = combatSettings;
            _paths = paths;
            _initialized = false;
            EnsureInitialized();
        }

        /// <summary>Sets the paths of the loaded map (when they were not dragged in the Inspector).</summary>
        public void SetPaths(IEnemyPathProvider paths)
        {
            _paths = paths;
        }

        /// <summary>Registers a hero status so every pooled enemy can create its looping VFX before the match.</summary>
        public void RegisterStatusVfx(StatusEffectData data)
        {
            if (data == null) return;

            for (int i = 0; i < _statusVfxData.Count; i++)
                if (ReferenceEquals(_statusVfxData[i], data)) return;

            foreach (ObjectPool<EnemyController> pool in _pools.Values)
            {
                if (pool.CountActive > 0)
                {
                    Debug.LogError($"{name}: status VFX must be registered before enemies become active.", this);
                    return;
                }
            }

            _statusVfxData.Add(data);
            foreach (ObjectPool<EnemyController> pool in _pools.Values)
            {
                int inactiveCount = pool.CountInactive;
                for (int i = 0; i < inactiveCount; i++)
                {
                    EnemyController enemy = pool.Get();
                    enemy.RegisterStatusVfx(data);
                    pool.Release(enemy);
                }
            }
        }

        /// <summary>Sets the boss summon config (when it was not dragged in the Inspector).</summary>
        public void SetBossSummonSettings(BossSummonSettings settings)
        {
            _bossSummonSettings = settings;
        }

        /// <summary>HP scaling of the current level; WaveManager.Init sets it from LevelData.</summary>
        public void SetHealthScaling(float hpGrowth, float hpMultiplier)
        {
            _hpGrowth = hpGrowth;
            _hpMultiplier = hpMultiplier;
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        private bool EnsureInitialized()
        {
            if (_initialized) return true;

            if (_settings == null || _combatSettings == null)
            {
                Debug.LogError($"{name}: EnemyManager cần EnemySettings và CombatSettings trong Inspector.", this);
                return false;
            }

            // Unity null check on the component: an unassigned serialized field is a fake-null object in the Editor.
            if (_paths == null && _levelPaths != null) _paths = _levelPaths;
            int capacity = Mathf.Max(_settings.ListCapacity, _settings.MaxActiveEnemies);
            _queue = new SpawnQueue(_settings.MaxActiveEnemies, capacity);
            _active = new List<EnemyController>(capacity);
            _killedDuringUpdate = new List<EnemyController>(capacity);
            _reachedDuringUpdate = new List<EnemyController>(capacity);
            _summonBuffer = new List<SpawnRequest>(capacity);
            _initialized = true;
            return true;
        }

        /// <summary>Fills the pools for a level: per type, its largest count in one wave + ExtraPrewarmPerType.</summary>
        public void Prewarm(LevelData level)
        {
            if (!EnsureInitialized() || level == null || level.waves == null) return;

            // Load time only, so the temporary dictionary is fine.
            var maxPerType = new Dictionary<EnemyData, int>();
            foreach (WaveData wave in level.waves)
            {
                if (wave == null || wave.groups == null) continue;
                foreach (SpawnGroup group in wave.groups)
                {
                    if (group == null || group.enemy == null || group.count <= 0) continue;
                    maxPerType.TryGetValue(group.enemy, out int current);
                    if (group.count > current) maxPerType[group.enemy] = group.count;
                }
            }

            int cap = _settings.MaxActiveEnemies;
            foreach (KeyValuePair<EnemyData, int> pair in maxPerType)
            {
                Prewarm(pair.Key, Mathf.Min(pair.Value, cap) + _settings.ExtraPrewarmPerType);
            }
        }

        /// <summary>Makes sure at least 'count' instances of this enemy exist in its pool.</summary>
        public void Prewarm(EnemyData data, int count)
        {
            if (!EnsureInitialized()) return;
            ObjectPool<EnemyController> pool = GetOrCreatePool(data);
            if (pool == null) return;

            int missing = count - pool.CountAll;
            if (missing <= 0) return;

            _prewarming = true;
            var created = new List<EnemyController>(missing);
            for (int i = 0; i < missing; i++) created.Add(pool.Get());
            foreach (EnemyController enemy in created) pool.Release(enemy);
            _prewarming = false;
        }

        /// <summary>
        /// Spawns an enemy now, or queues it when the cap is reached. wave &lt;= 0 means no wave tracks it.
        /// Boss summons pass the boss's wave and distance so the wave waits for them (S2).
        /// </summary>
        public void RequestSpawn(EnemyData data, int pathIndex, int wave, float startDistance = 0f)
        {
            RequestSpawn(new SpawnRequest(data, pathIndex, wave, startDistance));
        }

        public void RequestSpawn(in SpawnRequest request)
        {
            if (!EnsureInitialized()) return;
            if (request.Enemy == null)
            {
                Debug.LogError($"{name}: RequestSpawn với EnemyData null.", this);
                return;
            }
            if (GetOrCreatePool(request.Enemy) == null) return;

            _counter.Add(request.Wave);
            if (_queue.Admit(request, _active.Count)) SpawnNow(request);
        }

        /// <summary>For WaveManager: starts counting a wave.</summary>
        public void BeginWave(int wave)
        {
            _counter.BeginWave(wave);
        }

        /// <summary>For WaveManager: the wave's schedule has requested every enemy.</summary>
        public void MarkWaveScheduleComplete(int wave)
        {
            if (_counter.MarkScheduleComplete(wave)) OnWaveEnemiesFinished?.Invoke(wave);
        }

        /// <summary>Enemies of this wave still queued or alive (boss summons included).</summary>
        public int GetOutstandingCount(int wave) => _counter.GetOutstanding(wave);

        /// <summary>Returns every enemy to its pool and empties the queue. Raises no kill, Castle or wave events.</summary>
        public void ClearAll()
        {
            if (!_initialized) return;

            _queue.Clear();
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                EnemyController enemy = _active[i];
                enemy.ActiveIndex = -1;
                OnEnemyDespawned?.Invoke(enemy);
                ReturnToPool(enemy);
            }
            _active.Clear();
            _killedDuringUpdate.Clear();
            _reachedDuringUpdate.Clear();
            _summonBuffer.Clear();
            _counter.Clear();
        }

        /// <summary>Called by EnemyController after it raised OnEnemyKilled.</summary>
        internal void NotifyKilled(EnemyController enemy)
        {
            if (_isUpdating) _killedDuringUpdate.Add(enemy);
            else Despawn(enemy);
        }

        private void Update()
        {
            if (!_initialized) return;

            float deltaTime = Time.deltaTime;
            _isUpdating = true;
            _summonBuffer.Clear();
            for (int i = 0; i < _active.Count; i++)
            {
                EnemyController enemy = _active[i];
                if (enemy.Tick(deltaTime, _summonBuffer)) _reachedDuringUpdate.Add(enemy);
            }
            _isUpdating = false;

            // Event handlers below may call ClearAll (match over), which empties these lists: re-check Count each step.
            for (int i = 0; i < _killedDuringUpdate.Count; i++) Despawn(_killedDuringUpdate[i]);
            _killedDuringUpdate.Clear();

            for (int i = 0; i < _reachedDuringUpdate.Count; i++)
            {
                EnemyController enemy = _reachedDuringUpdate[i];
                // Castle damage first, so a lost match is decided before the wave can be reported finished.
                OnEnemyReachedCastle?.Invoke(enemy.Data.castleDamage);
                Despawn(enemy);
            }
            _reachedDuringUpdate.Clear();

            // Boss summons collected during Tick, spawned after the loop so the active list is not modified while walking it.
            for (int i = 0; i < _summonBuffer.Count; i++) RequestSpawn(_summonBuffer[i]);
            _summonBuffer.Clear();

            while (_queue.TryRelease(_active.Count, out SpawnRequest request)) SpawnNow(request);
        }

        private void SpawnNow(in SpawnRequest request)
        {
            IEnemyPath path = _paths?.GetPath(request.PathIndex);
            if (path == null)
            {
                if (_paths == null) Debug.LogError($"{name}: EnemyManager chưa có LevelPaths; không sinh được {request.Enemy.id}.", this);
                DropRequest(request.Wave);
                return;
            }

            EnemyData data = request.Enemy;
            EnemyController enemy = _pools[data].Get();
            float maxHP = EnemyHealthFormula.Calculate(data.baseHP, _hpGrowth, request.Wave, _hpMultiplier);
            enemy.Spawn(data, path, request.PathIndex, maxHP, request.Wave, request.StartDistance);
            enemy.gameObject.SetActive(true);

            enemy.ActiveIndex = _active.Count;
            _active.Add(enemy);
            OnEnemySpawned?.Invoke(enemy);
        }

        private void Despawn(EnemyController enemy)
        {
            int index = enemy.ActiveIndex;
            if (index < 0 || index >= _active.Count || _active[index] != enemy) return;

            // Swap-remove: O(1), no allocation. Order of the active list is not meaningful.
            int last = _active.Count - 1;
            EnemyController moved = _active[last];
            _active[index] = moved;
            moved.ActiveIndex = index;
            _active.RemoveAt(last);
            enemy.ActiveIndex = -1;

            int wave = enemy.Wave;
            OnEnemyDespawned?.Invoke(enemy);
            ReturnToPool(enemy);
            DropRequest(wave);
        }

        private void ReturnToPool(EnemyController enemy)
        {
            enemy.OnDespawn();
            if (enemy.Data != null && _pools.TryGetValue(enemy.Data, out ObjectPool<EnemyController> pool)) pool.Release(enemy);
            else enemy.gameObject.SetActive(false);
        }

        private void DropRequest(int wave)
        {
            if (_counter.Remove(wave)) OnWaveEnemiesFinished?.Invoke(wave);
        }

        private ObjectPool<EnemyController> GetOrCreatePool(EnemyData data)
        {
            if (data == null) return null;
            if (_pools.TryGetValue(data, out ObjectPool<EnemyController> pool)) return pool;

            if (data.prefab == null || data.prefab.GetComponent<EnemyController>() == null)
            {
                Debug.LogError($"{name}: EnemyData {data.id} thiếu prefab có EnemyController.", data);
                return null;
            }

            pool = new ObjectPool<EnemyController>(
                createFunc: () => CreateInstance(data),
                actionOnGet: null,
                actionOnRelease: enemy => enemy.gameObject.SetActive(false),
                actionOnDestroy: enemy => { if (enemy != null) Destroy(enemy.gameObject); },
                collectionCheck: false,
                defaultCapacity: 16,
                maxSize: 1000);
            _pools.Add(data, pool);
            return pool;
        }

        private EnemyController CreateInstance(EnemyData data)
        {
            if (!_prewarming && _warnedPoolGrowth.Add(data))
            {
                Debug.LogWarning($"{name}: pool {data.id} vượt số đã prewarm, phải tạo thêm trong trận. Tăng ExtraPrewarmPerType trong EnemySettings.", this);
            }

            GameObject go = Instantiate(data.prefab, transform);
            go.name = data.prefab.name;
            go.SetActive(false);
            EnemyController enemy = go.GetComponent<EnemyController>();
            enemy.Initialize(this, _combatSettings, _bossSummonSettings);
            for (int i = 0; i < _statusVfxData.Count; i++)
                enemy.RegisterStatusVfx(_statusVfxData[i]);
            return enemy;
        }

        private void OnDestroy()
        {
            foreach (ObjectPool<EnemyController> pool in _pools.Values) pool.Clear();
            _pools.Clear();
        }
    }
}
