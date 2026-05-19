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
            BoardLayout board = BoardLayout.Compute(3, 3, 420f);
            MobileWorldLayout world = new MobileWorldLayout(board, Vector2.zero, 0.01f);
            GameEvent hit = new GameEvent(
                GameEventType.BlockDestroyed,
                row: 1,
                col: 2,
                orbiterId: "o1",
                score: 100,
                fireSide: FireSide.Right,
                fireRow: 1,
                fireCol: -1);

            bool found = world.TryFirePoint(hit, out Vector2 firePoint);

            Assert.That(found, Is.True);
            Assert.That(firePoint.x, Is.GreaterThan(world.CellCenter(2, 1).x));
        }

        [Test]
        public void EventTargetUsesHitCellCenter()
        {
            BoardLayout board = BoardLayout.Compute(3, 3, 420f);
            MobileWorldLayout world = new MobileWorldLayout(board, new Vector2(0.5f, 0.25f), 0.01f);
            GameEvent hit = new GameEvent(GameEventType.BlockDamaged, row: 2, col: 1);

            Assert.That(world.EventTarget(hit), Is.EqualTo(world.CellCenter(1, 2)));
        }
    }
}
