using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using FateBastion.Audio;
using FateBastion.Editor.Import;
using Object = UnityEngine.Object;

namespace FateBastion.Editor.UI
{
    /// <summary>
    /// Tools > Map > Dress Map_Level1 (C-17): turns the C-06 graybox into a real map with Kenney Nature/Castle models.
    /// Everything it makes lives under Visuals/Dressing and is rebuilt from scratch on each run (same seed = same
    /// layout). Gameplay objects keep their colliders and layers; only their graybox box renderers are switched off.
    /// Trees and big rocks stay outside the walls, and decor inside the play area has no collider, so placement and
    /// the road are untouched.
    /// </summary>
    public static class MapDresser
    {
        public const string MapPath = PostProcessingInstaller.MapPath;
        public const string SettingsFolder = "Assets/_Project/Data/Settings";
        public const string SettingsAssetName = "MapDressingSettings";
        public const string GeneratedFolder = "Assets/_Project/Art/Map/Generated";
        public const string MaterialFolder = "Assets/_Project/Art/Map/Materials";
        public const string NatureFolder = "Assets/ThirdParty/Kenney/NatureKit/";
        public const string CastleFolder = "Assets/ThirdParty/Kenney/CastleKit/";

        public const string DressingName = "Dressing";
        public const string SunName = "Sun";
        private const string VisualsName = "Visuals";
        private const string GroundPath = "Gameplay/Ground";
        private const string RoadPath = "Gameplay/Road";
        private const string ObstaclesPath = "Gameplay/Obstacles";
        private const string CastlePath = "Landmarks/Castle";
        private const string GatePath = "Landmarks/Gate";

        // Technical: the outer ground quad sits just under the play-area ground top (y 0) so the two never z-fight.
        private const float OuterGroundY = -0.03f;
        private const float WaterSurfaceY = 0.06f;

        [MenuItem("Tools/Map/Dress Map_Level1")]
        public static void DressFromMenu() => Dress(MapPath, LoadOrCreateSettings());

        /// <summary>Settings asset, created with the default Kenney model lists on first use.</summary>
        public static MapDressingSettings LoadOrCreateSettings()
        {
            var settings = AssetFolderUtility.LoadOrCreate<MapDressingSettings>(SettingsFolder, SettingsAssetName, out bool created);
            if (created || settings.trees == null || settings.trees.Length == 0)
            {
                FillDefaultModels(settings);
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
            }

            return settings;
        }

        /// <summary>Rebuilds Visuals/Dressing of the map prefab; false if the map or its C-06 hierarchy is missing.</summary>
        public static bool Dress(string mapPath, MapDressingSettings settings)
        {
            if (settings == null || AssetDatabase.LoadAssetAtPath<GameObject>(mapPath) == null)
            {
                Debug.LogError($"[Map] {mapPath} or MapDressingSettings not found.");
                return false;
            }

            AssetFolderUtility.EnsureFolder(GeneratedFolder);
            AssetFolderUtility.EnsureFolder(MaterialFolder);
            Material grass = LoadOrCreateMaterial("M_Map_Grass", settings.grassColor, 0.1f);
            Material dirt = LoadOrCreateMaterial("M_Map_Dirt", settings.dirtColor, 0.05f);
            Material water = LoadOrCreateMaterial("M_Map_Water", settings.waterColor, 0.85f);

            GameObject root = PrefabUtility.LoadPrefabContents(mapPath);
            try
            {
                Transform visuals = root.transform.Find(VisualsName);
                Transform road = root.transform.Find(RoadPath);
                Transform ground = root.transform.Find(GroundPath);
                if (visuals == null || road == null || ground == null)
                {
                    Debug.LogError($"[Map] {mapPath} is missing Visuals, {RoadPath} or {GroundPath} (C-06 hierarchy).");
                    return false;
                }

                Transform old = visuals.Find(DressingName);
                if (old != null) Object.DestroyImmediate(old.gameObject);
                Transform dressing = new GameObject(DressingName).transform;
                dressing.SetParent(visuals, false);

                SetMaterial(ground, grass);
                SetMaterial(road, dirt);
                List<Vector2> roadCorners = RoadCorners(road);
                List<Rect> blockers = Blockers(root.transform, settings.blockerMargin);
                float roadKeepOut = RoadHalfWidth + settings.roadClearance;

                BuildSun(dressing, settings);
                BuildOuterGround(dressing, grass, settings);
                DressCastle(root.transform.Find(CastlePath), dressing);
                DressGate(root.transform.Find(GatePath), dressing);
                DressObstacles(root.transform.Find(ObstaclesPath), dressing, water, settings);

                var rng = new System.Random(settings.seed);
                float play = settings.playHalfSize;
                float wallOut = play + 1.5f;
                var scenery = new Rect(-settings.sceneryHalfSize, -settings.sceneryHalfSize,
                    settings.sceneryHalfSize * 2f, settings.sceneryHalfSize * 2f);
                var playRect = new Rect(-play, -play, play * 2f, play * 2f);

                Transform forest = Group("Forest", dressing, Vector3.zero);
                foreach (Vector2 p in MapDressing.Scatter(settings.seed + 1, scenery, settings.forestAttempts,
                             settings.forestSpacing, p => !MapDressing.InsideSquare(p, wallOut)))
                    Scatter(forest, Pick(settings.trees, rng), p, rng, settings.treeScale, ShadowCastingMode.On);
                foreach (Vector2 p in MapDressing.Scatter(settings.seed + 2, scenery, settings.outerRockAttempts,
                             settings.outerRockSpacing, p => !MapDressing.InsideSquare(p, wallOut + 1f)))
                    Scatter(forest, Pick(settings.outerRocks, rng), p, rng, settings.outerRockScale, ShadowCastingMode.On);

                // Small decor: no shadows, it is under 1 m and there are hundreds of pieces.
                Transform decor = Group("Decor", dressing, Vector3.zero);
                foreach (Vector2 p in MapDressing.Scatter(settings.seed + 3, playRect, settings.decorAttempts,
                             settings.decorSpacing, p => !MapDressing.InsideAny(p, blockers) &&
                                                         MapDressing.DistanceToTriangles(p, roadCorners) > roadKeepOut))
                    Scatter(decor, Pick(settings.decor, rng), p, rng, settings.decorScale, ShadowCastingMode.Off);
                foreach (Vector2 p in MapDressing.Scatter(settings.seed + 4, playRect, settings.pebbleAttempts,
                             settings.pebbleSpacing, p =>
                             {
                                 float d = MapDressing.DistanceToTriangles(p, roadCorners);
                                 return d > RoadHalfWidth + 0.1f && d < roadKeepOut && !MapDressing.InsideAny(p, blockers);
                             }))
                    Scatter(decor, Pick(settings.pebbles, rng), p, rng, settings.pebbleScale, ShadowCastingMode.Off);

                PrefabUtility.SaveAsPrefabAsset(root, mapPath);
                PostProcessingInstaller.EnsureGrading(PostProcessingInstaller.ProfilePath);
                Debug.Log($"[Map] Dressed {mapPath}: forest {forest.childCount} pieces, decor {decor.childCount} pieces.");
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // Road is a 3 m SplineExtrude (C-06); half width is the layout contract, not a balance number.
        private const float RoadHalfWidth = 1.5f;

        private static void BuildSun(Transform dressing, MapDressingSettings settings)
        {
            var sunGo = new GameObject(SunName);
            sunGo.transform.SetParent(dressing, false);
            sunGo.transform.rotation = Quaternion.Euler(settings.sunEuler);
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = settings.sunColor;
            sun.intensity = settings.sunIntensity;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = settings.sunShadowStrength;

            var atmosphere = dressing.gameObject.AddComponent<MapAtmosphere>();
            var data = new SerializedObject(atmosphere);
            data.FindProperty("_sun").objectReferenceValue = sun;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildOuterGround(Transform dressing, Material grass, MapDressingSettings settings)
        {
            GameObject outer = Quad("OuterGround", dressing, grass, ShadowCastingMode.Off);
            float size = settings.sceneryHalfSize * 2f + 40f;
            outer.transform.SetPositionAndRotation(new Vector3(0f, OuterGroundY, 0f), Quaternion.Euler(90f, 0f, 0f));
            outer.transform.localScale = new Vector3(size, size, 1f);
        }

        /// <summary>Keep with four hexagon corner towers and walls, on the Castle box footprint (6 x 6 m).</summary>
        private static void DressCastle(Transform box, Transform dressing)
        {
            if (box == null) return;
            HideBox(box);
            Transform castle = Group("Castle", dressing, Foot(box));
            float half = box.lossyScale.x * 0.5f - 0.5f;
            const float corner = 2f, keep = 3f;
            for (int i = 0; i < 4; i++)
            {
                var at = new Vector3(i % 2 == 0 ? -half : half, 0f, i < 2 ? -half : half);
                float y = Stack(castle, at, corner, 0f, "tower-hexagon-base", "tower-hexagon-mid", "tower-hexagon-roof");
                if (i == 0) Place(castle, CastleFolder + "flag", at + Vector3.up * y, 0f, corner);
            }

            for (int side = 0; side < 4; side++)
            {
                float yaw = side * 90f;
                Quaternion turn = Quaternion.Euler(0f, yaw, 0f);
                for (int k = -1; k <= 1; k += 2)
                    Place(castle, CastleFolder + "wall", turn * new Vector3(k * corner * 0.5f, 0f, half), yaw, corner);
            }

            float top = Stack(castle, Vector3.zero, keep, 0f, "tower-square-base", "tower-square-mid-windows",
                "tower-square-mid", "tower-square-top-roof-high");
            Place(castle, CastleFolder + "flag-banner-long", new Vector3(0f, top * 0.55f, -keep * 0.5f - 0.02f), 90f, keep);
        }

        /// <summary>Two square towers either side of a raised portcullis on the Gate box (opening along its X).</summary>
        private static void DressGate(Transform box, Transform dressing)
        {
            if (box == null) return;
            HideBox(box);
            Transform gate = Group("Gate", dressing, Foot(box));
            gate.rotation = box.rotation;
            float side = box.lossyScale.z * 0.5f + 0.6f;
            const float tower = 2.2f;
            float towerTop = 0f;
            for (int k = -1; k <= 1; k += 2)
                towerTop = Stack(gate, new Vector3(0f, 0f, k * side), tower, 0f, "tower-square-base", "tower-square-mid",
                    "tower-square-top-roof-high");

            // Opening as tall as the Gate box; the portcullis hangs raised in its top part so the gate reads as open
            // (enemies come out of it). The metal-gate model is 0.07 x 0.73 x 0.70 m with its width along Z.
            float span = box.lossyScale.z + 0.4f;
            float opening = box.lossyScale.y;
            float raised = opening * 0.45f;
            GameObject portcullis = Place(gate, CastleFolder + "metal-gate", new Vector3(0f, opening - raised, 0f), 0f, 1f);
            if (portcullis != null)
            {
                Bounds b = ModelBounds(portcullis);
                portcullis.transform.localScale = new Vector3(3f, raised / Mathf.Max(b.size.y, 0.01f),
                    span / Mathf.Max(b.size.z, 0.01f));
            }

            // Lintel over the opening: a wall piece turned to run along Z and stretched across the span.
            GameObject lintel = Place(gate, CastleFolder + "wall", new Vector3(0f, opening, 0f), 90f, 1f);
            if (lintel != null) lintel.transform.localScale = new Vector3(span, 1.2f, 1.2f);
            Place(gate, CastleFolder + "flag", new Vector3(0f, towerTop, side), 0f, tower);
        }

        /// <summary>Kenney rocks over each Rock_* box, a water surface and rim over the Water box.</summary>
        private static void DressObstacles(Transform obstacles, Transform dressing, Material water, MapDressingSettings settings)
        {
            if (obstacles == null) return;
            Transform group = Group("Obstacles", dressing, Vector3.zero);
            var rng = new System.Random(settings.seed + 5);
            foreach (Transform box in obstacles)
            {
                if (box.name.StartsWith("Rock"))
                {
                    HideBox(box);
                    float size = Mathf.Max(box.lossyScale.x, box.lossyScale.z);
                    GameObject rock = PlaceModel(group, Pick(settings.obstacleRocks, rng), Foot(box),
                        (float)rng.NextDouble() * 360f, 1f);
                    if (rock == null) continue;
                    Bounds b = ModelBounds(rock);
                    // Cover the collider box so heroes are never placed "inside" a visibly empty spot.
                    rock.transform.localScale = Vector3.one * (size * 1.15f / Mathf.Max(Mathf.Max(b.size.x, b.size.z), 0.01f));
                }
                else if (box.name.StartsWith("Water"))
                {
                    HideBox(box);
                    GameObject surface = Quad("WaterSurface", group, water, ShadowCastingMode.Off);
                    surface.transform.SetPositionAndRotation(new Vector3(box.position.x, WaterSurfaceY, box.position.z),
                        box.rotation * Quaternion.Euler(90f, 0f, 0f));
                    surface.transform.localScale = new Vector3(box.lossyScale.x, box.lossyScale.z, 1f);
                    DressWaterRim(group, box, settings, rng);
                }
            }
        }

        private static void DressWaterRim(Transform group, Transform box, MapDressingSettings settings, System.Random rng)
        {
            float hx = box.lossyScale.x * 0.5f, hz = box.lossyScale.z * 0.5f;
            int count = Mathf.RoundToInt((hx + hz) * 2f);
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / count * 4f;
                Vector3 local = t < 1f ? new Vector3(Mathf.Lerp(-hx, hx, t), 0f, -hz)
                    : t < 2f ? new Vector3(hx, 0f, Mathf.Lerp(-hz, hz, t - 1f))
                    : t < 3f ? new Vector3(Mathf.Lerp(hx, -hx, t - 2f), 0f, hz)
                    : new Vector3(-hx, 0f, Mathf.Lerp(hz, -hz, t - 3f));
                Vector3 at = box.position + box.rotation * local;
                at.y = 0f;
                PlaceModel(group, Pick(settings.waterEdge, rng), at, (float)rng.NextDouble() * 360f,
                    Mathf.Lerp(settings.pebbleScale.x, settings.pebbleScale.y, (float)rng.NextDouble()));
            }
        }

        private static float Stack(Transform parent, Vector3 at, float scale, float yaw, params string[] pieces)
        {
            float y = 0f;
            for (int i = 0; i < pieces.Length; i++)
            {
                GameObject piece = Place(parent, CastleFolder + pieces[i], at + Vector3.up * y, yaw, scale);
                if (piece != null) y += ModelBounds(piece).size.y;
            }

            return y;
        }

        private static GameObject Place(Transform parent, string modelPath, Vector3 localPosition, float yaw, float scale)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath + ".fbx");
            if (model == null)
            {
                Debug.LogWarning($"[Map] Model {modelPath}.fbx not found; skipped.");
                return null;
            }

            return PlaceModel(parent, model, parent.TransformPoint(localPosition), parent.eulerAngles.y + yaw, scale);
        }

        private static GameObject PlaceModel(Transform parent, GameObject model, Vector3 worldPosition, float yaw, float scale)
        {
            if (model == null) return null;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, parent);
            instance.transform.SetPositionAndRotation(worldPosition, Quaternion.Euler(0f, yaw, 0f));
            instance.transform.localScale = Vector3.one * scale;
            GameObjectUtility.SetStaticEditorFlags(instance, StaticEditorFlags.BatchingStatic);
            return instance;
        }

        private static Bounds ModelBounds(GameObject instance)
        {
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(instance.transform.position, Vector3.zero);
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            return b;
        }

        private static Transform Group(string name, Transform parent, Vector3 position)
        {
            Transform t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.position = position;
            return t;
        }

        private static GameObject Quad(string name, Transform parent, Material material, ShadowCastingMode shadows)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = shadows;
            return go;
        }

        /// <summary>Centre of the box's footprint on the ground (y 0).</summary>
        private static Vector3 Foot(Transform box) => new Vector3(box.position.x, 0f, box.position.z);

        private static void HideBox(Transform box)
        {
            if (box.TryGetComponent(out MeshRenderer renderer)) renderer.enabled = false;
        }

        private static void SetMaterial(Transform target, Material material)
        {
            if (target.TryGetComponent(out MeshRenderer renderer)) renderer.sharedMaterial = material;
        }

        private static List<Vector2> RoadCorners(Transform road)
        {
            var corners = new List<Vector2>();
            Mesh mesh = road.TryGetComponent(out MeshFilter filter) ? filter.sharedMesh : null;
            if (mesh == null)
            {
                Debug.LogWarning("[Map] Road has no mesh; decor is not kept off the road.");
                return corners;
            }

            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i++)
            {
                Vector3 w = road.TransformPoint(vertices[triangles[i]]);
                corners.Add(new Vector2(w.x, w.z));
            }

            return corners;
        }

        /// <summary>XZ rects of every gameplay collider box (obstacles, Gate, Castle), grown by the margin.</summary>
        private static List<Rect> Blockers(Transform root, float margin)
        {
            var rects = new List<Rect>();
            foreach (string path in new[] { ObstaclesPath, "Landmarks" })
            {
                Transform group = root.Find(path);
                if (group == null) continue;
                foreach (BoxCollider box in group.GetComponentsInChildren<BoxCollider>())
                {
                    Bounds b = box.bounds;
                    rects.Add(Rect.MinMaxRect(b.min.x - margin, b.min.z - margin, b.max.x + margin, b.max.z + margin));
                }
            }

            return rects;
        }

        /// <summary>
        /// One static prefab instance per piece: Unity static-batches them at build and on Play, so the map keeps few
        /// draw calls without storing a baked mesh (a combined mesh is ~16 MB of text with Force Text serialization).
        /// </summary>
        private static void Scatter(Transform parent, GameObject model, Vector2 p, System.Random rng, Vector2 scaleRange,
            ShadowCastingMode shadows)
        {
            float scale = Mathf.Lerp(scaleRange.x, scaleRange.y, (float)rng.NextDouble());
            GameObject piece = PlaceModel(parent, model, new Vector3(p.x, 0f, p.y), (float)rng.NextDouble() * 360f, scale);
            if (piece == null || shadows == ShadowCastingMode.On) return;
            foreach (Renderer r in piece.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = shadows;
        }

        private static GameObject Pick(GameObject[] models, System.Random rng)
        {
            return models == null || models.Length == 0 ? null : models[rng.Next(models.Length)];
        }

        private static Material LoadOrCreateMaterial(string name, Color color, float smoothness)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void FillDefaultModels(MapDressingSettings s)
        {
            s.trees = Load(NatureFolder, "tree_default", "tree_oak", "tree_fat", "tree_detailed", "tree_tall", "tree_cone",
                "tree_simple", "tree_pineDefaultA", "tree_pineDefaultB", "tree_pineRoundA", "tree_pineRoundC",
                "tree_pineTallA", "tree_pineTallB");
            s.outerRocks = Load(NatureFolder, "rock_largeA", "rock_largeB", "rock_largeC", "rock_largeD", "rock_largeE",
                "rock_largeF", "rock_tallA", "rock_tallB", "rock_tallC", "log_large");
            s.decor = Load(NatureFolder, "grass", "grass_large", "grass_leafs", "grass_leafsLarge", "grass_leafs",
                "plant_bush", "plant_bushLarge", "plant_bushSmall", "flower_redA", "flower_yellowA", "flower_purpleA",
                "mushroom_redGroup", "mushroom_tanGroup", "stump_round", "log");
            s.pebbles = Load(NatureFolder, "rock_smallA", "rock_smallB", "rock_smallC", "rock_smallD", "rock_smallFlatA",
                "grass_leafs");
            s.obstacleRocks = Load(NatureFolder, "rock_tallA", "rock_tallB", "rock_largeD", "rock_largeF");
            s.waterEdge = Load(NatureFolder, "rock_smallA", "rock_smallFlatA", "lily_large", "lily_small", "grass_leafs");
        }

        private static GameObject[] Load(string folder, params string[] names)
        {
            var models = new List<GameObject>();
            foreach (string n in names)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(folder + n + ".fbx");
                if (model != null) models.Add(model);
                else Debug.LogWarning($"[Map] {folder}{n}.fbx not found.");
            }

            return models.ToArray();
        }

    }
}
