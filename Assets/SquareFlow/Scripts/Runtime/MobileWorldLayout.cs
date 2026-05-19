using SquareFlow.Core;
using UnityEngine;

namespace SquareFlow.Runtime
{
    public readonly struct MobileWorldLayout
    {
        public const float DefaultWorldUnitsPerLayoutPixel = 0.01f;
        public static readonly Vector2 DefaultBoardCenter = new Vector2(0f, 0.85f);

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

        public static MobileWorldLayout Create(BoardLayout board)
        {
            return new MobileWorldLayout(board, DefaultBoardCenter, DefaultWorldUnitsPerLayoutPixel);
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
