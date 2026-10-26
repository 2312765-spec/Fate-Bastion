using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FateBastion.Enemies;
using FateBastion.Tests.PlayMode.S2;
using FateBastion.UI.World;

namespace FateBastion.Tests.PlayMode.UI
{
    /// <summary>
    /// EnemyTestRig (straight path along +X) + a camera looking at the start of the path + an overlay canvas with
    /// WorldUiRoot, a health bar "prefab" and the off-screen arrow, all built in code. Dispose in TearDown.
    /// </summary>
    public sealed class WorldUiTestRig
    {
        public const float PathLength = 100f;

        public readonly EnemyTestRig Enemies;
        public readonly Camera Camera;
        public readonly WorldUiSettings Settings;
        public readonly WorldUiRoot Root;
        public readonly OffscreenWarning Warning;
        public readonly RectTransform Arrow;

        private readonly List<Object> _owned = new List<Object>();

        public WorldUiTestRig(bool enableRoot = true)
        {
            Enemies = new EnemyTestRig(PathLength);

            var cameraObject = new GameObject("WorldUiTestCamera");
            Camera = cameraObject.AddComponent<Camera>();
            _owned.Add(cameraObject);
            LookAtPathDistance(10f);

            Settings = ScriptableObject.CreateInstance<WorldUiSettings>();
            _owned.Add(Settings);

            var canvasObject = new GameObject("WorldUiTestCanvas", typeof(RectTransform));
            canvasObject.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            _owned.Add(canvasObject);
            var canvasRect = (RectTransform)canvasObject.transform;

            WorldHealthBar barTemplate = CreateBarTemplate(canvasRect);

            var barParent = new GameObject("HealthBars", typeof(RectTransform));
            barParent.transform.SetParent(canvasRect, false);

            var arrowObject = new GameObject("OffscreenArrow", typeof(RectTransform), typeof(Image));
            arrowObject.transform.SetParent(canvasRect, false);
            Arrow = (RectTransform)arrowObject.transform;
            Warning = arrowObject.AddComponent<OffscreenWarning>();
            Warning.Configure(Arrow);

            // Inactive while wiring, so OnEnable runs after Configure.
            var rootObject = new GameObject("WorldUiRoot");
            rootObject.SetActive(false);
            rootObject.transform.SetParent(canvasRect, false);
            Root = rootObject.AddComponent<WorldUiRoot>();
            Root.Configure(Enemies.Manager, Camera, Settings, barTemplate, (RectTransform)barParent.transform, Warning);
            if (enableRoot) rootObject.SetActive(true);
        }

        public EnemyData CreateStillEnemy(string id, float baseHP = 100f, bool boss = false) =>
            Enemies.CreateEnemy(id, baseHP, moveSpeed: 0f, boss: boss);

        public EnemyController SpawnAt(EnemyData data, float distance)
        {
            Enemies.Manager.RequestSpawn(data, 0, 0, distance);
            IReadOnlyList<EnemyController> active = Enemies.Manager.ActiveEnemies;
            return active[active.Count - 1];
        }

        /// <summary>Puts the camera 20 m in front of the path point, so it sees about ±15 m around it.</summary>
        public void LookAtPathDistance(float distance)
        {
            var target = new Vector3(distance, 0f, 0f);
            Camera.transform.position = target + new Vector3(0f, 8f, -20f);
            Camera.transform.LookAt(target);
        }

        public void Dispose()
        {
            Enemies.Dispose();
            foreach (Object o in _owned)
                if (o != null) Object.Destroy(o);
        }

        private WorldHealthBar CreateBarTemplate(Transform parent)
        {
            var template = new GameObject("BarTemplate", typeof(RectTransform));
            template.SetActive(false);
            template.transform.SetParent(parent, false);
            _owned.Add(template);

            var fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(template.transform, false);
            var fill = fillObject.GetComponent<Image>();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;

            var bar = template.AddComponent<WorldHealthBar>();
            bar.Configure(fill);
            return bar;
        }
    }
}
