using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using FateBastion.Enemies;

namespace FateBastion.UI.World
{
    /// <summary>
    /// Gives each enemy on the field a pooled health bar: Attach on OnEnemySpawned, Detach on OnEnemyDespawned
    /// (TEAM_ASSIGNMENT 3.1 #9). HP comes from OnHealthChanged; the only per-frame work is positioning damaged bars
    /// and ticking hit flashes (S11).
    /// </summary>
    public sealed class WorldHealthBarPresenter
    {
        // Pool upper bound; far above the 100-enemy cap, only there so a bug cannot grow it forever.
        private const int MaxPooledBars = 512;

        private readonly WorldHealthBar _prefab;
        private readonly RectTransform _parent;
        private readonly WorldUiSettings _settings;
        private readonly ObjectPool<WorldHealthBar> _bars;
        private readonly Stack<EnemyUiBinding> _freeBindings;
        private readonly Dictionary<EnemyController, EnemyUiBinding> _byEnemy;
        private readonly List<EnemyUiBinding> _bound;
        private readonly HitFlashTracker _flashes;

        public WorldHealthBarPresenter(WorldHealthBar prefab, RectTransform parent, WorldUiSettings settings)
        {
            _prefab = prefab;
            _parent = parent;
            _settings = settings;

            int capacity = Mathf.Max(settings.BindingCapacity, settings.HealthBarPrewarm);
            _bars = new ObjectPool<WorldHealthBar>(CreateBar, null, bar => bar.Hide(), DestroyBar,
                collectionCheck: false, defaultCapacity: capacity, maxSize: MaxPooledBars);
            _freeBindings = new Stack<EnemyUiBinding>(capacity);
            _byEnemy = new Dictionary<EnemyController, EnemyUiBinding>(capacity);
            _bound = new List<EnemyUiBinding>(capacity);
            _flashes = new HitFlashTracker(capacity);

            Prewarm(settings.HealthBarPrewarm);
        }

        /// <summary>Enemies that currently have a bar.</summary>
        public int BoundCount => _bound.Count;

        /// <summary>Bars instantiated so far (pooled + in use); stays flat during a match after prewarm.</summary>
        public int CreatedCount => _bars.CountAll;

        /// <summary>Bars currently drawn. Loops over the bound list: for tests and the debug panel, not per frame.</summary>
        public int VisibleCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _bound.Count; i++)
                    if (_bound[i].Bar.IsShown) count++;
                return count;
            }
        }

        /// <summary>Enemies flashing white right now.</summary>
        public int FlashingCount => _flashes.ActiveCount;

        public bool TryGetBar(EnemyController enemy, out WorldHealthBar bar)
        {
            if (enemy != null && _byEnemy.TryGetValue(enemy, out EnemyUiBinding binding))
            {
                bar = binding.Bar;
                return true;
            }
            bar = null;
            return false;
        }

        public void Attach(EnemyController enemy)
        {
            if (enemy == null || _byEnemy.ContainsKey(enemy)) return;

            WorldHealthBar bar = _bars.Get();
            bar.ResetState(enemy.IsBoss, _settings.BossBarScale);
            // An enemy can already be damaged when the UI is enabled mid-match.
            bar.SetHealth(enemy.CurrentHP, enemy.MaxHP);

            EnemyUiBinding binding = _freeBindings.Count > 0 ? _freeBindings.Pop() : new EnemyUiBinding();
            // Spawn time, not per frame: one lookup per spawn for the variant's HitFlash (may be absent).
            HitFlash flash = enemy.GetComponentInChildren<HitFlash>(true);
            binding.Bind(enemy, bar, flash, _flashes, _settings);
            binding.Index = _bound.Count;
            _bound.Add(binding);
            _byEnemy.Add(enemy, binding);
        }

        public void Detach(EnemyController enemy)
        {
            if (enemy == null || !_byEnemy.TryGetValue(enemy, out EnemyUiBinding binding)) return;
            _byEnemy.Remove(enemy);

            // Swap-remove: O(1), order is not meaningful.
            int index = binding.Index;
            int last = _bound.Count - 1;
            EnemyUiBinding moved = _bound[last];
            _bound[index] = moved;
            moved.Index = index;
            _bound.RemoveAt(last);

            WorldHealthBar bar = binding.Bar;
            binding.Unbind();
            _bars.Release(bar);
            _freeBindings.Push(binding);
        }

        public void DetachAll()
        {
            for (int i = _bound.Count - 1; i >= 0; i--) Detach(_bound[i].Enemy);
        }

        /// <summary>Fades the hit flashes; called once per frame by WorldUiRoot.</summary>
        public void TickFlashes(float deltaTime) => _flashes.Tick(deltaTime);

        /// <summary>Moves every damaged bar over its enemy; bars of enemies behind the camera are hidden.</summary>
        public void UpdatePositions(Camera camera)
        {
            if (camera == null) return;

            for (int i = 0; i < _bound.Count; i++)
            {
                EnemyUiBinding binding = _bound[i];
                WorldHealthBar bar = binding.Bar;
                if (!bar.IsDamaged) continue;

                Vector3 screenPoint = camera.WorldToScreenPoint(binding.Enemy.HealthBarAnchor.position);
                bool inFront = screenPoint.z > 0f;
                bar.SetOnScreen(inFront);
                if (inFront) bar.SetScreenPosition(screenPoint);
            }
        }

        private void Prewarm(int count)
        {
            if (count <= 0) return;
            // Load time only, so the temporary array is fine.
            var created = new WorldHealthBar[count];
            for (int i = 0; i < count; i++) created[i] = _bars.Get();
            for (int i = 0; i < count; i++) _bars.Release(created[i]);
        }

        private WorldHealthBar CreateBar()
        {
            WorldHealthBar bar = Object.Instantiate(_prefab, _parent, false);
            bar.InitializeHidden();
            return bar;
        }

        private static void DestroyBar(WorldHealthBar bar)
        {
            if (bar != null) Object.Destroy(bar.gameObject);
        }
    }
}
