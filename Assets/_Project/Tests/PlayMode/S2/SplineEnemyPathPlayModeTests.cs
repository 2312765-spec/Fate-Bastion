using System.Collections;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.TestTools;
using FateBastion.Enemies;

namespace FateBastion.Tests.PlayMode.S2
{
    /// <summary>SplineEnemyPath turns a SplineContainer into distance-based positions in world space (S2).</summary>
    public class SplineEnemyPathPlayModeTests
    {
        private GameObject _go;
        private SplineContainer _container;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("Path");
            _go.transform.position = new Vector3(0f, 0f, 5f);
            _container = _go.AddComponent<SplineContainer>();
            var spline = new Spline();
            spline.Add(new BezierKnot(new float3(0f, 0f, 0f)));
            spline.Add(new BezierKnot(new float3(10f, 0f, 0f)));
            _container.Spline = spline;
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_go);
        }

        [UnityTest]
        public IEnumerator Length_StraightTenMetreSpline_IsTen()
        {
            var path = new SplineEnemyPath(_container);
            yield return null;
            Assert.AreEqual(10f, path.Length, 0.01f);
        }

        [UnityTest]
        public IEnumerator Evaluate_Halfway_WorldPositionAndForward()
        {
            var path = new SplineEnemyPath(_container);
            path.Evaluate(5f, out Vector3 position, out Vector3 forward);
            yield return null;

            Assert.AreEqual(5f, position.x, 0.1f);
            Assert.AreEqual(5f, position.z, 0.01f, "includes the container's transform");
            Assert.Greater(Vector3.Dot(forward.normalized, Vector3.right), 0.99f);
        }

        [UnityTest]
        public IEnumerator Evaluate_BeyondEnds_Clamped()
        {
            var path = new SplineEnemyPath(_container);
            path.Evaluate(-3f, out Vector3 start, out _);
            path.Evaluate(50f, out Vector3 end, out _);
            yield return null;

            Assert.AreEqual(0f, start.x, 0.01f);
            Assert.AreEqual(10f, end.x, 0.01f);
        }

        [UnityTest]
        public IEnumerator LevelPaths_MissingIndex_ReturnsNullWithError()
        {
            var mapGo = new GameObject("Map");
            mapGo.SetActive(false);
            var paths = mapGo.AddComponent<LevelPaths>();
            paths.Configure(_container, null);
            mapGo.SetActive(true);

            Assert.IsNotNull(paths.GetPath(0));
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("pathIndex 1"));
            Assert.IsNull(paths.GetPath(1));
            yield return null;
            Object.Destroy(mapGo);
        }
    }
}
