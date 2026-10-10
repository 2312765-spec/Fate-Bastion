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
                FrameCamera(camera, instance, settings);
                RenderCamera(camera, rt);

                RenderTexture.active = rt;
                pixels.ReadPixels(new Rect(0, 0, settings.size, settings.size), 0, 0);
                pixels.Apply();
                return pixels.EncodeToPNG();
            }
            finally
            {
                RenderTexture.active = previousActive;
                Object.DestroyImmediate(instance);
            }
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

        /// <summary>Places the camera so the model's renderer bounds fill the frame with the configured margin.</summary>
        private static void FrameCamera(Camera camera, GameObject instance, HeroIconSettings settings)
        {
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
            var bounds = new Bounds(instance.transform.position, Vector3.one);
            if (renderers.Length > 0)
            {
                bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
            }

            Vector3 target = new Vector3(bounds.center.x, bounds.min.y + bounds.size.y * settings.lookAtHeight, bounds.center.z);
            float radius = bounds.extents.magnitude;
            float distance = radius * settings.distanceMultiplier / Mathf.Sin(settings.fieldOfView * 0.5f * Mathf.Deg2Rad);

            // Hero faces +Z, so yaw 0 puts the camera on +Z looking back at its face.
            Quaternion rotation = Quaternion.Euler(settings.pitch, 180f + settings.yaw, 0f);
            camera.transform.position = target - rotation * Vector3.forward * distance;
            camera.transform.rotation = rotation;
            camera.nearClipPlane = Mathf.Max(0.01f, distance - radius * 2f);
            camera.farClipPlane = distance + radius * 2f;
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
