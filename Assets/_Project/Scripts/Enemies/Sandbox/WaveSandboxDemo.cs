#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using FateBastion.Core;
using FateBastion.Combat;

namespace FateBastion.Enemies.Sandbox
{
    /// <summary>
    /// Sandbox/A_Enemies driver for the S2 handoff: on-screen buttons start waves, kill everything or clear the
    /// field, and show active/queued enemies, Castle damage, kills and finished waves. Plays the role GameManager
    /// (S5) will have. Uses OnGUI buttons, not keys, so it does not bypass InputRouter.
    /// </summary>
    public class WaveSandboxDemo : MonoBehaviour
    {
        [SerializeField] private WaveManager _waveManager;
        [SerializeField] private EnemyManager _enemyManager;
        [SerializeField] private LevelData _level;

        [Tooltip("Start the next wave automatically when one finishes, like the Resting state will.")]
        [SerializeField] private bool _autoNextWave = true;

        [SerializeField, Min(0f)] private float _restSeconds = 3f;

        private int _nextWave = 1;
        private int _castleDamageTaken;
        private int _kills;
        private int _goldFromKills;
        private string _finished = "";
        private float _restTimer = -1f;

        private void Awake()
        {
            if (_waveManager == null || _enemyManager == null || _level == null)
            {
                Debug.LogError($"{name}: kéo WaveManager, EnemyManager và LevelData vào WaveSandboxDemo.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (_waveManager == null || _enemyManager == null) return;
            _waveManager.OnWaveFinished += HandleWaveFinished;
            _enemyManager.OnEnemyReachedCastle += HandleReachedCastle;
            CombatEvents.OnEnemyKilled += HandleKilled;
        }

        private void OnDisable()
        {
            if (_waveManager == null || _enemyManager == null) return;
            _waveManager.OnWaveFinished -= HandleWaveFinished;
            _enemyManager.OnEnemyReachedCastle -= HandleReachedCastle;
            CombatEvents.OnEnemyKilled -= HandleKilled;
        }

        private void Start()
        {
            _waveManager.Init(_level, null);
        }

        private void Update()
        {
            if (_restTimer < 0f) return;
            _restTimer -= Time.deltaTime;
            if (_restTimer <= 0f)
            {
                _restTimer = -1f;
                StartNextWave();
            }
        }

        private void StartNextWave()
        {
            if (_nextWave > _waveManager.WaveCount) return;
            _waveManager.StartWave(_nextWave);
            _nextWave++;
        }

        private void HandleWaveFinished(int wave)
        {
            _finished += wave + " ";
            if (_autoNextWave && _nextWave <= _waveManager.WaveCount) _restTimer = _restSeconds;
        }

        private void HandleReachedCastle(int damage) => _castleDamageTaken += damage;

        private void HandleKilled(EnemyKilledInfo info)
        {
            _kills++;
            _goldFromKills += info.GoldReward;
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10f, 10f, 320f, 400f), GUI.skin.box);
            GUILayout.Label($"Level: {_level.name}   Next wave: {_nextWave}/{_waveManager.WaveCount}");
            GUILayout.Label($"Active: {_enemyManager.ActiveEnemies.Count}   Queued: {_enemyManager.QueuedCount}");
            GUILayout.Label($"Castle damage taken: {_castleDamageTaken}");
            GUILayout.Label($"Kills: {_kills}   Gold (event): {_goldFromKills}");
            GUILayout.Label($"Finished waves: {_finished}");
            if (_restTimer >= 0f) GUILayout.Label($"Next wave in {_restTimer:0.0} s");

            if (GUILayout.Button($"Start wave {_nextWave}")) StartNextWave();
            if (GUILayout.Button("Kill all active")) KillAll();
            if (GUILayout.Button("Stop and clear"))
            {
                _restTimer = -1f;
                _waveManager.StopAndClear();
            }
            if (GUILayout.Button("Restart level"))
            {
                _restTimer = -1f;
                _nextWave = 1;
                _finished = "";
                _waveManager.Init(_level, null);
            }
            _autoNextWave = GUILayout.Toggle(_autoNextWave, "Auto next wave");
            GUILayout.EndArea();
        }

        private void KillAll()
        {
            var hit = new DamageInfo(1e9f, DamageType.True, Element.Fire, this, true, null, Vector3.zero);
            // Kills despawn immediately and swap-remove from the list, so walk it backwards.
            for (int i = _enemyManager.ActiveEnemies.Count - 1; i >= 0; i--)
            {
                if (i < _enemyManager.ActiveEnemies.Count) _enemyManager.ActiveEnemies[i].TakeDamage(hit);
            }
        }
    }
}
#endif
