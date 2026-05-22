using SquareFlow.Core;
using UnityEngine;

namespace SquareFlow.Runtime
{
    public readonly struct MobileWorldLayout
    {
        public const float DefaultWorldUnitsPerLayoutPixel = 0.0215f;
        public const float DefaultFitMarginWorldUnits = 0.35f;
        public const float DefaultPlayfieldTopWorldY = 8.15f;
        public const float DefaultPlayfieldBottomWorldY = -4.65f;
        public static readonly Vector2 DefaultBoardCenter = new Vector2(0f, 0.75f);

        private readonly BoardLayout board;

        public MobileWorldLayout(BoardLayout board, Vector2 boardCenter, float worldUnitsPerLayoutPixel)
        {
            this.board = board;
            BoardCenter = boardCenter;
            WorldUnitsPerLayoutPixel = Mathf.Max(0.001f, worldUnitsPerLayoutPixel);
        }

        public bool IsValid => board != null;
        public Vector2 BoardCenter { get; }
        public float WorldUnitsPerLayoutPixel { get; }
        public float CellSize => IsValid ? board.Cell * WorldUnitsPerLayoutPixel : 0f;
        public float CanvasWidth => IsValid ? board.CanvasWidth * WorldUnitsPerLayoutPixel : 0f;
        public float CanvasHeight => IsValid ? board.CanvasHeight * WorldUnitsPerLayoutPixel : 0f;
        public Rect CanvasBounds => IsValid
            ? Rect.MinMaxRect(
                BoardCenter.x - CanvasWidth * 0.5f,
                BoardCenter.y - CanvasHeight * 0.5f,
                BoardCenter.x + CanvasWidth * 0.5f,
                BoardCenter.y + CanvasHeight * 0.5f)
            : new Rect(BoardCenter, Vector2.zero);
        public Rect OrbitBounds
        {
            get
            {
                if (!IsValid) return new Rect(BoardCenter, Vector2.zero);

                float minX = BoardCenter.x + (board.OrbitX - board.CanvasWidth * 0.5f) * WorldUnitsPerLayoutPixel;
                float maxX = BoardCenter.x + (board.OrbitX + board.OrbitWidth - board.CanvasWidth * 0.5f) * WorldUnitsPerLayoutPixel;
                float maxY = BoardCenter.y + (board.CanvasHeight * 0.5f - board.OrbitY) * WorldUnitsPerLayoutPixel;
                float minY = BoardCenter.y + (board.CanvasHeight * 0.5f - board.OrbitY - board.OrbitHeight) * WorldUnitsPerLayoutPixel;
                return Rect.MinMaxRect(minX, minY, maxX, maxY);
            }
        }

        public static MobileWorldLayout Create(BoardLayout board)
        {
            return new MobileWorldLayout(board, DefaultBoardCenter, DefaultWorldUnitsPerLayoutPixel);
        }

        public static MobileWorldLayout Create(BoardLayout board, Rect visibleWorldRect)
        {
            return CreateFitting(board, PlayfieldRect(visibleWorldRect), DefaultFitMarginWorldUnits);
        }

        public static MobileWorldLayout CreateFitting(BoardLayout board, Rect fitRect, float marginWorldUnits)
        {
            if (board == null)
                return new MobileWorldLayout(null, fitRect.center, DefaultWorldUnitsPerLayoutPixel);

            float width = Mathf.Max(0.1f, fitRect.width - marginWorldUnits * 2f);
            float height = Mathf.Max(0.1f, fitRect.height - marginWorldUnits * 2f);
            float scale = Mathf.Min(width / board.CanvasWidth, height / board.CanvasHeight);
            return new MobileWorldLayout(board, fitRect.center, scale);
        }

        public static Rect PlayfieldRect(Rect visibleWorldRect)
        {
            float yMin = Mathf.Max(visibleWorldRect.yMin, DefaultPlayfieldBottomWorldY);
            float yMax = Mathf.Min(visibleWorldRect.yMax, DefaultPlayfieldTopWorldY);
            if (yMax <= yMin + 1f)
                return visibleWorldRect;

            return Rect.MinMaxRect(visibleWorldRect.xMin, yMin, visibleWorldRect.xMax, yMax);
        }

        public Vector2 CellCenter(int col, int row)
        {
            if (!IsValid) return BoardCenter;
            return ToWorld(new Vector2(
                board.CellCenterX(col) - board.CanvasWidth * 0.5f,
                board.CanvasHeight * 0.5f - board.CellCenterY(row)));
        }

        public Vector2 PathPosition(float distance)
        {
            if (!IsValid) return BoardCenter;
            Vector2 point = board.PathPosition(distance);
            return ToWorld(new Vector2(
                point.x - board.CanvasWidth * 0.5f,
                board.CanvasHeight * 0.5f - point.y));
        }

        public Vector2 EventTarget(GameEvent gameEvent)
        {
            return CellCenter(gameEvent.Col, gameEvent.Row);
        }

        public bool TryFirePoint(GameEvent gameEvent, out Vector2 worldPosition)
        {
            if (!IsValid || !gameEvent.HasFirePoint)
            {
                worldPosition = BoardCenter;
                return false;
            }

            for (int i = 0; i < board.FirePoints.Count; i++)
            {
                FirePoint point = board.FirePoints[i];
                if (point.Side != gameEvent.FireSide || point.Row != gameEvent.FireRow || point.Col != gameEvent.FireCol)
                    continue;

                worldPosition = PathPosition(point.Distance);
                return true;
            }

            worldPosition = BoardCenter;
            return false;
        }

        private Vector2 ToWorld(Vector2 boardAnchoredPoint)
        {
            return BoardCenter + boardAnchoredPoint * WorldUnitsPerLayoutPixel;
        }
    }
}
