using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SquareFlow.Core;

namespace SquareFlow.Tests
{
    public sealed class GameRulesTests
    {
        [Test]
        public void FireFromColumnRespectsMaxActiveLimit()
        {
            GameState state = MakeStateWithColumnShooters(6);
            GameRules rules = new GameRules(state, BoardLayout.Compute(1, 1, 320));

            for (int i = 0; i < SquareFlowConstants.MaxActiveOrbiters; i++)
                Assert.That(rules.FireFromColumn(0), Is.True);

            Assert.That(rules.FireFromColumn(0), Is.False);
            Assert.That(state.ActiveOrbiters.Count, Is.EqualTo(SquareFlowConstants.MaxActiveOrbiters));
        }

        [Test]
        public void FireFromWaitingIncrementsMovesAndRemovesQueueItem()
        {
            GameState state = MakeStateWithColumnShooters(0);
            state.WaitingQueue.Add(new Shooter("w0", FlowColor.Red, 1, false));
            state.WaitingQueue.Add(new Shooter("w1", FlowColor.Blue, 2, false));
            GameRules rules = new GameRules(state, BoardLayout.Compute(1, 1, 320));

            bool fired = rules.FireFromWaiting(1);

            Assert.That(fired, Is.True);
            Assert.That(state.Moves, Is.EqualTo(1));
            Assert.That(state.WaitingQueue.Select(x => x.Id), Is.EqualTo(new[] { "w0" }));
            Assert.That(state.ActiveOrbiters.Single().Id, Is.EqualTo("w1"));
        }

        [Test]
        public void FireFromColumnRecordsFiredEvent()
        {
            GameState state = MakeStateWithColumnShooters(1);
            GameRules rules = new GameRules(state, BoardLayout.Compute(1, 1, 320));

            Assert.That(rules.FireFromColumn(0), Is.True);

            GameEvent fired = state.Events.Single(x => x.Type == GameEventType.Fired);
            Assert.That(fired.OrbiterId, Is.EqualTo("s0"));
        }

        [Test]
        public void FireFromWaitingRecordsFiredEvent()
        {
            GameState state = MakeStateWithColumnShooters(0);
            state.WaitingQueue.Add(new Shooter("w0", FlowColor.Red, 1, false));
            GameRules rules = new GameRules(state, BoardLayout.Compute(1, 1, 320));

            Assert.That(rules.FireFromWaiting(0), Is.True);

            GameEvent fired = state.Events.Single(x => x.Type == GameEventType.Fired);
            Assert.That(fired.OrbiterId, Is.EqualTo("w0"));
        }

        [Test]
        public void ActiveOrbiterCanHitFirePointAtStartDistance()
        {
            BoardShape shape = new BoardShape("One", BoardShape.Mask(new[] { 1 }));
            BoardCell[,] grid = { { BoardCell.Normal(FlowColor.Red, 1) } };
            GameState state = GameState.Create(shape, grid, EmptyColumns(), 1);
            BoardLayout layout = BoardLayout.Compute(1, 1, 320);
            FirePoint startPoint = layout.FirePoints.Single(x => x.Distance < 0.001f);
            ActiveOrbiter orbiter = new ActiveOrbiter(new Shooter("s0", FlowColor.Red, 1, false));
            state.ActiveOrbiters.Add(orbiter);
            GameRules rules = new GameRules(state, layout);

            List<GameEvent> events = rules.Advance(1f / SquareFlowConstants.Speed);

            Assert.That(startPoint.Side, Is.EqualTo(FireSide.Bottom));
            Assert.That(events.Any(x => x.Type == GameEventType.BlockDestroyed), Is.True);
            Assert.That(state.Grid[0, 0].IsOccupied, Is.False);
        }

        [Test]
        public void FireRejectedByFullActiveSlotsRecordsBlockedEvent()
        {
            GameState state = MakeStateWithColumnShooters(1);
            for (int i = 0; i < SquareFlowConstants.MaxActiveOrbiters; i++)
                state.ActiveOrbiters.Add(new ActiveOrbiter(new Shooter("a" + i, FlowColor.Red, 1, false)));
            GameRules rules = new GameRules(state, BoardLayout.Compute(1, 1, 320));

            Assert.That(rules.FireFromColumn(0), Is.False);

            Assert.That(state.Events.Count(x => x.Type == GameEventType.Blocked), Is.EqualTo(1));
        }

        [Test]
        public void CompletedOrbiterWithLeftoverAmmoEntersWaitingQueueAfterAdvance()
        {
            BoardShape shape = new BoardShape("One", BoardShape.Mask(new[] { 1 }));
            GameState state = GameState.Create(shape, new[,] { { BoardCell.Empty } }, EmptyColumns(), 1);
            state.ActiveOrbiters.Add(new ActiveOrbiter(new Shooter("s0", FlowColor.Red, 2, false)));
            BoardLayout layout = BoardLayout.Compute(1, 1, 320);
            GameRules rules = new GameRules(state, layout);

            rules.Advance(layout.Perimeter / SquareFlowConstants.Speed);

            Assert.That(state.ActiveOrbiters, Is.Empty);
            Assert.That(state.WaitingQueue.Count, Is.EqualTo(1));
            Assert.That(state.WaitingQueue[0].Ammo, Is.EqualTo(2));
            Assert.That(state.Events.Any(x => x.Type == GameEventType.OrbiterQueued), Is.True);
        }

        [Test]
        public void NonmatchingBlockerPreventsHitsThroughAdvance()
        {
            BoardShape shape = new BoardShape("Line", BoardShape.Mask(new[] { 1, 1 }));
            BoardCell[,] grid = { { BoardCell.Normal(FlowColor.Blue, 1), BoardCell.Normal(FlowColor.Red, 1) } };
            GameState state = GameState.Create(shape, grid, EmptyColumns(), 1);
            ActiveOrbiter orbiter = new ActiveOrbiter(new Shooter("s0", FlowColor.Red, 1, false));
            BoardLayout layout = BoardLayout.Compute(1, 2, 320);
            FirePoint leftPoint = layout.FirePoints.Single(x => x.Side == FireSide.Left && x.Row == 0);
            orbiter.Distance = leftPoint.Distance - 1f;
            state.ActiveOrbiters.Add(orbiter);
            GameRules rules = new GameRules(state, layout);

            List<GameEvent> events = rules.Advance(2f / SquareFlowConstants.Speed);

            Assert.That(events.Any(x => x.Type == GameEventType.BlockDestroyed), Is.False);
            Assert.That(state.Grid[0, 0].IsOccupied, Is.True);
            Assert.That(state.Grid[0, 1].IsOccupied, Is.True);
            Assert.That(orbiter.Ammo, Is.EqualTo(1));
        }

        [Test]
        public void NormalHitDamagesThenDestroyedHitScores()
        {
            BoardShape shape = new BoardShape("One", BoardShape.Mask(new[] { 1 }));
            BoardCell[,] grid = { { BoardCell.Normal(FlowColor.Red, 2) } };
            GameState state = GameState.Create(shape, grid, EmptyColumns(), 2);
            BoardLayout layout = BoardLayout.Compute(1, 1, 320);
            ActiveOrbiter orbiter = new ActiveOrbiter(new Shooter("s0", FlowColor.Red, 2, false));
            orbiter.Distance = layout.FirePoints[0].Distance - 1f;
            state.ActiveOrbiters.Add(orbiter);
            GameRules rules = new GameRules(state, layout);

            rules.Advance(2f / SquareFlowConstants.Speed);
            Assert.That(state.Grid[0, 0].Hp, Is.EqualTo(1));
            Assert.That(state.Score, Is.EqualTo(0));

            FirePoint nextPoint = layout.FirePoints.First(x => x.Distance > orbiter.Distance);
            orbiter.Distance = nextPoint.Distance - 1f;
            rules.Advance(2f / SquareFlowConstants.Speed);

            Assert.That(state.Grid[0, 0].IsOccupied, Is.False);
            Assert.That(state.Score, Is.EqualTo(200));
        }

        [Test]
        public void HitEventsIncludeFirePointUsedForShot()
        {
            BoardShape shape = new BoardShape("Line", BoardShape.Mask(new[] { 1, 1 }));
            BoardCell[,] grid = { { BoardCell.Normal(FlowColor.Red, 1), BoardCell.Empty } };
            GameState state = GameState.Create(shape, grid, EmptyColumns(), 1);
            BoardLayout layout = BoardLayout.Compute(1, 2, 320);
            FirePoint point = layout.FirePoints.Single(x => x.Side == FireSide.Top && x.Col == 0);
            ActiveOrbiter orbiter = new ActiveOrbiter(new Shooter("s0", FlowColor.Red, 1, false));
            orbiter.Distance = point.Distance - 1f;
            state.ActiveOrbiters.Add(orbiter);
            GameRules rules = new GameRules(state, layout);

            List<GameEvent> events = rules.Advance(2f / SquareFlowConstants.Speed);

            GameEvent hit = events.Single(x => x.Type == GameEventType.BlockDestroyed);
            Assert.That(hit.HasFirePoint, Is.True);
            Assert.That(hit.FireSide, Is.EqualTo(FireSide.Top));
            Assert.That(hit.FireRow, Is.EqualTo(-1));
            Assert.That(hit.FireCol, Is.EqualTo(0));
        }

        [Test]
        public void BombHitClearsNeighborsAndScores()
        {
            BoardShape shape = new BoardShape("Block", BoardShape.Mask(new[] { 1, 1, 1 }, new[] { 1, 1, 1 }, new[] { 1, 1, 1 }));
            BoardCell[,] grid = FilledGrid(3, 3, FlowColor.Red, 1);
            grid[0, 0] = BoardCell.Bomb();
            GameState state = GameState.Create(shape, grid, EmptyColumns(), 1);
            BoardLayout layout = BoardLayout.Compute(3, 3, 400);
            FirePoint point = layout.FirePoints.Single(x => x.Side == FireSide.Top && x.Col == 0);
            ActiveOrbiter orbiter = new ActiveOrbiter(new Shooter("s0", FlowColor.Blue, 1, false));
            orbiter.Distance = point.Distance - 1f;
            state.ActiveOrbiters.Add(orbiter);
            GameRules rules = new GameRules(state, layout);

            rules.Advance(2f / SquareFlowConstants.Speed);

            Assert.That(state.Grid[0, 0].IsOccupied, Is.False);
            Assert.That(state.Grid[0, 1].IsOccupied, Is.False);
            Assert.That(state.Grid[1, 0].IsOccupied, Is.False);
            Assert.That(state.Grid[1, 1].IsOccupied, Is.False);
            Assert.That(state.Score, Is.EqualTo(350));
        }

        [Test]
        public void BombClearsCenterAndNeighbors()
        {
            BoardShape shape = new BoardShape("Block", BoardShape.Mask(new[] { 1, 1, 1 }, new[] { 1, 1, 1 }, new[] { 1, 1, 1 }));
            BoardCell[,] grid = FilledGrid(3, 3, FlowColor.Red, 1);
            grid[1, 1] = BoardCell.Bomb();

            GameState state = GameState.Create(shape, grid, EmptyColumns(), 1);
            GameRules rules = new GameRules(state, BoardLayout.Compute(3, 3, 400));

            int cleared = rules.DetonateBomb(1, 1);

            Assert.That(cleared, Is.EqualTo(9));
            foreach (BoardCell cell in state.Grid) Assert.That(cell.IsOccupied, Is.False);
        }

        [Test]
        public void EmptyBoardWins()
        {
            BoardShape shape = new BoardShape("One", BoardShape.Mask(new[] { 1 }));
            GameState state = GameState.Create(shape, new[,] { { BoardCell.Empty } }, EmptyColumns(), 1);
            GameRules rules = new GameRules(state, BoardLayout.Compute(1, 1, 320));

            rules.CheckEndConditions();

            Assert.That(state.Result, Is.EqualTo(GameResult.Won));
            Assert.That(state.Events.Count(x => x.Type == GameEventType.ResultChanged), Is.EqualTo(1));
            Assert.That(state.Events.Single(x => x.Type == GameEventType.ResultChanged).Score, Is.EqualTo((int)GameResult.Won));
        }

        [Test]
        public void AdvanceReturnsResultChangedEventWhenEndConditionChanges()
        {
            BoardShape shape = new BoardShape("One", BoardShape.Mask(new[] { 1 }));
            GameState state = GameState.Create(shape, new[,] { { BoardCell.Empty } }, EmptyColumns(), 1);
            GameRules rules = new GameRules(state, BoardLayout.Compute(1, 1, 320));

            List<GameEvent> events = rules.Advance(0f);

            Assert.That(events.Count(x => x.Type == GameEventType.ResultChanged), Is.EqualTo(1));
        }

        [Test]
        public void ResultChangedEventIsRecordedOnlyOnFirstTransition()
        {
            BoardShape shape = new BoardShape("One", BoardShape.Mask(new[] { 1 }));
            GameState state = GameState.Create(shape, new[,] { { BoardCell.Empty } }, EmptyColumns(), 1);
            GameRules rules = new GameRules(state, BoardLayout.Compute(1, 1, 320));

            rules.CheckEndConditions();
            rules.CheckEndConditions();

            Assert.That(state.Events.Count(x => x.Type == GameEventType.ResultChanged), Is.EqualTo(1));
        }

        [Test]
        public void FullWaitingQueueLoses()
        {
            GameState state = MakeStateWithColumnShooters(0);
            for (int i = 0; i < SquareFlowConstants.WaitQueueLimit; i++)
                state.WaitingQueue.Add(new Shooter("w" + i, FlowColor.Red, 1, false));
            GameRules rules = new GameRules(state, BoardLayout.Compute(1, 1, 320));

            rules.CheckEndConditions();

            Assert.That(state.Result, Is.EqualTo(GameResult.LostWait));
        }

        [Test]
        public void NoActiveQueuedOrColumnShootersLosesOutOfShooters()
        {
            GameState state = MakeStateWithColumnShooters(0);
            GameRules rules = new GameRules(state, BoardLayout.Compute(1, 1, 320));

            rules.CheckEndConditions();

            Assert.That(state.Result, Is.EqualTo(GameResult.LostOutOfShooters));
        }

        private static GameState MakeStateWithColumnShooters(int count)
        {
            BoardShape shape = new BoardShape("One", BoardShape.Mask(new[] { 1 }));
            BoardCell[,] grid = { { BoardCell.Normal(FlowColor.Red, 99) } };
            List<Shooter>[] columns = EmptyColumns();
            for (int i = 0; i < count; i++)
                columns[0].Add(new Shooter("s" + i, FlowColor.Red, 1, false));
            return GameState.Create(shape, grid, columns, 1);
        }

        private static BoardCell[,] FilledGrid(int rows, int cols, FlowColor color, int hp)
        {
            BoardCell[,] grid = new BoardCell[rows, cols];
            for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                grid[r, c] = BoardCell.Normal(color, hp);
            return grid;
        }

        private static List<Shooter>[] EmptyColumns()
        {
            return new[] { new List<Shooter>(), new List<Shooter>(), new List<Shooter>() };
        }
    }
}
