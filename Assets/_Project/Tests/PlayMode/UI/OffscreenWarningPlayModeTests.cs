using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using FateBastion.Enemies;
using FateBastion.Tests.PlayMode.S2;
using FateBastion.UI.World;

namespace FateBastion.Tests.PlayMode.UI
{
    /// <summary>Red edge arrow for the enemy nearest the Castle once it is past 80% of the path (C-14, S10).</summary>
    public class OffscreenWarningPlayModeTests
    {
        // Longer than one scan interval, so Refresh always rescans.
        private const float ScanStep = 1f;

        private WorldUiTestRig _rig;

        [TearDown]
        public void TearDown()
        {
            _rig?.Dispose();
            _rig = null;
        }

        [UnityTest]
        public IEnumerator NoEnemyPastThreshold_ArrowHidden()
        {
            _rig = new WorldUiTestRig();
            _rig.SpawnAt(_rig.CreateStillEnemy("normal"), 10f);
            yield return null;

            _rig.Root.Refresh(ScanStep);

            Assert.IsFalse(_rig.Warning.IsArrowVisible);
            Assert.IsNull(_rig.Warning.Target);
        }

        [UnityTest]
        public IEnumerator EnemyPastThresholdOffscreen_ArrowOnRightEdge()
        {
            _rig = new WorldUiTestRig();
            _rig.SpawnAt(_rig.CreateStillEnemy("normal"), 85f); // Camera looks at x = 10; enemy is far to the right.
            yield return null;

            _rig.Root.Refresh(ScanStep);

            Assert.IsTrue(_rig.Warning.IsArrowVisible);
            float rightEdge = _rig.Camera.pixelWidth - _rig.Settings.WarningEdgeMargin;
            Assert.AreEqual(rightEdge, _rig.Arrow.position.x, 1f);
            Assert.AreEqual(0f, Mathf.DeltaAngle(0f, _rig.Arrow.eulerAngles.z), 30f, "Mũi tên phải chỉ sang phải.");
        }

        [UnityTest]
        public IEnumerator EnemyPastThresholdOnScreen_ArrowHidden()
        {
            _rig = new WorldUiTestRig();
            EnemyController enemy = _rig.SpawnAt(_rig.CreateStillEnemy("normal"), 85f);
            _rig.LookAtPathDistance(85f);
            yield return null;

            _rig.Root.Refresh(ScanStep);

            Assert.AreSame(enemy, _rig.Warning.Target);
            Assert.IsFalse(_rig.Warning.IsArrowVisible);
        }

        [UnityTest]
        public IEnumerator SeveralPastThreshold_TargetsClosestToCastle()
        {
            _rig = new WorldUiTestRig();
            EnemyData data = _rig.CreateStillEnemy("normal");
            _rig.SpawnAt(data, 82f);
            EnemyController closest = _rig.SpawnAt(data, 95f);
            _rig.SpawnAt(data, 88f);
            yield return null;

            _rig.Root.Refresh(ScanStep);

            Assert.AreSame(closest, _rig.Warning.Target);
        }

        [UnityTest]
        public IEnumerator TargetDespawned_ArrowHiddenBeforeNextScan()
        {
            _rig = new WorldUiTestRig();
            EnemyController enemy = _rig.SpawnAt(_rig.CreateStillEnemy("normal"), 85f);
            yield return null;
            _rig.Root.Refresh(ScanStep);
            Assert.IsTrue(_rig.Warning.IsArrowVisible);

            enemy.TakeDamage(EnemyTestRig.TrueHit(1e9f));
            _rig.Root.Refresh(0f); // No rescan this frame.

            Assert.IsNull(_rig.Warning.Target);
            Assert.IsFalse(_rig.Warning.IsArrowVisible);
        }

        [UnityTest]
        public IEnumerator Scan_RunsAtSettingsRate_NotEveryFrame()
        {
            _rig = new WorldUiTestRig();
            yield return null;

            // No yields from here on: LateUpdate would advance the scan timer with real time.
            _rig.Root.Refresh(ScanStep); // Consume a scan with an empty field.
            _rig.SpawnAt(_rig.CreateStillEnemy("normal"), 85f);

            float interval = 1f / _rig.Settings.WarningScansPerSecond;
            _rig.Root.Refresh(interval * 0.5f);
            Assert.IsNull(_rig.Warning.Target, "Chưa tới lượt quét.");

            _rig.Root.Refresh(interval);
            Assert.IsNotNull(_rig.Warning.Target);
        }
    }
}
