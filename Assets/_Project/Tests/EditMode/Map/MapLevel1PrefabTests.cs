using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using FateBastion.Enemies;

namespace FateBastion.Tests.EditMode.Map
{
    /// <summary>Checks the graybox map prefab Map_Level1 (C-06) before it is handed to A and B.</summary>
    public class MapLevel1PrefabTests
    {
        private const string PrefabPath = "Assets/_Project/Prefabs/Map/Map_Level1.prefab";

        // Hierarchy contract of the map prefab (C-06); C-17 only adds art under Visuals/.
        private const string MainPathName = "Paths/Path_Main";
        private const string FlyingPathName = "Paths/Path_Flying";
        private const string GroundName = "Gameplay/Ground";
        private const string RoadName = "Gameplay/Road";
        private const string ObstaclesName = "Gameplay/Obstacles";
        private const string BoundsName = "Gameplay/Bounds";
        private const string GateName = "Landmarks/Gate";
        private const string CastleName = "Landmarks/Castle";
        private const string SpawnName = "Landmarks/SummonerSpawn";
        private const string VisualsName = "Visuals";

        // Test asmdef does not reference Unity.Splines (shared asmdef), so spline components are found by type name.
        private const string SplineContainerType = "SplineContainer";
        private const string SplineExtrudeType = "SplineExtrude";

        // Tolerances for checking the layout, not balance numbers: S2 asks for a main path of about 90 m,
        // flying enemies fly 2 m high, the map is about 60 x 60 m (GDD 3).
        private const float MainPathMinLength = 85f;
        private const float MainPathMaxLength = 95f;
        private const float FlyingHeight = 2f;
        private const float HeightTolerance = 0.1f;
        private const float LandmarkReach = 5f;
        private const float MapHalfSize = 30f;
        private const float RoadHalfWidth = 1.5f;
        private const float HeroFootprint = 0.8f;
        private const float SampleStep = 0.5f;

        private GameObject _root;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null) return;
            _root = PrefabUtility.LoadPrefabContents(PrefabPath);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            if (_root != null) PrefabUtility.UnloadPrefabContents(_root);
            _root = null;
        }

        [Test]
        public void Prefab_Exists_HasLevelPathsOnRoot()
        {
            RequirePrefab();
            Assert.IsNotNull(_root.GetComponent<LevelPaths>(), "Gốc Map_Level1 phải có component LevelPaths.");
        }

        [Test]
        public void LevelPaths_BothSplines_AreAssignedToChildren()
        {
            RequirePrefab();
            var serialized = new SerializedObject(_root.GetComponent<LevelPaths>());
            Object main = serialized.FindProperty("_mainPath").objectReferenceValue;
            Object flying = serialized.FindProperty("_flyingPath").objectReferenceValue;

            Assert.AreSame(FindByType(MainPathName, SplineContainerType), main, "LevelPaths._mainPath phải trỏ tới Paths/Path_Main.");
            Assert.AreSame(FindByType(FlyingPathName, SplineContainerType), flying, "LevelPaths._flyingPath phải trỏ tới Paths/Path_Flying.");
        }

        [Test]
        public void MainPath_Length_IsAbout90m()
        {
            RequirePrefab();
            float length = MainPath().Length;

            Assert.That(length, Is.InRange(MainPathMinLength, MainPathMaxLength), $"Đường chính dài {length:0.0} m.");
        }

        [Test]
        public void FlyingPath_Length_IsShorterThanMainPath()
        {
            RequirePrefab();
            Assert.Less(FlyingPath().Length, MainPath().Length);
        }

        [Test]
        public void FlyingPath_Height_Is2mAboveGround()
        {
            RequirePrefab();
            IEnemyPath flying = FlyingPath();
            float groundY = GroundTopY();

            for (float d = 0f; d <= flying.Length; d += SampleStep)
            {
                flying.Evaluate(d, out Vector3 position, out _);
                Assert.AreEqual(groundY + FlyingHeight, position.y, HeightTolerance, $"Đường bay ở {d:0.0} m không cao 2 m.");
            }
        }

        [Test]
        public void MainPath_Height_IsOnGround()
        {
            RequirePrefab();
            IEnemyPath main = MainPath();
            float groundY = GroundTopY();

            for (float d = 0f; d <= main.Length; d += SampleStep)
            {
                main.Evaluate(d, out Vector3 position, out _);
                Assert.AreEqual(groundY, position.y, HeightTolerance, $"Đường chính ở {d:0.0} m không nằm trên mặt đất.");
            }
        }

        [Test]
        public void BothPaths_StartAtGate_EndAtCastle()
        {
            RequirePrefab();
            Vector3 gate = Find<Transform>(GateName).position;
            Vector3 castle = Find<Transform>(CastleName).position;

            AssertEnds(MainPath(), gate, castle, "Đường chính");
            AssertEnds(FlyingPath(), gate, castle, "Đường bay");
        }

        [Test]
        public void Ground_HasCollider_OnPlaceableLayer()
        {
            RequirePrefab();
            AssertColliderOnLayer(Find<Transform>(GroundName).gameObject, "Placeable");
        }

        [Test]
        public void Road_HasExtrudedCollider_OnPathLayer()
        {
            RequirePrefab();
            Transform road = Find<Transform>(RoadName);
            Component extrude = FindByType(RoadName, SplineExtrudeType);
            Object container = new SerializedObject(extrude).FindProperty("m_Container").objectReferenceValue;

            Assert.AreSame(FindByType(MainPathName, SplineContainerType), container, "SplineExtrude của Road phải dùng Path_Main.");
            AssertColliderOnLayer(road.gameObject, "Path");
        }

        [Test]
        public void GateAndCastle_HaveColliders_OnObstacleLayer()
        {
            RequirePrefab();
            AssertColliderOnLayer(Find<Transform>(GateName).gameObject, "Obstacle");
            AssertColliderOnLayer(Find<Transform>(CastleName).gameObject, "Obstacle");
        }

        [Test]
        public void ObstaclesAndBounds_AllColliders_OnObstacleLayer()
        {
            RequirePrefab();
            int obstacle = LayerMask.NameToLayer("Obstacle");
            Collider[] obstacles = Find<Transform>(ObstaclesName).GetComponentsInChildren<Collider>(true);
            Collider[] bounds = Find<Transform>(BoundsName).GetComponentsInChildren<Collider>(true);

            Assert.IsNotEmpty(obstacles, "Gameplay/Obstacles chưa có vật cản nào.");
            Assert.GreaterOrEqual(bounds.Length, 4, "Gameplay/Bounds cần 4 tường viền.");
            foreach (Collider c in obstacles) Assert.AreEqual(obstacle, c.gameObject.layer, $"{c.name} phải ở layer Obstacle.");
            foreach (Collider c in bounds) Assert.AreEqual(obstacle, c.gameObject.layer, $"{c.name} phải ở layer Obstacle.");
        }

        [Test]
        public void Visuals_HaveNoColliders()
        {
            RequirePrefab();
            Collider[] colliders = Find<Transform>(VisualsName).GetComponentsInChildren<Collider>(true);

            // Collider trong Visuals/ sẽ làm sai luật đặt tướng (S4) – collider gameplay nằm trong Gameplay/ hoặc Landmarks/.
            Assert.IsEmpty(colliders, "Visuals/ chỉ chứa hình ảnh, không có collider.");
        }

        [Test]
        public void SummonerSpawn_IsInsideMap_AndOffTheRoad()
        {
            RequirePrefab();
            Vector3 spawn = Find<Transform>(SpawnName).position;
            IEnemyPath main = MainPath();

            Assert.That(Mathf.Abs(spawn.x), Is.LessThan(MapHalfSize), "SummonerSpawn nằm ngoài map.");
            Assert.That(Mathf.Abs(spawn.z), Is.LessThan(MapHalfSize), "SummonerSpawn nằm ngoài map.");
            Assert.GreaterOrEqual(spawn.y, GroundTopY() - HeightTolerance, "SummonerSpawn nằm dưới mặt đất.");
            Assert.GreaterOrEqual(MinDistanceXZ(main, spawn), RoadHalfWidth + HeroFootprint, "SummonerSpawn quá gần đường.");
        }

        // ---------- helpers ----------

        private void RequirePrefab()
        {
            if (_root == null) Assert.Fail($"Chưa có prefab {PrefabPath} – dựng theo hướng dẫn C-06.");
        }

        private T Find<T>(string path) where T : Component
        {
            Transform child = _root.transform.Find(path);
            Assert.IsNotNull(child, $"Map_Level1 thiếu object {path}.");
            T component = child.GetComponent<T>();
            Assert.IsNotNull(component, $"{path} thiếu component {typeof(T).Name}.");
            return component;
        }

        private Component FindByType(string path, string typeName)
        {
            Transform child = Find<Transform>(path);
            Component component = child.GetComponent(typeName);
            Assert.IsNotNull(component, $"{path} thiếu component {typeName}.");
            return component;
        }

        // Check the references first so a missing spline fails the assert instead of logging LevelPaths errors.
        private IEnemyPath MainPath() => GetPath(LevelPaths.MainPathIndex, "_mainPath");

        private IEnemyPath FlyingPath() => GetPath(LevelPaths.FlyingPathIndex, "_flyingPath");

        private IEnemyPath GetPath(int pathIndex, string field)
        {
            var levelPaths = _root.GetComponent<LevelPaths>();
            Assert.IsNotNull(levelPaths, "Gốc Map_Level1 phải có component LevelPaths.");
            Assert.IsNotNull(new SerializedObject(levelPaths).FindProperty(field).objectReferenceValue, $"LevelPaths.{field} chưa gắn Spline.");
            return levelPaths.GetPath(pathIndex);
        }

        private float GroundTopY()
        {
            var box = Find<Transform>(GroundName).GetComponent<BoxCollider>();
            Assert.IsNotNull(box, "Gameplay/Ground dùng BoxCollider (khối dày, mặt trên ở y = 0).");
            Vector3 top = box.transform.TransformPoint(box.center + Vector3.up * box.size.y * 0.5f);
            return top.y;
        }

        private static void AssertColliderOnLayer(GameObject go, string layerName)
        {
            Assert.IsNotNull(go.GetComponent<Collider>(), $"{go.name} thiếu Collider.");
            Assert.AreEqual(LayerMask.NameToLayer(layerName), go.layer, $"{go.name} phải ở layer {layerName}.");
        }

        private static void AssertEnds(IEnemyPath path, Vector3 gate, Vector3 castle, string label)
        {
            path.Evaluate(0f, out Vector3 start, out _);
            path.Evaluate(path.Length, out Vector3 end, out _);

            Assert.LessOrEqual(DistanceXZ(start, gate), LandmarkReach, $"{label} không bắt đầu ở Cổng.");
            Assert.LessOrEqual(DistanceXZ(end, castle), LandmarkReach, $"{label} không kết thúc ở Thành.");
        }

        private static float MinDistanceXZ(IEnemyPath path, Vector3 point)
        {
            float min = float.MaxValue;
            for (float d = 0f; d <= path.Length; d += SampleStep)
            {
                path.Evaluate(d, out Vector3 position, out _);
                min = Mathf.Min(min, DistanceXZ(position, point));
            }

            return min;
        }

        private static float DistanceXZ(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
