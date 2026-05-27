using System.Collections.Generic;
using TMPro;
using SquareFlow.Core;
using SquareFlow.UI;
using UnityEngine;

namespace SquareFlow.Runtime
{
    public sealed class BoardWorldView : MonoBehaviour
    {
        private const int DepthSortingOrder = 0;
        private const int FaceSortingOrder = 1;
        private const int HighlightSortingOrder = 2;
        private const int LabelSortingOrder = 7;
        private const int HitFlashSortingOrder = 8;

        private readonly List<CellView> cells = new List<CellView>();
        private BoardShape boundShape;
        private string boundActiveMaskSignature;
        private BoardLayout boundBoard;
        private MobileWorldLayout boundWorld;

        private void Update()
        {
            UpdateHitFeedback();
        }

        public void Bind(GameState state, BoardLayout board, MobileWorldLayout world, SquareFlowTheme theme)
        {
            if (state == null || board == null || !world.IsValid)
            {
                Clear();
                return;
            }

            string activeMaskSignature = ActiveMaskSignature(state.Shape);
            bool needsRebuild = boundShape == null
                || boundShape.Rows != state.Shape.Rows
                || boundShape.Cols != state.Shape.Cols
                || cells.Count != state.Shape.ActiveCellCount()
                || boundActiveMaskSignature != activeMaskSignature
                || cells.Count == 0;

            if (needsRebuild)
                Rebuild(state);

            boundShape = state.Shape;
            boundActiveMaskSignature = activeMaskSignature;
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
                cell.Label.transform.localPosition = new Vector3(0f, 0.05f, -0.08f);
                cell.Label.fontSize = SquareFlowVisualMetrics.CellLabelFontSize;
                cell.Label.rectTransform.sizeDelta = Vector2.one * tileSize * 1.25f;
                cell.HitFlash.transform.localScale = Vector3.one * tileSize;
                cell.HitFlash.transform.localPosition = new Vector3(0f, 0f, -0.1f);
                cell.BasePosition = center;
                Sprite faceSprite = SquareFlowWorldSprites.BlockForCell(boardCell);
                bool usesTexturedSprite = faceSprite != SquareFlowWorldSprites.RoundedRect;
                cell.Face.sprite = faceSprite;
                cell.Face.color = usesTexturedSprite ? Color.white : CellColor(boardCell, theme);
                cell.Depth.gameObject.SetActive(boardCell.IsOccupied && !usesTexturedSprite);
                cell.Highlight.gameObject.SetActive(boardCell.IsOccupied && !usesTexturedSprite);
                cell.Depth.color = boardCell.IsOccupied ? LerpColor(cell.Face.color, Color.black, SquareFlowVisualMetrics.TileDepthDarkenAmount) : Color.clear;
                cell.Highlight.color = boardCell.IsOccupied ? ColorWithAlpha(Color.white, SquareFlowVisualMetrics.TileTopHighlightAlpha) : Color.clear;
                cell.Label.text = LabelForCell(boardCell);
                cell.Label.color = boardCell.Type == BoardCellType.Bomb || boardCell.Color == FlowColor.Yellow ? new Color32(26, 23, 64, 255) : Color.white;
                cell.BaseFaceColor = cell.Face.color;
            }
        }

        public bool PlayHitFeedback(int row, int col, bool heavyImpact)
        {
            CellView cell = FindCell(row, col);
            if (cell == null || !cell.Root.activeSelf) return false;

            cell.HitElapsed = 0f;
            cell.HitDuration = SquareFlowVisualMetrics.CellHitFeedbackDurationSeconds;
            cell.HitStrength = heavyImpact ? SquareFlowVisualMetrics.CellHitHeavyShakeMultiplier : 1f;
            cell.HitPhase = (row * 23f + col * 37f) * 0.19f;
            ApplyHitFeedback(cell, 0f);
            return true;
        }

        public void Clear()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                DestroyImmediateOrRuntime(transform.GetChild(i).gameObject);

            cells.Clear();
            boundShape = null;
            boundActiveMaskSignature = null;
            boundBoard = null;
            boundWorld = default;
        }

        private void Rebuild(GameState state)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                DestroyImmediateOrRuntime(transform.GetChild(i).gameObject);

            cells.Clear();
            boundShape = state.Shape;
            boundActiveMaskSignature = ActiveMaskSignature(state.Shape);

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

            SpriteRenderer depth = CreateRenderer(root.transform, "Depth", SquareFlowWorldSprites.RoundedRect, DepthSortingOrder);
            SpriteRenderer face = CreateRenderer(root.transform, "Face", SquareFlowWorldSprites.RoundedRect, FaceSortingOrder);
            SpriteRenderer highlight = CreateRenderer(root.transform, "Highlight", SquareFlowWorldSprites.Square, HighlightSortingOrder);
            TextMeshPro label = CreateLabel(root.transform);
            SpriteRenderer hitFlash = CreateRenderer(root.transform, "HitFlash", SquareFlowWorldSprites.RoundedRect, HitFlashSortingOrder);
            hitFlash.gameObject.SetActive(false);

            return new CellView(row, col, root, depth, face, highlight, label, hitFlash);
        }

        private CellView FindCell(int row, int col)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                CellView cell = cells[i];
                if (cell.Row == row && cell.Col == col)
                    return cell;
            }

            return null;
        }

        private void UpdateHitFeedback()
        {
            for (int i = 0; i < cells.Count; i++)
            {
                CellView cell = cells[i];
                if (cell.HitDuration <= 0f) continue;

                cell.HitElapsed += Time.deltaTime;
                if (cell.HitElapsed >= cell.HitDuration)
                {
                    ResetHitFeedback(cell);
                    continue;
                }

                ApplyHitFeedback(cell, cell.HitElapsed / cell.HitDuration);
            }
        }

        private void ApplyHitFeedback(CellView cell, float progress)
        {
            float t = Mathf.Clamp01(progress);
            float intensity = 1f - t;
            float shakeSize = boundWorld.IsValid
                ? boundWorld.CellSize * SquareFlowVisualMetrics.CellHitShakeAmplitudeScale
                : 0.05f;
            float shake = shakeSize * cell.HitStrength * intensity;
            float angle = cell.HitPhase + cell.HitElapsed * SquareFlowVisualMetrics.CellHitShakeFrequency;
            Vector2 offset = new Vector2(Mathf.Sin(angle * 1.7f), Mathf.Cos(angle * 2.3f)) * shake;

            cell.Root.transform.position = new Vector3(cell.BasePosition.x + offset.x, cell.BasePosition.y + offset.y, 0f);
            cell.Face.color = LerpColor(cell.BaseFaceColor, Color.white, SquareFlowVisualMetrics.CellHitFaceFlashAmount * intensity);
            cell.HitFlash.gameObject.SetActive(true);
            cell.HitFlash.color = ColorWithAlpha(Color.white, SquareFlowVisualMetrics.CellHitFlashAlpha * intensity);
        }

        private void ResetHitFeedback(CellView cell)
        {
            cell.HitDuration = 0f;
            cell.HitElapsed = 0f;
            cell.Root.transform.position = new Vector3(cell.BasePosition.x, cell.BasePosition.y, 0f);
            cell.Face.color = cell.BaseFaceColor;
            cell.HitFlash.gameObject.SetActive(false);
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

        private static TextMeshPro CreateLabel(Transform parent)
        {
            GameObject go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            TextMeshPro label = go.AddComponent<TextMeshPro>();
            label.alignment = TextAlignmentOptions.Center;
            label.fontStyle = FontStyles.Bold;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;
            MeshRenderer renderer = label.GetComponent<MeshRenderer>();
            renderer.sortingOrder = LabelSortingOrder;
            return label;
        }

        private static string ActiveMaskSignature(BoardShape shape)
        {
            char[] signature = new char[shape.Rows * shape.Cols];
            int index = 0;
            for (int r = 0; r < shape.Rows; r++)
            for (int c = 0; c < shape.Cols; c++)
                signature[index++] = shape.IsActive(r, c) ? '1' : '0';

            return new string(signature);
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
            go.SetActive(false);
            if (Application.isPlaying)
            {
                go.transform.SetParent(null, false);
                Destroy(go);
            }
            else
                DestroyImmediate(go);
        }

        private sealed class CellView
        {
            public CellView(int row, int col, GameObject root, SpriteRenderer depth, SpriteRenderer face, SpriteRenderer highlight, TextMeshPro label, SpriteRenderer hitFlash)
            {
                Row = row;
                Col = col;
                Root = root;
                Depth = depth;
                Face = face;
                Highlight = highlight;
                Label = label;
                HitFlash = hitFlash;
            }

            public int Row { get; }
            public int Col { get; }
            public GameObject Root { get; }
            public SpriteRenderer Depth { get; }
            public SpriteRenderer Face { get; }
            public SpriteRenderer Highlight { get; }
            public TextMeshPro Label { get; }
            public SpriteRenderer HitFlash { get; }
            public Vector2 BasePosition { get; set; }
            public Color BaseFaceColor { get; set; }
            public float HitElapsed { get; set; }
            public float HitDuration { get; set; }
            public float HitStrength { get; set; }
            public float HitPhase { get; set; }
        }
    }
}
