using System.Collections.Generic;
using UnityEngine;
using FateBastion.Enemies;

namespace FateBastion.UI.World
{
    /// <summary>
    /// Red arrow on the screen edge pointing at the enemy nearest the Castle once it passed the progress threshold
    /// (S10: 80%). Scans the enemy list a few times per second; hidden while that enemy is on screen.
    /// Has no Update: WorldUiRoot calls Tick.
    /// </summary>
    [DisallowMultipleComponent]
    public class OffscreenWarning : MonoBehaviour
    {
        [Tooltip("Arrow shown at the screen edge. Its graphic must point right (+X) at rotation 0.")]
        [SerializeField] private RectTransform _arrow;

        private ClosestToCastleSelector _selector;
        private float _scanTimer;

        /// <summary>Enemy the warning follows; null when none is past the threshold.</summary>
        public EnemyController Target { get; private set; }

        public bool IsArrowVisible => _arrow != null && _arrow.gameObject.activeSelf;

        /// <summary>For tests and the layout builder.</summary>
        public void Configure(RectTransform arrow)
        {
            _arrow = arrow;
        }

        public void Tick(Camera camera, IReadOnlyList<EnemyController> enemies, WorldUiSettings settings, float unscaledDeltaTime)
        {
            if (_arrow == null || settings == null) return;

            _scanTimer -= unscaledDeltaTime;
            if (_scanTimer <= 0f)
            {
                _scanTimer = 1f / settings.WarningScansPerSecond;
                Rescan(enemies, settings.WarningProgressThreshold);
            }

            if (Target == null || camera == null)
            {
                SetArrowVisible(false);
                return;
            }

            Vector3 screenPoint = camera.WorldToScreenPoint(Target.HealthBarAnchor.position);
            var screenSize = new Vector2(camera.pixelWidth, camera.pixelHeight);
            if (OffscreenMath.IsOnScreen(screenPoint, screenSize))
            {
                SetArrowVisible(false);
                return;
            }

            Vector2 position = OffscreenMath.GetEdgePosition(screenPoint, screenSize, settings.WarningEdgeMargin, out float angle);
            _arrow.SetPositionAndRotation(new Vector3(position.x, position.y, 0f), Quaternion.Euler(0f, 0f, angle));
            SetArrowVisible(true);
        }

        /// <summary>Called on OnEnemyDespawned, so the arrow never points at a pooled enemy until the next scan.</summary>
        public void NotifyDespawned(EnemyController enemy)
        {
            if (enemy != Target) return;
            Target = null;
            SetArrowVisible(false);
        }

        public void Hide()
        {
            Target = null;
            SetArrowVisible(false);
        }

        private void Rescan(IReadOnlyList<EnemyController> enemies, float threshold)
        {
            Target = null;
            if (enemies == null) return;

            if (_selector == null) _selector = new ClosestToCastleSelector(threshold);
            _selector.Begin();
            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyController enemy = enemies[i];
                if (enemy.IsAlive) _selector.Consider(i, enemy.Progress);
            }
            if (_selector.BestIndex >= 0) Target = enemies[_selector.BestIndex];
        }

        private void SetArrowVisible(bool visible)
        {
            if (_arrow != null && _arrow.gameObject.activeSelf != visible) _arrow.gameObject.SetActive(visible);
        }
    }
}
