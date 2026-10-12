using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using FateBastion.Core;
using FateBastion.Combat;

namespace FateBastion.Tests.PlayMode.S1
{
    /// <summary>PlayMode tests for Projectile: hits a live target once; a target that died mid-flight takes no damage (S1).</summary>
    public class ProjectilePlayModeTests
    {
        private const float TimeoutSeconds = 3f;

        private sealed class FakeTarget : MonoBehaviour, IDamageable
        {
            public bool Alive = true;
            public int Hits;
            public float LastAmount;

            public bool IsAlive => Alive;

            public void TakeDamage(in DamageInfo info)
            {
                Hits++;
                LastAmount = info.Amount;
            }
        }

        private GameObject _projectileObject;
        private GameObject _targetObject;
        private Projectile _projectile;
        private FakeTarget _target;
        private int _finishedCount;

        [SetUp]
        public void SetUp()
        {
            _projectileObject = new GameObject("Projectile");
            _projectile = _projectileObject.AddComponent<Projectile>();
            _projectile.Finished += _ => _finishedCount++;
            _finishedCount = 0;

            _targetObject = new GameObject("Target");
            _targetObject.transform.position = new Vector3(10f, 0f, 0f);
            _target = _targetObject.AddComponent<FakeTarget>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_projectileObject);
            Object.Destroy(_targetObject);
        }

        private static DamageInfo Hit(float amount)
        {
            return new DamageInfo(amount, DamageType.Physical, Element.Lightning, null, true, null, Vector3.zero);
        }

        private IEnumerator WaitUntilFinished()
        {
            float elapsed = 0f;
            while (_finishedCount == 0 && elapsed < TimeoutSeconds)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Launch_TargetAlive_DealsDamageOnceAndDeactivates()
        {
            _projectile.Launch(Vector3.zero, _target, _targetObject.transform, Hit(12f), 40f);

            yield return WaitUntilFinished();

            Assert.AreEqual(1, _finishedCount);
            Assert.AreEqual(1, _target.Hits);
            Assert.AreEqual(12f, _target.LastAmount, 1e-5f);
            Assert.IsTrue(_projectile.LastFlightHitTarget);
            Assert.IsFalse(_projectileObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator Launch_TargetDiesMidFlight_FliesToLastPositionWithoutDamage()
        {
            _projectile.Launch(Vector3.zero, _target, _targetObject.transform, Hit(12f), 5f);
            yield return null;
            yield return null;

            // Target dies, then its (pooled) object is moved far away in the same frame.
            _target.Alive = false;
            _targetObject.transform.position = new Vector3(100f, 0f, 100f);

            yield return WaitUntilFinished();

            Assert.AreEqual(1, _finishedCount);
            Assert.AreEqual(0, _target.Hits);
            Assert.IsFalse(_projectile.LastFlightHitTarget);
            Assert.AreEqual(10f, _projectileObject.transform.position.x, 1e-3f);
            Assert.AreEqual(0f, _projectileObject.transform.position.z, 1e-3f);
            Assert.IsFalse(_projectileObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator Launch_FromInactiveGameObject_StillHits()
        {
            // Pooled projectiles wait inactive until launched.
            _projectileObject.SetActive(false);
            _projectile.Launch(Vector3.zero, _target, _targetObject.transform, Hit(12f), 40f);

            yield return WaitUntilFinished();

            Assert.AreEqual(1, _target.Hits);
        }

        [UnityTest]
        public IEnumerator Launch_ComponentDisabled_StillHits()
        {
            // Component checkbox unticked in the Inspector by mistake.
            _projectile.enabled = false;
            _projectile.Launch(Vector3.zero, _target, _targetObject.transform, Hit(12f), 40f);

            yield return WaitUntilFinished();

            Assert.AreEqual(1, _target.Hits);
        }

        [UnityTest]
        public IEnumerator Launch_TargetDeadAtLaunch_NoDamage()
        {
            _target.Alive = false;
            _projectile.Launch(Vector3.zero, _target, _targetObject.transform, Hit(12f), 40f);

            yield return WaitUntilFinished();

            Assert.AreEqual(1, _finishedCount);
            Assert.AreEqual(0, _target.Hits);
        }
    }
}
