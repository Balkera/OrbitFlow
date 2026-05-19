using System.Collections.Generic;
using SquareFlow.Core;
using SquareFlow.UI;
using UnityEngine;

namespace SquareFlow.Runtime
{
    public sealed class BoardWorldView : MonoBehaviour
    {
        private readonly List<CellView> cells = new List<CellView>();
        private BoardShape boundShape;
        private BoardLayout boundBoard;
        private MobileWorldLayout boundWorld;

        public void Bind(GameState state, BoardLayout board, MobileWorldLayout world, SquareFlowTheme theme)
        {
            if (state == null || board == null || !world.IsValid)
            {
                Clear();
                return;
            }

            bool needsRebuild = boundShape == null
                || boundShape.Rows != state.Shape.Rows
                || boundShape.Cols != state.Shape.Cols
                || cells.Count != state.Shape.ActiveCellCount()
                || cells.Count == 0;

            if (needsRebuild)
                Rebuild(state);

            boundShape = state.Shape;
            boundBoard = board;
            boundWorld = world;
            RefreshCells(state, theme);
        }

        public void RefreshCells(GameState state, SquareFlowTheme theme)
        {
            if (state == null || boundBoard == null || !boundWorld.IsValid) return;

            for (int i = 0; i < cells.Count; i++)
            {
                CellView cell = cells[i];
                BoardCell boardCell = state.Grid[cell.Row, cell.Col];
                bool visible = state.Shape.IsActive(cell.Row, cell.Col);
                cell.Root.SetActive(visible);
                if (!visible) continue;

                Vector2 center = boundWorld.CellCenter(cell.Col, cell.Row);
                cell.Root.transform.position = new Vector3(center.x, center.y, 0f);
                float tileSize = boundWorld.CellSize * SquareFlowVisualMetrics.TileFaceScale;
                cell.Face.transform.localScale = Vector3.one * tileSize;
                cell.Depth.transform.localScale = Vector3.one * tileSize;
                cell.Depth.transform.localPosition = new Vector3(0f, -boundWorld.CellSize * SquareFlowVisualMetrics.TileDepthDropScale, 0.04f);
                cell.Highlight.transform.localScale = new Vector3(tileSize * 0.72f, Mathf.Max(0.03f, tileSize * 0.08f), 1f);
                cell.Highlight.transform.localPosition = new Vector3(0f, tileSize * 0.3f, -0.04f);
                cell.Label.transform.localPosition = new Vector3(0f, 0f, -0.08f);
                cell.Label.characterSize = Mathf.Max(0.16f, boundWorld.CellSize * 0.32f);
                cell.Face.color = CellColor(boardCell, theme);
                cell.Depth.color = boardCell.IsOccupied ? LerpColor(cell.Face.color, Color.black, SquareFlowVisualMetrics.TileDepthDarkenAmount) : Color.clear;
                cell.Highlight.color = boardCell.IsOccupied ? ColorWithAlpha(Color.white, SquareFlowVisualMetrics.TileTopHighlightAlpha) : Color.clear;
                cell.Label.text = LabelForCell(boardCell);
                cell.Label.color = boardCell.Type == BoardCellType.Bomb || boardCell.Color == FlowColor.Yellow ? new Color32(26, 23, 64, 255) : Color.white;
            }
        }

        public void Clear()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                DestroyImmediateOrRuntime(transform.GetChild(i).gameObject);

            cells.Clear();
            boundShape = null;
            boundBoard = null;
            boundWorld = default;
        }

        private void Rebuild(GameState state)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                DestroyImmediateOrRuntime(transform.GetChild(i).gameObject);

            cells.Clear();
            boundShape = state.Shape;

            for (int r = 0; r < state.Shape.Rows; r++)
            for (int c = 0; c < state.Shape.Cols; c++)
            {
                if (!state.Shape.IsActive(r, c)) continue;
                cells.Add(CreateCell(r, c));
            }
        }

        private CellView CreateCell(int row, int col)
        {
            GameObject root = new GameObject("WorldCell_" + row + "_" + col);
            root.transform.SetParent(transform, false);

            SpriteRenderer depth = CreateRenderer(root.transform, "Depth", SquareFlowWorldSprites.RoundedRect, 2);
            SpriteRenderer face = CreateRenderer(root.transform, "Face", SquareFlowWorldSprites.RoundedRect, 1);
            SpriteRenderer highlight = CreateRenderer(root.transform, "Highlight", SquareFlowWorldSprites.Square, 0);
            TextMesh label = CreateLabel(root.transform);

            return new CellView(row, col, root, depth, face, highlight, label);
        }

        private static SpriteRenderer CreateRenderer(Transform parent, string name, Sprite sprite, int order)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return renderer;
        }

        private static TextMesh CreateLabel(Transform parent)
        {
            GameObject go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            TextMesh label = go.AddComponent<TextMesh>();
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = 64;
            MeshRenderer renderer = label.GetComponent<MeshRenderer>();
            renderer.sortingOrder = 7;
            return label;
        }

        private static Color CellColor(BoardCell cell, SquareFlowTheme theme)
        {
            if (!cell.IsOccupied) return theme.CellEmpty;
            if (cell.Type == BoardCellType.Bomb) return theme.Bomb;

            switch (cell.Color)
            {
                case FlowColor.Blue:
                    return theme.Blue;
                case FlowColor.Yellow:
                    return theme.Yellow;
                case FlowColor.Green:
                    return theme.Green;
                default:
                    return theme.Red;
            }
        }

        private static string LabelForCell(BoardCell cell)
        {
            if (cell.Type == BoardCellType.Bomb) return "*";
            if (cell.Type == BoardCellType.Normal && cell.Hp > 1) return cell.Hp.ToString();
            return string.Empty;
        }

        private static Color LerpColor(Color from, Color to, float t)
        {
            return new Color(
                Mathf.Lerp(from.r, to.r, t),
                Mathf.Lerp(from.g, to.g, t),
                Mathf.Lerp(from.b, to.b, t),
                from.a);
        }

        private static Color ColorWithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private static void DestroyImmediateOrRuntime(GameObject go)
        {
            if (Application.isPlaying)
                Destroy(go);
            else
                DestroyImmediate(go);
        }

        private readonly struct CellView
        {
            public CellView(int row, int col, GameObject root, SpriteRenderer depth, SpriteRenderer face, SpriteRenderer highlight, TextMesh label)
            {
                Row = row;
                Col = col;
                Root = root;
                Depth = depth;
                Face = face;
                Highlight = highlight;
                Label = label;
            }

            public int Row { get; }
            public int Col { get; }
            public GameObject Root { get; }
            public SpriteRenderer Depth { get; }
            public SpriteRenderer Face { get; }
            public SpriteRenderer Highlight { get; }
            public TextMesh Label { get; }
        }
    }
}
