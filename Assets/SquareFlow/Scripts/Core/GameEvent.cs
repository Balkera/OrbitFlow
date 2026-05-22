namespace SquareFlow.Core
{
    public enum GameEventType
    {
        Fired,
        BlockDamaged,
        BlockDestroyed,
        BombDetonated,
        OrbiterQueued,
        OrbiterRemoved,
        ResultChanged,
        Blocked
    }

    public readonly struct GameEvent
    {
        public GameEvent(
            GameEventType type,
            int row = -1,
            int col = -1,
            string orbiterId = null,
            int score = 0,
            FireSide? fireSide = null,
            int fireRow = -1,
            int fireCol = -1)
        {
            Type = type;
            Row = row;
            Col = col;
            OrbiterId = orbiterId;
            Score = score;
            HasFirePoint = fireSide.HasValue;
            FireSide = fireSide.GetValueOrDefault();
            FireRow = fireRow;
            FireCol = fireCol;
        }

        public GameEventType Type { get; }
        public int Row { get; }
        public int Col { get; }
        public string OrbiterId { get; }
        public int Score { get; }
        public bool HasFirePoint { get; }
        public FireSide FireSide { get; }
        public int FireRow { get; }
        public int FireCol { get; }
    }
}
