using System.Collections.Generic;
using UnityEngine;
using FateBastion.Enemies;

namespace FateBastion.Heroes
{
    /// <summary>
    /// Pool of placed heroes (S4, TEAM_ASSIGNMENT 3.1 #3). <see cref="Prewarm"/> creates every copy a deck can ever
    /// place while the match loads, so placing never instantiates. Get: ResetState → Init(heroData) →
    /// SetEnemyManager. Release: OnDespawn first, then back to the pool.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HeroPool : MonoBehaviour
    {
        private const string HeroLayerName = "Hero";

        [Tooltip("Hero_Base (A): used for heroes whose HeroData has no prefab yet.")]
        [SerializeField] private HeroController _fallbackPrefab;

        [Tooltip("Optional level-3 aura FX (C) given to every HeroLevel.")]
        [SerializeField] private GameObject _topLevelAuraPrefab;

        [Tooltip("Parent of pooled heroes. Empty = this object.")]
        [SerializeField] private Transform _container;

        private readonly Dictionary<HeroData, Stack<HeroLevel>> _free = new Dictionary<HeroData, Stack<HeroLevel>>();
        private readonly Dictionary<HeroData, int> _created = new Dictionary<HeroData, int>();
        private readonly List<HeroLevel> _active = new List<HeroLevel>(16);
        private HeroLevelTable _levelTable;
        private EnemyManager _enemyManager;
        private int _heroLayer = -1;

        /// <summary>Heroes on the field now.</summary>
        public IReadOnlyList<HeroLevel> Active => _active;

        private Transform Container => _container != null ? _container : transform;

        /// <summary>For tests and scenes built in code.</summary>
        public void Configure(HeroController fallbackPrefab, GameObject topLevelAuraPrefab = null)
        {
            _fallbackPrefab = fallbackPrefab;
            _topLevelAuraPrefab = topLevelAuraPrefab;
        }

        public void SetEnemyManager(EnemyManager enemyManager) => _enemyManager = enemyManager;

        /// <summary>
        /// Creates up to max-per-hero copies of every hero of the deck (load time). Calling it again only tops up.
        /// </summary>
        public void Prewarm(IReadOnlyList<HeroData> heroes, PlacementRules rules, HeroLevelTable levelTable, EnemyManager enemyManager)
        {
            _levelTable = levelTable;
            _enemyManager = enemyManager;
            _heroLayer = LayerMask.NameToLayer(HeroLayerName);
            if (_heroLayer < 0) Debug.LogError($"{name}: layer '{HeroLayerName}' is missing (Tags & Layers).", this);
            if (heroes == null) return;

            if (_enemyManager != null)
            {
                for (int i = 0; i < heroes.Count; i++)
                {
                    HeroData data = heroes[i];
                    if (data == null) continue;
                    if (data.passiveSkill != null) _enemyManager.RegisterStatusVfx(data.passiveSkill.status);
                    if (data.ultimateSkill != null) _enemyManager.RegisterStatusVfx(data.ultimateSkill.status);
                }
            }

            for (int i = 0; i < heroes.Count; i++)
            {
                HeroData data = heroes[i];
                if (data == null) continue;

                _created.TryGetValue(data, out int created);
                int target = rules.MaxPerHeroOf(data.rarity);
                for (int n = created; n < target; n++)
                {
                    HeroLevel hero = Create(data);
                    if (hero == null) break;
                    FreeStack(data).Push(hero);
                }
            }
        }

        /// <summary>Takes an initialized hero for <paramref name="data"/>; null when the pool cannot create it.</summary>
        public HeroLevel Get(HeroData data, Vector3 position, Quaternion rotation)
        {
            if (data == null) return null;

            Stack<HeroLevel> free = FreeStack(data);
            HeroLevel hero;
            if (free.Count > 0)
            {
                hero = free.Pop();
            }
            else
            {
                // Should not happen: Prewarm creates the per-hero cap. Kept as a safety net, loudly.
                Debug.LogWarning($"{name}: pool for '{data.id}' was empty; creating a hero during the match.", this);
                hero = Create(data);
                if (hero == null) return null;
            }

            Transform t = hero.transform;
            t.SetPositionAndRotation(position, rotation);
            hero.gameObject.SetActive(true);

            HeroController controller = hero.Controller;
            controller.ResetState();
            controller.Init(data);
            if (_enemyManager != null) controller.SetEnemyManager(_enemyManager);

            _active.Add(hero);
            return hero;
        }

        /// <summary>Returns a hero taken with <see cref="Get"/>.</summary>
        public void Release(HeroLevel hero)
        {
            if (hero == null || !_active.Remove(hero)) return;

            HeroData data = hero.Data;
            hero.Controller.OnDespawn();
            hero.Clear();
            hero.gameObject.SetActive(false);

            if (data != null)
            {
                FreeStack(data).Push(hero);
            }
            else
            {
                Debug.LogError($"{name}: released hero '{hero.name}' had no HeroData; it is dropped from the pool.", hero);
            }
        }

        /// <summary>End of the match: every hero back to the pool (S4).</summary>
        public void ReleaseAll()
        {
            for (int i = _active.Count - 1; i >= 0; i--) Release(_active[i]);
        }

        private Stack<HeroLevel> FreeStack(HeroData data)
        {
            if (!_free.TryGetValue(data, out Stack<HeroLevel> stack))
            {
                stack = new Stack<HeroLevel>(8);
                _free.Add(data, stack);
            }

            return stack;
        }

        private HeroLevel Create(HeroData data)
        {
            HeroController prefab = data.prefab != null ? data.prefab.GetComponent<HeroController>() : null;
            if (prefab == null) prefab = _fallbackPrefab;
            if (prefab == null)
            {
                Debug.LogError($"{name}: '{data.id}' has no prefab and HeroPool has no fallback (Hero_Base).", this);
                return null;
            }

            HeroController instance = Instantiate(prefab, Container);
            GameObject go = instance.gameObject;
            go.name = $"{data.id}_{CountCreated(data)}";

            // ASSUMPTION: the hero's selection collider sits on the root, which the pool moves to the Hero layer so
            // the placement CheckSphere and hero picking see it. Hero_Base ships on Default.
            if (_heroLayer >= 0) go.layer = _heroLayer;

            // Hero_Base carries HeroLevel; a hero prefab that is not a Variant of it would crash Setup, so fail loudly instead.
            HeroLevel level = go.GetComponent<HeroLevel>();
            if (level == null)
            {
                Debug.LogError($"{name}: prefab of '{data.id}' has no HeroLevel; make it a Variant of Hero_Base.", prefab);
                Destroy(go);
                return null;
            }

            level.Setup(_levelTable, _topLevelAuraPrefab);
            instance.PrewarmSkillVfx(data.passiveSkill, data.ultimateSkill);

            go.SetActive(false);
            return level;
        }

        private int CountCreated(HeroData data)
        {
            _created.TryGetValue(data, out int created);
            created++;
            _created[data] = created;
            return created;
        }
    }
}
