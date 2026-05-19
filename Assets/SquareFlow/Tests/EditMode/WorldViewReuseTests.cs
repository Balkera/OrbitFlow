using System.Collections.Generic;
using NUnit.Framework;
using SquareFlow.Core;
using SquareFlow.Runtime;
using SquareFlow.UI;
using UnityEngine;

namespace SquareFlow.Tests
{
    public sealed class WorldViewReuseTests
    {
        [Test]
        public void BoardWorldViewRefreshesCellsWithoutCreatingMoreObjects()
        {
            GameObject host = new GameObject("BoardWorldViewHost");
            try
            {
                BoardWorldView view = host.AddComponent<BoardWorldView>();
                BoardShape shape = new BoardShape("Two", BoardShape.Mask(new[] { 1, 1 }));
                BoardCell[,] grid = { { BoardCell.Normal(FlowColor.Red, 1), BoardCell.Normal(FlowColor.Blue, 1) } };
                GameState state = GameState.Create(shape, grid, EmptyColumns(), 1);
                BoardLayout board = BoardLayout.Compute(1, 2, 320f);
                MobileWorldLayout world = new MobileWorldLayout(board, Vector2.zero, 0.01f);
                SquareFlowTheme theme = new SquareFlowTheme(true);

                view.Bind(state, board, world, theme);
                int childrenAfterBind = host.transform.childCount;

                state.Grid[0, 0] = BoardCell.Empty;
                view.RefreshCells(state, theme);

                Assert.That(host.transform.childCount, Is.EqualTo(childrenAfterBind));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        private static List<Shooter>[] EmptyColumns()
        {
            return new[] { new List<Shooter>(), new List<Shooter>(), new List<Shooter>() };
        }
    }
}
