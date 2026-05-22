using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SquareFlow.Core;
using UnityEngine;

namespace SquareFlow.Tests
{
    public sealed class BoardGenerationTests
    {
        [Test]
        public void CatalogContainsTheTenSourceShapes()
        {
            ExpectedShape[] expected = SourceShapes();

            Assert.That(BoardShapeCatalog.Count, Is.EqualTo(10));
            for (int i = 0; i < expected.Length; i++)
            {
                BoardShape shape = BoardShapeCatalog.GetShape(i + 1);
                Assert.That(shape.Name, Is.EqualTo(expected[i].Name));
                Assert.That(shape.Rows, Is.EqualTo(expected[i].Rows.Length));
                Assert.That(shape.Cols, Is.EqualTo(expected[i].Rows[0].Length));
            }

            Assert.That(BoardShapeCatalog.GetShape(11).Name, Is.EqualTo("Diamond"));
        }

        [Test]
        public void CatalogMasksMatchTheSourceRows()
        {
            ExpectedShape[] expected = SourceShapes();

            for (int i = 0; i < expected.Length; i++)
            {
                BoardShape shape = BoardShapeCatalog.GetShape(i + 1);

                Assert.That(shape.ActiveCellCount(), Is.EqualTo(CountActive(expected[i].Rows)), expected[i].Name);
                for (int r = 0; r < expected[i].Rows.Length; r++)
                for (int c = 0; c < expected[i].Rows[r].Length; c++)
                {
                    bool expectedActive = expected[i].Rows[r][c] == '1';
                    Assert.That(shape.IsActive(r, c), Is.EqualTo(expectedActive), $"{expected[i].Name} ({r},{c})");
                }
            }
        }

        [Test]
        public void GeneratedGridOnlyFillsActiveShapeCells()
        {
            BoardShape shape = BoardShapeCatalog.GetShape(1);
            BoardCell[,] grid = BoardGenerator.Generate(shape, 1, new FixedFlowRandom(0.25));

            for (int r = 0; r < shape.Rows; r++)
            for (int c = 0; c < shape.Cols; c++)
            {
                Assert.That(grid[r, c].IsOccupied, Is.EqualTo(shape.IsActive(r, c)));
            }
        }

        [Test]
        public void GeneratedGridCreatesSourceDensityBombsInCenterBiasedCandidates()
        {
            for (int level = 1; level <= BoardShapeCatalog.Count; level++)
            {
                BoardShape shape = BoardShapeCatalog.GetShape(level);
                BoardCell[,] grid = BoardGenerator.Generate(shape, level, new FixedFlowRandom(0.15));
                HashSet<string> candidates = CenterBiasedCandidates(shape);
                int expectedBombs = Math.Max(1, (int)Math.Floor(shape.ActiveCellCount() * 0.04));
                int bombs = 0;

                for (int r = 0; r < shape.Rows; r++)
                for (int c = 0; c < shape.Cols; c++)
                {
                    if (grid[r, c].Type != BoardCellType.Bomb)
                        continue;

                    bombs++;
                    Assert.That(shape.IsActive(r, c), Is.True, $"{shape.Name} bomb at ({r},{c})");
                    Assert.That(candidates.Contains(Key(r, c)), Is.True, $"{shape.Name} bomb at ({r},{c})");
                }

                Assert.That(bombs, Is.EqualTo(expectedBombs), shape.Name);
            }
        }

        [Test]
        public void GeneratedHpStaysWithinLevelScaledMaximum()
        {
            BoardShape shape = BoardShapeCatalog.GetShape(10);
            int level = 20;
            BoardCell[,] grid = BoardGenerator.Generate(shape, level, new FixedFlowRandom(0.99));
            int maxHp = BoardGenerator.GetMaxHp(level);

            foreach (BoardCell cell in grid)
            {
                if (cell.Type == BoardCellType.Normal)
                    Assert.That(cell.Hp, Is.InRange(1, maxHp));
            }
        }

        private static ExpectedShape[] SourceShapes()
        {
            return new[]
            {
                new ExpectedShape("Diamond",
                    "001111100",
                    "011111110",
                    "111111111",
                    "011111110",
                    "001111100",
                    "000111000",
                    "000010000"),
                new ExpectedShape("Dino",
                    "0000000111100",
                    "0000001111110",
                    "0000001101110",
                    "0000001111110",
                    "0000001111000",
                    "0000001111100",
                    "1000011110000",
                    "1100111100000",
                    "1111111111100",
                    "0111111100000",
                    "0011111000000",
                    "0001111000000",
                    "0001101100000",
                    "0001100110000",
                    "0011000011000"),
                new ExpectedShape("Heart",
                    "00110001100",
                    "01111011110",
                    "11111111111",
                    "11111111111",
                    "11111111111",
                    "01111111110",
                    "00111111100",
                    "00011111000",
                    "00001110000",
                    "00000100000",
                    "00000000000"),
                new ExpectedShape("Pizza",
                    "00011111000",
                    "00111111100",
                    "01111111110",
                    "11111111000",
                    "11111110000",
                    "11111100000",
                    "11111110000",
                    "11111111000",
                    "01111111110",
                    "00111111100",
                    "00011111000"),
                new ExpectedShape("Smiley",
                    "00011111000",
                    "00111111100",
                    "01111111110",
                    "11101110111",
                    "11111111111",
                    "11111111111",
                    "11011111011",
                    "11101110111",
                    "01110001110",
                    "00111111100",
                    "00011111000"),
                new ExpectedShape("Fish",
                    "0000001100000",
                    "1000011111000",
                    "1100111111100",
                    "1111111111110",
                    "1111111110111",
                    "1111111111110",
                    "1100111111100",
                    "1000011111000",
                    "0000001100000"),
                new ExpectedShape("Skull",
                    "00111111100",
                    "01111111110",
                    "11111111111",
                    "11101110111",
                    "11111111111",
                    "01111111110",
                    "00111111100",
                    "00010101000",
                    "00010101000",
                    "00000000000",
                    "00000000000"),
                new ExpectedShape("Tree",
                    "00000100000",
                    "00001110000",
                    "00011111000",
                    "00111111100",
                    "00011111000",
                    "00111111100",
                    "01111111110",
                    "00111111100",
                    "01111111110",
                    "11111111111",
                    "00001110000",
                    "00001110000",
                    "00001110000"),
                new ExpectedShape("Hourglass",
                    "111111111",
                    "111111111",
                    "011111110",
                    "001111100",
                    "000111000",
                    "000010000",
                    "000111000",
                    "001111100",
                    "011111110",
                    "111111111",
                    "111111111"),
                new ExpectedShape("Crown",
                    "1000001000001",
                    "1100011100011",
                    "1110011100111",
                    "1110111110111",
                    "1111111111111",
                    "1111111111111",
                    "1111111111111",
                    "1111111111111",
                    "1111111111111")
            };
        }

        private static int CountActive(IEnumerable<string> rows)
        {
            return rows.Sum(row => row.Count(cell => cell == '1'));
        }

        private static HashSet<string> CenterBiasedCandidates(BoardShape shape)
        {
            List<Vector2Int> active = new List<Vector2Int>();
            for (int r = 0; r < shape.Rows; r++)
            for (int c = 0; c < shape.Cols; c++)
                if (shape.IsActive(r, c))
                    active.Add(new Vector2Int(c, r));

            float centerRow = shape.Rows / 2f;
            float centerCol = shape.Cols / 2f;
            active.Sort((a, b) =>
            {
                float da = Mathf.Abs(a.y - centerRow) + Mathf.Abs(a.x - centerCol);
                float db = Mathf.Abs(b.y - centerRow) + Mathf.Abs(b.x - centerCol);
                return da.CompareTo(db);
            });

            int candidateCount = Mathf.CeilToInt(active.Count * 0.4f);
            return new HashSet<string>(active.Take(candidateCount).Select(pos => Key(pos.y, pos.x)));
        }

        private static string Key(int row, int col)
        {
            return row + "," + col;
        }

        private sealed class ExpectedShape
        {
            public ExpectedShape(string name, params string[] rows)
            {
                Name = name;
                Rows = rows;
            }

            public string Name { get; }
            public string[] Rows { get; }
        }
    }
}
