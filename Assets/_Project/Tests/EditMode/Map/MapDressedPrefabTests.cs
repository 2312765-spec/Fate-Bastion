using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using FateBastion.Audio;
using FateBastion.Editor.UI;

namespace FateBastion.Tests.EditMode.Map
{
    /// <summary>
    /// Checks the dressed Map_Level1 (C-17): art lives under Visuals/Dressing, has no colliders, stays off the road and
    /// out of the play area where it must, and the Sun drives the lighting. Gameplay checks stay in MapLevel1PrefabTests.
    /// </summary>
    public class MapDressedPrefabTests
    {
        private const string DressingPath = "Visuals/" + MapDresser.DressingName;
        // Layout contract (C-06/GDD 3): road is 3 m wide, walls at ±30.5 m.
        private const float RoadHalfWidth = 1.5f;
        private const float WallHalf = 30.5f;

        private GameObject _root;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(MapDresser.MapPath) == null) return;
            _root = PrefabUtility.LoadPrefabContents(MapDresser.MapPath);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            if (_root != null) PrefabUtility.UnloadPrefabContents(_root);
            _root = null;
        }

        [Test]
        public void Dressing_HasNoColliders()
        {
            Transform dressing = RequireDressing();
            Collider[] colliders = dressing.GetComponentsInChildren<Collider>(true);
            Assert.AreEqual(0, colliders.Length,
                colliders.Length > 0 ? $"Đồ trang trí có collider: {colliders[0].name} (chặn đặt tướng/raycast)." : "");
        }

        [Test]
        public void Sun_IsDirectionalWithShadows_AndDrivesAtmosphere()
        {
            Transform dressing = RequireDressing();
            Light sun = dressing.Find(MapDresser.SunName)?.GetComponent<Light>();
            Assert.IsNotNull(sun, "Thiếu Visuals/Dressing/Sun.");
            Assert.AreEqual(LightType.Directional, sun.type);
            Assert.AreNotEqual(LightShadows.None, sun.shadows);

            var atmosphere = dressing.GetComponent<MapAtmosphere>();
            Assert.IsNotNull(atmosphere, "Thiếu MapAtmosphere trên Dressing.");
            Assert.AreSame(sun, atmosphere.Sun);
        }

        [Test]
        public void Decor_StaysOffTheRoadAndInsideWalls()
        {
            Transform decor = RequireDressing().Find("Decor");
            Assert.IsNotNull(decor);
            Assert.Greater(decor.childCount, 0);
            List<Vector2> road = RoadCorners();
            foreach (Transform piece in decor)
            {
                var p = new Vector2(piece.position.x, piece.position.z);
                Assert.Greater(MapDressing.DistanceToTriangles(p, road), RoadHalfWidth, $"{piece.name} nằm trên đường.");
                Assert.IsTrue(MapDressing.InsideSquare(p, WallHalf), $"{piece.name} nằm ngoài tường.");
            }
        }

        [Test]
        public void Forest_StaysOutsideThePlayArea()
        {
            Transform forest = RequireDressing().Find("Forest");
            Assert.IsNotNull(forest);
            Assert.Greater(forest.childCount, 0);
            foreach (Transform piece in forest)
            {
                var p = new Vector2(piece.position.x, piece.position.z);
                Assert.IsFalse(MapDressing.InsideSquare(p, WallHalf), $"{piece.name} mọc trong vùng chơi.");
            }
        }

        [Test]
        public void Graybox_GameplayBoxesKeepCollidersButHideTheirMesh()
        {
            RequireDressing();
            foreach (string path in new[] { "Landmarks/Castle", "Landmarks/Gate", "Gameplay/Obstacles/Water" })
            {
                Transform box = _root.transform.Find(path);
                Assert.IsNotNull(box, path);
                Assert.IsNotNull(box.GetComponent<BoxCollider>(), $"{path} phải giữ collider.");
                Assert.IsFalse(box.GetComponent<MeshRenderer>().enabled, $"{path}: hộp graybox phải ẩn.");
            }
        }

        [Test]
        public void MatchProfile_HasBloomTonemappingAndVignette()
        {
            System.Type profileType = System.Type.GetType(PostProcessingInstaller.ProfileTypeName);
            Object profile = AssetDatabase.LoadAssetAtPath(PostProcessingInstaller.ProfilePath, profileType);
            Assert.IsNotNull(profile, "Thiếu PP_Match.");
            var has = profileType.GetMethod("Has", new[] { typeof(System.Type) });
            foreach (string typeName in new[] { PostProcessingInstaller.TonemappingTypeName, PostProcessingInstaller.VignetteTypeName })
            {
                System.Type component = System.Type.GetType(typeName);
                Assert.IsNotNull(component, typeName);
                Assert.IsTrue((bool)has.Invoke(profile, new object[] { component }), $"PP_Match thiếu {component.Name}.");
            }
        }

        [Test]
        public void Dress_RunTwice_SameLayoutAndNoDuplicates()
        {
            const string copy = "Assets/_Project/Prefabs/Map/__MapDressTest.prefab";
            Assert.IsTrue(AssetDatabase.CopyAsset(MapDresser.MapPath, copy));
            try
            {
                MapDressingSettings settings = MapDresser.LoadOrCreateSettings();
                Assert.IsTrue(MapDresser.Dress(copy, settings));
                Snapshot first = Take(copy);
                Assert.IsTrue(MapDresser.Dress(copy, settings));
                Snapshot second = Take(copy);

                Assert.AreEqual(1, second.DressingRoots, "Chạy lần 2 tạo thêm Dressing.");
                Assert.AreEqual(first.Pieces, second.Pieces);
                Assert.AreEqual(first.FirstDecor, second.FirstDecor);
            }
            finally
            {
                AssetDatabase.DeleteAsset(copy);
            }
        }

        private struct Snapshot
        {
            public int DressingRoots;
            public int Pieces;
            public Vector3 FirstDecor;
        }

        private static Snapshot Take(string path)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Transform visuals = root.transform.Find("Visuals");
                int roots = 0;
                foreach (Transform child in visuals)
                    if (child.name == MapDresser.DressingName) roots++;
                Transform dressing = visuals.Find(MapDresser.DressingName);
                Transform decor = dressing.Find("Decor");
                return new Snapshot
                {
                    DressingRoots = roots,
                    Pieces = dressing.Find("Forest").childCount + decor.childCount,
                    FirstDecor = decor.GetChild(0).position
                };
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private Transform RequireDressing()
        {
            if (_root == null) Assert.Ignore("Chưa có Map_Level1.");
            Transform dressing = _root.transform.Find(DressingPath);
            if (dressing == null) Assert.Ignore("Map chưa được dựng: chạy Tools > Map > Dress Map_Level1.");
            return dressing;
        }

        private List<Vector2> RoadCorners()
        {
            Transform road = _root.transform.Find("Gameplay/Road");
            Mesh mesh = road.GetComponent<MeshFilter>().sharedMesh;
            var corners = new List<Vector2>();
            Vector3[] v = mesh.vertices;
            foreach (int i in mesh.triangles)
            {
                Vector3 w = road.TransformPoint(v[i]);
                corners.Add(new Vector2(w.x, w.z));
            }

            return corners;
        }
    }
}
