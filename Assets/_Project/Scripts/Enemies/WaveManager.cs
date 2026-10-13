using System;
using System.Collections.Generic;
using UnityEngine;

namespace FateBastion.Enemies
{
    /// <summary>
    /// Runs the waves of a level (S2). GameManager (S5) calls Init, StartWave and StopAndClear and listens to
    /// OnWaveFinished (TEAM_ASSIGNMENT 3.1 #2). Spawn timing uses scaled time; several waves may run at once
    /// and each one reports OnWaveFinished on its own. Only GameManager raises GameEvents.OnWaveStarted/Cleared.
    /// </summary>
    public class WaveManager : MonoBehaviour
    {
        [SerializeField] private EnemyManager _enemyManager;

        /// <summary>The wave spawned everything and none of its enemies remain (boss summons included). Raised once per wave.</summary>
        public event Action<int> OnWaveFinished;

        private readonly List<WaveSchedule> _running = new List<WaveSchedule>(4);
        private readonly Stack<WaveSchedule> _spare = new Stack<WaveSchedule>(4);
        private readonly List<SpawnRequest> _buffer = new List<SpawnRequest>(64);

        private LevelData _level;
        private EndlessSettings _endless;
        private bool _subscribed;

        public LevelData Level => _level;

        /// <summary>Number of waves in the level (15); Endless continues past it.</summary>
        public int WaveCount => _level != null && _level.waves != null ? _level.waves.Length : 0;

        /// <summary>For tests and Sandbox scenes built in code.</summary>
        public void Configure(EnemyManager enemyManager)
        {
            Unsubscribe();
            _enemyManager = enemyManager;
            if (isActiveAndEnabled) Subscribe();
        }

        private void Awake()
        {
            if (_enemyManager == null) Debug.LogError($"{name}: kéo EnemyManager vào WaveManager.", this);
        }

        private void OnEnable() => Subscribe();
        private void OnDisable() => Unsubscribe();

        /// <summary>Prepares a level: clears the field, sets HP scaling and prewarms the pools. endless = null in campaign.</summary>
        public void Init(LevelData level, EndlessSettings endless)
        {
            StopAndClear();
            _level = level;
            _endless = endless;

            if (level == null)
            {
                Debug.LogError($"{name}: WaveManager.Init với LevelData null.", this);
                return;
            }
            if (_enemyManager == null) return;

            _enemyManager.SetHealthScaling(level.hpGrowth, level.hpMultiplier);
            _enemyManager.Prewarm(level);
        }

        /// <summary>Starts spawning a wave (1-based). Waves still running keep going.</summary>
        public void StartWave(int wave)
        {
            if (_enemyManager == null) return;
            WaveData data = ResolveWave(wave);
            if (data == null) return;

            WaveSchedule schedule = _spare.Count > 0 ? _spare.Pop() : new WaveSchedule();
            schedule.Begin(data, wave);
            _enemyManager.BeginWave(wave);
            _running.Add(schedule);

            // Groups with startDelay 0 spawn this frame.
            TickSchedules(0f);
        }

        /// <summary>Stops every schedule and returns all enemies to the pool. Raises no OnWaveFinished.</summary>
        public void StopAndClear()
        {
            for (int i = 0; i < _running.Count; i++) _spare.Push(_running[i]);
            _running.Clear();
            if (_enemyManager != null) _enemyManager.ClearAll();
        }

        private void Update()
        {
            if (_running.Count > 0) TickSchedules(Time.deltaTime);
        }

        private void TickSchedules(float deltaTime)
        {
            for (int i = _running.Count - 1; i >= 0; i--)
            {
                // A handler of OnWaveFinished may have called StopAndClear or StartWave.
                if (i >= _running.Count) continue;

                WaveSchedule schedule = _running[i];
                _buffer.Clear();
                schedule.Tick(deltaTime, _buffer);
                for (int j = 0; j < _buffer.Count; j++) _enemyManager.RequestSpawn(_buffer[j]);

                if (!schedule.IsComplete || i >= _running.Count || _running[i] != schedule) continue;

                _running.RemoveAt(i);
                _spare.Push(schedule);
                _enemyManager.MarkWaveScheduleComplete(schedule.Wave);
            }
        }

        private WaveData ResolveWave(int wave)
        {
            if (_level == null)
            {
                Debug.LogError($"{name}: gọi WaveManager.Init trước StartWave.", this);
                return null;
            }

            if (wave >= 1 && wave <= WaveCount)
            {
                WaveData data = _level.waves[wave - 1];
                if (data == null) Debug.LogError($"{name}: {_level.name} thiếu WaveData cho wave {wave}.", _level);
                return data;
            }

            // STUB: Vô tận (wave > 15 lặp wave loopFromWave..loopToWave của màn 3) – thay bằng A-06.
            string endlessNote = _endless != null ? " (Vô tận làm ở A-06)" : string.Empty;
            Debug.LogError($"{name}: {_level.name} không có wave {wave}{endlessNote}.", _level);
            return null;
        }

        private void HandleWaveEnemiesFinished(int wave)
        {
            OnWaveFinished?.Invoke(wave);
        }

        private void Subscribe()
        {
            if (_subscribed || _enemyManager == null) return;
            _enemyManager.OnWaveEnemiesFinished += HandleWaveEnemiesFinished;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            if (_enemyManager != null) _enemyManager.OnWaveEnemiesFinished -= HandleWaveEnemiesFinished;
            _subscribed = false;
        }
    }
}
