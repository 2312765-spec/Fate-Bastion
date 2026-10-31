using UnityEditor;
using UnityEngine;
using FateBastion.Heroes;

namespace FateBastion.Editor.UI
{
    // C-09 part 2: effects played by A's code (SkillData.vfx, status, meteor fall), B's HeroPool (level 3) and the
    // buff icons above a hero's head. Each prefab is built to a unit size; the caller scales it (see the issue to A).
    public static partial class FxPrefabBuilder
    {
        public const string AreaFireName = "FX_Skill_Area_Fire";
        public const string AreaIceName = "FX_Skill_Area_Ice";
        public const string AreaLightningName = "FX_Skill_Area_Lightning";
        public const string ConeFireName = "FX_Skill_Cone_Fire";
        public const string MeteorFallName = "FX_Meteor_Fall";
        public const string DragonLineName = "FX_Ult_DragonLine";
        public const string FrostZoneName = "FX_Skill_FrostZone";
        public const string StatusBurnName = "FX_Status_Burn";
        public const string StatusSlowName = "FX_Status_Slow";
        public const string HeroLevel3Name = "FX_Hero_Level3";
        public const string BuffDamageName = "FX_Buff_Damage";
        public const string BuffAttackSpeedName = "FX_Buff_AttackSpeed";
        public const string BuffIconsObjectName = "BuffIcons";

        /// <summary>One-shot effects A plays from SkillData.vfx (AreaAtPoint, Cone) and MeteorAimIndicator (fall).</summary>
        public static readonly string[] SkillOneShotNames =
        {
            AreaFireName, AreaIceName, AreaLightningName, ConeFireName, MeteorFallName
        };

        /// <summary>Looping effects: Line/Zone fields and statuses (A), level 3 (B), buff icons (C).</summary>
        public static readonly string[] SkillLoopNames =
        {
            DragonLineName, FrostZoneName, StatusBurnName, StatusSlowName, HeroLevel3Name, BuffDamageName, BuffAttackSpeedName
        };

        /// <summary>Built along +Z from the pivot, 1 m long: the caller scales Z by length (Line) or range (Cone).</summary>
        public static readonly string[] ForwardNames = { ConeFireName, DragonLineName };

        // Meteor falls from this height (×radius once scaled) and lands exactly when MeteorAbility explodes (0.8 s).
        public const float MeteorFallHeight = 5f;
        public const float MeteorFallSeconds = 0.8f;
        private const float BuffIconHeight = 0.3f;
        private const float BuffIconSpacing = 0.45f;

        [MenuItem("Tools/FX/Add Buff Indicator To Selected Hero Prefab")]
        public static void AddBuffIndicatorFromMenu()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrEmpty(path) || !path.EndsWith(".prefab"))
            {
                Debug.LogError("[FX] Select a hero prefab (or Variant) in the Project window first.");
                return;
            }

            AddBuffIndicator(path, PrefabFolder);
        }

        /// <summary>
        /// Adds BuffIcons (HeroBuffIndicator + both icon prefabs, hidden) under HeroVisual.Overhead of the hero prefab
        /// at <paramref name="heroPrefabPath"/>. False when the prefab has no Overhead or already has the icons.
        /// </summary>
        public static bool AddBuffIndicator(string heroPrefabPath, string prefabFolder)
        {
            var damage = AssetDatabase.LoadAssetAtPath<GameObject>(GetPrefabPath(prefabFolder, BuffDamageName));
            var attackSpeed = AssetDatabase.LoadAssetAtPath<GameObject>(GetPrefabPath(prefabFolder, BuffAttackSpeedName));
            if (damage == null || attackSpeed == null)
            {
                Debug.LogError($"[FX] {BuffDamageName} / {BuffAttackSpeedName} not found; run 'Create FX Prefabs' first.");
                return false;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(heroPrefabPath);
            try
            {
                var visual = root.GetComponentInChildren<HeroVisual>(true);
                if (visual == null || visual.Overhead == null)
                {
                    Debug.LogError($"[FX] {heroPrefabPath} has no HeroVisual with Overhead; run Tools > Validate Hero Prefabs.");
                    return false;
                }

                if (visual.Overhead.Find(BuffIconsObjectName) != null)
                {
                    Debug.Log($"[FX] {heroPrefabPath} already has {BuffIconsObjectName}; not changed.");
                    return false;
                }

                var icons = new GameObject(BuffIconsObjectName);
                icons.transform.SetParent(visual.Overhead, false);
                icons.transform.localPosition = new Vector3(0f, BuffIconHeight, 0f);
                var damageIcon = (GameObject)PrefabUtility.InstantiatePrefab(damage, icons.transform);
                var attackSpeedIcon = (GameObject)PrefabUtility.InstantiatePrefab(attackSpeed, icons.transform);
                damageIcon.SetActive(false);
                attackSpeedIcon.SetActive(false);
                icons.AddComponent<HeroBuffIndicator>().Configure(damageIcon, attackSpeedIcon, BuffIconSpacing);

                PrefabUtility.SaveAsPrefabAsset(root, heroPrefabPath);
                Debug.Log($"[FX] Buff icons added above '{visual.Overhead.name}' in {heroPrefabPath}.");
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static int BuildSkillFx(string folder, MaterialCache materials)
        {
            int created = 0;
            created += BuildAreas(folder, materials);
            created += BuildConeAndLine(folder, materials);
            created += BuildZoneAndStatuses(folder, materials);
            created += BuildMeteorFall(folder, materials);
            created += BuildLevel3AndBuffs(folder, materials);
            return created;
        }

        // AreaAtPoint: built for a 1 m radius on the ground; A scales by SkillData.radius.
        private static int BuildAreas(string folder, MaterialCache materials)
        {
            int created = 0;
            created += SaveLayers(folder, AreaFireName, materials, new[]
            {
                new Layer
                {
                    Name = AreaFireName, Texture = FxTexture.Flame, Additive = true, Shape = ShapeKind.DiscUp,
                    ShapeRadius = 0.8f, Speed = new Vector2(0.3f, 0.8f), Velocity = new Vector3(0f, 1.2f, 0f),
                    Lifetime = new Vector2(0.4f, 0.7f), Size = new Vector2(0.5f, 0.9f), Color = new Color(1f, 0.5f, 0.15f),
                    Burst = 18, Curve = SizeCurve.Shrink, MaxParticles = 20
                },
                new Layer
                {
                    Name = "Smoke", Texture = FxTexture.Smoke, Shape = ShapeKind.DiscUp, ShapeRadius = 0.7f,
                    Velocity = new Vector3(0f, 0.6f, 0f), Lifetime = new Vector2(0.8f, 1.2f), Size = new Vector2(0.6f, 1f),
                    Color = new Color(0.3f, 0.27f, 0.25f, 0.6f), Burst = 8, Curve = SizeCurve.Grow, MaxParticles = 10
                },
                Flash("Flash", new Color(1f, 0.7f, 0.4f), 1.6f, 0.18f, new Vector3(0f, 0.3f, 0f)),
                Shockwave("Shockwave", new Color(1f, 0.55f, 0.2f, 0.8f), 2f, 0.35f)
            });

            created += SaveLayers(folder, AreaIceName, materials, new[]
            {
                new Layer
                {
                    Name = AreaIceName, Texture = FxTexture.Star, Additive = true, Shape = ShapeKind.DiscUp,
                    ShapeRadius = 0.8f, Speed = new Vector2(0.5f, 1.2f), Velocity = new Vector3(0f, 1.5f, 0f), Gravity = 1.2f,
                    Lifetime = new Vector2(0.5f, 0.8f), Size = new Vector2(0.25f, 0.45f), Color = new Color(0.7f, 0.95f, 1f),
                    Burst = 14, Curve = SizeCurve.Shrink, MaxParticles = 16
                },
                new Layer
                {
                    Name = "Frost", Texture = FxTexture.Glow, Additive = true, Render = RenderKind.Flat,
                    Lifetime = new Vector2(0.6f, 0.6f), Size = new Vector2(2f, 2f), Color = new Color(0.5f, 0.85f, 1f, 0.6f),
                    Burst = 1, Curve = SizeCurve.Grow, MaxParticles = 1, Offset = new Vector3(0f, GroundLift, 0f)
                },
                Flash("Flash", new Color(0.7f, 0.9f, 1f), 1.4f, 0.18f, new Vector3(0f, 0.3f, 0f)),
                Shockwave("Shockwave", new Color(0.6f, 0.9f, 1f, 0.8f), 2f, 0.35f)
            });

            created += SaveLayers(folder, AreaLightningName, materials, new[]
            {
                new Layer
                {
                    Name = AreaLightningName, Texture = FxTexture.Spark, Additive = true, Render = RenderKind.Stretched,
                    ShapeRadius = 0.3f, Speed = new Vector2(4f, 7f), Lifetime = new Vector2(0.15f, 0.3f),
                    Size = new Vector2(0.12f, 0.2f), Color = new Color(0.85f, 0.8f, 1f), Burst = 16,
                    Curve = SizeCurve.Shrink, MaxParticles = 18, Offset = new Vector3(0f, 0.5f, 0f)
                },
                // Bolt from the sky: one stretched glow falling fast onto the centre.
                new Layer
                {
                    Name = "Bolt", Texture = FxTexture.Glow, Additive = true, Render = RenderKind.Stretched,
                    Velocity = new Vector3(0f, -25f, 0f), Lifetime = new Vector2(0.15f, 0.15f), Size = new Vector2(0.6f, 0.6f),
                    Color = new Color(0.9f, 0.85f, 1f), Burst = 1, MaxParticles = 1, Offset = new Vector3(0f, 4f, 0f)
                },
                Flash("Flash", new Color(0.85f, 0.8f, 1f), 2f, 0.15f, new Vector3(0f, 0.3f, 0f)),
                Shockwave("Shockwave", new Color(0.75f, 0.7f, 1f, 0.8f), 2f, 0.3f)
            });

            return created;
        }

        // Cone and Line start at the hero (pivot) and run along +Z for 1 m; A scales X by width and Z by length/range.
        private static int BuildConeAndLine(string folder, MaterialCache materials)
        {
            int created = 0;
            var muzzleHeight = new Vector3(0f, 0.6f, 0f);

            // 1 m along +Z with 1 m half-width at the end: a 45 degree cone; particles stop before z = 1.
            created += SaveLayers(folder, ConeFireName, materials, new[]
            {
                new Layer
                {
                    Name = ConeFireName, Texture = FxTexture.Flame, Additive = true, Shape = ShapeKind.ConeForward,
                    ShapeAngle = 40f, ShapeRadius = 0.05f, Duration = 0.3f, Rate = 60f, Speed = new Vector2(1.6f, 1.9f),
                    Lifetime = new Vector2(0.45f, 0.5f), Size = new Vector2(0.15f, 0.5f), Color = new Color(1f, 0.55f, 0.15f),
                    Curve = SizeCurve.Grow, MaxParticles = 30, Offset = muzzleHeight
                },
                new Layer
                {
                    Name = "Embers", Texture = FxTexture.Spark, Additive = true, Render = RenderKind.Stretched,
                    Shape = ShapeKind.ConeForward, ShapeAngle = 45f, ShapeRadius = 0.05f, Speed = new Vector2(1.5f, 2f),
                    Lifetime = new Vector2(0.3f, 0.5f), Size = new Vector2(0.06f, 0.1f), Color = new Color(1f, 0.8f, 0.3f),
                    Burst = 12, Curve = SizeCurve.Shrink, MaxParticles = 14, Offset = muzzleHeight
                }
            });

            // Fire wall over the Line field (x in [-0.5, 0.5], z in [0, 1]) for its duration.
            var lineCentre = new Vector3(0f, GroundLift, 0.5f);
            var lineArea = new Vector3(1f, 0.05f, 1f);
            created += SaveLayers(folder, DragonLineName, materials, new[]
            {
                new Layer
                {
                    Name = DragonLineName, Texture = FxTexture.Flame, Additive = true, Loop = true, Duration = 1f,
                    Shape = ShapeKind.Box, ShapeScale = lineArea, Velocity = new Vector3(0f, 1.5f, 0f), Rate = 40f,
                    Lifetime = new Vector2(0.5f, 0.8f), Size = new Vector2(0.5f, 0.9f), Color = new Color(1f, 0.45f, 0.1f),
                    Curve = SizeCurve.Shrink, MaxParticles = 40, Offset = lineCentre
                },
                new Layer
                {
                    Name = "Embers", Texture = FxTexture.Spark, Additive = true, Render = RenderKind.Stretched, Loop = true,
                    Duration = 1f, Shape = ShapeKind.Box, ShapeScale = lineArea, Velocity = new Vector3(0f, 2.5f, 0f),
                    Rate = 25f, Lifetime = new Vector2(0.4f, 0.7f), Size = new Vector2(0.05f, 0.1f),
                    Color = new Color(1f, 0.8f, 0.3f), Curve = SizeCurve.Shrink, MaxParticles = 30, Offset = lineCentre
                },
                new Layer
                {
                    Name = "Glow", Texture = FxTexture.Glow, Additive = true, Render = RenderKind.Flat, Loop = true,
                    Duration = 1f, Shape = ShapeKind.Box, ShapeScale = lineArea, Rate = 6f, Lifetime = new Vector2(1f, 1f),
                    Size = new Vector2(0.6f, 0.8f), Color = new Color(1f, 0.4f, 0.1f, 0.5f), MaxParticles = 8,
                    Offset = lineCentre
                }
            });

            return created;
        }

        private static int BuildZoneAndStatuses(string folder, MaterialCache materials)
        {
            int created = 0;
            var ground = new Vector3(0f, GroundLift, 0f);

            // Zone: 1 m radius around the hero; A scales by SkillData.radius and turns it off after duration.
            created += SaveLayers(folder, FrostZoneName, materials, new[]
            {
                new Layer
                {
                    Name = FrostZoneName, Texture = FxTexture.Glow, Additive = true, Render = RenderKind.Flat, Loop = true,
                    Duration = 2f, Shape = ShapeKind.DiscUp, ShapeRadius = 0.7f, Rate = 4f, Lifetime = new Vector2(1.5f, 2f),
                    Size = new Vector2(0.8f, 1.2f), Color = new Color(0.5f, 0.85f, 1f, 0.35f), MaxParticles = 10,
                    Offset = ground
                },
                new Layer
                {
                    Name = "Ring", Texture = FxTexture.Ring, Additive = true, Render = RenderKind.Flat, Loop = true,
                    Duration = 2f, Rate = 0.8f, Lifetime = new Vector2(2f, 2f), Size = new Vector2(2f, 2f),
                    Color = new Color(0.6f, 0.9f, 1f, 0.6f), MaxParticles = 3, Offset = ground
                },
                new Layer
                {
                    Name = "Flakes", Texture = FxTexture.Star, Additive = true, Loop = true, Duration = 2f,
                    Shape = ShapeKind.DiscUp, ShapeRadius = 1f, Velocity = new Vector3(0f, 0.5f, 0f), Rate = 10f,
                    Lifetime = new Vector2(1f, 1.5f), Size = new Vector2(0.08f, 0.15f), Color = new Color(0.85f, 0.95f, 1f),
                    Curve = SizeCurve.Shrink, MaxParticles = 20
                }
            });

            // Statuses sit on a normal enemy (root on the ground, ~1.6 m tall); A scales them up for the boss.
            // Burn simulates in world space so the flames trail behind a walking enemy.
            created += SaveLayers(folder, StatusBurnName, materials, new[]
            {
                new Layer
                {
                    Name = StatusBurnName, Texture = FxTexture.Flame, Additive = true, Loop = true, World = true,
                    Duration = 1f, ShapeRadius = 0.25f, Velocity = new Vector3(0f, 0.9f, 0f), Rate = 10f,
                    Lifetime = new Vector2(0.35f, 0.55f), Size = new Vector2(0.25f, 0.4f), Color = new Color(1f, 0.5f, 0.15f),
                    Curve = SizeCurve.Shrink, MaxParticles = 10, Offset = new Vector3(0f, HitHeight, 0f)
                },
                new Layer
                {
                    Name = "Embers", Texture = FxTexture.Spark, Additive = true, Loop = true, World = true, Duration = 1f,
                    ShapeRadius = 0.3f, Velocity = new Vector3(0f, 1.2f, 0f), Rate = 4f, Lifetime = new Vector2(0.4f, 0.6f),
                    Size = new Vector2(0.04f, 0.07f), Color = new Color(1f, 0.75f, 0.3f), Curve = SizeCurve.Shrink,
                    MaxParticles = 6, Offset = new Vector3(0f, HitHeight, 0f)
                }
            });

            created += SaveLayers(folder, StatusSlowName, materials, new[]
            {
                new Layer
                {
                    Name = StatusSlowName, Texture = FxTexture.Ring, Additive = true, Render = RenderKind.Flat, Loop = true,
                    Duration = 1.5f, Rate = 1f, Lifetime = new Vector2(1.5f, 1.5f), Size = new Vector2(1.2f, 1.2f),
                    Color = new Color(0.6f, 0.9f, 1f, 0.6f), MaxParticles = 3, Offset = ground
                },
                new Layer
                {
                    Name = "Flakes", Texture = FxTexture.Star, Additive = true, Loop = true, Duration = 1f,
                    ShapeRadius = 0.4f, Gravity = 0.15f, Rate = 5f, Lifetime = new Vector2(0.8f, 1.2f),
                    Size = new Vector2(0.08f, 0.14f), Color = new Color(0.8f, 0.95f, 1f), Curve = SizeCurve.Shrink,
                    MaxParticles = 8, Offset = new Vector3(0f, 0.9f, 0f)
                }
            });

            return created;
        }

        // The shadow on the ground is MeteorAimFX/ImpactMarker (already shown while falling); this is the rock itself.
        // Built for a 1 m blast radius: scaled ×4 it falls from 20 m.
        private static int BuildMeteorFall(string folder, MaterialCache materials)
        {
            var start = new Vector3(0f, MeteorFallHeight, 0f);
            var fall = new Vector3(0f, -MeteorFallHeight / MeteorFallSeconds, 0f);
            var lifetime = new Vector2(MeteorFallSeconds, MeteorFallSeconds);
            return SaveLayers(folder, MeteorFallName, materials, new[]
            {
                new Layer
                {
                    Name = MeteorFallName, Texture = FxTexture.Flame, Additive = true, Velocity = fall, Lifetime = lifetime,
                    Size = new Vector2(0.9f, 0.9f), Color = new Color(1f, 0.6f, 0.2f), Burst = 1, MaxParticles = 1,
                    TrailLifetime = 0.35f, NoFade = true, Offset = start
                },
                new Layer
                {
                    Name = "Core", Texture = FxTexture.Glow, Additive = true, Velocity = fall, Lifetime = lifetime,
                    Size = new Vector2(0.5f, 0.5f), Color = new Color(1f, 0.9f, 0.6f), Burst = 1, MaxParticles = 1,
                    NoFade = true, Offset = start
                }
            });
        }

        private static int BuildLevel3AndBuffs(string folder, MaterialCache materials)
        {
            var gold = new Color(1f, 0.85f, 0.3f);
            int created = SaveLayers(folder, HeroLevel3Name, materials, new[]
            {
                new Layer
                {
                    Name = HeroLevel3Name, Texture = FxTexture.Glow, Additive = true, Loop = true, Duration = 2f,
                    Shape = ShapeKind.DiscUp, ShapeRadius = 0.5f, Velocity = new Vector3(0f, 1f, 0f), Rate = 8f,
                    Lifetime = new Vector2(1f, 1.4f), Size = new Vector2(0.08f, 0.14f), Color = gold,
                    Curve = SizeCurve.Shrink, MaxParticles = 14
                },
                new Layer
                {
                    Name = "Beam", Texture = FxTexture.Glow, Additive = true, Render = RenderKind.Stretched, Loop = true,
                    Duration = 2f, Shape = ShapeKind.DiscUp, ShapeRadius = 0.3f, Velocity = new Vector3(0f, 1.5f, 0f),
                    Rate = 1.5f, Lifetime = new Vector2(1f, 1f), Size = new Vector2(0.5f, 0.5f),
                    Color = new Color(1f, 0.8f, 0.3f, 0.5f), MaxParticles = 4
                },
                new Layer
                {
                    Name = "Sparkle", Texture = FxTexture.Star, Additive = true, Loop = true, Duration = 2f,
                    ShapeRadius = 0.4f, Rate = 3f, Lifetime = new Vector2(0.6f, 0.6f), Size = new Vector2(0.15f, 0.25f),
                    Color = gold, Curve = SizeCurve.Shrink, MaxParticles = 4, Offset = new Vector3(0f, 1f, 0f)
                }
            });

            created += SaveLayers(folder, BuffDamageName, materials, new[] { BuffIcon(BuffDamageName, FxTexture.IconDamage, new Color(1f, 0.3f, 0.25f)) });
            created += SaveLayers(folder, BuffAttackSpeedName, materials, new[] { BuffIcon(BuffAttackSpeedName, FxTexture.IconAttackSpeed, new Color(1f, 0.85f, 0.2f)) });
            return created;
        }

        // One upright icon that is always there: a new particle is born each 1 s cycle and lives slightly longer, so
        // there is never a frame without one (Max 2 lets the short overlap through).
        private static Layer BuffIcon(string layerName, FxTexture icon, Color color) => new Layer
        {
            Name = layerName, Texture = icon, Loop = true, Duration = 1f, Burst = 1, Lifetime = new Vector2(1.02f, 1.02f),
            Size = new Vector2(0.35f, 0.35f), Color = color, FixedRotation = true, NoFade = true, Curve = SizeCurve.Pulse,
            MaxParticles = 2
        };
    }
}
