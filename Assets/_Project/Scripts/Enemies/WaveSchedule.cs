using System.Collections.Generic;
using UnityEngine;

namespace FateBastion.Enemies
{
    /// <summary>
    /// Spawn timing of one wave (S2): every SpawnGroup runs in parallel, first spawn at startDelay, then one per
    /// interval. Driven by scaled time, so x2 spawns twice as fast and pause stops it. Reusable via Begin;
    /// Tick allocates nothing once the arrays are big enough.
    /// </summary>
    public sealed class WaveSchedule
    {
        // Float accumulation drifts by ~1e-6 per frame; keeps "spawn at exactly 1 s" from slipping a frame.
        private const float TimeEpsilon = 1e-4f;

        private SpawnGroup[] _groups;
        private bool[] _valid = new bool[0];
        private int[] _spawned = new int[0];
        private float _elapsed;

        public int Wave { get; private set; }
        public int TotalCount { get; private set; }
        public int SpawnedCount { get; private set; }
        public bool IsComplete => SpawnedCount >= TotalCount;

        public void Begin(WaveData data, int wave)
        {
            Wave = wave;
            _elapsed = 0f;
            SpawnedCount = 0;
            TotalCount = 0;
            _groups = data != null ? data.groups : null;

            int groupCount = _groups != null ? _groups.Length : 0;
            if (_valid.Length < groupCount)
            {
                _valid = new bool[groupCount];
                _spawned = new int[groupCount];
            }

            for (int i = 0; i < groupCount; i++)
            {
                SpawnGroup group = _groups[i];
                _spawned[i] = 0;
                _valid[i] = group != null && group.count > 0 && group.enemy != null;

                if (group != null && group.count > 0 && group.enemy == null)
                {
                    Debug.LogWarning($"WaveSchedule: {data.name} group {i} has no EnemyData; its {group.count} enemies are skipped.", data);
                }

                if (_valid[i]) TotalCount += group.count;
            }
        }

        /// <summary>Advances scaled time and appends every spawn that is due to output. Returns how many were added.</summary>
        public int Tick(float deltaTime, List<SpawnRequest> output)
        {
            if (IsComplete || _groups == null) return 0;
            if (deltaTime > 0f) _elapsed += deltaTime;

            int added = 0;
            for (int i = 0; i < _groups.Length; i++)
            {
                if (!_valid[i]) continue;

                SpawnGroup group = _groups[i];
                float interval = Mathf.Max(0f, group.interval);
                while (_spawned[i] < group.count)
                {
                    float due = group.startDelay + _spawned[i] * interval;
                    if (_elapsed + TimeEpsilon < due) break;

                    output.Add(new SpawnRequest(group.enemy, group.pathIndex, Wave));
                    _spawned[i]++;
                    added++;
                }
            }

            SpawnedCount += added;
            return added;
        }
    }
}
