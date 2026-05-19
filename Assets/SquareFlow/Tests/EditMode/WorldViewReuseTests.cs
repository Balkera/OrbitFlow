using System.Collections.Generic;
using System.Reflection;
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

        [Test]
        public void BoardWorldViewRebuildsWhenSameSizedShapeHasDifferentActiveCellCount()
        {
            GameObject host = new GameObject("BoardWorldViewHost");
            try
            {
                BoardWorldView view = host.AddComponent<BoardWorldView>();
                BoardShape fullShape = new BoardShape("Full", BoardShape.Mask(new[] { 1, 1 }, new[] { 1, 1 }));
                BoardShape sparseShape = new BoardShape("Sparse", BoardShape.Mask(new[] { 1, 0 }, new[] { 0, 1 }));
                BoardCell[,] grid =
                {
                    { BoardCell.Normal(FlowColor.Red, 1), BoardCell.Normal(FlowColor.Blue, 1) },
                    { BoardCell.Normal(FlowColor.Green, 1), BoardCell.Normal(FlowColor.Yellow, 1) }
                };
                BoardLayout board = BoardLayout.Compute(2, 2, 320f);
                MobileWorldLayout world = new MobileWorldLayout(board, Vector2.zero, 0.01f);
                SquareFlowTheme theme = new SquareFlowTheme(true);

                view.Bind(GameState.Create(fullShape, grid, EmptyColumns(), 1), board, world, theme);
                view.Bind(GameState.Create(sparseShape, grid, EmptyColumns(), 1), board, world, theme);

                Assert.That(host.transform.childCount, Is.EqualTo(sparseShape.ActiveCellCount()));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void BoardWorldViewRebuildsWhenSameSizedShapeMovesActiveCells()
        {
            GameObject host = new GameObject("BoardWorldViewHost");
            try
            {
                BoardWorldView view = host.AddComponent<BoardWorldView>();
                BoardShape diagonalShape = new BoardShape("Diagonal", BoardShape.Mask(new[] { 1, 0 }, new[] { 0, 1 }));
                BoardShape antiDiagonalShape = new BoardShape("AntiDiagonal", BoardShape.Mask(new[] { 0, 1 }, new[] { 1, 0 }));
                BoardCell[,] grid =
                {
                    { BoardCell.Normal(FlowColor.Red, 1), BoardCell.Normal(FlowColor.Blue, 1) },
                    { BoardCell.Normal(FlowColor.Green, 1), BoardCell.Normal(FlowColor.Yellow, 1) }
                };
                BoardLayout board = BoardLayout.Compute(2, 2, 320f);
                MobileWorldLayout world = new MobileWorldLayout(board, Vector2.zero, 0.01f);
                SquareFlowTheme theme = new SquareFlowTheme(true);

                view.Bind(GameState.Create(diagonalShape, grid, EmptyColumns(), 1), board, world, theme);
                view.Bind(GameState.Create(antiDiagonalShape, grid, EmptyColumns(), 1), board, world, theme);

                Assert.That(host.transform.childCount, Is.EqualTo(antiDiagonalShape.ActiveCellCount()));
                Assert.That(HasChildNamed(host.transform, "WorldCell_0_1"), Is.True);
                Assert.That(HasChildNamed(host.transform, "WorldCell_1_0"), Is.True);
                Assert.That(HasChildNamed(host.transform, "WorldCell_0_0"), Is.False);
                Assert.That(HasChildNamed(host.transform, "WorldCell_1_1"), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void BoardWorldViewUsesBackToFrontSortingOrders()
        {
            GameObject host = new GameObject("BoardWorldViewHost");
            try
            {
                BoardWorldView view = host.AddComponent<BoardWorldView>();
                BoardShape shape = new BoardShape("One", BoardShape.Mask(new[] { 1 }));
                BoardCell[,] grid = { { BoardCell.Normal(FlowColor.Red, 1) } };
                BoardLayout board = BoardLayout.Compute(1, 1, 320f);
                MobileWorldLayout world = new MobileWorldLayout(board, Vector2.zero, 0.01f);
                SquareFlowTheme theme = new SquareFlowTheme(true);

                view.Bind(GameState.Create(shape, grid, EmptyColumns(), 1), board, world, theme);
                Transform cell = host.transform.GetChild(0);

                Assert.That(cell.Find("Depth").GetComponent<SpriteRenderer>().sortingOrder, Is.EqualTo(0));
                Assert.That(cell.Find("Face").GetComponent<SpriteRenderer>().sortingOrder, Is.EqualTo(1));
                Assert.That(cell.Find("Highlight").GetComponent<SpriteRenderer>().sortingOrder, Is.EqualTo(2));
                Assert.That(cell.Find("Label").GetComponent<MeshRenderer>().sortingOrder, Is.EqualTo(7));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void OrbitRingWorldViewReusesSegmentsForSameLayout()
        {
            GameObject host = new GameObject("OrbitRingWorldViewHost");
            try
            {
                OrbitRingWorldView view = host.AddComponent<OrbitRingWorldView>();
                BoardLayout board = BoardLayout.Compute(2, 2, 320f);
                MobileWorldLayout world = new MobileWorldLayout(board, Vector2.zero, 0.01f);
                SquareFlowTheme theme = new SquareFlowTheme(true);

                view.Bind(board, world, theme);
                int childrenAfterBind = host.transform.childCount;
                Transform firstSegment = host.transform.GetChild(0);

                view.Bind(board, world, theme);

                Assert.That(host.transform.childCount, Is.EqualTo(childrenAfterBind));
                Assert.That(host.transform.GetChild(0), Is.SameAs(firstSegment));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void OrbiterWorldViewReusesObjectForSameOrbiterId()
        {
            GameObject host = new GameObject("OrbiterWorldViewHost");
            try
            {
                OrbiterWorldView view = host.AddComponent<OrbiterWorldView>();
                BoardLayout board = BoardLayout.Compute(1, 1, 320f);
                MobileWorldLayout world = new MobileWorldLayout(board, Vector2.zero, 0.01f);
                SquareFlowTheme theme = new SquareFlowTheme(true);
                List<ActiveOrbiter> orbiters = new List<ActiveOrbiter>
                {
                    new ActiveOrbiter(new Shooter("same-id", FlowColor.Red, 1, false))
                };

                view.Refresh(orbiters, world, theme);
                Transform first = host.transform.GetChild(0);
                orbiters[0].Distance = board.Perimeter * 0.5f;
                view.Refresh(orbiters, world, theme);

                Assert.That(host.transform.childCount, Is.EqualTo(1));
                Assert.That(host.transform.GetChild(0), Is.EqualTo(first));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void OrbiterWorldViewReusesReleasedObjectForDifferentOrbiterId()
        {
            GameObject host = new GameObject("OrbiterWorldViewHost");
            try
            {
                OrbiterWorldView view = host.AddComponent<OrbiterWorldView>();
                BoardLayout board = BoardLayout.Compute(1, 1, 320f);
                MobileWorldLayout world = new MobileWorldLayout(board, Vector2.zero, 0.01f);
                SquareFlowTheme theme = new SquareFlowTheme(true);
                List<ActiveOrbiter> orbiters = new List<ActiveOrbiter>
                {
                    new ActiveOrbiter(new Shooter("first-id", FlowColor.Red, 1, false))
                };

                view.Refresh(orbiters, world, theme);
                Transform first = host.transform.GetChild(0);
                orbiters.Clear();
                view.Refresh(orbiters, world, theme);
                orbiters.Add(new ActiveOrbiter(new Shooter("second-id", FlowColor.Blue, 1, false)));
                view.Refresh(orbiters, world, theme);

                Assert.That(host.transform.childCount, Is.EqualTo(1));
                Assert.That(host.transform.GetChild(0), Is.SameAs(first));
                Assert.That(first.name, Is.EqualTo("WorldOrbiter_second-id"));
                Assert.That(first.gameObject.activeSelf, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void WorldEffectsControllerResetsRendererWhenReusingCirclePool()
        {
            GameObject host = new GameObject("WorldEffectsHost");
            try
            {
                WorldEffectsController effects = host.AddComponent<WorldEffectsController>();
                Queue<SpriteRenderer> circlePool = Pool(effects, "circlePool");
                SpriteRenderer renderer = NewEffectRenderer(host.transform, "WorldShotCore", SquareFlowWorldSprites.Circle, 12);
                renderer.gameObject.SetActive(false);
                circlePool.Enqueue(renderer);

                SpriteRenderer reused = Take(effects, circlePool, "WorldShotGlow", SquareFlowWorldSprites.Glow, 11);

                Assert.That(reused, Is.SameAs(renderer));
                Assert.That(reused.gameObject.name, Is.EqualTo("WorldShotGlow"));
                Assert.That(reused.sprite, Is.SameAs(SquareFlowWorldSprites.Glow));
                Assert.That(reused.sortingOrder, Is.EqualTo(11));
                Assert.That(reused.gameObject.activeSelf, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void WorldEffectsControllerClearReclaimsInactiveChildrenIntoPools()
        {
            GameObject host = new GameObject("WorldEffectsHost");
            try
            {
                WorldEffectsController effects = host.AddComponent<WorldEffectsController>();
                SpriteRenderer line = NewEffectRenderer(host.transform, "WorldShotStreak", SquareFlowWorldSprites.Square, 10);
                SpriteRenderer circle = NewEffectRenderer(host.transform, "WorldShotGlow", SquareFlowWorldSprites.Glow, 11);

                effects.Clear();

                Queue<SpriteRenderer> linePool = Pool(effects, "linePool");
                Queue<SpriteRenderer> circlePool = Pool(effects, "circlePool");
                Assert.That(linePool.Count, Is.EqualTo(1));
                Assert.That(circlePool.Count, Is.EqualTo(1));
                Assert.That(line.gameObject.activeSelf, Is.False);
                Assert.That(circle.gameObject.activeSelf, Is.False);
                Assert.That(linePool.Peek(), Is.SameAs(line));
                Assert.That(circlePool.Peek(), Is.SameAs(circle));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void WorldEffectsControllerBurstShotsCannotGrowBeyondEffectChildCap()
        {
            GameObject host = new GameObject("WorldEffectsHost");
            try
            {
                WorldEffectsController effects = host.AddComponent<WorldEffectsController>();

                for (int i = 0; i < WorldEffectsController.MaxConcurrentShots + 20; i++)
                    effects.PlayShot(Vector2.zero, Vector2.right, Color.red, false);

                Assert.That(host.transform.childCount, Is.LessThanOrEqualTo(WorldEffectsController.MaxEffectChildCount));

                effects.Clear();

                int extraRenderers = 10;
                for (int i = 0; i < WorldEffectsController.MaxLinePoolSize + extraRenderers; i++)
                    Release(effects, NewEffectRenderer(host.transform, "WorldShotStreak", SquareFlowWorldSprites.Square, 10));

                for (int i = 0; i < WorldEffectsController.MaxCirclePoolSize + extraRenderers; i++)
                    Release(effects, NewEffectRenderer(host.transform, "WorldShotGlow", SquareFlowWorldSprites.Glow, 11));

                Assert.That(host.transform.childCount, Is.LessThanOrEqualTo(WorldEffectsController.MaxEffectChildCount));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        private static bool HasChildNamed(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
                if (parent.GetChild(i).name == name)
                    return true;

            return false;
        }

        private static List<Shooter>[] EmptyColumns()
        {
            return new[] { new List<Shooter>(), new List<Shooter>(), new List<Shooter>() };
        }

        private static Queue<SpriteRenderer> Pool(WorldEffectsController effects, string fieldName)
        {
            FieldInfo field = typeof(WorldEffectsController).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            return (Queue<SpriteRenderer>)field.GetValue(effects);
        }

        private static SpriteRenderer Take(WorldEffectsController effects, Queue<SpriteRenderer> pool, string name, Sprite sprite, int order)
        {
            MethodInfo method = typeof(WorldEffectsController).GetMethod("Take", BindingFlags.Instance | BindingFlags.NonPublic);
            return (SpriteRenderer)method.Invoke(effects, new object[] { pool, name, sprite, order });
        }

        private static void Release(WorldEffectsController effects, SpriteRenderer renderer)
        {
            MethodInfo method = typeof(WorldEffectsController).GetMethod("Release", BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(effects, new object[] { renderer });
        }

        private static SpriteRenderer NewEffectRenderer(Transform parent, string name, Sprite sprite, int order)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return renderer;
        }
    }
}
