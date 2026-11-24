using System;
using System.Collections.Generic;
using UnityEngine;

namespace FateBastion.Editor.UI
{
    /// <summary>
    /// Pure placement rules for Tools > Map > Dress Map_Level1 (C-17): distance to the road on the ground plane and a
    /// seeded scatter. No scene or AssetDatabase access, so the rules are unit tested.
    /// </summary>
    public static class MapDressing
    {
        /// <summary>
        /// Shortest XZ distance from <paramref name="point"/> to a triangle soup (three corners per triangle, as the road
        /// mesh gives them). 0 when the point lies on a triangle.
        /// </summary>
        public static float DistanceToTriangles(Vector2 point, IReadOnlyList<Vector2> corners)
        {
            float best = float.MaxValue;
            for (int i = 0; i + 2 < corners.Count; i += 3)
            {
                Vector2 a = corners[i], b = corners[i + 1], c = corners[i + 2];
                // Degenerate (zero-area on XZ, e.g. a vertical cap) triangles only count through their edges.
                if (Mathf.Abs(Cross(c, a, b)) > 1e-6f && Inside(point, a, b, c)) return 0f;
                best = Mathf.Min(best, DistanceToSegment(point, a, b));
                best = Mathf.Min(best, DistanceToSegment(point, b, c));
                best = Mathf.Min(best, DistanceToSegment(point, c, a));
            }

            return best;
        }

        /// <summary>
        /// Seeded Poisson-like scatter: up to <paramref name="attempts"/> random points in <paramref name="area"/>, kept
        /// when at least <paramref name="spacing"/> from every kept point and accepted by <paramref name="accept"/>.
        /// The same seed and inputs always give the same points, so re-running the tool keeps the layout.
        /// </summary>
        public static List<Vector2> Scatter(int seed, Rect area, int attempts, float spacing, Func<Vector2, bool> accept)
        {
            var rng = new System.Random(seed);
            var kept = new List<Vector2>();
            // Grid of cell size = spacing: a neighbour closer than spacing is always in the 3x3 cells around a point.
            var grid = new Dictionary<Vector2Int, List<Vector2>>();
            float spacingSq = spacing * spacing;
            for (int i = 0; i < attempts; i++)
            {
                var p = new Vector2(area.xMin + (float)rng.NextDouble() * area.width,
                    area.yMin + (float)rng.NextDouble() * area.height);
                Vector2Int cell = Cell(p, spacing);
                if (spacing > 0f && HasNeighbour(grid, cell, p, spacingSq)) continue;
                if (accept != null && !accept(p)) continue;

                kept.Add(p);
                if (!grid.TryGetValue(cell, out List<Vector2> list)) grid[cell] = list = new List<Vector2>();
                list.Add(p);
            }

            return kept;
        }

        /// <summary>True when <paramref name="point"/> is inside the square of half size <paramref name="half"/>.</summary>
        public static bool InsideSquare(Vector2 point, float half)
        {
            return Mathf.Abs(point.x) <= half && Mathf.Abs(point.y) <= half;
        }

        /// <summary>True when <paramref name="point"/> is inside any rect of <paramref name="rects"/>.</summary>
        public static bool InsideAny(Vector2 point, IReadOnlyList<Rect> rects)
        {
            for (int i = 0; i < rects.Count; i++)
            {
                if (rects[i].Contains(point)) return true;
            }

            return false;
        }

        private static bool HasNeighbour(Dictionary<Vector2Int, List<Vector2>> grid, Vector2Int cell, Vector2 p, float spacingSq)
        {
            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                if (!grid.TryGetValue(new Vector2Int(cell.x + dx, cell.y + dy), out List<Vector2> list)) continue;
                for (int i = 0; i < list.Count; i++)
                {
                    if ((list[i] - p).sqrMagnitude < spacingSq) return true;
                }
            }

            return false;
        }

        private static Vector2Int Cell(Vector2 p, float size)
        {
            if (size <= 0f) return Vector2Int.zero;
            return new Vector2Int(Mathf.FloorToInt(p.x / size), Mathf.FloorToInt(p.y / size));
        }

        private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float lengthSq = ab.sqrMagnitude;
            float t = lengthSq > 0f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / lengthSq) : 0f;
            return Vector2.Distance(p, a + ab * t);
        }

        private static bool Inside(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Cross(p, a, b), d2 = Cross(p, b, c), d3 = Cross(p, c, a);
            bool hasNegative = d1 < 0f || d2 < 0f || d3 < 0f;
            bool hasPositive = d1 > 0f || d2 > 0f || d3 > 0f;
            return !(hasNegative && hasPositive);
        }

        private static float Cross(Vector2 p, Vector2 a, Vector2 b)
        {
            return (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);
        }
    }
}
