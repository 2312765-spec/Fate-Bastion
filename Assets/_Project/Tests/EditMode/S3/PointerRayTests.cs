using NUnit.Framework;
using UnityEngine;
using FateBastion.Player;

namespace FateBastion.Tests.EditMode.S3
{
    /// <summary>PointerRay only aims into the world in the Summoner map, with a pointer and camera, off the UI (S3, S7).</summary>
    public class PointerRayTests
    {
        private GameObject _cameraGo;
        private Camera _camera;
        private Vector2 _screenCenter;

        [SetUp]
        public void SetUp()
        {
            // Third-person style: above the ground, looking down at 45°.
            _cameraGo = new GameObject("PointerRay camera (test)");
            _cameraGo.transform.SetPositionAndRotation(new Vector3(0f, 10f, -10f), Quaternion.Euler(45f, 0f, 0f));
            _camera = _cameraGo.AddComponent<Camera>();
            _screenCenter = new Vector2(_camera.pixelWidth * 0.5f, _camera.pixelHeight * 0.5f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_cameraGo);
        }

        [Test]
        public void TryBuild_UiMapActive_ReturnsFalse()
        {
            Assert.IsFalse(PointerRay.TryBuild(false, true, _screenCenter, _camera, false, out _));
        }

        [Test]
        public void TryBuild_PointerOverUi_ReturnsFalse()
        {
            Assert.IsFalse(PointerRay.TryBuild(true, true, _screenCenter, _camera, true, out _));
        }

        [Test]
        public void TryBuild_NoPointer_ReturnsFalse()
        {
            Assert.IsFalse(PointerRay.TryBuild(true, false, _screenCenter, _camera, false, out _));
        }

        [Test]
        public void TryBuild_NoCamera_ReturnsFalse()
        {
            Assert.IsFalse(PointerRay.TryBuild(true, true, _screenCenter, null, false, out _));
        }

        [Test]
        public void TryBuild_CameraDisabled_ReturnsFalse()
        {
            _camera.enabled = false;

            Assert.IsFalse(PointerRay.TryBuild(true, true, _screenCenter, _camera, false, out _));
        }

        [Test]
        public void TryBuild_Valid_MatchesCameraScreenPointToRay()
        {
            var position = new Vector2(_camera.pixelWidth * 0.25f, _camera.pixelHeight * 0.75f);
            Ray expected = _camera.ScreenPointToRay(position);

            Assert.IsTrue(PointerRay.TryBuild(true, true, position, _camera, false, out Ray ray));
            Assert.That(Vector3.Distance(expected.origin, ray.origin), Is.LessThan(1e-4f));
            Assert.That(Vector3.Distance(expected.direction, ray.direction), Is.LessThan(1e-4f));
        }

        [Test]
        public void TryBuild_ScreenCenter_PointsDownAlongCameraForward()
        {
            Assert.IsTrue(PointerRay.TryBuild(true, true, _screenCenter, _camera, false, out Ray ray));

            Assert.That(ray.direction.y, Is.LessThan(0f));
            Assert.That(Vector3.Angle(_cameraGo.transform.forward, ray.direction), Is.LessThan(0.5f));
        }
    }
}
