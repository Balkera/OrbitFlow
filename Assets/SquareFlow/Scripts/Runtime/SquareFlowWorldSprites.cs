using UnityEngine;

namespace SquareFlow.Runtime
{
    public static class SquareFlowWorldSprites
    {
        private static Sprite roundedRect;
        private static Sprite circle;
        private static Sprite glow;
        private static Sprite square;

        public static Sprite RoundedRect
        {
            get
            {
                Ensure();
                return roundedRect;
            }
        }

        public static Sprite Circle
        {
            get
            {
                Ensure();
                return circle;
            }
        }

        public static Sprite Glow
        {
            get
            {
                Ensure();
                return glow;
            }
        }

        public static Sprite Square
        {
            get
            {
                Ensure();
                return square;
            }
        }

        public static void Ensure()
        {
            if (roundedRect != null) return;

            roundedRect = CreateRoundedRectSprite(96, 22);
            circle = CreateCircleSprite(64, 0.5f, 0.5f, "SquareFlowWorldCircle");
            glow = CreateCircleSprite(96, 0.5f, 0f, "SquareFlowWorldGlow");
            square = CreateSolidSprite(8, "SquareFlowWorldSquare");
        }

        private static Sprite CreateSolidSprite(int size, string name)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = name;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                texture.SetPixel(x, y, Color.white);

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static Sprite CreateRoundedRectSprite(int size, int radius)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "SquareFlowWorldRoundedRect";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            float r = radius - 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float px = x < radius ? radius - x - 0.5f : x >= size - radius ? x - (size - radius) + 0.5f : 0f;
                float py = y < radius ? radius - y - 0.5f : y >= size - radius ? y - (size - radius) + 0.5f : 0f;
                float distance = Mathf.Sqrt(px * px + py * py);
                float alpha = px == 0f && py == 0f ? 1f : Mathf.Clamp01(r + 1f - distance);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }

        private static Sprite CreateCircleSprite(int size, float solidRadius, float edgeAlpha, string name)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = name;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            float center = (size - 1f) * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center) / center;
                float dy = (y - center) / center;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = edgeAlpha > 0f
                    ? Mathf.Clamp01((solidRadius - distance) * size * 0.28f + edgeAlpha)
                    : Mathf.Pow(Mathf.Clamp01(1f - distance), 2f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
