using UnityEngine;
using SquareFlow.Core;

namespace SquareFlow.Runtime
{
    public static class SquareFlowWorldSprites
    {
        private static Sprite roundedRect;
        private static Sprite circle;
        private static Sprite glow;
        private static Sprite square;
        private static Sprite blockRed;
        private static Sprite blockBlue;
        private static Sprite blockYellow;
        private static Sprite blockGreen;
        private static Sprite blockOrange;
        private static Sprite gridCellTray;
        private static Sprite orbitRed;
        private static Sprite orbitBlue;
        private static Sprite orbitYellow;
        private static Sprite orbitGreen;
        private static Sprite orbitOrange;
        private static Sprite skyBackground;

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

        public static Sprite SkyBackground
        {
            get
            {
                Ensure();
                return skyBackground;
            }
        }

        public static Sprite BlockForCell(BoardCell cell)
        {
            Ensure();
            if (!cell.IsOccupied) return gridCellTray != null ? gridCellTray : roundedRect;
            if (cell.Type == BoardCellType.Bomb) return blockOrange != null ? blockOrange : roundedRect;

            switch (cell.Color)
            {
                case FlowColor.Blue:
                    return blockBlue != null ? blockBlue : roundedRect;
                case FlowColor.Yellow:
                    return blockYellow != null ? blockYellow : roundedRect;
                case FlowColor.Green:
                    return blockGreen != null ? blockGreen : roundedRect;
                default:
                    return blockRed != null ? blockRed : roundedRect;
            }
        }

        public static Sprite OrbitForShooter(FlowColor color, bool wild)
        {
            Ensure();
            if (wild || color == FlowColor.Wild) return orbitOrange != null ? orbitOrange : circle;

            switch (color)
            {
                case FlowColor.Blue:
                    return orbitBlue != null ? orbitBlue : circle;
                case FlowColor.Yellow:
                    return orbitYellow != null ? orbitYellow : circle;
                case FlowColor.Green:
                    return orbitGreen != null ? orbitGreen : circle;
                default:
                    return orbitRed != null ? orbitRed : circle;
            }
        }

        public static void Ensure()
        {
            if (roundedRect != null) return;

            roundedRect = CreateRoundedRectSprite(96, 22);
            circle = CreateCircleSprite(64, 0.5f, 0.5f, "SquareFlowWorldCircle");
            glow = CreateCircleSprite(96, 0.5f, 0f, "SquareFlowWorldGlow");
            square = CreateSolidSprite(8, "SquareFlowWorldSquare");
            blockRed = LoadBlockSprite("FlowBlockRed");
            blockBlue = LoadBlockSprite("FlowBlockBlue");
            blockYellow = LoadBlockSprite("FlowBlockYellow");
            blockGreen = LoadBlockSprite("FlowBlockGreen");
            blockOrange = LoadBlockSprite("FlowBlockOrange");
            gridCellTray = LoadBlockSprite("FlowGridCellTray");
            orbitRed = LoadOrbitSprite("FlowOrbitRed");
            orbitBlue = LoadOrbitSprite("FlowOrbitBlue");
            orbitYellow = LoadOrbitSprite("FlowOrbitYellow");
            orbitGreen = LoadOrbitSprite("FlowOrbitGreen");
            orbitOrange = LoadOrbitSprite("FlowOrbitOrange");
            skyBackground = LoadBackgroundSprite("FlowSkyBackground");
        }

        private static Sprite LoadBlockSprite(string resourceName)
        {
            return LoadResourceSprite("SquareFlow/Grid/" + resourceName, resourceName);
        }

        private static Sprite LoadOrbitSprite(string resourceName)
        {
            return LoadResourceSprite("SquareFlow/Orbits/" + resourceName, resourceName);
        }

        private static Sprite LoadBackgroundSprite(string resourceName)
        {
            return LoadResourceSprite("SquareFlow/Backgrounds/" + resourceName, resourceName);
        }

        private static Sprite LoadResourceSprite(string resourcePath, string resourceName)
        {
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null) return null;

            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), texture.width);
            sprite.name = resourceName + "Sprite";
            return sprite;
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
