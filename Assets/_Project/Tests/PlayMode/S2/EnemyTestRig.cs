using System.Collections.Generic;
using UnityEngine;
using FateBastion.Core;
using FateBastion.Combat;
using FateBastion.Enemies;

namespace FateBastion.Tests.PlayMode.S2
{
    /// <summary>Straight path along +X from the origin, so PlayMode tests do not depend on a map prefab.</summary>
    public sealed class StraightTestPath : IEnemyPath
    {
        public StraightTestPath(float length)
        {
            Length = length;
        }

        public float Length { get; }

        public void Evaluate(float distance, out Vector3 position, out Vector3 forward)
        {
            position = new Vector3(Mathf.Clamp(distance, 0f, Length), 0f, 0f);
            forward = Vector3.right;
        }
    }

    /// <summary>Gives the same straight path for every path index.</summary>
    public sealed class TestPathProvider : IEnemyPathProvider
    {
        private readonly IEnemyPath _path;

        public TestPathProvider(IEnemyPath path)
        {
            _path = path;
        }

        public IEnemyPath GetPath(int pathIndex) => _path;
    }

    /// <summary>
    /// Builds an EnemyManager (+ optional WaveManager) with settings, a straight path and an enemy "prefab"
    /// created in code, and counts every event. Dispose in TearDown.
    /// </summary>
    public sealed class EnemyTestRig
    {
        public readonly GameObject Root;
        public readonly EnemyManager Manager;
        public readonly EnemySettings Settings;
        public readonly CombatSettings CombatSettings;

        public int ReachedCount;
        public int LastCastleDamage;
        public int SpawnedCount;
        public int DespawnedCount;
        public int KilledCount;
        public EnemyKilledInfo LastKilled;
        public int MaxActiveSeen;

        private readonly List<Object> _owned = new List<Object>();

        public EnemyTestRig(float pathLength)
        {
            Settings = ScriptableObject.CreateInstance<EnemySettings>();
            CombatSettings = ScriptableObject.CreateInstance<CombatSettings>();
            _owned.Add(Settings);
            _owned.Add(CombatSettings);

            // Inactive while wiring, so Awake runs after Configure.
            Root = new GameObject("EnemyTestRig");
            Root.SetActive(false);
            Manager = Root.AddComponent<EnemyManager>();
            Manager.Configure(Settings, CombatSettings, new TestPathProvider(new StraightTestPath(pathLength)));
            Root.SetActive(true);

            Manager.OnEnemyReachedCastle += damage =>
            {
                ReachedCount++;
                LastCastleDamage = damage;
            };
            Manager.OnEnemySpawned += _ =>
            {
                SpawnedCount++;
                if (Manager.ActiveEnemies.Count > MaxActiveSeen) MaxActiveSeen = Manager.ActiveEnemies.Count;
            };
            Manager.OnEnemyDespawned += _ => DespawnedCount++;
            CombatEvents.OnEnemyKilled += OnKilled;
        }

        private void OnKilled(EnemyKilledInfo info)
        {
            KilledCount++;
            LastKilled = info;
        }

        /// <summary>Enemy data with an inactive template GameObject as its prefab.</summary>
        public EnemyData CreateEnemy(string id, float baseHP = 100f, float moveSpeed = 1f, int castleDamage = 1,
            int goldReward = 5, bool flying = false, bool boss = false)
        {
            var template = new GameObject("Template_" + id);
            template.SetActive(false);
            template.AddComponent<EnemyController>();
            _owned.Add(template);

            var data = ScriptableObject.CreateInstance<EnemyData>();
            data.id = id;
            data.baseHP = baseHP;
            data.moveSpeed = moveSpeed;
            data.castleDamage = castleDamage;
            data.goldReward = goldReward;
            data.flying = flying;
            data.boss = boss;
            data.prefab = template;
            _owned.Add(data);
            return data;
        }

        public WaveData CreateWave(params SpawnGroup[] groups)
        {
            var wave = ScriptableObject.CreateInstance<WaveData>();
            wave.groups = groups;
            _owned.Add(wave);
            return wave;
        }

        public LevelData CreateLevel(params WaveData[] waves)
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.waves = waves;
            level.hpGrowth = 1f;
            level.hpMultiplier = 1f;
            _owned.Add(level);
            return level;
        }

        /// <summary>Boss summon config for the manager under test (A-05).</summary>
        public BossSummonSettings CreateBossSummonSettings(EnemyData summonEnemy, int count = 3, float intervalSeconds = 12f)
        {
            var settings = ScriptableObject.CreateInstance<BossSummonSettings>();
            settings.Configure(summonEnemy, count, intervalSeconds);
            _owned.Add(settings);
            return settings;
        }

        public WaveManager AddWaveManager()
        {
            var go = new GameObject("WaveManager");
            go.SetActive(false);
            var waves = go.AddComponent<WaveManager>();
            waves.Configure(Manager);
            go.SetActive(true);
            _owned.Add(go);
            return waves;
        }

        public static DamageInfo TrueHit(float amount, bool canHitFlying = true)
        {
            return new DamageInfo(amount, DamageType.True, Element.Fire, null, canHitFlying, null, Vector3.zero);
        }

        /// <summary>Kills every active enemy; iterates a copy because kills despawn immediately.</summary>
        public void KillAllActive()
        {
            var copy = new List<EnemyController>(Manager.ActiveEnemies);
            foreach (EnemyController enemy in copy) enemy.TakeDamage(TrueHit(1e9f));
        }

        public void Dispose()
        {
            CombatEvents.OnEnemyKilled -= OnKilled;
            CombatEvents.ResetAll();
            Object.Destroy(Root);
            foreach (Object o in _owned)
                if (o != null) Object.Destroy(o);
        }
    }
}
