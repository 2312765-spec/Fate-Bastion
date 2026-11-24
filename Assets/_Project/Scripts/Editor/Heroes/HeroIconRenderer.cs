using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using FateBastion.Editor.Import;
using FateBastion.Heroes;

namespace FateBastion.Editor.Heroes
{
    /// <summary>
    /// Tools > Render Hero Icons: renders each HeroData.prefab on a transparent background to Art/Icons/&lt;id&gt;.png
    /// and assigns the sprite to HeroData.icon. Re-running overwrites the same file (S0b).
    /// </summary>
    public static class HeroIconRenderer
    {
        public const string IconFolder = "Assets/_Project/Art/Icons";
        public const string SettingsFolder = "Assets/_Project/Data/Settings";
        public const string SettingsAssetName = "HeroIconSettings";

        /// <summary>One fixed path per hero id, so a second run overwrites instead of adding a file.</summary>
        public static string GetIconAssetPath(string heroId) => IconFolder + "/" + heroId + ".png";

        [MenuItem("Tools/Render Hero Icons")]
        public static void RenderAll()
        {
            HeroIconSettings settings = AssetFolderUtility.LoadOrCreate<HeroIconSettings>(SettingsFolder, SettingsAssetName, out _);
            List<HeroData> heroes = LoadHeroes();
            AssetFolderUtility.EnsureFolder(IconFolder);

            // Our own preview scene instead of PreviewRenderUtility: PreviewRenderUtility.Render needs BeginPreview,
            // and a half torn-down preview scene crashes the editor's GI tick. Closing in finally always cleans up.
            Scene scene = EditorSceneManager.NewPreviewScene();
            Camera camera = CreateRig(scene, settings);
            RenderTexture rt = RenderTexture.GetTemporary(new RenderTextureDescriptor(settings.size, settings.size,
                RenderTextureFormat.ARGB32, 24) { sRGB = true });
            var pixels = new Texture2D(settings.size, settings.size, TextureFormat.RGBA32, false);
            var rendered = new List<HeroData>();
            try
            {
                camera.targetTexture = rt;
                for (int i = 0; i < heroes.Count; i++)
                {
                    HeroData hero = heroes[i];
                    EditorUtility.DisplayProgressBar("Render Hero Icons", hero.id, (float)i / heroes.Count);
                    if (hero.prefab == null)
                    {
                        Debug.LogWarning($"[Render Hero Icons] {hero.id}: HeroData.prefab is not assigned, skipped.", hero);
                        continue;
                    }

                    File.WriteAllBytes(GetIconAssetPath(hero.id), RenderPng(scene, camera, rt, pixels, hero.prefab, settings));
                    rendered.Add(hero);
                }
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.ReleaseTemporary(rt);
                Object.DestroyImmediate(pixels);
                EditorSceneManager.ClosePreviewScene(scene);
                EditorUtility.ClearProgressBar();
            }

            // Import after the preview scene is closed so no asset refresh runs while it is open.
            for (int i = 0; i < rendered.Count; i++)
            {
                HeroData hero = rendered[i];
                hero.icon = ImportAsSprite(GetIconAssetPath(hero.id), settings.size);
                EditorUtility.SetDirty(hero);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Render Hero Icons] Rendered {rendered.Count}/{heroes.Count} icons to {IconFolder}.");
        }

        private static List<HeroData> LoadHeroes()
        {
            var heroes = new List<HeroData>();
            string[] guids = AssetDatabase.FindAssets("t:" + nameof(HeroData), new[] { ValidateHeroPrefabsWindow.HeroDataFolder });
            for (int i = 0; i < guids.Length; i++)
            {
                var hero = AssetDatabase.LoadAssetAtPath<HeroData>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (hero != null && !string.IsNullOrEmpty(hero.id))
                {
                    heroes.Add(hero);
                }
            }

            return heroes;
        }

        /// <summary>Camera plus key and fill lights, all inside the preview scene so nothing touches the open scenes.</summary>
        private static Camera CreateRig(Scene scene, HeroIconSettings settings)
        {
            var cameraGo = new GameObject("IconCamera");
            SceneManager.MoveGameObjectToScene(cameraGo, scene);
            var camera = cameraGo.AddComponent<Camera>();
            camera.enabled = false;
            camera.scene = scene;
            camera.cameraType = CameraType.Preview;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            camera.fieldOfView = settings.fieldOfView;
            camera.allowHDR = false;
            camera.allowMSAA = false;

            CreateLight(scene, "KeyLight", settings.keyLightEuler, settings.keyLightIntensity);
            CreateLight(scene, "FillLight", settings.fillLightEuler, settings.fillLightIntensity);
            return camera;
        }

        private static void CreateLight(Scene scene, string name, Vector3 euler, float intensity)
        {
            var go = new GameObject(name);
            SceneManager.MoveGameObjectToScene(go, scene);
            go.transform.rotation = Quaternion.Euler(euler);
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
        }

        // ASSUMPTION: the model is rendered in its default (bind) pose; Spec S0b does not ask for an Idle frame.
        private static byte[] RenderPng(Scene scene, Camera camera, RenderTexture rt, Texture2D pixels, GameObject prefab,
            HeroIconSettings settings)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            RenderTexture previousActive = RenderTexture.active;
            try
            {
                float distance = FrameCamera(camera, instance, settings, out Vector3 target);
                Capture(camera, rt, pixels, settings.size);

                // Mesh bounds are looser than the silhouette (skinned bounds, back corners), so refine from what was
                // actually drawn: centre the opaque pixels and scale them to the margin. Two passes settle within a pixel.
                for (int pass = 0; pass < RefinePasses; pass++)
                {
                    if (!HeroIconFraming.TryGetOpaqueRect(pixels.GetPixels32(), settings.size, OpaqueAlpha, out RectInt opaque))
                        break;

                    float refined = HeroIconFraming.RefineFromOpaqueRect(opaque, settings.size, distance,
                        settings.fieldOfView, settings.distanceMultiplier, out Vector2 shift);
                    target += camera.transform.right * shift.x + camera.transform.up * shift.y;
                    distance = refined;
                    camera.transform.position = target - camera.transform.forward * distance;
                    Capture(camera, rt, pixels, settings.size);
                }

                return pixels.EncodeToPNG();
            }
            finally
            {
                RenderTexture.active = previousActive;
                Object.DestroyImmediate(instance);
            }
        }

        // Technical constants: refine passes after the first render, and the alpha that counts as part of the hero
        // (skips the faint anti-aliased fringe).
        private const int RefinePasses = 2;
        private const byte OpaqueAlpha = 8;

        private static void Capture(Camera camera, RenderTexture rt, Texture2D pixels, int size)
        {
            RenderCamera(camera, rt);
            RenderTexture.active = rt;
            pixels.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            pixels.Apply();
        }

        private static void RenderCamera(Camera camera, RenderTexture rt)
        {
            // URP path: render requests are the supported way to render a camera on demand.
            var request = new RenderPipeline.StandardRequest { destination = rt };
            if (RenderPipeline.SupportsRenderRequest(camera, request))
            {
                RenderPipeline.SubmitRenderRequest(camera, request);
            }
            else
            {
                camera.Render();
            }
        }

        /// <summary>
        /// Places the camera so the model's mesh bounds fill the frame with the configured margin. Particle renderers
        /// (rarity aura, buff icons, level 3 FX) are hidden and left out of the bounds: unsimulated they draw nothing
        /// useful, and the buff icons above the head would shrink every hero in the icon.
        /// </summary>
        private static float FrameCamera(Camera camera, GameObject instance, HeroIconSettings settings, out Vector3 aim)
        {
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
            var bounds = new Bounds(instance.transform.position, Vector3.one);
            bool hasModel = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (!HeroIconFraming.IsModelRenderer(renderers[i]))
                {
                    renderers[i].enabled = false;
                    continue;
                }

                if (hasModel) bounds.Encapsulate(renderers[i].bounds);
                else bounds = renderers[i].bounds;
                hasModel = true;
            }

            if (!hasModel) Debug.LogWarning($"[Render Hero Icons] {instance.name}: no mesh renderer found; framing the prefab root.");

            Vector3 target = new Vector3(bounds.center.x, bounds.min.y + bounds.size.y * settings.lookAtHeight, bounds.center.z);
            // Hero faces +Z, so yaw 0 puts the camera on +Z looking back at its face.
            Quaternion rotation = Quaternion.Euler(settings.pitch, 180f + settings.yaw, 0f);
            float distance = HeroIconFraming.FitDistance(bounds, target, rotation, settings.fieldOfView,
                settings.distanceMultiplier);
            float radius = bounds.extents.magnitude;

            camera.transform.position = target - rotation * Vector3.forward * distance;
            camera.transform.rotation = rotation;
            // Refining may move the camera closer, so the near plane leaves room for the whole model in front.
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = distance + radius * 4f;
            aim = target;
            return distance;
        }

        private static Sprite ImportAsSprite(string path, int size)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = Mathf.NextPowerOfTwo(size);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
