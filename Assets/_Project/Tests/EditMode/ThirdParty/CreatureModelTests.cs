using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using FateBastion.Editor.Heroes;

namespace FateBastion.Tests.EditMode.ThirdParty
{
    /// <summary>Checks the Quaternius models placed on the enemy Variants and the Summoner (C-08).</summary>
    public class CreatureModelTests
    {
        // ASSUMPTION: per-enemy budget so 100 enemies stay within 30 FPS on Intel UHD (GDD 13).
        private const int MaxEnemyTriangles = 3000;
        private const float HeightTolerance = 0.1f;   // fraction of the target height

        private static readonly string[] EnemyPrefabs =
            { "Enemy_Normal", "Enemy_Fast", "Enemy_Armor", "Enemy_Fly", "Enemy_Boss" };

        [TestCase("CharacterArmature|Walk", "Walk")]
        [TestCase("CharacterArmature|CharacterArmature|Fast_Flying", "Fast_Flying")]
        [TestCase("Spell1", "Spell1")]
        public void ClipNameFromTake_RigPrefixed_KeepsClipName(string take, string expected)
        {
            Assert.AreEqual(expected, CreatureModelBuilder.ClipNameFromTake(take));
        }

        [TestCaseSource(nameof(EnemyPrefabs))]
        public void Enemy_Visual_HasLoopingClipAndCulls(string prefabName)
        {
            GameObject visual = RequireVisual(EnemyPath(prefabName));

            var animator = visual.GetComponent<Animator>();
            Assert.IsNotNull(animator, "Visual phải có Animator.");
            Assert.AreEqual(AnimatorCullingMode.CullCompletely, animator.cullingMode);
            Assert.IsFalse(animator.applyRootMotion);
            var controller = animator.runtimeAnimatorController as AnimatorController;
            Assert.IsNotNull(controller);
            AnimatorState state = controller.layers[0].stateMachine.defaultState;
            var clip = state.motion as AnimationClip;
            Assert.IsNotNull(clip, "State mặc định phải có clip.");
            Assert.IsTrue(clip.isLooping, $"Clip '{clip.name}' của {prefabName} phải lặp.");
        }

        [TestCaseSource(nameof(EnemyPrefabs))]
        public void Enemy_Visual_WithinTriangleBudget(string prefabName)
        {
            GameObject visual = RequireVisual(EnemyPath(prefabName));

            int triangles = 0;
            foreach (var s in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                for (int i = 0; i < s.sharedMesh.subMeshCount; i++) triangles += (int)s.sharedMesh.GetIndexCount(i) / 3;
            }

            Assert.LessOrEqual(triangles, MaxEnemyTriangles);
        }

        [TestCaseSource(nameof(EnemyPrefabs))]
        public void Enemy_Prefab_CapsuleHiddenAndBarAboveHead(string prefabName)
        {
            string path = EnemyPath(prefabName);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Transform model = root.transform.Find(CreatureModelBuilder.ModelName);
                var capsule = model.GetComponent<MeshRenderer>();
                Assert.IsTrue(capsule == null || !capsule.enabled, "Capsule tạm phải bị tắt.");

                Transform visual = model.Find(CreatureModelBuilder.VisualName);
                Bounds b = CreatureModelBuilder.Measure(visual.gameObject);
                Transform bar = root.transform.Find(CreatureModelBuilder.HealthBarAnchorName);
                Assert.Greater(bar.position.y, b.max.y, "Thanh máu phải nằm trên đầu model.");

                float target = TargetHeight(prefabName);
                Assert.That(b.size.y, Is.EqualTo(target).Within(target * HeightTolerance));
                Assert.That(b.min.y, Is.EqualTo(0f).Within(0.02f), "Chân model phải chạm mặt đất của prefab.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [Test]
        public void Summoner_Animator_HasSpeedAndCast()
        {
            GameObject visual = RequireVisual(CreatureModelBuilder.PlayerPrefabPath);

            var controller = visual.GetComponent<Animator>().runtimeAnimatorController as AnimatorController;
            Assert.IsNotNull(controller);
            bool speed = false, cast = false;
            foreach (AnimatorControllerParameter p in controller.parameters)
            {
                speed |= p.name == CreatureModelBuilder.SpeedParameter && p.type == AnimatorControllerParameterType.Float;
                cast |= p.name == CreatureModelBuilder.CastParameter && p.type == AnimatorControllerParameterType.Trigger;
            }

            Assert.IsTrue(speed, "Thiếu float Speed (PlayerController).");
            Assert.IsTrue(cast, "Thiếu trigger Cast (PlayerController).");
        }

        [Test]
        public void Summoner_Visual_IsHumanSized()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(CreatureModelBuilder.PlayerPrefabPath);
            try
            {
                Transform visual = FindDeep(root.transform, CreatureModelBuilder.VisualName);
                Assert.IsNotNull(visual, "Player chưa có Visual: chạy Tools > Player > Apply Summoner Model.");
                Bounds b = CreatureModelBuilder.Measure(visual.gameObject);
                Assert.That(b.size.y, Is.EqualTo(CreatureModelBuilder.SummonerHeight)
                    .Within(CreatureModelBuilder.SummonerHeight * HeightTolerance));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static string EnemyPath(string prefabName) =>
            CreatureModelBuilder.EnemyPrefabFolder + "/" + prefabName + ".prefab";

        private static float TargetHeight(string prefabName)
        {
            foreach (CreatureModelBuilder.EnemyArt e in CreatureModelBuilder.Enemies)
            {
                if (e.Prefab == prefabName) return e.Height;
            }

            Assert.Fail($"{prefabName} không có trong CreatureModelBuilder.Enemies.");
            return 0f;
        }

        private static GameObject RequireVisual(string prefabPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.IsNotNull(prefab, $"Thiếu {prefabPath}.");
            Transform visual = FindDeep(prefab.transform, CreatureModelBuilder.VisualName);
            Assert.IsNotNull(visual, $"{prefabPath} chưa có Visual: chạy công cụ Apply ... Model.");
            return visual.gameObject;
        }

        private static Transform FindDeep(Transform parent, string childName)
        {
            if (parent.name == childName) return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindDeep(parent.GetChild(i), childName);
                if (found != null) return found;
            }

            return null;
        }
    }
}
