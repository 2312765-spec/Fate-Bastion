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
        private DamageNumber _template;

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

            _template = CreateNumberTemplate(canvasRect);

            // Inactive while wiring, so Start runs after Configure.
            var spawnerObject = new GameObject("DamageNumbers", typeof(RectTransform));
            spawnerObject.SetActive(false);
            spawnerObject.transform.SetParent(canvasRect, false);
            _spawner = spawnerObject.AddComponent<DamageNumberSpawner>();
            _spawner.Configure(_settings, _template, (RectTransform)spawnerObject.transform, camera);
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

        // TMP_Text.SetText builds an Inspector string under #if UNITY_EDITOR (TMP_Text.cs), so it allocates in the
        // Editor only. Players (Development/Release) do not: run these via Test Runner > Run on Player (Standalone).
        [UnityPlatform(exclude = new[] { RuntimePlatform.WindowsEditor, RuntimePlatform.OSXEditor, RuntimePlatform.LinuxEditor })]
        [UnityTest]
        public IEnumerator DamageDealt_100SameFrame_NoGCAlloc()
        {
            // Warm-up: two full cycles (every number shown, then back to the pool), like after the first waves.
            // Frames must pass so the canvas processes its rebuild queues and returns their pooled objects,
            // as it does every frame in the game.
            for (int cycle = 0; cycle < 2; cycle++)
            {
                for (int i = 0; i < Hits; i++) CombatEvents.RaiseDamageDealt(Hit(i + 1, DamageType.True));
                yield return null;
                _spawner.Refresh(_settings.DamageNumberLifetime + 0.01f);
                yield return null;
            }
            Assert.AreEqual(0, _spawner.ActiveCount);

            Assert.That(() =>
            {
                for (int i = 0; i < Hits; i++) CombatEvents.RaiseDamageDealt(Hit(i + 2, DamageType.True));
            }, Is.Not.AllocatingGCMemory());
        }

        // The two tests below split one Show into its parts, so a GC failure above points at the culprit.
        // Each first repeats the exact call being measured, then lets frames pass, like a match after a few hits.
        // TMP_Text.SetText builds an Inspector string under #if UNITY_EDITOR (TMP_Text.cs), so it allocates in the
        // Editor only. Players (Development/Release) do not: run these via Test Runner > Run on Player (Standalone).
        [UnityPlatform(exclude = new[] { RuntimePlatform.WindowsEditor, RuntimePlatform.OSXEditor, RuntimePlatform.LinuxEditor })]
        [UnityTest]
        public IEnumerator Show_HiddenNumber_NoGCAlloc()
        {
            DamageNumber number = CreateStandaloneNumber();
            for (int i = 0; i < 2; i++)
            {
                number.Show(12345, _settings.MeteorColor, 1.5f, Vector3.zero);
                yield return null;
                number.Hide();
                yield return null;
            }

            Assert.That(() => number.Show(54321, _settings.MeteorColor, 1.5f, Vector3.zero), Is.Not.AllocatingGCMemory());
        }

        // TMP_Text.SetText builds an Inspector string under #if UNITY_EDITOR (TMP_Text.cs), so it allocates in the
        // Editor only. Players (Development/Release) do not: run these via Test Runner > Run on Player (Standalone).
        [UnityPlatform(exclude = new[] { RuntimePlatform.WindowsEditor, RuntimePlatform.OSXEditor, RuntimePlatform.LinuxEditor })]
        [UnityTest]
        public IEnumerator Show_VisibleNumber_NoGCAlloc()
        {
            DamageNumber number = CreateStandaloneNumber();
            number.Show(1, Color.white, 1f, Vector3.zero);
            yield return null;
            number.Show(12345, _settings.MeteorColor, 1.5f, Vector3.zero);
            yield return null;

            Assert.That(() => number.Show(54321, Color.white, 1f, Vector3.zero), Is.Not.AllocatingGCMemory());
        }

        private DamageNumber CreateStandaloneNumber()
        {
            DamageNumber number = Object.Instantiate(_template, _canvasObject.transform, false);
            number.InitializeHidden(_settings.DamageNumberFontSize);
            return number;
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
