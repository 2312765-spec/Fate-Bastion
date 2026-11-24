using NUnit.Framework;
using UnityEngine;
using FateBastion.Editor.Heroes;

namespace FateBastion.Tests.EditMode.S0b
{
    /// <summary>Framing math of Render Hero Icons (C-02): the model's box fills the square icon with the set margin.</summary>
    public class HeroIconFramingTests
    {
        private const float Fov = 25f;

        [Test]
        public void FitDistance_FrontView_TallestHalfTouchesFrameEdge()
        {
            // 1 m wide, 2 m tall, flat box seen straight on: the 1 m half-height decides the distance.
            var bounds = new Bounds(Vector3.zero, new Vector3(1f, 2f, 0f));

            float distance = HeroIconFraming.FitDistance(bounds, Vector3.zero, Quaternion.identity, Fov, 1f);

            Assert.AreEqual(1f / Mathf.Tan(Fov * 0.5f * Mathf.Deg2Rad), distance, 1e-4f);
        }

        [Test]
        public void FitDistance_LargerMargin_MovesCameraBackProportionally()
        {
            var bounds = new Bounds(Vector3.zero, new Vector3(1f, 2f, 0f));

            float tight = HeroIconFraming.FitDistance(bounds, Vector3.zero, Quaternion.identity, Fov, 1f);
            float loose = HeroIconFraming.FitDistance(bounds, Vector3.zero, Quaternion.identity, Fov, 1.2f);

            Assert.AreEqual(tight * 1.2f, loose, 1e-4f);
        }

        [Test]
        public void FitDistance_AnyAngle_EveryCornerInsideFrame()
        {
            var bounds = new Bounds(new Vector3(0f, 0.9f, 0f), new Vector3(0.8f, 1.8f, 0.5f));
            Vector3 target = new Vector3(0f, 1f, 0f);
            Quaternion rotation = Quaternion.Euler(12f, 200f, 0f);

            float distance = HeroIconFraming.FitDistance(bounds, target, rotation, Fov, 1f);

            Vector3 cameraPosition = target - rotation * Vector3.forward * distance;
            float tan = Mathf.Tan(Fov * 0.5f * Mathf.Deg2Rad);
            float widest = 0f;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = new Vector3(i % 2 == 0 ? bounds.min.x : bounds.max.x,
                    (i / 2) % 2 == 0 ? bounds.min.y : bounds.max.y, i / 4 == 0 ? bounds.min.z : bounds.max.z);
                Vector3 local = Quaternion.Inverse(rotation) * (corner - cameraPosition);
                Assert.Greater(local.z, 0f, "Corner behind the camera.");
                float ratio = Mathf.Max(Mathf.Abs(local.x), Mathf.Abs(local.y)) / (local.z * tan);
                Assert.LessOrEqual(ratio, 1f + 1e-4f, "Corner outside the frame.");
                widest = Mathf.Max(widest, ratio);
            }

            Assert.AreEqual(1f, widest, 1e-4f, "Margin 1 should make the widest corner touch the edge.");
        }

        [Test]
        public void TryGetOpaqueRect_FewOpaquePixels_ReturnsTightRect()
        {
            const int size = 16;
            var pixels = new Color32[size * size];
            pixels[3 * size + 2] = new Color32(255, 0, 0, 255);   // x 2, y 3
            pixels[10 * size + 12] = new Color32(0, 255, 0, 200); // x 12, y 10
            pixels[15 * size + 15] = new Color32(0, 0, 255, 5);   // faint fringe, below threshold

            Assert.IsTrue(HeroIconFraming.TryGetOpaqueRect(pixels, size, 8, out RectInt rect));
            Assert.AreEqual(new RectInt(2, 3, 11, 8), rect);
        }

        [Test]
        public void TryGetOpaqueRect_FullyTransparent_ReturnsFalse()
        {
            Assert.IsFalse(HeroIconFraming.TryGetOpaqueRect(new Color32[64], 8, 8, out _));
        }

        [Test]
        public void RefineFromOpaqueRect_SmallOffCentreHero_CentresAndScalesToMargin()
        {
            // 256 px icon, hero 128 px tall sitting 32 px above centre: move up, and halve the distance to fill 100 %.
            var opaque = new RectInt(96, 96, 64, 128);
            float tan = Mathf.Tan(Fov * 0.5f * Mathf.Deg2Rad);

            float distance = HeroIconFraming.RefineFromOpaqueRect(opaque, 256, 10f, Fov, 1f, out Vector2 shift);

            float metresPerPixel = 2f * 10f * tan / 256f;
            Assert.AreEqual(0f, shift.x, 1e-4f);
            Assert.AreEqual(32f * metresPerPixel, shift.y, 1e-4f);
            Assert.AreEqual(5f, distance, 1e-4f);
        }

        [Test]
        public void RefineFromOpaqueRect_AlreadyFramed_KeepsDistance()
        {
            // Longest side already 256 / 1.08 px and centred: nothing to change.
            int side = Mathf.RoundToInt(256f / 1.08f);
            var opaque = new RectInt((256 - side) / 2, (256 - side) / 2, side, side);

            float distance = HeroIconFraming.RefineFromOpaqueRect(opaque, 256, 7f, Fov, 1.08f, out Vector2 shift);

            Assert.AreEqual(7f, distance, 0.05f);
            Assert.Less(shift.magnitude, 0.05f);
        }

        [Test]
        public void IsModelRenderer_ParticleRenderer_IsExcluded()
        {
            var go = new GameObject("Icon framing test");
            try
            {
                var mesh = new GameObject("Mesh").AddComponent<MeshRenderer>();
                mesh.transform.SetParent(go.transform);
                var particles = new GameObject("Particles").AddComponent<ParticleSystem>();
                particles.transform.SetParent(go.transform);

                Assert.IsTrue(HeroIconFraming.IsModelRenderer(mesh));
                Assert.IsFalse(HeroIconFraming.IsModelRenderer(particles.GetComponent<ParticleSystemRenderer>()));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
