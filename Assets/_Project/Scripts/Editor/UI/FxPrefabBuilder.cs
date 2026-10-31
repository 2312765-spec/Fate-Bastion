using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using FateBastion.Audio;
using FateBastion.Editor.Import;
using FateBastion.Player;

namespace FateBastion.Editor.UI
{
    /// <summary>
    /// Tools > FX (C-09): builds the particle prefabs (hits, deaths, meteor explosion, projectile variants, rarity auras,
    /// Command Aura ring), FxSettings, and wires FxService into _Audio and the ring into Player. Only creates what is
    /// missing, never overwrites, so prefabs tuned by hand in the Editor survive a re-run.
    /// </summary>
    public static partial class FxPrefabBuilder
    {
        public const string PrefabFolder = "Assets/_Project/Prefabs/FX";
        public const string MaterialFolder = "Assets/_Project/Art/Materials/FX";
        public const string TextureFolder = "Assets/_Project/Art/Textures/FX";
        public const string KenneyFolderName = "Kenney";
        public const string ProjectileBasePath = PrefabFolder + "/Projectile_Base.prefab";
        public const string SettingsPath = "Assets/_Project/Data/Settings/FxSettings.asset";
        public const string AudioPrefabPath = "Assets/_Project/Prefabs/Systems/_Audio.prefab";
        public const string PlayerPrefabPath = "Assets/_Project/Prefabs/Player/Player.prefab";
        public const string AuraSettingsPath = "Assets/_Project/Data/Settings/CommandAuraSettings.asset";

        public const string HitFireName = "FX_Hit_Fire";
        public const string HitIceName = "FX_Hit_Ice";
        public const string HitLightningName = "FX_Hit_Lightning";
        public const string EnemyDeathName = "FX_EnemyDeath";
        public const string BossDeathName = "FX_BossDeath";
        public const string MeteorExplosionName = "FX_Meteor_Explosion";
        public const string CommandAuraName = "FX_CommandAura";

        /// <summary>One-shot effects played by FxService, in FxKind order.</summary>
        public static readonly string[] OneShotNames =
        {
            HitFireName, HitIceName, HitLightningName, EnemyDeathName, BossDeathName, MeteorExplosionName
        };

        /// <summary>Variants of Projectile_Base; C-08 assigns one to each ranged hero's Variant (SkillRunner._projectilePrefab).</summary>
        public static readonly string[] ProjectileNames =
        {
            "Projectile_Fire", "Projectile_Ice", "Projectile_Lightning", "Projectile_Arrow"
        };

        /// <summary>Looping rarity auras in Rarity order; C-08 nests one under each hero's HeroVisual.AuraAnchor.</summary>
        public static readonly string[] AuraNames =
        {
            "FX_Aura_Common", "FX_Aura_Rare", "FX_Aura_Epic", "FX_Aura_Legendary"
        };

        private const string ParticleShader = "Universal Render Pipeline/Particles/Unlit";
        private const string ProjectileVisualName = "ProjectileVisual";
        private const string SummonerName = "Summoner";
        private const int GeneratedTextureSize = 128;
        // Flat rings sit this far above the anchor so they do not z-fight with the ground under the hero.
        private const float GroundLift = 0.05f;
        // Max Particle Size is a fraction of the screen (default 0.5): a 12 m ring seen up close hits it and shrinks.
        // Ground rings must keep their world size, so the clamp is lifted to 10 screens.
        public const float FlatMaxParticleSize = 10f;
        // Roughly chest height of a 1.6-2 m enemy model; hits and deaths spawn above the enemy root by this much.
        private const float HitHeight = 0.8f;

        private enum FxTexture { Glow, Spark, Flame, Smoke, Star, Ring, IconDamage, IconAttackSpeed }

        private enum RenderKind { Billboard, Stretched, Flat }

        private enum SizeCurve { Constant, Shrink, Grow, Pulse }

        // Sphere: burst around a point. Box: Line area. ConeForward: opens along +Z. DiscUp: flat circle on the ground.
        private enum ShapeKind { Sphere, Box, ConeForward, DiscUp }

        // Kenney Particle Pack (CC0), "PNG (Transparent)" folder: white shapes on alpha, tinted by start colour.
        // Copy only these files into Art/Textures/FX/Kenney; a missing one falls back to a generated soft texture.
        private static readonly string[] KenneyFiles =
        {
            "circle_05.png", // Glow
            "spark_05.png",  // Spark
            "flame_03.png",  // Flame
            "smoke_04.png",  // Smoke
            "star_06.png",   // Star
            "circle_03.png", // Ring
            null,            // IconDamage: drawn by GenerateIcon
            null             // IconAttackSpeed: drawn by GenerateIcon
        };

        private struct Layer
        {
            public string Name;
            public FxTexture Texture;
            public bool Additive;
            public bool Loop;
            public bool World;
            public float Duration;
            public Vector2 Lifetime;
            public Vector2 Speed;
            public Vector2 Size;
            public Color Color;
            public int Burst;
            public float Rate;
            public float RateOverDistance;
            public float ShapeRadius;
            public float Gravity;
            public RenderKind Render;
            public SizeCurve Curve;
            public int MaxParticles;
            public Vector3 Offset;
            public ShapeKind Shape;
            public Vector3 ShapeScale;
            public float ShapeAngle;
            public Vector3 Velocity;
            public float TrailLifetime;
            public bool FixedRotation;
            public bool NoFade;
        }

        [MenuItem("Tools/FX/1. Create FX Prefabs")]
        public static void CreateFromMenu() => Create(PrefabFolder, MaterialFolder, TextureFolder, ProjectileBasePath);

        [MenuItem("Tools/FX/2. Create FxSettings + Add FxService to _Audio")]
        public static void WireAudioFromMenu()
        {
            FxSettings settings = CreateOrFillSettings(SettingsPath, PrefabFolder);
            if (settings != null) AddServiceToAudioPrefab(AudioPrefabPath, settings);
        }

        [MenuItem("Tools/FX/3. Add Command Aura Ring to Player")]
        public static void AddRingFromMenu() => AddCommandAuraRing(PlayerPrefabPath, PrefabFolder, AuraSettingsPath);

        [MenuItem("Tools/FX/4. Fix Rings Shrinking When Zoomed")]
        public static void FixRingsFromMenu() => FixFlatRingClamp(PrefabFolder);

        [MenuItem("Tools/FX/Run All")]
        public static void RunAll()
        {
            Create(PrefabFolder, MaterialFolder, TextureFolder, ProjectileBasePath);
            WireAudioFromMenu();
            AddRingFromMenu();
        }

        public static string GetPrefabPath(string folder, string prefabName) => folder + "/" + prefabName + ".prefab";

        /// <summary>
        /// Creates every missing FX prefab in <paramref name="prefabFolder"/>; returns how many were created, or -1 when
        /// the URP particle shader is missing. Existing prefabs are left untouched.
        /// </summary>
        public static int Create(string prefabFolder, string materialFolder, string textureFolder, string projectileBasePath)
        {
            if (Shader.Find(ParticleShader) == null)
            {
                Debug.LogError($"[FX] Shader '{ParticleShader}' not found; is URP installed?");
                return -1;
            }

            AssetFolderUtility.EnsureFolder(prefabFolder);
            AssetFolderUtility.EnsureFolder(materialFolder);
            AssetFolderUtility.EnsureFolder(textureFolder + "/" + KenneyFolderName);

            var materials = new MaterialCache(materialFolder, textureFolder);
            int created = 0;
            try
            {
                materials.RefreshExisting();
                created += BuildOneShots(prefabFolder, materials);
                created += BuildProjectiles(prefabFolder, projectileBasePath, materials);
                created += BuildAuras(prefabFolder, materials);
                created += BuildSkillFx(prefabFolder, materials);
                FixFlatRingClamp(prefabFolder);
            }
            finally
            {
                AssetDatabase.SaveAssets();
            }

            if (materials.UsedFallback)
                Debug.LogWarning($"[FX] Some Kenney textures are missing from {textureFolder}/{KenneyFolderName}; generated soft textures were used. See the C-09 setup guide.");
            Debug.Log($"[FX] Created {created} prefab(s) in {prefabFolder}.");
            return created;
        }

        /// <summary>
        /// Lifts Max Particle Size on every ground ring (Horizontal Billboard) of the prefabs in <paramref name="folder"/>,
        /// so prefabs built before the fix stop shrinking when the camera zooms in. Touches only that field; returns how
        /// many prefabs changed.
        /// </summary>
        public static int FixFlatRingClamp(string folder)
        {
            int changed = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null || !NeedsRingFix(prefab)) continue;

                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    foreach (ParticleSystemRenderer renderer in root.GetComponentsInChildren<ParticleSystemRenderer>(true))
                    {
                        if (IsClampedRing(renderer)) renderer.maxParticleSize = FlatMaxParticleSize;
                    }

                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    changed++;
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            if (changed > 0) Debug.Log($"[FX] Ground rings no longer shrink on zoom in {changed} prefab(s).");
            return changed;
        }

        private static bool NeedsRingFix(GameObject prefab)
        {
            foreach (ParticleSystemRenderer renderer in prefab.GetComponentsInChildren<ParticleSystemRenderer>(true))
            {
                if (IsClampedRing(renderer)) return true;
            }

            return false;
        }

        private static bool IsClampedRing(ParticleSystemRenderer renderer) =>
            renderer.renderMode == ParticleSystemRenderMode.HorizontalBillboard
            && renderer.maxParticleSize < FlatMaxParticleSize;

        /// <summary>Loads or creates FxSettings and fills every empty prefab slot from <paramref name="prefabFolder"/>.</summary>
        public static FxSettings CreateOrFillSettings(string settingsPath, string prefabFolder)
        {
            var settings = AssetDatabase.LoadAssetAtPath<FxSettings>(settingsPath);
            if (settings == null)
            {
                AssetFolderUtility.EnsureFolder(Path.GetDirectoryName(settingsPath)?.Replace('\\', '/'));
                settings = ScriptableObject.CreateInstance<FxSettings>();
                AssetDatabase.CreateAsset(settings, settingsPath);
            }

            for (int kind = 0; kind < OneShotNames.Length; kind++)
            {
                FxEntry entry = settings.Get((FxKind)kind);
                if (entry.prefab != null) continue;

                var prefab = AssetDatabase.LoadAssetAtPath<ParticleSystem>(GetPrefabPath(prefabFolder, OneShotNames[kind]));
                if (prefab == null) Debug.LogWarning($"[FX] {OneShotNames[kind]} not found; run 'Create FX Prefabs' first.");
                entry.prefab = prefab;
            }

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            return settings;
        }

        /// <summary>Adds FxService to the _Audio prefab (once) and assigns <paramref name="settings"/>.</summary>
        public static bool AddServiceToAudioPrefab(string audioPrefabPath, FxSettings settings)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(audioPrefabPath) == null)
            {
                Debug.LogError($"[FX] {audioPrefabPath} not found; FxService not added.");
                return false;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(audioPrefabPath);
            try
            {
                FxService service = root.TryGetComponent(out FxService existing) ? existing : root.AddComponent<FxService>();
                var so = new SerializedObject(service);
                so.FindProperty("_settings").objectReferenceValue = settings;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, audioPrefabPath);
                Debug.Log($"[FX] FxService on {audioPrefabPath} uses {AssetDatabase.GetAssetPath(settings)}.");
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>Nests FX_CommandAura under Summoner in Player.prefab, scaled to the aura radius (ring is built for 1 m).</summary>
        public static bool AddCommandAuraRing(string playerPrefabPath, string prefabFolder, string auraSettingsPath)
        {
            var ring = AssetDatabase.LoadAssetAtPath<GameObject>(GetPrefabPath(prefabFolder, CommandAuraName));
            var auraSettings = AssetDatabase.LoadAssetAtPath<CommandAuraSettings>(auraSettingsPath);
            if (ring == null || auraSettings == null)
            {
                Debug.LogError($"[FX] Missing {CommandAuraName} or {auraSettingsPath}; Player.prefab not changed.");
                return false;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(playerPrefabPath);
            try
            {
                Transform summoner = FindChild(root.transform, SummonerName);
                if (summoner == null)
                {
                    Debug.LogError($"[FX] Player.prefab has no '{SummonerName}'; ring not added.");
                    return false;
                }

                Transform instance = summoner.Find(CommandAuraName);
                if (instance == null)
                {
                    instance = ((GameObject)PrefabUtility.InstantiatePrefab(ring, summoner)).transform;
                    instance.localPosition = Vector3.zero;
                    instance.localRotation = Quaternion.identity;
                }

                float radius = auraSettings.Radius;
                instance.localScale = new Vector3(radius, radius, radius);
                PrefabUtility.SaveAsPrefabAsset(root, playerPrefabPath);
                Debug.Log($"[FX] {CommandAuraName} under Summoner, radius {radius} m. Re-run after changing CommandAuraSettings.radius.");
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static int BuildOneShots(string folder, MaterialCache materials)
        {
            int created = 0;
            created += SaveLayers(folder, HitFireName, materials, HitLayers(new Color(1f, 0.55f, 0.15f),
                new Layer
                {
                    Name = "Sparks", Texture = FxTexture.Spark, Additive = true, Render = RenderKind.Stretched,
                    Lifetime = new Vector2(0.35f, 0.6f), Speed = new Vector2(2.5f, 5f), Size = new Vector2(0.18f, 0.3f),
                    Color = new Color(1f, 0.7f, 0.25f), Burst = 12, ShapeRadius = 0.15f, Gravity = 0.6f,
                    Curve = SizeCurve.Shrink, MaxParticles = 16
                }));
            created += SaveLayers(folder, HitIceName, materials, HitLayers(new Color(0.55f, 0.85f, 1f),
                new Layer
                {
                    Name = "Shards", Texture = FxTexture.Star, Additive = true,
                    Lifetime = new Vector2(0.45f, 0.7f), Speed = new Vector2(1.5f, 3f), Size = new Vector2(0.3f, 0.5f),
                    Color = new Color(0.7f, 0.95f, 1f), Burst = 8, ShapeRadius = 0.15f, Gravity = 1f,
                    Curve = SizeCurve.Shrink, MaxParticles = 12
                }));
            created += SaveLayers(folder, HitLightningName, materials, HitLayers(new Color(0.8f, 0.75f, 1f),
                new Layer
                {
                    Name = "Arcs", Texture = FxTexture.Spark, Additive = true, Render = RenderKind.Stretched,
                    Lifetime = new Vector2(0.18f, 0.3f), Speed = new Vector2(5f, 8f), Size = new Vector2(0.12f, 0.2f),
                    Color = new Color(0.85f, 0.8f, 1f), Burst = 10, ShapeRadius = 0.1f,
                    Curve = SizeCurve.Shrink, MaxParticles = 14
                }));

            created += SaveLayers(folder, EnemyDeathName, materials, new[]
            {
                new Layer
                {
                    Name = EnemyDeathName, Texture = FxTexture.Smoke, Lifetime = new Vector2(0.6f, 0.9f),
                    Speed = new Vector2(0.6f, 1.4f), Size = new Vector2(0.5f, 0.9f), Color = new Color(0.75f, 0.72f, 0.8f, 0.7f),
                    Burst = 10, ShapeRadius = 0.3f, Gravity = -0.1f, Curve = SizeCurve.Grow, MaxParticles = 12,
                    Offset = new Vector3(0f, 0.5f, 0f)
                },
                Flash("Flash", Color.white, 0.9f, 0.15f, new Vector3(0f, 0.5f, 0f))
            });

            created += SaveLayers(folder, BossDeathName, materials, new[]
            {
                new Layer
                {
                    Name = BossDeathName, Texture = FxTexture.Smoke, Lifetime = new Vector2(1f, 1.5f),
                    Speed = new Vector2(1f, 2.5f), Size = new Vector2(1.2f, 2.2f), Color = new Color(0.6f, 0.55f, 0.65f, 0.8f),
                    Burst = 30, ShapeRadius = 0.8f, Gravity = -0.1f, Curve = SizeCurve.Grow, MaxParticles = 32,
                    Offset = new Vector3(0f, 1f, 0f)
                },
                Flash("Flash", new Color(1f, 0.9f, 0.6f), 4f, 0.25f, new Vector3(0f, 1f, 0f)),
                new Layer
                {
                    Name = "Sparks", Texture = FxTexture.Spark, Additive = true, Render = RenderKind.Stretched,
                    Lifetime = new Vector2(0.5f, 0.9f), Speed = new Vector2(3f, 7f), Size = new Vector2(0.1f, 0.2f),
                    Color = new Color(1f, 0.8f, 0.3f), Burst = 30, ShapeRadius = 0.5f, Gravity = 1f,
                    Curve = SizeCurve.Shrink, MaxParticles = 32, Offset = new Vector3(0f, 1f, 0f)
                },
                Shockwave("Shockwave", new Color(1f, 0.85f, 0.5f, 0.7f), 6f, 0.5f)
            });

            // Built for a 1 m blast radius; FxService scales it by the meteor radius (4 m).
            created += SaveLayers(folder, MeteorExplosionName, materials, new[]
            {
                new Layer
                {
                    Name = MeteorExplosionName, Texture = FxTexture.Flame, Additive = true,
                    Lifetime = new Vector2(0.4f, 0.7f), Speed = new Vector2(0.8f, 2f), Size = new Vector2(0.7f, 1.2f),
                    Color = new Color(1f, 0.5f, 0.15f), Burst = 25, ShapeRadius = 0.4f, Curve = SizeCurve.Shrink,
                    MaxParticles = 30
                },
                Flash("Flash", new Color(1f, 0.8f, 0.5f), 2f, 0.2f, Vector3.zero),
                new Layer
                {
                    Name = "Smoke", Texture = FxTexture.Smoke, Lifetime = new Vector2(1.2f, 1.8f),
                    Speed = new Vector2(0.4f, 1f), Size = new Vector2(0.8f, 1.5f), Color = new Color(0.3f, 0.27f, 0.25f, 0.8f),
                    Burst = 15, ShapeRadius = 0.6f, Gravity = -0.05f, Curve = SizeCurve.Grow, MaxParticles = 20
                },
                new Layer
                {
                    Name = "Sparks", Texture = FxTexture.Spark, Additive = true, Render = RenderKind.Stretched,
                    Lifetime = new Vector2(0.5f, 0.9f), Speed = new Vector2(3f, 6f), Size = new Vector2(0.06f, 0.12f),
                    Color = new Color(1f, 0.75f, 0.3f), Burst = 30, ShapeRadius = 0.3f, Gravity = 1.2f,
                    Curve = SizeCurve.Shrink, MaxParticles = 32
                },
                Shockwave("Shockwave", new Color(1f, 0.6f, 0.25f, 0.8f), 2f, 0.4f)
            });

            return created;
        }

        private static int BuildProjectiles(string folder, string projectileBasePath, MaterialCache materials)
        {
            var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(projectileBasePath);
            if (basePrefab == null)
            {
                Debug.LogError($"[FX] {projectileBasePath} not found; projectile variants skipped.");
                return 0;
            }

            Color[] colors =
            {
                new Color(1f, 0.5f, 0.15f),   // Fire
                new Color(0.5f, 0.85f, 1f),   // Ice
                new Color(0.75f, 0.7f, 1f),   // Lightning
                new Color(1f, 0.95f, 0.8f)    // Arrow: thin pale streak
            };
            float[] headSizes = { 0.45f, 0.4f, 0.4f, 0.2f };

            int created = 0;
            // Preview scene: building the variants never dirties the scene the user has open.
            Scene preview = EditorSceneManager.NewPreviewScene();
            try
            {
                for (int i = 0; i < ProjectileNames.Length; i++)
                {
                    string path = GetPrefabPath(folder, ProjectileNames[i]);
                    if (Exists(path)) continue;

                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab, preview);
                    instance.name = ProjectileNames[i];
                    Transform sphere = instance.transform.Find(ProjectileVisualName);
                    if (sphere != null) sphere.gameObject.SetActive(false);

                    AddLayer(instance.transform, new Layer
                    {
                        Name = "Head", Texture = FxTexture.Glow, Additive = true, Loop = true, Duration = 1f,
                        Lifetime = new Vector2(0.1f, 0.15f), Size = new Vector2(headSizes[i], headSizes[i]),
                        Color = colors[i], Rate = 30f, MaxParticles = 10
                    }, materials);
                    AddLayer(instance.transform, new Layer
                    {
                        Name = "Trail", Texture = FxTexture.Glow, Additive = true, Loop = true, World = true, Duration = 1f,
                        Lifetime = new Vector2(0.2f, 0.3f), Size = new Vector2(headSizes[i] * 0.6f, headSizes[i] * 0.6f),
                        Color = colors[i], RateOverDistance = 25f, Curve = SizeCurve.Shrink, MaxParticles = 40
                    }, materials);
                    instance.AddComponent<ParticleRestartOnEnable>();

                    // Saving a connected instance creates a Prefab Variant: Projectile settings still come from the base.
                    PrefabUtility.SaveAsPrefabAsset(instance, path);
                    UnityEngine.Object.DestroyImmediate(instance);
                    created++;
                }
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(preview);
            }

            return created;
        }

        private static int BuildAuras(string folder, MaterialCache materials)
        {
            Color[] colors =
            {
                new Color(0.85f, 0.85f, 0.85f, 0.35f), // Common
                new Color(0.3f, 0.6f, 1f, 0.55f),      // Rare
                new Color(0.7f, 0.35f, 1f, 0.6f),      // Epic
                new Color(1f, 0.8f, 0.25f, 0.7f)       // Legendary
            };
            float[] moteRates = { 0f, 3f, 6f, 10f };
            float[] ringSizes = { 1.6f, 1.6f, 1.8f, 2f };

            int created = 0;
            for (int i = 0; i < AuraNames.Length; i++)
            {
                var layers = new List<Layer>
                {
                    new Layer
                    {
                        Name = AuraNames[i], Texture = FxTexture.Ring, Additive = true, Loop = true, Duration = 2.5f,
                        Lifetime = new Vector2(2.5f, 2.5f), Size = new Vector2(ringSizes[i], ringSizes[i]),
                        Color = colors[i], Rate = 0.8f, Render = RenderKind.Flat, MaxParticles = 4,
                        Offset = new Vector3(0f, GroundLift, 0f)
                    }
                };
                if (moteRates[i] > 0f)
                {
                    layers.Add(new Layer
                    {
                        Name = "Motes", Texture = FxTexture.Glow, Additive = true, Loop = true, Duration = 2f,
                        Lifetime = new Vector2(1f, 1.6f), Speed = new Vector2(0.05f, 0.15f), Size = new Vector2(0.08f, 0.15f),
                        Color = colors[i], Rate = moteRates[i], ShapeRadius = ringSizes[i] * 0.4f, Gravity = -0.2f,
                        Curve = SizeCurve.Shrink, MaxParticles = 20
                    });
                }

                created += SaveLayers(folder, AuraNames[i], materials, layers.ToArray());
            }

            // Built for a 1 m radius (2 m across); AddCommandAuraRing scales it by CommandAuraSettings.radius.
            created += SaveLayers(folder, CommandAuraName, materials, new[]
            {
                new Layer
                {
                    Name = CommandAuraName, Texture = FxTexture.Ring, Additive = true, Loop = true, Duration = 3f,
                    Lifetime = new Vector2(3f, 3f), Size = new Vector2(2f, 2f), Color = new Color(1f, 0.85f, 0.35f, 0.35f),
                    Rate = 0.5f, Render = RenderKind.Flat, MaxParticles = 3,
                    // Scaled with the ring (×6), so the lift stays a few centimetres.
                    Offset = new Vector3(0f, GroundLift * 0.5f, 0f)
                }
            });

            return created;
        }

        /// <summary>
        /// HitPoint is the enemy root (on the ground: SkillRunner and Projectile aim at enemy.transform), so hit layers
        /// are lifted to body height; otherwise half the flash is under the ground and the sparks fly into it.
        /// </summary>
        private static Layer[] HitLayers(Color flashColor, Layer burst)
        {
            var bodyHeight = new Vector3(0f, HitHeight, 0f);
            burst.Offset = bodyHeight;
            return new[] { Flash(null, flashColor, 1.4f, 0.2f, bodyHeight), burst };
        }

        private static Layer Flash(string layerName, Color color, float size, float lifetime, Vector3 offset) => new Layer
        {
            Name = layerName, Texture = FxTexture.Glow, Additive = true, Lifetime = new Vector2(lifetime, lifetime),
            Size = new Vector2(size, size), Color = color, Burst = 1, Curve = SizeCurve.Shrink, MaxParticles = 1,
            Offset = offset
        };

        private static Layer Shockwave(string layerName, Color color, float size, float lifetime) => new Layer
        {
            Name = layerName, Texture = FxTexture.Ring, Additive = true, Lifetime = new Vector2(lifetime, lifetime),
            Size = new Vector2(size, size), Color = color, Burst = 1, Render = RenderKind.Flat, Curve = SizeCurve.Grow,
            MaxParticles = 1, Offset = new Vector3(0f, GroundLift, 0f)
        };

        /// <summary>First layer goes on the root (FxService needs a root ParticleSystem), the rest become children.</summary>
        private static int SaveLayers(string folder, string prefabName, MaterialCache materials, Layer[] layers)
        {
            string path = GetPrefabPath(folder, prefabName);
            if (Exists(path)) return 0;

            var root = new GameObject(prefabName);
            try
            {
                ConfigureLayer(root, layers[0], materials, isRoot: true);
                for (int i = 1; i < layers.Length; i++) AddLayer(root.transform, layers[i], materials);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                return 1;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void AddLayer(Transform parent, Layer layer, MaterialCache materials)
        {
            var go = new GameObject(layer.Name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = layer.Offset;
            ConfigureLayer(go, layer, materials, isRoot: false);
        }

        /// <param name="isRoot">The root transform is placed by FxService, so its offset moves the emitter shape instead.</param>
        private static void ConfigureLayer(GameObject go, Layer layer, MaterialCache materials, bool isRoot)
        {
            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = system.main;
            main.duration = Mathf.Max(0.05f, layer.Duration > 0f ? layer.Duration : 0.1f);
            main.loop = layer.Loop;
            main.playOnAwake = true;
            main.startDelay = 0f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(layer.Lifetime.x, layer.Lifetime.y);
            main.startSpeed = new ParticleSystem.MinMaxCurve(layer.Speed.x, layer.Speed.y);
            main.startSize = new ParticleSystem.MinMaxCurve(layer.Size.x, layer.Size.y);
            main.startRotation = layer.Render == RenderKind.Billboard && !layer.FixedRotation
                ? new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f)
                : new ParticleSystem.MinMaxCurve(0f);
            main.startColor = layer.Color;
            main.gravityModifier = layer.Gravity;
            main.simulationSpace = layer.World ? ParticleSystemSimulationSpace.World : ParticleSystemSimulationSpace.Local;
            // Hierarchy: the transform scale (meteor radius, aura radius) scales the whole effect.
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = layer.MaxParticles > 0 ? layer.MaxParticles : 16;
            // Scaled time: ×2 plays twice as fast and pause freezes it, like the rest of the battle (CLAUDE.md 7).
            main.useUnscaledTime = false;
            main.stopAction = ParticleSystemStopAction.None;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = layer.Rate;
            emission.rateOverDistance = layer.RateOverDistance;
            emission.SetBursts(layer.Burst > 0
                ? new[] { new ParticleSystem.Burst(0f, (short)layer.Burst) }
                : Array.Empty<ParticleSystem.Burst>());

            ParticleSystem.ShapeModule shape = system.shape;
            bool offsetInShape = isRoot && layer.Offset != Vector3.zero;
            shape.enabled = layer.ShapeRadius > 0f || offsetInShape || layer.Shape != ShapeKind.Sphere;
            shape.radius = Mathf.Max(0.0001f, layer.ShapeRadius);
            shape.position = offsetInShape ? layer.Offset : Vector3.zero;
            switch (layer.Shape)
            {
                case ShapeKind.Box:
                    shape.shapeType = ParticleSystemShapeType.Box;
                    shape.scale = layer.ShapeScale;
                    break;
                case ShapeKind.ConeForward:
                    // Unity's cone already opens along +Z.
                    shape.shapeType = ParticleSystemShapeType.Cone;
                    shape.angle = layer.ShapeAngle;
                    break;
                case ShapeKind.DiscUp:
                    // Circle lies on XY; -90 degrees on X lays it on the ground (XZ).
                    shape.shapeType = ParticleSystemShapeType.Circle;
                    shape.rotation = new Vector3(-90f, 0f, 0f);
                    break;
                default:
                    shape.shapeType = ParticleSystemShapeType.Sphere;
                    break;
            }

            ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
            velocity.enabled = layer.Velocity != Vector3.zero;
            if (velocity.enabled)
            {
                velocity.space = ParticleSystemSimulationSpace.Local;
                velocity.x = new ParticleSystem.MinMaxCurve(layer.Velocity.x);
                velocity.y = new ParticleSystem.MinMaxCurve(layer.Velocity.y);
                velocity.z = new ParticleSystem.MinMaxCurve(layer.Velocity.z);
            }

            ParticleSystem.TrailModule trails = system.trails;
            trails.enabled = layer.TrailLifetime > 0f;
            if (trails.enabled)
            {
                trails.lifetime = new ParticleSystem.MinMaxCurve(layer.TrailLifetime);
                trails.dieWithParticles = true;
            }

            ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
            color.enabled = !layer.NoFade;
            color.color = new ParticleSystem.MinMaxGradient(layer.Loop ? FadeInOut() : FadeOut());

            ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
            size.enabled = layer.Curve != SizeCurve.Constant;
            if (size.enabled)
            {
                AnimationCurve curve;
                switch (layer.Curve)
                {
                    case SizeCurve.Shrink: curve = AnimationCurve.Linear(0f, 1f, 1f, 0f); break;
                    case SizeCurve.Pulse:
                        curve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.5f, 1.15f), new Keyframe(1f, 1f));
                        break;
                    default: curve = AnimationCurve.EaseInOut(0f, 0.3f, 1f, 1f); break;
                }
                size.size = new ParticleSystem.MinMaxCurve(1f, curve);
            }

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = materials.Get(layer.Texture, layer.Additive);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            if (trails.enabled) renderer.trailMaterial = renderer.sharedMaterial;
            switch (layer.Render)
            {
                case RenderKind.Stretched:
                    renderer.renderMode = ParticleSystemRenderMode.Stretch;
                    renderer.lengthScale = 2f;
                    renderer.velocityScale = 0.05f;
                    break;
                case RenderKind.Flat:
                    renderer.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
                    renderer.maxParticleSize = FlatMaxParticleSize;
                    break;
                default:
                    renderer.renderMode = ParticleSystemRenderMode.Billboard;
                    break;
            }
        }

        private static Gradient FadeOut()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.4f), new GradientAlphaKey(0f, 1f) });
            return gradient;
        }

        private static Gradient FadeInOut()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(0f, 1f) });
            return gradient;
        }

        private static bool Exists(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) return false;
            Debug.Log($"[FX] {path} already exists; not overwritten. Delete it first to rebuild.");
            return true;
        }

        private static Transform FindChild(Transform root, string childName)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == childName) return child;
            }

            return null;
        }

        /// <summary>One shared material per texture and blend, so effects batch together (C-15).</summary>
        private sealed class MaterialCache
        {
            private readonly string _materialFolder;
            private readonly string _textureFolder;
            private readonly Dictionary<string, Material> _materials = new Dictionary<string, Material>();

            public bool UsedFallback { get; private set; }

            public MaterialCache(string materialFolder, string textureFolder)
            {
                _materialFolder = materialFolder;
                _textureFolder = textureFolder;
            }

            public Material Get(FxTexture texture, bool additive)
            {
                string materialName = MaterialName(texture, additive);
                if (_materials.TryGetValue(materialName, out Material cached)) return cached;

                string path = _materialFolder + "/" + materialName + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = BuildMaterial(materialName, LoadTexture(texture), additive);
                    AssetDatabase.CreateAsset(material, path);
                }
                else if (IsGenerated(material.GetTexture("_BaseMap")))
                {
                    // Kenney files copied in after a first run replace the fallback; a texture set by hand is kept.
                    Texture2D current = LoadTexture(texture);
                    if (!IsGenerated(current))
                    {
                        material.SetTexture("_BaseMap", current);
                        EditorUtility.SetDirty(material);
                    }
                }

                _materials.Add(materialName, material);
                return material;
            }

            /// <summary>Visits every material already on disk, so a re-run swaps in Kenney textures even when no prefab is rebuilt.</summary>
            public void RefreshExisting()
            {
                foreach (FxTexture texture in Enum.GetValues(typeof(FxTexture)))
                {
                    foreach (bool additive in new[] { true, false })
                    {
                        string path = $"{_materialFolder}/{MaterialName(texture, additive)}.mat";
                        if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) Get(texture, additive);
                    }
                }
            }

            private static string MaterialName(FxTexture texture, bool additive) =>
                $"M_FX_{texture}_{(additive ? "Add" : "Alpha")}";

            private static bool IsGenerated(Texture texture) =>
                texture == null || Path.GetFileName(AssetDatabase.GetAssetPath(texture)).StartsWith("FX_Soft");

            private Texture2D LoadTexture(FxTexture texture)
            {
                if (KenneyFiles[(int)texture] == null)
                    return GenerateIcon($"{_textureFolder}/FX_{texture}.png", texture == FxTexture.IconAttackSpeed);

                string kenneyPath = $"{_textureFolder}/{KenneyFolderName}/{KenneyFiles[(int)texture]}";
                var kenney = AssetDatabase.LoadAssetAtPath<Texture2D>(kenneyPath);
                if (kenney != null)
                {
                    ConfigureImporter(kenneyPath);
                    return kenney;
                }

                UsedFallback = true;
                return texture == FxTexture.Ring
                    ? GenerateTexture(_textureFolder + "/FX_SoftRing.png", d => Mathf.Clamp01(1f - Mathf.Abs(d.magnitude - 0.85f) / 0.12f))
                    : GenerateTexture(_textureFolder + "/FX_SoftCircle.png", d => Mathf.Clamp01(1f - d.magnitude));
            }

            private static Material BuildMaterial(string materialName, Texture2D texture, bool additive)
            {
                var material = new Material(Shader.Find(ParticleShader)) { name = materialName };
                material.SetTexture("_BaseMap", texture);
                material.SetFloat("_Surface", 1f);                  // Transparent
                material.SetFloat("_Blend", additive ? 2f : 0f);    // URP: 0 Alpha, 2 Additive
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
                material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                material.SetFloat("_DstBlendAlpha", (float)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
                material.SetFloat("_ZWrite", 0f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.SetOverrideTag("RenderType", "Transparent");
                material.renderQueue = (int)RenderQueue.Transparent;
                material.enableInstancing = true;
                return material;
            }
        }

        /// <summary>White soft dot or ring on alpha, written once as PNG; used when a Kenney texture is missing.</summary>
        /// <param name="alphaAt">Coverage at a point in [-1, 1] squared space, y up; squared for a soft falloff.</param>
        private static Texture2D GenerateTexture(string path, Func<Vector2, float> alphaAt) =>
            WriteTexture(path, p =>
            {
                float alpha = alphaAt(p);
                return alpha * alpha;
            });

        /// <summary>
        /// Buff icon drawn on alpha (white, tinted by the particle colour): one thick arrow (damage up) or two chevrons
        /// (attack speed up). Readable at 0.4 m above a hero; any PNG saved under the same name replaces it.
        /// </summary>
        private static Texture2D GenerateIcon(string path, bool doubleChevron) =>
            WriteTexture(path, p => doubleChevron ? Chevrons(p) : Arrow(p));

        private static float Arrow(Vector2 p)
        {
            bool head = p.y >= 0.05f && p.y <= 0.9f && Mathf.Abs(p.x) <= (0.9f - p.y) * 0.95f;
            bool shaft = p.y >= -0.85f && p.y < 0.05f && Mathf.Abs(p.x) <= 0.24f;
            return head || shaft ? 1f : 0f;
        }

        private static float Chevrons(Vector2 p)
        {
            const float thickness = 0.36f;
            const float gap = 0.62f;
            if (Mathf.Abs(p.x) > 0.8f) return 0f;
            float top = 0.85f - 0.9f * Mathf.Abs(p.x);
            bool upper = p.y <= top && p.y >= top - thickness;
            bool lower = p.y <= top - gap && p.y >= top - gap - thickness;
            return upper || lower ? 1f : 0f;
        }

        /// <summary>Renders coverage with 4x4 supersampling (smooth edges), saves it as PNG once and imports it.</summary>
        private static Texture2D WriteTexture(string path, Func<Vector2, float> coverage)
        {
            const int samples = 4;
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;

            var texture = new Texture2D(GeneratedTextureSize, GeneratedTextureSize, TextureFormat.RGBA32, false);
            float half = GeneratedTextureSize * 0.5f;
            for (int y = 0; y < GeneratedTextureSize; y++)
            {
                for (int x = 0; x < GeneratedTextureSize; x++)
                {
                    float sum = 0f;
                    for (int sy = 0; sy < samples; sy++)
                    {
                        for (int sx = 0; sx < samples; sx++)
                        {
                            var p = new Vector2((x + (sx + 0.5f) / samples - half) / half,
                                (y + (sy + 0.5f) / samples - half) / half);
                            sum += coverage(p);
                        }
                    }

                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, sum / (samples * samples)));
                }
            }

            byte[] png = texture.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(texture);
            File.WriteAllBytes(Path.Combine(Directory.GetParent(Application.dataPath).FullName, path), png);
            AssetDatabase.ImportAsset(path);
            ConfigureImporter(path);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static void ConfigureImporter(string path)
        {
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) return;
            if (importer.alphaIsTransparency && importer.wrapMode == TextureWrapMode.Clamp) return;

            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();
        }
    }
}
