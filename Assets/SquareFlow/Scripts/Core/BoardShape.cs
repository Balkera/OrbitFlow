using System;

namespace SquareFlow.Core
{
    public sealed class BoardShape
    {
        private readonly bool[,] cells;

        public BoardShape(string name, bool[,] cells)
        {
            Name = name;
            this.cells = cells;
            Rows = cells.GetLength(0);
            Cols = cells.GetLength(1);
        }

        public string Name { get; }
        public int Rows { get; }
        public int Cols { get; }

        public bool IsActive(int row, int col)
        {
            return row >= 0 && row < Rows && col >= 0 && col < Cols && cells[row, col];
        }

        public int ActiveCellCount()
        {
            int count = 0;
            for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Cols; c++)
                if (cells[r, c]) count++;
            return count;
        }

        public bool[,] CopyCells()
        {
            return (bool[,])cells.Clone();
        }

        public static bool[,] Mask(params int[][] rows)
        {
            if (rows.Length == 0) throw new ArgumentException("Shape needs at least one row.", nameof(rows));
            int cols = rows[0].Length;
            bool[,] mask = new bool[rows.Length, cols];
            for (int r = 0; r < rows.Length; r++)
            {
                if (rows[r].Length != cols) throw new ArgumentException("Shape rows need equal widths.", nameof(rows));
                for (int c = 0; c < cols; c++) mask[r, c] = rows[r][c] != 0;
            }
            return mask;
        }
    }
}
