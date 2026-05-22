using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SquareFlow.Core;

namespace SquareFlow.Tests
{
    public sealed class ShooterGenerationTests
    {
        [Test]
        public void ShooterGenerationCreatesThreeColumnsAndEnoughNormalAmmo()
        {
            BoardShape shape = BoardShapeCatalog.GetShape(1);
            BoardCell[,] grid = new BoardCell[shape.Rows, shape.Cols];
            for (int r = 0; r < shape.Rows; r++)
            for (int c = 0; c < shape.Cols; c++)
                grid[r, c] = shape.IsActive(r, c) ? BoardCell.Normal(FlowColor.Red, 1) : BoardCell.Empty;

            List<Shooter>[] columns = ShooterGenerator.BuildColumns(grid, shape, 1, new FixedFlowRandom(0.2));
            int redAmmo = columns.SelectMany(x => x).Where(s => !s.Wild && s.Color == FlowColor.Red).Sum(s => s.Ammo);

            Assert.That(columns.Length, Is.EqualTo(3));
            Assert.That(redAmmo, Is.GreaterThanOrEqualTo(shape.ActiveCellCount()));
            Assert.That(columns.SelectMany(x => x).Count(), Is.GreaterThan(3));
        }

        [Test]
        public void HiddenFlagOnlyAppearsAfterTheFrontShooter()
        {
            BoardShape shape = BoardShapeCatalog.GetShape(1);
            BoardCell[,] grid = BoardGenerator.Generate(shape, 5, new FixedFlowRandom(0.3));

            List<Shooter>[] columns = ShooterGenerator.BuildColumns(grid, shape, 5, new FixedFlowRandom(0.1));

            foreach (List<Shooter> column in columns)
                if (column.Count > 0)
                    Assert.That(column[0].Hidden, Is.False);
        }

        [Test]
        public void AddsSixExtraShootersBeyondRequiredAmmoChunks()
        {
            BoardShape shape = MixedShape();
            BoardCell[,] grid = MixedGrid();
            int requiredChunks = 3 + 2 + 1 + 1;

            List<Shooter>[] columns = ShooterGenerator.BuildColumns(grid, shape, 1, new FixedFlowRandom(0.0));

            Assert.That(columns.SelectMany(x => x).Count(), Is.EqualTo(requiredChunks + SquareFlowConstants.ExtraShooterCount));
        }

        [Test]
        public void DistributesShootersRoundRobinAcrossThreeColumns()
        {
            BoardShape shape = MixedShape();
            BoardCell[,] grid = MixedGrid();

            List<Shooter>[] columns = ShooterGenerator.BuildColumns(grid, shape, 1, new FixedFlowRandom(0.0));
            int min = columns.Min(column => column.Count);
            int max = columns.Max(column => column.Count);

            Assert.That(max - min, Is.LessThanOrEqualTo(1));
        }

        [Test]
        public void MixedColorNormalAmmoCoversEachColorTotalAtLowLevel()
        {
            BoardShape shape = MixedShape();
            BoardCell[,] grid = MixedGrid();

            List<Shooter>[] columns = ShooterGenerator.BuildColumns(grid, shape, 1, new FixedFlowRandom(0.0));
            List<Shooter> shooters = columns.SelectMany(x => x).ToList();

            Assert.That(shooters.Where(s => !s.Wild && s.Color == FlowColor.Red).Sum(s => s.Ammo), Is.GreaterThanOrEqualTo(5));
            Assert.That(shooters.Where(s => !s.Wild && s.Color == FlowColor.Blue).Sum(s => s.Ammo), Is.GreaterThanOrEqualTo(3));
            Assert.That(shooters.Where(s => !s.Wild && s.Color == FlowColor.Yellow).Sum(s => s.Ammo), Is.GreaterThanOrEqualTo(2));
            Assert.That(shooters.Where(s => !s.Wild && s.Color == FlowColor.Green).Sum(s => s.Ammo), Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void HighLevelCanConvertShootersToWild()
        {
            BoardShape shape = MixedShape();
            BoardCell[,] grid = MixedGrid();

            List<Shooter>[] columns = ShooterGenerator.BuildColumns(grid, shape, 20, new FixedFlowRandom(0.0));

            Assert.That(columns.SelectMany(x => x).Any(s => s.Wild && s.Color == FlowColor.Wild), Is.True);
        }

        private static BoardShape MixedShape()
        {
            return new BoardShape("Mixed", BoardShape.Mask(new[] { 1, 1, 1 }, new[] { 1, 1, 1 }));
        }

        private static BoardCell[,] MixedGrid()
        {
            return new[,]
            {
                { BoardCell.Normal(FlowColor.Red, 3), BoardCell.Normal(FlowColor.Blue, 2), BoardCell.Normal(FlowColor.Yellow, 2) },
                { BoardCell.Normal(FlowColor.Red, 2), BoardCell.Normal(FlowColor.Blue, 1), BoardCell.Normal(FlowColor.Green, 1) }
            };
        }
    }
}
