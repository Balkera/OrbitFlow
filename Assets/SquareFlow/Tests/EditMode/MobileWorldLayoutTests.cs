using NUnit.Framework;
using SquareFlow.Core;
using SquareFlow.Runtime;
using UnityEngine;

namespace SquareFlow.Tests
{
    public sealed class MobileWorldLayoutTests
    {
        [Test]
        public void SingleCellCenterMapsToConfiguredBoardCenter()
        {
            BoardLayout board = BoardLayout.Compute(1, 1, 320f);
            MobileWorldLayout world = new MobileWorldLayout(board, new Vector2(1.25f, -0.5f), 0.01f);

            Vector2 center = world.CellCenter(0, 0);

            Assert.That(center.x, Is.EqualTo(1.25f).Within(0.001f));
            Assert.That(center.y, Is.EqualTo(-0.5f).Within(0.001f));
            Assert.That(world.CellSize, Is.EqualTo(board.Cell * 0.01f).Within(0.001f));
        }

        [Test]
        public void RowZeroIsAboveLaterRowsInWorldSpace()
        {
            BoardLayout board = BoardLayout.Compute(2, 1, 320f);
            MobileWorldLayout world = new MobileWorldLayout(board, Vector2.zero, 0.01f);

            Vector2 top = world.CellCenter(0, 0);
            Vector2 bottom = world.CellCenter(0, 1);

            Assert.That(top.y, Is.GreaterThan(bottom.y));
        }

        [Test]
        public void FirePointLookupUsesEventFireLane()
        {
            BoardLayout board = BoardLayout.Compute(4, 3, 420f);
            Vector2 boardCenter = new Vector2(0.75f, -0.35f);
            float scale = 0.015f;
            MobileWorldLayout world = new MobileWorldLayout(board, boardCenter, scale);

            AssertFirePointMatchesExactLane(
                board,
                world,
                boardCenter,
                scale,
                new GameEvent(
                    GameEventType.BlockDestroyed,
                    row: 0,
                    col: 2,
                    orbiterId: "o1",
                    score: 100,
                    fireSide: FireSide.Right,
                    fireRow: 2,
                    fireCol: -1),
                FireSide.Right,
                2,
                -1);

            AssertFirePointMatchesExactLane(
                board,
                world,
                boardCenter,
                scale,
                new GameEvent(
                    GameEventType.BlockDestroyed,
                    row: 0,
                    col: 2,
                    orbiterId: "o1",
                    score: 100,
                    fireSide: FireSide.Left,
                    fireRow: 2,
                    fireCol: -1),
                FireSide.Left,
                2,
                -1);

            AssertFirePointMatchesExactLane(
                board,
                world,
                boardCenter,
                scale,
                new GameEvent(
                    GameEventType.BlockDestroyed,
                    row: 0,
                    col: 2,
                    orbiterId: "o1",
                    score: 100,
                    fireSide: FireSide.Top,
                    fireRow: -1,
                    fireCol: 1),
                FireSide.Top,
                -1,
                1);
        }

        [Test]
        public void EventTargetUsesHitCellCenter()
        {
            BoardLayout board = BoardLayout.Compute(3, 3, 420f);
            MobileWorldLayout world = new MobileWorldLayout(board, new Vector2(0.5f, 0.25f), 0.01f);
            GameEvent hit = new GameEvent(GameEventType.BlockDamaged, row: 2, col: 1);

            Assert.That(world.EventTarget(hit), Is.EqualTo(world.CellCenter(1, 2)));
        }

        private static void AssertFirePointMatchesExactLane(
            BoardLayout board,
            MobileWorldLayout world,
            Vector2 boardCenter,
            float scale,
            GameEvent hit,
            FireSide side,
            int row,
            int col)
        {
            FirePoint matchingPoint = default;
            bool hasMatchingPoint = false;
            for (int i = 0; i < board.FirePoints.Count; i++)
            {
                FirePoint point = board.FirePoints[i];
                if (point.Side != side || point.Row != row || point.Col != col)
                    continue;

                matchingPoint = point;
                hasMatchingPoint = true;
                break;
            }

            Assert.That(hasMatchingPoint, Is.True);
            Vector2 boardPoint = board.PathPosition(matchingPoint.Distance);
            Vector2 expectedFirePoint = boardCenter + new Vector2(
                boardPoint.x - board.CanvasWidth * 0.5f,
                board.CanvasHeight * 0.5f - boardPoint.y) * scale;

            bool found = world.TryFirePoint(hit, out Vector2 firePoint);

            Assert.That(found, Is.True);
            Assert.That(firePoint.x, Is.EqualTo(expectedFirePoint.x).Within(0.001f));
            Assert.That(firePoint.y, Is.EqualTo(expectedFirePoint.y).Within(0.001f));
        }
    }
}
