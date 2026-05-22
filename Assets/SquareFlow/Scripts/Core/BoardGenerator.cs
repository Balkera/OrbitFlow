using System;
using System.Collections.Generic;
using UnityEngine;

namespace SquareFlow.Core
{
    public static class BoardGenerator
    {
        public static int GetMaxHp(int level)
        {
            int clamped = Mathf.Min(level, 20);
            return Mathf.Min(2 + Mathf.FloorToInt(clamped * 0.75f), 14);
        }

        public static BoardCell[,] Generate(BoardShape shape, int level, IFlowRandom random)
        {
            BoardCell[,] grid = new BoardCell[shape.Rows, shape.Cols];
            List<Vector2Int> active = new List<Vector2Int>();
            int maxHp = GetMaxHp(level);
            double power = Math.Max(0.25, Math.Min(level, 20) / 10.0);
            double[] thresholds = BuildThresholds(maxHp, power);

            for (int r = 0; r < shape.Rows; r++)
            for (int c = 0; c < shape.Cols; c++)
            {
                if (!shape.IsActive(r, c))
                {
                    grid[r, c] = BoardCell.Empty;
                    continue;
                }

                int hp = PickHp(thresholds, random.Value());
                FlowColor color = (FlowColor)random.Range(0, SquareFlowConstants.NormalColorCount);
                grid[r, c] = BoardCell.Normal(color, hp);
                active.Add(new Vector2Int(c, r));
            }

            PlaceBombs(grid, shape, active, random);
            return grid;
        }

        private static double[] BuildThresholds(int maxHp, double power)
        {
            double[] weights = new double[maxHp];
            double total = 0;
            for (int i = 0; i < maxHp; i++)
            {
                weights[i] = Math.Pow(i + 1, power);
                total += weights[i];
            }

            double[] thresholds = new double[maxHp];
            double cumulative = 0;
            for (int i = 0; i < maxHp; i++)
            {
                cumulative += weights[i] / total;
                thresholds[i] = cumulative;
            }
            return thresholds;
        }

        private static int PickHp(double[] thresholds, double value)
        {
            for (int i = 0; i < thresholds.Length; i++)
                if (value < thresholds[i])
                    return i + 1;
            return thresholds.Length;
        }

        private static void PlaceBombs(BoardCell[,] grid, BoardShape shape, List<Vector2Int> active, IFlowRandom random)
        {
            int bombCount = Mathf.Max(1, Mathf.FloorToInt(active.Count * 0.04f));
            float centerRow = shape.Rows / 2f;
            float centerCol = shape.Cols / 2f;
            active.Sort((a, b) =>
            {
                float da = Mathf.Abs(a.y - centerRow) + Mathf.Abs(a.x - centerCol);
                float db = Mathf.Abs(b.y - centerRow) + Mathf.Abs(b.x - centerCol);
                return da.CompareTo(db);
            });

            int candidateCount = Mathf.CeilToInt(active.Count * 0.4f);
            List<Vector2Int> candidates = active.GetRange(0, candidateCount);
            Shuffle(candidates, random);
            for (int i = 0; i < bombCount && i < candidates.Count; i++)
            {
                Vector2Int pos = candidates[i];
                grid[pos.y, pos.x] = BoardCell.Bomb();
            }
        }

        private static void Shuffle<T>(IList<T> list, IFlowRandom random)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
