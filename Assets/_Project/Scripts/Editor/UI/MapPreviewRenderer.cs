using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using FateBastion.Audio;

namespace FateBastion.Editor.UI
{
    /// <summary>
    /// Tools > Map > Render Map Preview (C-17): renders Map_Level1 from above and from player height into PNGs, with the
    /// map's own Sun and MapAtmosphere applied, for art review and the report. Uses a throw-away empty scene and never
    /// saves it.
    /// </summary>
    public static class MapPreviewRenderer
    {
        public const string DefaultOutputFolder = "TestResults/MapPreview";
        private const int Width = 1600;
        private const int Height = 900;

        private struct Shot
        {
            public string Name;
            public Vector3 Position;
            public Vector3 LookAt;
            public float Fov;
            public bool NoFog;
        }

        // Views: whole map from high above, then three player-height views (near the summoner spawn, the gate, the castle).
        private static readonly Shot[] Shots =
        {
            new Shot { Name = "aerial", Position = new Vector3(-48f, 52f, -48f), LookAt = new Vector3(-2f, 0f, 2f), Fov = 45f, NoFog = true },
            new Shot { Name = "top", Position = new Vector3(0f, 110f, -0.01f), LookAt = Vector3.zero, Fov = 36f, NoFog = true },
            new Shot { Name = "player_spawn", Position = new Vector3(-2f, 3.2f, -12f), LookAt = new Vector3(8f, 1f, 10f), Fov = 55f },
            new Shot { Name = "player_gate", Position = new Vector3(-12f, 3.2f, 14f), LookAt = new Vector3(-26f, 2f, 24f), Fov = 55f },
            new Shot { Name = "player_castle", Position = new Vector3(4f, 3.2f, -8f), LookAt = new Vector3(-12f, 3f, -19f), Fov = 55f },
        };

        [MenuItem("Tools/Map/Render Map Preview")]
        public static void RenderFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Render(MapDresser.MapPath, DefaultOutputFolder);
            EditorUtility.RevealInFinder(DefaultOutputFolder);
        }

        /// <summary>Batch entry: -executeMethod FateBastion.Editor.UI.MapPreviewRenderer.RenderBatch.</summary>
        public static void RenderBatch() => Render(MapDresser.MapPath, DefaultOutputFolder);

        public static void Render(string mapPath, string outputFolder)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(mapPath);
            if (prefab == null)
            {
                Debug.LogError($"[Map Preview] {mapPath} not found.");
                return;
            }

            Directory.CreateDirectory(outputFolder);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Skybox.mat");
            var map = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            MapAtmosphere atmosphere = map.GetComponentInChildren<MapAtmosphere>();
            if (atmosphere != null) atmosphere.Apply();

            bool fog = RenderSettings.fog;
            var cameraGo = new GameObject("PreviewCamera");
            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.farClipPlane = 400f;
            var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            var pixels = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = rt;
                foreach (Shot shot in Shots)
                {
                    camera.fieldOfView = shot.Fov;
                    // Fog is tuned for the close third-person camera; the overview shots would be washed out.
                    RenderSettings.fog = !shot.NoFog && fog;
                    camera.transform.position = shot.Position;
                    camera.transform.LookAt(shot.LookAt);
                    var request = new RenderPipeline.StandardRequest { destination = rt };
                    if (RenderPipeline.SupportsRenderRequest(camera, request)) RenderPipeline.SubmitRenderRequest(camera, request);
                    else camera.Render();

                    RenderTexture.active = rt;
                    pixels.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                    pixels.Apply();
                    RenderTexture.active = null;
                    File.WriteAllBytes(Path.Combine(outputFolder, "map_" + shot.Name + ".png"), pixels.EncodeToPNG());
                }
            }
            finally
            {
                camera.targetTexture = null;
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(pixels);
            }

            Debug.Log($"[Map Preview] {Shots.Length} images written to {outputFolder}.");
        }
    }
}
