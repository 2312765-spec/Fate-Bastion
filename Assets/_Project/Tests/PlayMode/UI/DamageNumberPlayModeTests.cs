using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using FateBastion.Core;
using FateBastion.UI.World;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace FateBastion.Tests.PlayMode.UI
{
    /// <summary>Damage numbers from CombatEvents.OnDamageDealt: at most 60 on screen, pooled, no GC (S10, S11, C-14).</summary>
    public class DamageNumberPlayModeTests
    {
        private const int Hits = 100;

        private GameObject _cameraObject;
        private GameObject _canvasObject;
        private WorldUiSettings _settings;
        private DamageNumberSpawner _spawner;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            CombatEvents.ResetAll();

            _cameraObject = new GameObject("DamageNumberTestCamera");
            Camera camera = _cameraObject.AddComponent<Camera>();
            _cameraObject.transform.position = new Vector3(0f, 8f, -20f);
            _cameraObject.transform.LookAt(Vector3.zero);

            _settings = ScriptableObject.CreateInstance<WorldUiSettings>();

            _canvasObject = new GameObject("DamageNumberTestCanvas", typeof(RectTransform));
            _canvasObject.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var canvasRect = (RectTransform)_canvasObject.transform;

            DamageNumber template = CreateNumberTemplate(canvasRect);

            // Inactive while wiring, so Start runs after Configure.
            var spawnerObject = new GameObject("DamageNumbers", typeof(RectTransform));
            spawnerObject.SetActive(false);
            spawnerObject.transform.SetParent(canvasRect, false);
            _spawner = spawnerObject.AddComponent<DamageNumberSpawner>();
            _spawner.Configure(_settings, template, (RectTransform)spawnerObject.transform, camera);
            spawnerObject.SetActive(true);

            yield return null; // Start: prewarm and subscribe.
        }

        [TearDown]
        public void TearDown()
        {
            CombatEvents.ResetAll();
            Object.Destroy(_canvasObject);
            Object.Destroy(_cameraObject);
            Object.Destroy(_settings);
        }

        private static DamageDealtInfo Hit(int amount, DamageType type = DamageType.Physical) =>
            new DamageDealtInfo(amount, type, Element.Fire, Vector3.zero, false);

        [Test]
        public void Prewarm_CreatesWholeBudget()
        {
            Assert.AreEqual(_settings.DamageNumberMax, _spawner.CreatedCount);
        }

        [Test]
        public void DamageDealt_100SameFrame_AtMost60Visible()
        {
            for (int i = 0; i < Hits; i++) CombatEvents.RaiseDamageDealt(Hit(i + 1));

            Assert.AreEqual(_settings.DamageNumberMax, _spawner.ActiveCount);
            Assert.AreEqual(_settings.DamageNumberMax, _spawner.CreatedCount, "Không được Instantiate số sát thương trong trận.");
        }

        [Test]
        public void DamageDealt_100SameFrame_NoGCAlloc()
        {
            CombatEvents.RaiseDamageDealt(Hit(1)); // Warm-up: first call JITs the handler.

            Assert.That(() =>
            {
                for (int i = 0; i < Hits; i++) CombatEvents.RaiseDamageDealt(Hit(i + 2, DamageType.True));
            }, Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void Refresh_100Numbers_NoGCAlloc()
        {
            for (int i = 0; i < Hits; i++) CombatEvents.RaiseDamageDealt(Hit(i + 1));
            _spawner.Refresh(0.01f); // Warm-up.

            Assert.That(() => _spawner.Refresh(0.01f), Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void Number_AfterLifetime_ReturnsToPool()
        {
            CombatEvents.RaiseDamageDealt(Hit(10));
            Assert.AreEqual(1, _spawner.ActiveCount);

            _spawner.Refresh(_settings.DamageNumberLifetime + 0.01f);

            Assert.AreEqual(0, _spawner.ActiveCount);
        }

        [Test]
        public void DamageDealt_ZeroAmount_NoNumber()
        {
            CombatEvents.RaiseDamageDealt(Hit(0));

            Assert.AreEqual(0, _spawner.ActiveCount);
        }

        private static DamageNumber CreateNumberTemplate(Transform parent)
        {
            // Active like the real prefab, so TMP initialises when the pool is prewarmed, not on the first hit.
            var template = new GameObject("NumberTemplate", typeof(RectTransform));
            template.transform.SetParent(parent, false);

            // The PlayMode test assembly has no TextMeshPro reference; add the TMP text by type name.
            System.Type tmpType = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
            if (tmpType == null) Assert.Ignore("TextMeshPro không có trong project.");
            template.AddComponent(tmpType);

            return template.AddComponent<DamageNumber>();
        }
    }
}
