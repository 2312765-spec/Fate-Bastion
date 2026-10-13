using System.Collections.Generic;
using UnityEngine;

namespace FateBastion.Enemies
{
    /// <summary>
    /// Caps how many enemies are alive at once (S2). Requests over the cap wait in FIFO order and still count
    /// toward their wave, so the wave cannot end while some are queued.
    /// </summary>
    public sealed class SpawnQueue
    {
        private readonly Queue<SpawnRequest> _waiting;

        public SpawnQueue(int maxActive, int initialCapacity = 64)
        {
            MaxActive = Mathf.Max(1, maxActive);
            _waiting = new Queue<SpawnRequest>(initialCapacity);
        }

        public int MaxActive { get; }
        public int Count => _waiting.Count;

        /// <summary>True: spawn it now. False: it was queued (cap reached, or older requests are still waiting).</summary>
        public bool Admit(in SpawnRequest request, int activeCount)
        {
            if (_waiting.Count == 0 && activeCount < MaxActive) return true;
            _waiting.Enqueue(request);
            return false;
        }

        /// <summary>Takes the oldest waiting request when a slot is free.</summary>
        public bool TryRelease(int activeCount, out SpawnRequest request)
        {
            if (_waiting.Count > 0 && activeCount < MaxActive)
            {
                request = _waiting.Dequeue();
                return true;
            }
            request = default;
            return false;
        }

        public void Clear() => _waiting.Clear();
    }
}
