using UnityEngine;

namespace FateBastion.Heroes
{
    /// <summary>Layer masks of the placement checks (S4): what the mouse ray can hit, what counts as Placeable, what blocks.</summary>
    public readonly struct PlacementMasks
    {
        /// <summary>Every surface the ray stops on (ground, road, water, cliffs…), never heroes or enemies.</summary>
        public readonly int Surface;

        /// <summary>The closest surface hit must be on one of these layers.</summary>
        public readonly int Placeable;

        /// <summary>Layers that may not touch the occupancy sphere (Hero, Path, Obstacle).</summary>
        public readonly int Blocking;

        public PlacementMasks(int surface, int placeable, int blocking)
        {
            Surface = surface;
            Placeable = placeable;
            Blocking = blocking;
        }

        /// <summary>From the project layers: Placeable / Path / Obstacle / Hero (Tags &amp; Layers, S0).</summary>
        public static PlacementMasks FromProjectLayers()
        {
            int placeable = LayerMask.GetMask("Placeable");
            int blocking = LayerMask.GetMask("Hero", "Path", "Obstacle");
            int surface = ~LayerMask.GetMask("Hero", "Enemy", "UI", "Ignore Raycast");
            return new PlacementMasks(surface, placeable, blocking);
        }
    }

    /// <summary>Physics part of one placement attempt: where the ray lands and whether the spot is free.</summary>
    public readonly struct PlacementProbeResult
    {
        public readonly bool HitSurface;
        public readonly Vector3 Point;
        public readonly bool HitPlaceable;
        public readonly bool AreaFree;

        public PlacementProbeResult(bool hitSurface, Vector3 point, bool hitPlaceable, bool areaFree)
        {
            HitSurface = hitSurface;
            Point = point;
            HitPlaceable = hitPlaceable;
            AreaFree = areaFree;
        }
    }

    /// <summary>
    /// S4 checks 4 and 5 with non-allocating queries: the closest surface under the ray must be Placeable, and a sphere
    /// of the occupancy radius at that point may not touch a hero, the path or an obstacle. Re-run on every click.
    /// </summary>
    public static class PlacementProbe
    {
        public const float MaxRayDistance = 500f;

        public static PlacementProbeResult Probe(Ray ray, in PlacementMasks masks, float occupancyRadius)
        {
            if (!Physics.Raycast(ray, out RaycastHit hit, MaxRayDistance, masks.Surface, QueryTriggerInteraction.Ignore))
            {
                return new PlacementProbeResult(false, Vector3.zero, false, false);
            }

            bool placeable = (masks.Placeable & (1 << hit.collider.gameObject.layer)) != 0;
            bool free = !Physics.CheckSphere(hit.point, occupancyRadius, masks.Blocking, QueryTriggerInteraction.Ignore);
            return new PlacementProbeResult(true, hit.point, placeable, free);
        }

        /// <summary>The placed hero under the ray (Hero layer), for selecting it.</summary>
        public static HeroLevel PickHero(Ray ray, int heroMask)
        {
            if (!Physics.Raycast(ray, out RaycastHit hit, MaxRayDistance, heroMask, QueryTriggerInteraction.Ignore)) return null;
            HeroLevel hero = hit.collider.GetComponentInParent<HeroLevel>();
            return hero != null && hero.IsPlaced ? hero : null;
        }
    }
}
