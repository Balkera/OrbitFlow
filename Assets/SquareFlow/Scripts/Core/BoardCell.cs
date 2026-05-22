using System;

namespace SquareFlow.Core
{
    public enum BoardCellType
    {
        Empty,
        Normal,
        Bomb
    }

    [Serializable]
    public struct BoardCell
    {
        public BoardCellType Type { get; private set; }
        public FlowColor Color { get; private set; }
        public int Hp { get; private set; }
        public bool IsOccupied => Type != BoardCellType.Empty;

        private BoardCell(BoardCellType type, FlowColor color, int hp)
        {
            Type = type;
            Color = color;
            Hp = hp;
        }

        public static BoardCell Empty => new BoardCell(BoardCellType.Empty, FlowColor.Red, 0);

        public static BoardCell Normal(FlowColor color, int hp)
        {
            if (color == FlowColor.Wild) throw new ArgumentException("Normal cells need a normal color.", nameof(color));
            if (hp < 1) throw new ArgumentOutOfRangeException(nameof(hp), "HP must be positive.");
            return new BoardCell(BoardCellType.Normal, color, hp);
        }

        public static BoardCell Bomb()
        {
            return new BoardCell(BoardCellType.Bomb, FlowColor.Wild, 1);
        }

        public BoardCell WithHp(int hp)
        {
            return Type == BoardCellType.Normal ? Normal(Color, hp) : this;
        }
    }
}
