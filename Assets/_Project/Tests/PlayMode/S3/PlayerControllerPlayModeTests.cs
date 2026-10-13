using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using FateBastion.Player;

namespace FateBastion.Tests.PlayMode.S3
{
    /// <summary>The Summoner moves in real time: 6 m/s at x1 and x2, still while paused, no jump on resume (S3).</summary>
    public class PlayerControllerPlayModeTests
    {
        private const float WalkSpeed = 6f;
        private const float SprintSpeed = 9f;
        private const float SpeedTolerance = 0.4f;
        private const float MeasureSeconds = 0.5f;

        private GameObject _ground;
        private GameObject _routerGo;
        private GameObject _player;
        private GameObject _camera;
        private GameObject _spawn;
        private FakeInputRouter _router;
        private PlayerController _controller;
        private PlayerMovementSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _ground.transform.position = new Vector3(0f, -0.5f, 0f);
            _ground.transform.localScale = new Vector3(400f, 1f, 400f);

            _routerGo = new GameObject("FakeInputRouter");
            _router = _routerGo.AddComponent<FakeInputRouter>();

            // Camera looking along +Z, so "forward" input moves the Summoner along +Z.
            _camera = new GameObject("Camera yaw source");
            _spawn = new GameObject("Spawn");
            _spawn.transform.position = new Vector3(0f, 1.1f, 0f);

            _settings = ScriptableObject.CreateInstance<PlayerMovementSettings>();

            _player = new GameObject("Summoner");
            _player.transform.position = _spawn.transform.position;
            _controller = _player.AddComponent<PlayerController>();
            _controller.Configure(_settings, _camera.transform, _spawn.transform);
        }

        [TearDown]
        public void TearDown()
        {
            // Tests change the time scale to check x2 and pause; only TimeController does this in the game.
            Time.timeScale = 1f;
            Object.DestroyImmediate(_player);
            Object.DestroyImmediate(_routerGo);
            Object.DestroyImmediate(_camera);
            Object.DestroyImmediate(_spawn);
            Object.DestroyImmediate(_ground);
            Object.DestroyImmediate(_settings);
        }

        private static Vector3 Planar(Vector3 v) => new Vector3(v.x, 0f, v.z);

        private IEnumerator Settle()
        {
            // Let Start subscribe and the CharacterController land.
            for (int i = 0; i < 10; i++) yield return null;
        }

        /// <summary>Real-time planar speed while input is held: distance / sum of unscaled frame times.</summary>
        private IEnumerator MeasureSpeed(System.Action<float> result)
        {
            Vector3 start = Planar(_player.transform.position);
            float elapsed = 0f;
            while (elapsed < MeasureSeconds)
            {
                yield return null;
                elapsed += Time.unscaledDeltaTime;
            }

            result(Vector3.Distance(start, Planar(_player.transform.position)) / elapsed);
        }

        [UnityTest]
        public IEnumerator Move_TimeScale1_Walks6MetersPerSecond()
        {
            yield return WalkAt(1f, sprint: false, WalkSpeed);
        }

        [UnityTest]
        public IEnumerator Move_TimeScale2_StillWalks6MetersPerSecondRealTime()
        {
            yield return WalkAt(2f, sprint: false, WalkSpeed);
        }

        [UnityTest]
        public IEnumerator Move_TimeScale2Sprint_Runs9MetersPerSecondRealTime()
        {
            yield return WalkAt(2f, sprint: true, SprintSpeed);
        }

        private IEnumerator WalkAt(float timeScale, bool sprint, float expectedSpeed)
        {
            yield return Settle();
            Time.timeScale = timeScale;
            _router.SendSprint(sprint);
            _router.SendMove(Vector2.up);
            yield return null;

            float speed = 0f;
            yield return MeasureSpeed(s => speed = s);

            Assert.AreEqual(expectedSpeed, speed, SpeedTolerance);
            Assert.Greater(_player.transform.position.z, 0f, "forward input moves along the camera forward (+Z)");
        }

        [UnityTest]
        public IEnumerator Pause_MapOffThenResume_NoMovementWhilePausedAndNoJump()
        {
            yield return Settle();
            _router.SendMove(Vector2.up);
            yield return null;
            yield return null;

            // Pausing: TimeController sets 0 and the router turns the Summoner map off (sends a zero move).
            Time.timeScale = 0f;
            _router.SendMove(Vector2.zero);
            yield return null;
            Vector3 pausedAt = _player.transform.position;
            for (int i = 0; i < 20; i++) yield return null;

            Assert.AreEqual(0f, Vector3.Distance(Planar(pausedAt), Planar(_player.transform.position)), 1e-3f);
            Assert.IsFalse(float.IsNaN(_player.transform.position.x));

            Time.timeScale = 1f;
            _router.SendMove(Vector2.up);
            Vector3 before = _player.transform.position;
            yield return null;
            float step = Vector3.Distance(Planar(before), Planar(_player.transform.position));

            // One frame of walking at most (plus slack for a slow test frame), never a catch-up jump.
            Assert.LessOrEqual(step, WalkSpeed * Time.unscaledDeltaTime + 0.05f);
        }

        [UnityTest]
        public IEnumerator FallOffMap_BelowResetHeight_ReturnsToSpawn()
        {
            yield return Settle();
            _spawn.transform.position = new Vector3(5f, 1.1f, 5f);

            _controller.TeleportTo(new Vector3(0f, _settings.FallResetY - 10f, 0f));
            for (int i = 0; i < 3; i++) yield return null;

            Assert.AreEqual(0f, Vector3.Distance(Planar(_spawn.transform.position), Planar(_player.transform.position)),
                0.05f);
            Assert.Greater(_player.transform.position.y, _settings.FallResetY);
        }

        [UnityTest]
        public IEnumerator Move_CameraYaw90_MovesAlongPositiveX()
        {
            yield return Settle();
            _camera.transform.rotation = Quaternion.Euler(30f, 90f, 0f);
            Vector3 start = _player.transform.position;

            _router.SendMove(Vector2.up);
            for (int i = 0; i < 10; i++) yield return null;

            Vector3 delta = Planar(_player.transform.position - start);
            Assert.Greater(delta.x, 0f);
            Assert.AreEqual(0f, delta.z, 0.01f);
        }
    }
}
