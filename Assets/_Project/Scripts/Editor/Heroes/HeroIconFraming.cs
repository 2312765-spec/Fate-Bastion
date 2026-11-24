using UnityEngine;

namespace FateBastion.Editor.Heroes
{
    /// <summary>Pure framing math for Render Hero Icons: how far the camera must sit so a box fills the square icon.</summary>
    public static class HeroIconFraming
    {
        /// <summary>
        /// Distance from <paramref name="target"/> along the camera's back axis at which every corner of
        /// <paramref name="bounds"/> fits inside a square frame of <paramref name="fieldOfView"/> degrees.
        /// <paramref name="margin"/> 1 = the widest corner touches the frame edge; larger leaves more room.
        /// </summary>
        public static float FitDistance(Bounds bounds, Vector3 target, Quaternion rotation, float fieldOfView, float margin)
        {
            // Square icon: horizontal and vertical half-angles are equal.
            float tan = Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad) / Mathf.Max(margin, 0.01f);
            Quaternion toCamera = Quaternion.Inverse(rotation);
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            float distance = 0f;
            for (int i = 0; i < 8; i++)
            {
                var corner = new Vector3((i & 1) == 0 ? min.x : max.x, (i & 2) == 0 ? min.y : max.y,
                    (i & 4) == 0 ? min.z : max.z);
                // Camera space: x right, y up, z forward (towards the model). Depth of the corner is distance + z.
                Vector3 local = toCamera * (corner - target);
                float needed = Mathf.Max(Mathf.Abs(local.x), Mathf.Abs(local.y)) / tan - local.z;
                distance = Mathf.Max(distance, needed);
            }

            return distance;
        }

        /// <summary>
        /// Pixel rectangle around every pixel whose alpha is above <paramref name="alphaThreshold"/> in a square
        /// image of side <paramref name="size"/> (row 0 = bottom, as Texture2D.GetPixels32 returns). False if empty.
        /// </summary>
        public static bool TryGetOpaqueRect(Color32[] pixels, int size, byte alphaThreshold, out RectInt rect)
        {
            int minX = size, minY = size, maxX = -1, maxY = -1;
            for (int y = 0; y < size; y++)
            {
                int row = y * size;
                for (int x = 0; x < size; x++)
                {
                    if (pixels[row + x].a <= alphaThreshold) continue;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }

            rect = maxX < 0 ? default : new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
            return maxX >= 0;
        }

        /// <summary>
        /// Second framing pass from the first render: <paramref name="shift"/> is how far to slide the camera in its
        /// own right/up plane (metres at the target's depth) to centre the opaque rect, and the return value is the
        /// new distance so the rect's longer side spans 1 / <paramref name="margin"/> of the icon.
        /// </summary>
        public static float RefineFromOpaqueRect(RectInt opaque, int size, float distance, float fieldOfView, float margin,
            out Vector2 shift)
        {
            float metresPerPixel = 2f * distance * Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad) / size;
            float half = size * 0.5f;
            shift = new Vector2((opaque.x + opaque.width * 0.5f - half) * metresPerPixel,
                (opaque.y + opaque.height * 0.5f - half) * metresPerPixel);

            float longest = Mathf.Max(opaque.width, opaque.height);
            float wanted = size / Mathf.Max(margin, 0.01f);
            // Narrow FOV: on-screen size is close to inversely proportional to distance.
            return distance * longest / wanted;
        }

        /// <summary>Renderers that belong to the model itself; particles (rarity aura, buff icons) are left out.</summary>
        public static bool IsModelRenderer(Renderer renderer)
        {
            return renderer is SkinnedMeshRenderer || renderer is MeshRenderer;
        }
    }
}
