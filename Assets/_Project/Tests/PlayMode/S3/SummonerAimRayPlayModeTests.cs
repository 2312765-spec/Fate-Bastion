using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using FateBastion.Core;
using FateBastion.Heroes;
using FateBastion.Player;

namespace FateBastion.Tests.PlayMode.S3
{
    /// <summary>SummonerInputRouter as IMeteorAimRaySource / IPointerSource with a virtual mouse (S3, S7).</summary>
    public class SummonerAimRayPlayModeTests
    {
        private static readonly Vector2 MousePosition = new Vector2(120f, 80f);

        private GameObject _routerGo;
        private GameObject _cameraGo;
        private SummonerInputRouter _router;
        private Camera _camera;
        private InputActionAsset _asset;
        private Mouse _mouse;
        private Mouse _previousMouse;

        [SetUp]
        public void SetUp()
        {
            _previousMouse = Mouse.current;
            _mouse = InputSystem.AddDevice<Mouse>();
            _mouse.MakeCurrent();
            SetMousePosition(MousePosition);

            _cameraGo = new GameObject("Aim camera (test)");
            _cameraGo.transform.SetPositionAndRotation(new Vector3(0f, 10f, -10f), Quaternion.Euler(45f, 0f, 0f));
            _camera = _cameraGo.AddComponent<Camera>();

            _asset = TestInputAssetFactory.Create();
            _routerGo = new GameObject("SummonerInputRouter (test)");
            _router = _routerGo.AddComponent<SummonerInputRouter>();
            _router.Initialize(_asset);
            _router.SetPointerCamera(_camera);
        }

        [TearDown]
        public void TearDown()
        {
            // Immediate, so InputRouter.Instance is cleared before the next test creates a router.
            if (_routerGo != null) Object.DestroyImmediate(_routerGo);
            Object.DestroyImmediate(_cameraGo);
            Object.DestroyImmediate(_asset);
            InputSystem.RemoveDevice(_mouse);
            if (_previousMouse != null && _previousMouse.added) _previousMouse.MakeCurrent();
        }

        // Through the event queue and an update, so the value lands in the player state buffer the router reads.
        private void SetMousePosition(Vector2 position)
        {
            InputSystem.QueueStateEvent(_mouse, new MouseState { position = position });
            InputSystem.Update();
        }

        [Test]
        public void TryGetAimRay_SummonerMap_ReturnsCameraRayThroughMouse()
        {
            Ray expected = _camera.ScreenPointToRay(MousePosition);

            Assert.IsTrue(_router.TryGetAimRay(out Ray ray));
            Assert.That(Vector3.Distance(expected.origin, ray.origin), Is.LessThan(1e-4f));
            Assert.That(Vector3.Distance(expected.direction, ray.direction), Is.LessThan(1e-4f));
        }

        [Test]
        public void TryGetAimRay_UiMap_ReturnsFalse()
        {
            _router.SetActionMap(ActionMapId.UI);

            Assert.IsFalse(_router.TryGetAimRay(out _));
        }

        [Test]
        public void TryGetAimRay_UiThenSummoner_ReturnsTrueAgain()
        {
            _router.SetActionMap(ActionMapId.UI);
            _router.SetActionMap(ActionMapId.Summoner);

            Assert.IsTrue(_router.TryGetAimRay(out _));
        }

        [Test]
        public void TryGetAimRay_NoCamera_ReturnsFalse()
        {
            _router.SetPointerCamera(null);

            Assert.IsFalse(_router.TryGetAimRay(out _));
        }

        [Test]
        public void TryGetAimRay_MouseMoved_FollowsMouse()
        {
            var moved = new Vector2(300f, 200f);
            SetMousePosition(moved);
            Ray expected = _camera.ScreenPointToRay(moved);

            Assert.IsTrue(_router.TryGetAimRay(out Ray ray));
            Assert.That(Vector3.Distance(expected.direction, ray.direction), Is.LessThan(1e-4f));
        }

        [Test]
        public void TryGetPointerPosition_WithMouse_ReturnsMousePosition()
        {
            IPointerSource pointer = _router;

            Assert.IsTrue(pointer.TryGetPointerPosition(out Vector2 position));
            Assert.AreEqual(MousePosition, position);
        }

        [Test]
        public void Router_ImplementsAimRaySource_ForMeteorAimIndicator()
        {
            // MeteorAimIndicator._aimRaySourceComponent takes a MonoBehaviour and casts it to this interface.
            MonoBehaviour component = _router;

            Assert.IsTrue(component is IMeteorAimRaySource);
        }
    }
}
