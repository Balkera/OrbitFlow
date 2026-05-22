using System.Collections.Generic;

namespace SquareFlow.Core
{
    public sealed class GameState
    {
        private GameState()
        {
        }

        public BoardShape Shape { get; private set; }
        public BoardCell[,] Grid { get; private set; }
        public List<Shooter>[] ShooterColumns { get; private set; }
        public List<Shooter> WaitingQueue { get; } = new List<Shooter>();
        public List<ActiveOrbiter> ActiveOrbiters { get; } = new List<ActiveOrbiter>();
        public List<GameEvent> Events { get; } = new List<GameEvent>();
        public int Level { get; private set; }
        public int Moves { get; set; }
        public int Score { get; set; }
        public float Combo { get; set; } = 1f;
        public float ComboTimer { get; set; }
        public GameResult Result { get; set; }

        public static GameState Create(BoardShape shape, BoardCell[,] grid, List<Shooter>[] shooterColumns, int level)
        {
            return new GameState
            {
                Shape = shape,
                Grid = grid,
                ShooterColumns = shooterColumns,
                Level = level,
                Result = GameResult.None
            };
        }

        public bool AnyBlocksRemaining()
        {
            foreach (BoardCell cell in Grid)
                if (cell.IsOccupied)
                    return true;
            return false;
        }

        public bool HasAvailableShooters()
        {
            if (ActiveOrbiters.Count > 0 || WaitingQueue.Count > 0) return true;
            for (int i = 0; i < ShooterColumns.Length; i++)
                if (ShooterColumns[i].Count > 0)
                    return true;
            return false;
        }
    }
}
