using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace FateBastion.Editor.Heroes
{
    /// <summary>
    /// C-08: puts the Quaternius (CC0) models on the five enemy Variants and on the Summoner in Player.prefab.
    /// Tools > Enemies > Apply Enemy Models and Tools > Player > Apply Summoner Model. Each run rebuilds a "Visual"
    /// child under the prefab's "Model" (whose placeholder capsule renderer is switched off), so the gameplay objects,
    /// colliders and Enemy_Base stay untouched. Models come from Assets/ThirdParty/Quaternius (Source~/build/*.py).
    /// </summary>
    public static class CreatureModelBuilder
    {
        public const string VisualName = "Visual";
        public const string ModelName = "Model";
        public const string HealthBarAnchorName = "HealthBarAnchor";

        public const string EnemyPrefabFolder = "Assets/_Project/Prefabs/Enemies";
        public const string PlayerPrefabPath = "Assets/_Project/Prefabs/Player/Player.prefab";
        public const string MonstersFolder = "Assets/ThirdParty/Quaternius/Creatures/UltimateMonsters";
        public const string SummonerFolder = "Assets/ThirdParty/Quaternius/Characters/RPGCharacters";
        public const string EnemyAnimationsFolder = "Assets/_Project/Art/Animations/Enemies";
        public const string PlayerAnimationsFolder = "Assets/_Project/Art/Animations/Player";
        public const string MaterialFolder = "Assets/_Project/Art/Materials/Creatures";

        public const string SpeedParameter = "Speed";   // PlayerController.SpeedHash
        public const string CastParameter = "Cast";     // PlayerController.CastHash

        private const string LitShader = "Universal Render Pipeline/Lit";
        private const float Smoothness = 0.15f;            // matte toy-like monsters
        private const float HealthBarGap = 0.3f;           // above the model's head, metres

        /// <summary>One enemy type: which Quaternius model, which looping clip, how tall in the game.</summary>
        public struct EnemyArt
        {
            public string Prefab, Model, Clip;
            public float Height, ClipSpeed;

            public EnemyArt(string prefab, string model, string clip, float height, float clipSpeed)
            {
                Prefab = prefab; Model = model; Clip = clip; Height = height; ClipSpeed = clipSpeed;
            }
        }

        // ASSUMPTION: heights are art choices (Spec gives none): a hero is 1.6-1.9 m, the boss towers at 3 m.
        // ASSUMPTION: clip speed is eyeballed against moveSpeed (2.5 / 4.5 / 1.8 / 3 / 1.5 m/s), slower for the heavy ones.
        public static readonly EnemyArt[] Enemies =
        {
            new EnemyArt("Enemy_Normal", "Orc", "Walk", 1.6f, 1.0f),
            new EnemyArt("Enemy_Fast", "Ninja", "Run", 1.4f, 1.0f),
            new EnemyArt("Enemy_Armor", "Orc_Skull", "Walk", 1.8f, 0.8f),
            new EnemyArt("Enemy_Fly", "Dragon", "Fast_Flying", 1.0f, 1.0f),
            new EnemyArt("Enemy_Boss", "Demon", "Walk", 3.0f, 0.7f),
        };

        public const float SummonerHeight = 1.7f;           // ASSUMPTION: a human like Tu sĩ (1.7 m)
        private const string SummonerModel = "Wizard";
        private const string SummonerTexture = "Wizard_Texture";
        private const string SummonerStaffTexture = "Wizard_Staff_Texture";
        private const string StaffRendererKeyword = "Staff";
        private static readonly string[] SummonerLocomotion = { "Idle", "Walk", "Run" };
        private const string SummonerCastClip = "Spell1";
        // Speed thresholds in m/s: PlayerController moves at 6 (walk) and 9 (sprint) m/s.
        private static readonly float[] SummonerThresholds = { 0f, 3f, 6f };
        private const float SummonerSprintSpeed = 9f;

        [MenuItem("Tools/Enemies/Apply Enemy Models")]
        public static void ApplyEnemiesFromMenu()
        {
            int n = ApplyEnemies(EnemyPrefabFolder, MonstersFolder, EnemyAnimationsFolder, MaterialFolder);
            Debug.Log($"[Creature Models] {n} of {Enemies.Length} enemy Variants updated.");
        }

        [MenuItem("Tools/Player/Apply Summoner Model")]
        public static void ApplySummonerFromMenu()
        {
            bool ok = ApplySummoner(PlayerPrefabPath, SummonerFolder, PlayerAnimationsFolder, MaterialFolder);
            Debug.Log($"[Creature Models] Summoner {(ok ? "updated" : "NOT updated, see errors above")}.");
        }

        public static int ApplyEnemies(string prefabFolder, string modelFolder, string animFolder, string matFolder)
        {
            EnsureFolder(animFolder);
            EnsureFolder(matFolder);
            var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(modelFolder + "/Atlas_Monsters.png");
            Material material = CreateMaterial(matFolder + "/M_Monsters.mat", atlas, null);
            int count = 0;
            for (int i = 0; i < Enemies.Length; i++)
            {
                EnemyArt e = Enemies[i];
                string modelPath = modelFolder + "/" + e.Model + ".fbx";
                if (!ConfigureImporter(modelPath, new[] { e.Clip })) continue;
                AnimationClip clip = FindClip(modelPath, e.Clip);
                if (clip == null)
                {
                    Debug.LogError($"[Creature Models] {modelPath} has no clip '{e.Clip}'.");
                    continue;
                }

                var controller = CreateLoopController(animFolder + "/AC_" + e.Prefab + ".controller", clip, e.ClipSpeed);
                string prefabPath = prefabFolder + "/" + e.Prefab + ".prefab";
                if (Place(prefabPath, modelPath, controller, e.Height, r => material, AnimatorCullingMode.CullCompletely,
                        true))
                {
                    count++;
                }
            }

            AssetDatabase.SaveAssets();
            return count;
        }

        public static bool ApplySummoner(string playerPath, string modelFolder, string animFolder, string matFolder)
        {
            EnsureFolder(animFolder);
            EnsureFolder(matFolder);
            string modelPath = modelFolder + "/" + SummonerModel + ".fbx";
            var loops = new List<string>(SummonerLocomotion);
            if (!ConfigureImporter(modelPath, loops.ToArray())) return false;

            Material body = CreateMaterial(matFolder + "/M_Summoner.mat",
                AssetDatabase.LoadAssetAtPath<Texture2D>(modelFolder + "/" + SummonerTexture + ".png"), null);
            Material staff = CreateMaterial(matFolder + "/M_Summoner_Staff.mat",
                AssetDatabase.LoadAssetAtPath<Texture2D>(modelFolder + "/" + SummonerStaffTexture + ".png"), null);
            AnimatorController controller = CreateSummonerController(animFolder + "/AC_Summoner.controller", modelPath);
            if (controller == null) return false;

            // The camera and input run on unscaled time; PlayerController sets the Animator update mode itself.
            bool ok = Place(playerPath, modelPath, controller, SummonerHeight,
                r => r.name.Contains(StaffRendererKeyword) ? staff : body, AnimatorCullingMode.AlwaysAnimate, false);
            AssetDatabase.SaveAssets();
            return ok;
        }

        /// <summary>Generic rig, every take imported and named by its clip ("CharacterArmature|Walk" → "Walk").</summary>
        private static bool ConfigureImporter(string modelPath, string[] loopClips)
        {
            if (!(AssetImporter.GetAtPath(modelPath) is ModelImporter importer))
            {
                Debug.LogError($"[Creature Models] {modelPath} not found; run Source~/build/*.py in Blender first.");
                return false;
            }

            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            // Materials are assigned on the renderers from the shared atlas; nothing to extract from the FBX.
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.SaveAndReimport();

            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            for (int i = 0; i < clips.Length; i++)
            {
                clips[i].name = ClipNameFromTake(clips[i].takeName);
                clips[i].loopTime = System.Array.IndexOf(loopClips, clips[i].name) >= 0;
                // Enemies follow the spline and the Summoner its CharacterController: clips stay in place.
                clips[i].lockRootPositionXZ = true;
                clips[i].lockRootRotation = true;
                clips[i].lockRootHeightY = true;
                clips[i].keepOriginalPositionXZ = true;
                clips[i].keepOriginalPositionY = true;
                clips[i].keepOriginalOrientation = true;
            }

            importer.clipAnimations = clips;
            importer.SaveAndReimport();
            return true;
        }

        /// <summary>Clip name from an FBX take: Blender prefixes takes with the rig ("CharacterArmature|Walk").</summary>
        public static string ClipNameFromTake(string takeName)
        {
            int bar = takeName.LastIndexOf('|');
            return bar >= 0 ? takeName.Substring(bar + 1) : takeName;
        }

        private static AnimationClip FindClip(string modelPath, string clipName)
        {
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(modelPath))
            {
                if (asset is AnimationClip clip && clip.name == clipName) return clip;
            }

            return null;
        }

        private static Material CreateMaterial(string path, Texture2D baseMap, Texture2D normal)
        {
            var shader = Shader.Find(LitShader);
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = System.IO.Path.GetFileNameWithoutExtension(path) };
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            material.SetTexture("_BaseMap", baseMap);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", Smoothness);
            // 100 enemies on screen (GDD 13): let URP instance them when SRP Batcher cannot.
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>One looping state; the enemy never changes animation (EnemyController drives no Animator).</summary>
        private static AnimatorController CreateLoopController(string path, AnimationClip clip, float speed)
        {
            AssetDatabase.DeleteAsset(path);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            AnimatorStateMachine sm = controller.layers[0].stateMachine;
            AnimatorState state = sm.AddState("Move");
            state.motion = clip;
            state.speed = speed;
            sm.defaultState = state;
            return controller;
        }

        /// <summary>Idle → Walk → Run blended on Speed; Cast trigger plays the spell once and returns.</summary>
        private static AnimatorController CreateSummonerController(string path, string modelPath)
        {
            var clips = new AnimationClip[SummonerLocomotion.Length];
            for (int i = 0; i < clips.Length; i++)
            {
                clips[i] = FindClip(modelPath, SummonerLocomotion[i]);
                if (clips[i] == null)
                {
                    Debug.LogError($"[Creature Models] {modelPath} has no clip '{SummonerLocomotion[i]}'.");
                    return null;
                }
            }

            AnimationClip cast = FindClip(modelPath, SummonerCastClip);
            AssetDatabase.DeleteAsset(path);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter(SpeedParameter, AnimatorControllerParameterType.Float);
            controller.AddParameter(CastParameter, AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine sm = controller.layers[0].stateMachine;
            AnimatorState move = controller.CreateBlendTreeInController("Locomotion", out BlendTree tree, 0);
            tree.blendParameter = SpeedParameter;
            tree.useAutomaticThresholds = false;
            for (int i = 0; i < clips.Length; i++) tree.AddChild(clips[i], SummonerThresholds[i]);
            // Sprint reuses Run, played faster, so the feet keep up at 9 m/s.
            tree.AddChild(clips[clips.Length - 1], SummonerSprintSpeed);
            ChildMotion[] children = tree.children;
            children[children.Length - 1].timeScale = SummonerSprintSpeed / SummonerThresholds[SummonerThresholds.Length - 1];
            tree.children = children;
            sm.defaultState = move;

            if (cast != null)
            {
                AnimatorState castState = sm.AddState("Cast");
                castState.motion = cast;
                AnimatorStateTransition toCast = sm.AddAnyStateTransition(castState);
                toCast.AddCondition(AnimatorConditionMode.If, 0f, CastParameter);
                toCast.duration = 0.1f;
                toCast.canTransitionToSelf = false;
                AnimatorStateTransition back = castState.AddTransition(move);
                back.hasExitTime = true;
                back.exitTime = 0.9f;
                back.duration = 0.15f;
            }
            else
            {
                Debug.LogWarning($"[Creature Models] {modelPath} has no '{SummonerCastClip}' clip; Cast does nothing.");
            }

            return controller;
        }

        private static bool Place(string prefabPath, string modelPath, RuntimeAnimatorController controller, float height,
            System.Func<Renderer, Material> materialFor, AnimatorCullingMode culling, bool moveHealthBar)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null || AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
            {
                Debug.LogError($"[Creature Models] {prefabPath} or {modelPath} not found.");
                return false;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                Transform slot = FindDeep(root.transform, ModelName);
                if (slot == null)
                {
                    Debug.LogError($"[Creature Models] {prefabPath} has no '{ModelName}' object.");
                    return false;
                }

                // The capsule placeholder stays (other overrides may point at it) but no longer draws.
                var capsule = slot.GetComponent<MeshRenderer>();
                if (capsule != null) capsule.enabled = false;
                Transform old = slot.Find(VisualName);
                if (old != null) Object.DestroyImmediate(old.gameObject);

                var visual = (GameObject)PrefabUtility.InstantiatePrefab(model, slot);
                visual.name = VisualName;
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = Vector3.one;
                float ground = slot.parent != null ? slot.parent.position.y : root.transform.position.y;
                visual.transform.position = new Vector3(slot.position.x, ground, slot.position.z);

                foreach (Renderer r in visual.GetComponentsInChildren<Renderer>(true))
                {
                    var mats = new Material[r.sharedMaterials.Length];
                    for (int i = 0; i < mats.Length; i++) mats[i] = materialFor(r);
                    r.sharedMaterials = mats;
                    if (r is SkinnedMeshRenderer skinned) skinned.updateWhenOffscreen = false;
                }

                Bounds b = Measure(visual);
                if (b.size.y > 0.001f) visual.transform.localScale *= height / b.size.y;
                b = Measure(visual);
                visual.transform.position += new Vector3(0f, ground - b.min.y, 0f);
                b = Measure(visual);

                var animator = visual.GetComponent<Animator>();
                if (animator == null) animator = visual.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = culling;

                if (moveHealthBar)
                {
                    Transform bar = FindDeep(root.transform, HealthBarAnchorName);
                    if (bar != null) bar.position = new Vector3(bar.position.x, b.max.y + HealthBarGap, bar.position.z);
                }

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Debug.Log($"[Creature Models] {prefabPath}: {model.name}, {b.size.y:0.00} m.");
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>World bounds of the visible meshes in their bind pose (skinned bounds are recomputed from the mesh).</summary>
        public static Bounds Measure(GameObject go)
        {
            bool any = false;
            var b = new Bounds();
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
            {
                Bounds rb;
                if (r is SkinnedMeshRenderer s && s.sharedMesh != null)
                {
                    s.ResetBounds();
                    rb = s.bounds;
                }
                else rb = r.bounds;

                if (!any) { b = rb; any = true; }
                else b.Encapsulate(rb);
            }

            return b;
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

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = System.IO.Path.GetDirectoryName(folder)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(folder));
        }
    }
}
