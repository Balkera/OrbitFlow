using System;
using System.Collections.Generic;
using UnityEngine;

namespace SquareFlow.Core
{
    public static class ShooterGenerator
    {
        public static List<Shooter>[] BuildColumns(BoardCell[,] grid, BoardShape shape, int level, IFlowRandom random)
        {
            int clamped = Mathf.Min(level, 20);
            int ammoBase = 2 + Mathf.FloorToInt(clamped * 0.7f);
            int ammoRange = 2 + Mathf.FloorToInt(clamped * 0.85f);
            float wildChance = Mathf.Max(0f, (clamped - 2) * 0.022f);
            int[] hpByColor = CountNormalHpByColor(grid, shape);
            List<Shooter> pool = new List<Shooter>();

            for (int color = 0; color < SquareFlowConstants.NormalColorCount; color++)
            {
                int remaining = hpByColor[color];
                while (remaining > 0)
                {
                    int ammo = Mathf.Min(random.Range(0, ammoRange) + ammoBase, remaining);
                    pool.Add(new Shooter(NewId(), (FlowColor)color, ammo, false));
                    remaining -= ammo;
                }
            }

            Shuffle(pool, random);
            for (int i = 0; i < SquareFlowConstants.ExtraShooterCount; i++)
            {
                bool wild = random.Value() < wildChance;
                FlowColor color = wild ? FlowColor.Wild : (FlowColor)random.Range(0, SquareFlowConstants.NormalColorCount);
                int ammo = random.Range(0, ammoRange) + ammoBase;
                pool.Add(new Shooter(NewId(), color, ammo, wild));
            }

            for (int i = 0; i < pool.Count; i++)
            {
                Shooter shooter = pool[i];
                if (!shooter.Wild && random.Value() < wildChance)
                    pool[i] = new Shooter(shooter.Id, FlowColor.Wild, shooter.Ammo, true, shooter.Hidden);
            }

            List<Shooter>[] columns = { new List<Shooter>(), new List<Shooter>(), new List<Shooter>() };
            for (int i = 0; i < pool.Count; i++)
            {
                int column = i % columns.Length;
                Shooter shooter = pool[i];
                bool hidden = columns[column].Count > 0 && random.Value() < 0.35f;
                columns[column].Add(new Shooter(shooter.Id, shooter.Color, shooter.Ammo, shooter.Wild, hidden));
            }

            return columns;
        }

        private static int[] CountNormalHpByColor(BoardCell[,] grid, BoardShape shape)
        {
            int[] counts = new int[SquareFlowConstants.NormalColorCount];
            for (int r = 0; r < shape.Rows; r++)
            for (int c = 0; c < shape.Cols; c++)
            {
                BoardCell cell = grid[r, c];
                if (shape.IsActive(r, c) && cell.Type == BoardCellType.Normal)
                    counts[(int)cell.Color] += cell.Hp;
            }

            return counts;
        }

        private static void Shuffle<T>(IList<T> list, IFlowRandom random)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private static string NewId()
        {
            return Guid.NewGuid().ToString("N");
        }
    }
}
