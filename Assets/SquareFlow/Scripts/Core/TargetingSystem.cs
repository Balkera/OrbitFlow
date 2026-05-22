namespace SquareFlow.Core
{
    public enum TargetSpecial
    {
        None,
        Bomb
    }

    public readonly struct TargetHit
    {
        public TargetHit(int row, int col, TargetSpecial special)
        {
            Row = row;
            Col = col;
            Special = special;
        }

        public int Row { get; }
        public int Col { get; }
        public TargetSpecial Special { get; }
    }

    public static class TargetingSystem
    {
        public static TargetHit? GetTarget(BoardCell[,] grid, BoardShape shape, FirePoint point, FlowColor color, bool wild)
        {
            if (point.Side == FireSide.Top)
            {
                for (int r = 0; r < shape.Rows; r++)
                {
                    TargetHit? hit = Check(grid, shape, r, point.Col, color, wild);
                    if (hit.HasValue || BlocksLine(grid, shape, r, point.Col)) return hit;
                }
            }
            else if (point.Side == FireSide.Bottom)
            {
                for (int r = shape.Rows - 1; r >= 0; r--)
                {
                    TargetHit? hit = Check(grid, shape, r, point.Col, color, wild);
                    if (hit.HasValue || BlocksLine(grid, shape, r, point.Col)) return hit;
                }
            }
            else if (point.Side == FireSide.Right)
            {
                for (int c = shape.Cols - 1; c >= 0; c--)
                {
                    TargetHit? hit = Check(grid, shape, point.Row, c, color, wild);
                    if (hit.HasValue || BlocksLine(grid, shape, point.Row, c)) return hit;
                }
            }
            else
            {
                for (int c = 0; c < shape.Cols; c++)
                {
                    TargetHit? hit = Check(grid, shape, point.Row, c, color, wild);
                    if (hit.HasValue || BlocksLine(grid, shape, point.Row, c)) return hit;
                }
            }

            return null;
        }

        private static bool BlocksLine(BoardCell[,] grid, BoardShape shape, int row, int col)
        {
            return shape.IsActive(row, col) && grid[row, col].IsOccupied;
        }

        private static TargetHit? Check(BoardCell[,] grid, BoardShape shape, int row, int col, FlowColor color, bool wild)
        {
            if (!shape.IsActive(row, col)) return null;

            BoardCell cell = grid[row, col];
            if (!cell.IsOccupied) return null;
            if (cell.Type == BoardCellType.Bomb) return new TargetHit(row, col, TargetSpecial.Bomb);
            if (cell.Type == BoardCellType.Normal && (wild || cell.Color == color)) return new TargetHit(row, col, TargetSpecial.None);
            return null;
        }
    }
}
