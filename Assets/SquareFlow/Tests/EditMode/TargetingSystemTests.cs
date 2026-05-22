using NUnit.Framework;
using SquareFlow.Core;

namespace SquareFlow.Tests
{
    public sealed class TargetingSystemTests
    {
        [Test]
        public void FindsFirstMatchingTargetFromTop()
        {
            BoardShape shape = new BoardShape("Test", BoardShape.Mask(new[] { 1 }, new[] { 1 }, new[] { 1 }));
            BoardCell[,] grid =
            {
                { BoardCell.Empty },
                { BoardCell.Normal(FlowColor.Red, 1) },
                { BoardCell.Normal(FlowColor.Red, 1) }
            };
            FirePoint point = new FirePoint(FireSide.Top, -1, 0, 0);

            TargetHit? hit = TargetingSystem.GetTarget(grid, shape, point, FlowColor.Red, false);

            Assert.That(hit.HasValue, Is.True);
            Assert.That(hit.Value.Row, Is.EqualTo(1));
            Assert.That(hit.Value.Col, Is.EqualTo(0));
        }

        [Test]
        public void WildTargetsFirstNormalCellRegardlessOfColor()
        {
            BoardShape shape = new BoardShape("Test", BoardShape.Mask(new[] { 1, 1, 1 }));
            BoardCell[,] grid = { { BoardCell.Normal(FlowColor.Blue, 1), BoardCell.Normal(FlowColor.Red, 1), BoardCell.Normal(FlowColor.Green, 1) } };
            FirePoint point = new FirePoint(FireSide.Left, 0, -1, 0);

            TargetHit? hit = TargetingSystem.GetTarget(grid, shape, point, FlowColor.Wild, true);

            Assert.That(hit.HasValue, Is.True);
            Assert.That(hit.Value.Col, Is.EqualTo(0));
        }

        [Test]
        public void BombIsAlwaysAValidTarget()
        {
            BoardShape shape = new BoardShape("Test", BoardShape.Mask(new[] { 1, 1 }));
            BoardCell[,] grid = { { BoardCell.Bomb(), BoardCell.Normal(FlowColor.Green, 1) } };
            FirePoint point = new FirePoint(FireSide.Left, 0, -1, 0);

            TargetHit? hit = TargetingSystem.GetTarget(grid, shape, point, FlowColor.Red, false);

            Assert.That(hit.HasValue, Is.True);
            Assert.That(hit.Value.Special, Is.EqualTo(TargetSpecial.Bomb));
            Assert.That(hit.Value.Col, Is.EqualTo(0));
        }

        [Test]
        public void FindsFirstMatchingTargetFromRight()
        {
            BoardShape shape = new BoardShape("Test", BoardShape.Mask(new[] { 1, 1, 1 }));
            BoardCell[,] grid = { { BoardCell.Normal(FlowColor.Green, 1), BoardCell.Normal(FlowColor.Red, 1), BoardCell.Empty } };
            FirePoint point = new FirePoint(FireSide.Right, 0, 3, 0);

            TargetHit? hit = TargetingSystem.GetTarget(grid, shape, point, FlowColor.Red, false);

            Assert.That(hit.HasValue, Is.True);
            Assert.That(hit.Value.Row, Is.EqualTo(0));
            Assert.That(hit.Value.Col, Is.EqualTo(1));
        }

        [Test]
        public void FindsFirstMatchingTargetFromBottom()
        {
            BoardShape shape = new BoardShape("Test", BoardShape.Mask(new[] { 1 }, new[] { 1 }, new[] { 1 }));
            BoardCell[,] grid =
            {
                { BoardCell.Normal(FlowColor.Yellow, 1) },
                { BoardCell.Normal(FlowColor.Blue, 1) },
                { BoardCell.Empty }
            };
            FirePoint point = new FirePoint(FireSide.Bottom, 3, 0, 0);

            TargetHit? hit = TargetingSystem.GetTarget(grid, shape, point, FlowColor.Blue, false);

            Assert.That(hit.HasValue, Is.True);
            Assert.That(hit.Value.Row, Is.EqualTo(1));
            Assert.That(hit.Value.Col, Is.EqualTo(0));
        }

        [Test]
        public void NonmatchingFrontCellBlocksTheFireLine()
        {
            BoardShape shape = new BoardShape("Test", BoardShape.Mask(new[] { 1, 1, 1 }));
            BoardCell[,] grid = { { BoardCell.Normal(FlowColor.Blue, 1), BoardCell.Normal(FlowColor.Red, 1), BoardCell.Normal(FlowColor.Red, 1) } };
            FirePoint point = new FirePoint(FireSide.Left, 0, -1, 0);

            TargetHit? hit = TargetingSystem.GetTarget(grid, shape, point, FlowColor.Red, false);

            Assert.That(hit.HasValue, Is.False);
        }

        [Test]
        public void NonmatchingFrontCellBlocksFromTop()
        {
            BoardShape shape = new BoardShape("Test", BoardShape.Mask(new[] { 1 }, new[] { 1 }, new[] { 1 }));
            BoardCell[,] grid =
            {
                { BoardCell.Normal(FlowColor.Blue, 1) },
                { BoardCell.Normal(FlowColor.Red, 1) },
                { BoardCell.Normal(FlowColor.Red, 1) }
            };
            FirePoint point = new FirePoint(FireSide.Top, -1, 0, 0);

            TargetHit? hit = TargetingSystem.GetTarget(grid, shape, point, FlowColor.Red, false);

            Assert.That(hit.HasValue, Is.False);
        }

        [Test]
        public void NonmatchingFrontCellBlocksFromRight()
        {
            BoardShape shape = new BoardShape("Test", BoardShape.Mask(new[] { 1, 1, 1 }));
            BoardCell[,] grid = { { BoardCell.Normal(FlowColor.Red, 1), BoardCell.Normal(FlowColor.Red, 1), BoardCell.Normal(FlowColor.Blue, 1) } };
            FirePoint point = new FirePoint(FireSide.Right, 0, 3, 0);

            TargetHit? hit = TargetingSystem.GetTarget(grid, shape, point, FlowColor.Red, false);

            Assert.That(hit.HasValue, Is.False);
        }

        [Test]
        public void NonmatchingFrontCellBlocksFromBottom()
        {
            BoardShape shape = new BoardShape("Test", BoardShape.Mask(new[] { 1 }, new[] { 1 }, new[] { 1 }));
            BoardCell[,] grid =
            {
                { BoardCell.Normal(FlowColor.Red, 1) },
                { BoardCell.Normal(FlowColor.Red, 1) },
                { BoardCell.Normal(FlowColor.Blue, 1) }
            };
            FirePoint point = new FirePoint(FireSide.Bottom, 3, 0, 0);

            TargetHit? hit = TargetingSystem.GetTarget(grid, shape, point, FlowColor.Red, false);

            Assert.That(hit.HasValue, Is.False);
        }
    }
}
