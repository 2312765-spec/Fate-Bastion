#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using UnityEngine;
using FateBastion.Core;

namespace FateBastion.Combat.Sandbox
{
    /// <summary>
    /// Sandbox/A_Combat driver for the S1 handoff: on-screen buttons fire a Projectile with burn, slow or
    /// lightning chain, kill the target mid-flight, and show HP, statuses and how many kill events were raised.
    /// Uses OnGUI buttons, not keys, so it does not bypass InputRouter.
    /// </summary>
    public class CombatSandboxDemo : MonoBehaviour
    {
        [SerializeField] private CombatSettings _settings;
        [SerializeField] private Projectile _projectile;
        [SerializeField] private Transform _muzzle;

        [Tooltip("Element 0 is the target that gets shot; the others are there for the lightning chain.")]
        [SerializeField] private SandboxDummyTarget[] _targets;

        [SerializeField] private StatusEffectData _burn;
        [SerializeField] private StatusEffectData _slow;

        [SerializeField, Min(0f)] private float _damage = 40f;
        [SerializeField] private DamageType _damageType = DamageType.Magic;
        [SerializeField, Min(0.1f)] private float _projectileSpeed = 8f;

        [Tooltip("Seconds after firing before the 'kill mid-flight' button kills the target.")]
        [SerializeField, Min(0f)] private float _killDelay = 0.3f;

        private Vector3[] _positions;
        private bool[] _alive;
        private int[] _chainResults;
        private bool _chainPending;
        private Element _currentElement;
        private int _killEvents;
        private int _damageEvents;

        private void Awake()
        {
            if (_settings == null || _projectile == null || _muzzle == null || _targets == null || _targets.Length == 0)
            {
                Debug.LogError($"{name}: thiếu tham chiếu (Settings, Projectile, Muzzle, Targets).", this);
                enabled = false;
                return;
            }

            _positions = new Vector3[_targets.Length];
            _alive = new bool[_targets.Length];
            _chainResults = new int[Mathf.Max(1, _settings.ChainTargets)];
        }

        private void OnEnable()
        {
            CombatEvents.OnEnemyKilled += HandleEnemyKilled;
            CombatEvents.OnDamageDealt += HandleDamageDealt;
            if (_projectile != null) _projectile.HitTarget += HandleProjectileHit;
        }

        private void OnDisable()
        {
            CombatEvents.OnEnemyKilled -= HandleEnemyKilled;
            CombatEvents.OnDamageDealt -= HandleDamageDealt;
            if (_projectile != null) _projectile.HitTarget -= HandleProjectileHit;
        }

        private void HandleEnemyKilled(EnemyKilledInfo info)
        {
            _killEvents++;
            Debug.Log($"[S1] OnEnemyKilled: {info.EnemyId}");
        }

        private void HandleDamageDealt(DamageDealtInfo info)
        {
            _damageEvents++;
            Debug.Log($"[S1] OnDamageDealt: {info.Amount} {info.Type} {info.Element}{(info.TargetWasKilled ? " (killed)" : "")}");
        }

        private void Fire(StatusEffectData status, Element element)
        {
            if (_projectile.IsFlying)
            {
                Debug.Log("[S1] Đạn đang bay, chờ nó chạm rồi bấm lại.");
                return;
            }

            SandboxDummyTarget target = _targets[0];
            if (!target.IsAlive) Debug.Log($"[S1] {target.name} đã chết: đạn sẽ không gây sát thương. Bấm 'Hồi sinh tất cả'.");
            _currentElement = element;
            _chainPending = element == Element.Lightning;

            var info = new DamageInfo(_damage, _damageType, element, this, true, status, _muzzle.position);
            _projectile.Launch(_muzzle.position, target, target.transform, info, _projectileSpeed);
            Debug.Log($"[S1] Bắn {element} vào {target.name}.");
        }

        private IEnumerator FireThenKill()
        {
            Fire(null, Element.Fire);
            yield return new WaitForSeconds(_killDelay);
            _targets[0].KillNow();
        }

        private void ThreeLethalHitsSameFrame()
        {
            SandboxDummyTarget target = _targets[0];
            float lethal = target.Health.Max * 10f;
            var info = new DamageInfo(lethal, DamageType.True, Element.Fire, this, true, null, target.transform.position);
            for (int i = 0; i < 3; i++) target.TakeDamage(info);
        }

        private void HandleProjectileHit(Projectile projectile, IDamageable hit)
        {
            if (!_chainPending) return;
            _chainPending = false;

            int first = -1;
            for (int i = 0; i < _targets.Length; i++)
            {
                _positions[i] = _targets[i].transform.position;
                _alive[i] = _targets[i].IsAlive;
                if (ReferenceEquals(_targets[i], hit)) first = i;
            }

            int count = ChainLightning.SelectTargets(first, _positions, _alive, _targets.Length,
                _settings.ChainTargets, _settings.ChainRadius, _chainResults);

            for (int bounce = 0; bounce < count; bounce++)
            {
                SandboxDummyTarget next = _targets[_chainResults[bounce]];
                float amount = ChainLightning.BounceDamage(_damage, _settings.ChainFalloff, bounce + 1);
                next.TakeDamage(new DamageInfo(amount, _damageType, _currentElement, this, true, null,
                    next.transform.position));
                Debug.DrawLine(_positions[bounce == 0 ? first : _chainResults[bounce - 1]], next.transform.position,
                    Color.cyan, 1f);
            }
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10f, 10f, 360f, Screen.height - 20f), GUI.skin.box);
            GUILayout.Label("S1 Combat Sandbox");

            if (GUILayout.Button("1. Bắn thường")) Fire(null, Element.Fire);
            if (GUILayout.Button("2. Bắn Lửa (Bỏng)")) Fire(_burn, Element.Fire);
            if (GUILayout.Button("3. Bắn Băng (Chậm)")) Fire(_slow, Element.Ice);
            if (GUILayout.Button("4. Bắn Lôi (nảy)")) Fire(null, Element.Lightning);
            if (GUILayout.Button("5. Bắn rồi giết mục tiêu giữa đường")) StartCoroutine(FireThenKill());
            if (GUILayout.Button("6. 3 đòn chí mạng cùng frame")) ThreeLethalHitsSameFrame();

            if (GUILayout.Button("Hồi sinh tất cả"))
            {
                for (int i = 0; i < _targets.Length; i++) _targets[i].Revive();
                _killEvents = 0;
                _damageEvents = 0;
            }

            GUILayout.Space(8f);
            GUILayout.Label($"OnEnemyKilled đã phát: {_killEvents}   OnDamageDealt: {_damageEvents}");
            GUILayout.Label($"Đạn: {(_projectile.IsFlying ? "đang bay" : (_projectile.LastFlightHitTarget ? "trúng" : "trượt / chưa bắn"))}");

            for (int i = 0; i < _targets.Length; i++)
            {
                SandboxDummyTarget t = _targets[i];
                if (t == null || t.Status == null) continue;
                GUILayout.Label($"{t.name}: HP {t.Health.Current:0}/{t.Health.Max:0}  " +
                                $"Bỏng {(t.Status.IsBurning ? "có" : "-")}  Chậm {t.Status.SlowAmount * 100f:0}%  " +
                                $"Đòn cuối {t.LastDamage}");
            }

            GUILayout.EndArea();
        }
    }
}
#endif
