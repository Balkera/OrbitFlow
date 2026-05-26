using System.Collections.Generic;
using UnityEngine;

namespace SquareFlow.Core
{
    public enum FireSide
    {
        Top,
        Right,
        Bottom,
        Left
    }

    public readonly struct FirePoint
    {
        public FirePoint(FireSide side, int row, int col, float distance)
        {
            Side = side;
            Row = row;
            Col = col;
            Distance = distance;
        }

        public FireSide Side { get; }
        public int Row { get; }
        public int Col { get; }
        public float Distance { get; }
    }

    public sealed class BoardLayout
    {
        public const float Gap = 3f;

        private BoardLayout()
        {
        }

        public float Cell { get; private set; }
        public float Pad { get; private set; }
        public float Inset { get; private set; }
        public float GridX { get; private set; }
        public float GridY { get; private set; }
        public float GridWidth { get; private set; }
        public float GridHeight { get; private set; }
        public float CanvasWidth { get; private set; }
        public float CanvasHeight { get; private set; }
        public float OrbitX { get; private set; }
        public float OrbitY { get; private set; }
        public float OrbitWidth { get; private set; }
        public float OrbitHeight { get; private set; }
        public float OrbitCenterX { get; private set; }
        public float OrbitCenterY { get; private set; }
        public float OrbitRadiusX { get; private set; }
        public float OrbitRadiusY { get; private set; }
        public float Perimeter { get; private set; }
        public List<FirePoint> FirePoints { get; private set; }

        public static BoardLayout Compute(int rows, int cols, float availableWidth)
        {
            float usable = Mathf.Max(availableWidth - 24f, 200f);
            float rawCell = (usable - (cols - 1) * Gap) / (3.44f + cols);
            float capCell = cols <= 5 ? 52f : cols <= 8 ? 48f : cols <= 11 ? 40f : cols <= 13 ? 34f : 30f;
            BoardLayout layout = new BoardLayout();
            layout.Cell = Mathf.Min(capCell, Mathf.Max(12f, Mathf.Floor(rawCell)));
            layout.Pad = Mathf.Round(layout.Cell * 1.72f);
            layout.Inset = Mathf.Round(layout.Cell * 0.62f);
            layout.GridWidth = cols * (layout.Cell + Gap) - Gap;
            layout.GridHeight = rows * (layout.Cell + Gap) - Gap;
            layout.CanvasWidth = layout.Pad * 2f + layout.GridWidth;
            layout.CanvasHeight = layout.Pad * 2f + layout.GridHeight;
            layout.OrbitX = layout.Inset;
            layout.OrbitY = layout.Inset;
            layout.OrbitWidth = layout.CanvasWidth - 2f * layout.Inset;
            layout.OrbitHeight = layout.CanvasHeight - 2f * layout.Inset;
            layout.OrbitRadiusX = layout.OrbitWidth * 0.5f;
            layout.OrbitRadiusY = layout.OrbitHeight * 0.5f;
            layout.OrbitCenterX = layout.OrbitX + layout.OrbitRadiusX;
            layout.OrbitCenterY = layout.OrbitY + layout.OrbitRadiusY;
            layout.GridX = layout.Pad;
            layout.GridY = layout.Pad;
            layout.Perimeter = 2f * (layout.OrbitWidth + layout.OrbitHeight);
            layout.FirePoints = layout.BuildFirePoints(rows, cols);
            return layout;
        }

        public float CellCenterX(int col)
        {
            return GridX + col * (Cell + Gap) + Cell / 2f;
        }

        public float CellCenterY(int row)
        {
            return GridY + row * (Cell + Gap) + Cell / 2f;
        }

        public Vector2 PathPosition(float distance)
        {
            float d = Mathf.Repeat(distance, Perimeter);
            if (d < OrbitWidth) return new Vector2(OrbitX + d, OrbitY);

            d -= OrbitWidth;
            if (d < OrbitHeight) return new Vector2(OrbitX + OrbitWidth, OrbitY + d);

            d -= OrbitHeight;
            if (d < OrbitWidth) return new Vector2(OrbitX + OrbitWidth - d, OrbitY + OrbitHeight);

            d -= OrbitWidth;
            return new Vector2(OrbitX, OrbitY + OrbitHeight - d);
        }

        private List<FirePoint> BuildFirePoints(int rows, int cols)
        {
            List<FirePoint> points = new List<FirePoint>();
            for (int c = 0; c < cols; c++)
            {
                float distance = CellCenterX(c) - OrbitX;
                points.Add(new FirePoint(FireSide.Top, -1, c, distance));
            }

            for (int r = 0; r < rows; r++)
            {
                float distance = OrbitWidth + (CellCenterY(r) - OrbitY);
                points.Add(new FirePoint(FireSide.Right, r, -1, distance));
            }

            for (int c = 0; c < cols; c++)
            {
                int col = cols - 1 - c;
                float rawDistance = OrbitWidth + OrbitHeight + (OrbitX + OrbitWidth - CellCenterX(col));
                points.Add(new FirePoint(FireSide.Bottom, -1, col, rawDistance));
            }

            for (int r = 0; r < rows; r++)
            {
                int row = rows - 1 - r;
                float rawDistance = 2f * OrbitWidth + OrbitHeight + (OrbitY + OrbitHeight - CellCenterY(row));
                points.Add(new FirePoint(FireSide.Left, row, -1, rawDistance));
            }

            points.Sort((a, b) => a.Distance.CompareTo(b.Distance));
            return points;
        }
    }
}
