using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using FateBastion.Heroes;

namespace FateBastion.Tests.PlayMode.S4
{
    /// <summary>
    /// S4 PlayMode criterion: no placing on the path, water, a cliff, or on top of another hero. Real colliders on the
    /// project layers, the same probe PlacementSystem runs on every frame and click.
    /// </summary>
    public class PlacementProbePlayModeTests
    {
        private const float Radius = 0.8f;
        private readonly List<GameObject> _objects = new List<GameObject>();
        private PlacementMasks _masks;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _masks = PlacementMasks.FromProjectLayers();

            // Ground x in [-10, 10]: Placeable. A road strip at x = 5, water at x = -6, a cliff box at z = 6.
            Box("Ground", "Placeable", new Vector3(0f, -0.5f, 0f), new Vector3(20f, 1f, 20f));
            Box("Path", "Path", new Vector3(5f, 0.01f, 0f), new Vector3(2f, 0.02f, 20f));
            Box("Water", "Water", new Vector3(-6f, 0.02f, 0f), new Vector3(3f, 0.04f, 20f));
            Box("Cliff", "Obstacle", new Vector3(0f, 1.5f, 6f), new Vector3(4f, 3f, 2f));
            Box("PlacedHero", "Hero", new Vector3(-2f, 0.5f, -5f), new Vector3(0.8f, 1f, 0.8f));

            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _objects) Object.Destroy(go);
            _objects.Clear();
        }

        private void Box(string name, string layer, Vector3 center, Vector3 size)
        {
            int layerIndex = LayerMask.NameToLayer(layer);
            Assert.GreaterOrEqual(layerIndex, 0, $"Layer '{layer}' missing from Tags & Layers.");

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.layer = layerIndex;
            go.transform.position = center;
            go.transform.localScale = size;
            _objects.Add(go);
        }

        private static Ray Down(float x, float z) => new Ray(new Vector3(x, 30f, z), Vector3.down);

        [Test]
        public void OpenGround_IsValid()
        {
            PlacementProbeResult r = PlacementProbe.Probe(Down(-1f, 0f), _masks, Radius);
            Assert.IsTrue(r.HitSurface);
            Assert.IsTrue(r.HitPlaceable);
            Assert.IsTrue(r.AreaFree);
            Assert.AreEqual(0f, r.Point.y, 0.01f);
        }

        [Test]
        public void OnThePath_NotPlaceable()
        {
            PlacementProbeResult r = PlacementProbe.Probe(Down(5f, 0f), _masks, Radius);
            Assert.IsFalse(r.HitPlaceable);
        }

        [Test]
        public void NextToThePath_Blocked()
        {
            // 0.5 m from the road edge (x = 4): ground is Placeable but the 0.8 m sphere touches the path.
            PlacementProbeResult r = PlacementProbe.Probe(Down(3.5f, 0f), _masks, Radius);
            Assert.IsTrue(r.HitPlaceable);
            Assert.IsFalse(r.AreaFree);
        }

        [Test]
        public void OnWater_NotPlaceable()
        {
            PlacementProbeResult r = PlacementProbe.Probe(Down(-6f, 0f), _masks, Radius);
            Assert.IsFalse(r.HitPlaceable);
        }

        [Test]
        public void OnCliff_NotPlaceable()
        {
            PlacementProbeResult r = PlacementProbe.Probe(Down(0f, 6f), _masks, Radius);
            Assert.IsFalse(r.HitPlaceable);
        }

        [Test]
        public void OnTopOfAHero_Blocked()
        {
            // The ray ignores heroes and lands on the ground beside it; the sphere then finds the hero.
            PlacementProbeResult r = PlacementProbe.Probe(Down(-2f, -5f), _masks, Radius);
            Assert.IsTrue(r.HitPlaceable);
            Assert.IsFalse(r.AreaFree);
        }

        [Test]
        public void PickHero_FindsOnlyPlacedHeroes()
        {
            int heroMask = LayerMask.GetMask("Hero");
            Assert.IsNull(PlacementProbe.PickHero(Down(-2f, -5f), heroMask), "A collider without HeroLevel is not a hero.");
            Assert.IsNull(PlacementProbe.PickHero(Down(-1f, 0f), heroMask));
        }

        [Test]
        public void NoSurface_NoHit()
        {
            PlacementProbeResult r = PlacementProbe.Probe(new Ray(new Vector3(0f, 30f, 0f), Vector3.up), _masks, Radius);
            Assert.IsFalse(r.HitSurface);
            Assert.IsFalse(r.HitPlaceable);
        }
    }
}
