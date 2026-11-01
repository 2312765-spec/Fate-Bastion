using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using FateBastion.Editor.Import;
using FateBastion.Enemies;
using FateBastion.Player;

namespace FateBastion.Editor.UI
{
    /// <summary>
    /// Tools > Meteor Aim: placeholder aim visuals (range ring, blast disc, impact marker) and the S7 wiring of
    /// MeteorAimIndicator + CommandAura into Player.prefab and the open scene. C-09 replaces the visuals later.
    /// </summary>
    public static class MeteorAimSetup
    {
        public const string FxPrefabPath = "Assets/_Project/Prefabs/FX/MeteorAimFX.prefab";
        public const string PlayerPrefabPath = "Assets/_Project/Prefabs/Player/Player.prefab";
        public const string MeshFolder = "Assets/_Project/Art/Meshes/FX";
        public const string MaterialFolder = "Assets/_Project/Art/Materials/FX";
        public const string MeteorSettingsPath = "Assets/_Project/Data/Settings/meteor_settings.asset";
        public const string AuraSettingsPath = "Assets/_Project/Data/Settings/CommandAuraSettings.asset";

        private const string UrpUnlitShader = "Universal Render Pipeline/Unlit";
        private const string SummonerName = "Summoner";
        private const string MainCameraName = "Main Camera";
        private const string RangeRingName = "RangeRing";
        private const string BlastPreviewName = "BlastPreview";
        private const string ImpactMarkerName = "ImpactMarker";

        // Meshes are 1 m across on XZ so MeteorAimIndicator.SetDiameter scales them straight to metres.
        private const int CircleSegments = 96;
        private const float UnitRadius = 0.5f;
        // 1.2 m wide band at the 20 m range (diameter 40 × 0.03).
        private const float RingInnerRadius = 0.47f;
        // Lifted above the ground point so the disc does not z-fight with the terrain it was raycast onto.
        private const float GroundLift = 0.05f;

        // Range ring keeps its material colour; the indicator tints the blast/impact discs with a property block.
        private static readonly Color RingColor = new Color(1f, 0.85f, 0.3f, 0.6f);
        private static readonly Color DiscColor = new Color(1f, 1f, 1f, 0.55f);

        private static readonly string[] GroundLayerNames = { "Placeable", "Path" };

        [MenuItem("Tools/Meteor Aim/1. Create Aim FX Prefab")]
        public static void CreateFxFromMenu()
        {
            if (CreateFxPrefab() != null)
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(FxPrefabPath));
        }

        [MenuItem("Tools/Meteor Aim/2. Wire Player Prefab")]
        public static void WirePlayerFromMenu() => WirePlayerPrefab();

        [MenuItem("Tools/Meteor Aim/3. Assign Scene References (open scene)")]
        public static void AssignSceneFromMenu() => AssignSceneReferences(SceneManager.GetActiveScene());

        /// <summary>Steps 1-3 for Game and Sandbox/C_Main; also the -executeMethod entry for batch mode.</summary>
        [MenuItem("Tools/Meteor Aim/Run All (Game + C_Main)")]
        public static void RunAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() || !WirePlayerPrefab()) return;

            foreach (string scenePath in WiredScenePaths)
            {
                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                if (AssignSceneReferences(scene)) EditorSceneManager.SaveScene(scene);
            }
        }

        private static readonly string[] WiredScenePaths =
        {
            "Assets/_Project/Scenes/Game.unity",
            "Assets/_Project/Scenes/Sandbox/C_Main.unity",
        };

        /// <summary>Creates MeteorAimFX.prefab with its meshes and materials; returns the existing one untouched if present.</summary>
        public static GameObject CreateFxPrefab()
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(FxPrefabPath);
            if (existing != null)
            {
                Debug.Log($"[Meteor Aim] {FxPrefabPath} already exists; not overwritten. Delete it first to rebuild.");
                return existing;
            }

            if (Shader.Find(UrpUnlitShader) == null)
            {
                Debug.LogError($"[Meteor Aim] Shader '{UrpUnlitShader}' not found; is URP installed?");
                return null;
            }

            AssetFolderUtility.EnsureFolder(MeshFolder);
            AssetFolderUtility.EnsureFolder(MaterialFolder);
            AssetFolderUtility.EnsureFolder("Assets/_Project/Prefabs/FX");

            Mesh ring = SaveAsset(BuildCircleMesh("UnitRing", RingInnerRadius), MeshFolder + "/UnitRing.asset");
            Mesh disc = SaveAsset(BuildCircleMesh("UnitDisc", 0f), MeshFolder + "/UnitDisc.asset");
            Material ringMaterial = SaveAsset(BuildTransparentMaterial("M_MeteorRange", RingColor),
                MaterialFolder + "/M_MeteorRange.mat");
            Material discMaterial = SaveAsset(BuildTransparentMaterial("M_MeteorBlast", DiscColor),
                MaterialFolder + "/M_MeteorBlast.mat");

            int ignoreRaycast = LayerMask.NameToLayer("Ignore Raycast");
            var root = new GameObject("MeteorAimFX") { layer = ignoreRaycast };
            try
            {
                AddVisual(root.transform, RangeRingName, ring, ringMaterial, ignoreRaycast);
                AddVisual(root.transform, BlastPreviewName, disc, discMaterial, ignoreRaycast);
                AddVisual(root.transform, ImpactMarkerName, disc, discMaterial, ignoreRaycast);
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, FxPrefabPath);
                Debug.Log($"[Meteor Aim] Created {FxPrefabPath}.");
                return prefab;
            }
            finally
            {
                Object.DestroyImmediate(root);
                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>
        /// Adds MeteorAimIndicator + CommandAura to Summoner, nests MeteorAimFX under the Player root and assigns every
        /// reference that lives inside the prefab. Wave/Enemy managers are scene objects: see AssignSceneReferences.
        /// </summary>
        public static bool WirePlayerPrefab()
        {
            GameObject fxPrefab = CreateFxPrefab();
            var meteorSettings = AssetDatabase.LoadAssetAtPath<MeteorSettings>(MeteorSettingsPath);
            var auraSettings = AssetDatabase.LoadAssetAtPath<CommandAuraSettings>(AuraSettingsPath);
            if (fxPrefab == null || meteorSettings == null || auraSettings == null)
            {
                Debug.LogError($"[Meteor Aim] Missing FX prefab, {MeteorSettingsPath} or {AuraSettingsPath}; Player.prefab not changed.");
                return false;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                Transform summoner = FindChild(root.transform, SummonerName);
                Transform mainCamera = FindChild(root.transform, MainCameraName);
                var router = root.GetComponentInChildren<SummonerInputRouter>(true);
                if (summoner == null || mainCamera == null || router == null)
                {
                    Debug.LogError($"[Meteor Aim] Player.prefab needs '{SummonerName}', '{MainCameraName}' and a {nameof(SummonerInputRouter)}; not changed.");
                    return false;
                }

                Transform fx = root.transform.Find(fxPrefab.name);
                if (fx == null)
                {
                    fx = ((GameObject)PrefabUtility.InstantiatePrefab(fxPrefab, root.transform)).transform;
                    fx.localPosition = Vector3.zero;
                }

                SetReference(router, "_pointerCamera", mainCamera.GetComponent<Camera>());

                MeteorAimIndicator indicator = GetOrAdd<MeteorAimIndicator>(summoner.gameObject);
                var so = new SerializedObject(indicator);
                so.FindProperty("_settings").objectReferenceValue = meteorSettings;
                so.FindProperty("_summoner").objectReferenceValue = summoner;
                so.FindProperty("_aimRaySourceComponent").objectReferenceValue = router;
                so.FindProperty("_groundLayers").intValue = LayerMask.GetMask(GroundLayerNames);
                so.FindProperty("_rangeIndicator").objectReferenceValue = fx.Find(RangeRingName);
                so.FindProperty("_blastPreview").objectReferenceValue = fx.Find(BlastPreviewName).GetComponent<Renderer>();
                so.FindProperty("_impactMarker").objectReferenceValue = fx.Find(ImpactMarkerName).GetComponent<Renderer>();
                so.ApplyModifiedPropertiesWithoutUndo();

                SetReference(GetOrAdd<CommandAura>(summoner.gameObject), "_settings", auraSettings);

                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
                Debug.Log("[Meteor Aim] Player.prefab wired. Run 'Assign Scene References' in Game and Sandbox/C_Main.");
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>Sets _waveManager/_enemyManager on every MeteorAimIndicator in <paramref name="scene"/> as instance overrides.</summary>
        public static bool AssignSceneReferences(Scene scene)
        {
            WaveManager waveManager = FindInScene<WaveManager>(scene);
            EnemyManager enemyManager = FindInScene<EnemyManager>(scene);
            if (waveManager == null || enemyManager == null)
            {
                Debug.LogError($"[Meteor Aim] '{scene.name}' needs one WaveManager and one EnemyManager; nothing assigned.");
                return false;
            }

            int count = 0;
            foreach (GameObject go in scene.GetRootGameObjects())
            {
                foreach (MeteorAimIndicator indicator in go.GetComponentsInChildren<MeteorAimIndicator>(true))
                {
                    var so = new SerializedObject(indicator);
                    so.FindProperty("_waveManager").objectReferenceValue = waveManager;
                    so.FindProperty("_enemyManager").objectReferenceValue = enemyManager;
                    so.ApplyModifiedProperties();
                    PrefabUtility.RecordPrefabInstancePropertyModifications(indicator);
                    count++;
                }
            }

            if (count == 0)
            {
                Debug.LogError($"[Meteor Aim] No MeteorAimIndicator in '{scene.name}'. Run 'Wire Player Prefab' first.");
                return false;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[Meteor Aim] Assigned WaveManager/EnemyManager on {count} indicator(s) in '{scene.name}'. Save the scene.");
            return true;
        }

        private static void AddVisual(Transform parent, string objectName, Mesh mesh, Material material, int layer)
        {
            var go = new GameObject(objectName) { layer = layer };
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        /// <summary>Flat disc (inner radius 0) or ring on XZ, facing up, diameter 1.</summary>
        private static Mesh BuildCircleMesh(string meshName, float innerRadius)
        {
            bool isRing = innerRadius > 0f;
            int vertexCount = isRing ? CircleSegments * 2 : CircleSegments + 1;
            var vertices = new Vector3[vertexCount];
            var uvs = new Vector2[vertexCount];
            var normals = new Vector3[vertexCount];
            var triangles = new int[CircleSegments * (isRing ? 6 : 3)];

            for (int i = 0; i < vertexCount; i++) normals[i] = Vector3.up;

            for (int i = 0; i < CircleSegments; i++)
            {
                float angle = i * Mathf.PI * 2f / CircleSegments;
                var dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                int next = (i + 1) % CircleSegments;
                if (isRing)
                {
                    vertices[i * 2] = dir * UnitRadius + Vector3.up * GroundLift;
                    vertices[i * 2 + 1] = dir * innerRadius + Vector3.up * GroundLift;
                    uvs[i * 2] = new Vector2((float)i / CircleSegments, 1f);
                    uvs[i * 2 + 1] = new Vector2((float)i / CircleSegments, 0f);
                    int t = i * 6;
                    // Clockwise seen from above so the face points up.
                    triangles[t] = i * 2;
                    triangles[t + 1] = i * 2 + 1;
                    triangles[t + 2] = next * 2;
                    triangles[t + 3] = next * 2;
                    triangles[t + 4] = i * 2 + 1;
                    triangles[t + 5] = next * 2 + 1;
                }
                else
                {
                    vertices[i + 1] = dir * UnitRadius + Vector3.up * GroundLift;
                    uvs[i + 1] = new Vector2(dir.x + 1f, dir.z + 1f) * 0.5f;
                    int t = i * 3;
                    triangles[t] = 0;
                    triangles[t + 1] = next + 1;
                    triangles[t + 2] = i + 1;
                }
            }

            if (!isRing)
            {
                vertices[0] = Vector3.up * GroundLift;
                uvs[0] = new Vector2(0.5f, 0.5f);
            }

            var mesh = new Mesh { name = meshName, vertices = vertices, uv = uvs, normals = normals, triangles = triangles };
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Material BuildTransparentMaterial(string materialName, Color color)
        {
            var material = new Material(Shader.Find(UrpUnlitShader)) { name = materialName };
            material.SetFloat("_Surface", 1f); // Transparent
            material.SetFloat("_Blend", 0f);   // Alpha
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
            material.SetColor("_BaseColor", color);
            return material;
        }

        private static T SaveAsset<T>(T asset, string path) where T : Object
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                Object.DestroyImmediate(asset);
                return existing;
            }

            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component =>
            go.TryGetComponent(out T component) ? component : go.AddComponent<T>();

        private static void SetReference(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Transform FindChild(Transform root, string childName)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == childName) return child;
            }

            return null;
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (GameObject go in scene.GetRootGameObjects())
            {
                T found = go.GetComponentInChildren<T>(true);
                if (found != null) return found;
            }

            return null;
        }
    }
}
