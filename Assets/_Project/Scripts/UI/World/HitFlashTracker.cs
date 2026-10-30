using System.Collections.Generic;

namespace FateBastion.UI.World
{
    /// <summary>The enemies flashing right now, ticked together from WorldUiRoot (one loop, no per-enemy Update).</summary>
    public sealed class HitFlashTracker
    {
        private readonly List<HitFlash> _active;

        public HitFlashTracker(int capacity)
        {
            _active = new List<HitFlash>(capacity);
        }

        public int ActiveCount => _active.Count;

        public void Start(HitFlash flash, UnityEngine.Color color, float duration)
        {
            if (flash == null) return;
            // A flash still running is already in the list: restart it in place instead of adding it twice.
            if (!flash.IsFlashing) _active.Add(flash);
            flash.Flash(color, duration);
        }

        public void Stop(HitFlash flash)
        {
            if (flash == null) return;
            flash.Clear();
            _active.Remove(flash);
        }

        public void Tick(float deltaTime)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (_active[i] != null && _active[i].Tick(deltaTime)) continue;

                // Swap-remove: order does not matter.
                int last = _active.Count - 1;
                _active[i] = _active[last];
                _active.RemoveAt(last);
            }
        }

        public void StopAll()
        {
            for (int i = 0; i < _active.Count; i++)
            {
                if (_active[i] != null) _active[i].Clear();
            }
            _active.Clear();
        }
    }
}
