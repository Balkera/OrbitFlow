using NUnit.Framework;
using SquareFlow.Core;
using SquareFlow.UI;
using UnityEngine;

namespace SquareFlow.Tests
{
    public sealed class BoardLayoutTests
    {
        [Test]
        public void ComputeMatchesHtmlGoldenValues()
        {
            BoardLayout layout = BoardLayout.Compute(7, 9, 900f);

            Assert.That(layout.Cell, Is.EqualTo(40f));
            Assert.That(layout.Pad, Is.EqualTo(69f));
            Assert.That(layout.Inset, Is.EqualTo(25f));
            Assert.That(layout.GridWidth, Is.EqualTo(384f));
            Assert.That(layout.GridHeight, Is.EqualTo(298f));
            Assert.That(layout.CanvasWidth, Is.EqualTo(522f));
            Assert.That(layout.CanvasHeight, Is.EqualTo(436f));
            Assert.That(layout.OrbitX, Is.EqualTo(25f));
            Assert.That(layout.OrbitY, Is.EqualTo(25f));
            Assert.That(layout.OrbitWidth, Is.EqualTo(472f));
            Assert.That(layout.OrbitHeight, Is.EqualTo(386f));
            Assert.That(layout.Perimeter, Is.EqualTo(1716f));
        }

        [Test]
        public void CellCentersMatchHtmlGoldenValues()
        {
            BoardLayout layout = BoardLayout.Compute(7, 9, 900f);

            Assert.That(layout.CellCenterX(0), Is.EqualTo(89f));
            Assert.That(layout.CellCenterX(8), Is.EqualTo(433f));
            Assert.That(layout.CellCenterY(0), Is.EqualTo(89f));
            Assert.That(layout.CellCenterY(6), Is.EqualTo(347f));
        }

        [Test]
        public void PathPositionReturnsRoundedOrbitPoints()
        {
            BoardLayout layout = BoardLayout.Compute(7, 9, 900f);

            Vector2 start = layout.PathPosition(0f);
            Assert.That(start.x, Is.GreaterThan(layout.OrbitX));
            Assert.That(start.y, Is.GreaterThan(layout.OrbitY));
            AssertPointOnOrbitEllipse(layout, start);

            Vector2 quarter = layout.PathPosition(layout.Perimeter * 0.25f);
            Assert.That(quarter.x, Is.GreaterThan(layout.OrbitX + layout.OrbitWidth * 0.5f));
            Assert.That(quarter.y, Is.LessThan(layout.OrbitY + layout.OrbitHeight * 0.5f));
            AssertPointOnOrbitEllipse(layout, quarter);

            Vector2 half = layout.PathPosition(layout.Perimeter * 0.5f);
            Assert.That(half.x, Is.GreaterThan(layout.OrbitX + layout.OrbitWidth * 0.5f));
            Assert.That(half.y, Is.GreaterThan(layout.OrbitY + layout.OrbitHeight * 0.5f));
            AssertPointOnOrbitEllipse(layout, half);
        }

        [Test]
        public void FirePointsHaveExpectedCountOrderAndExtremes()
        {
            BoardLayout layout = BoardLayout.Compute(7, 9, 900f);

            Assert.That(layout.FirePoints.Count, Is.EqualTo(32));
            Assert.That(layout.FirePoints[0].Side, Is.EqualTo(FireSide.Top));
            Assert.That(layout.FirePoints[0].Row, Is.EqualTo(-1));
            Assert.That(layout.FirePoints[0].Col, Is.EqualTo(0));
            Assert.That(layout.FirePoints[0].Distance, Is.EqualTo(64f));

            for (int i = 1; i < layout.FirePoints.Count; i++)
                Assert.That(layout.FirePoints[i].Distance, Is.GreaterThan(layout.FirePoints[i - 1].Distance));

            FirePoint last = layout.FirePoints[layout.FirePoints.Count - 1];
            Assert.That(last.Side, Is.EqualTo(FireSide.Left));
            Assert.That(last.Row, Is.EqualTo(0));
            Assert.That(last.Col, Is.EqualTo(-1));
            Assert.That(last.Distance, Is.EqualTo(1652f));
        }

        [Test]
        public void CatalogShapesKeepClockwiseFirePointOrder()
        {
            foreach (BoardShape shape in BoardShapeCatalog.All)
            {
                BoardLayout layout = BoardLayout.Compute(shape.Rows, shape.Cols, 860f);
                Assert.That(layout.FirePoints.Count, Is.EqualTo(shape.Rows * 2 + shape.Cols * 2), shape.Name);

                int index = 0;
                for (int col = 0; col < shape.Cols; col++, index++)
                {
                    AssertFirePoint(layout.FirePoints[index], FireSide.Top, -1, col, shape.Name);
                    if (index > 0)
                        Assert.That(layout.FirePoints[index].Distance, Is.GreaterThan(layout.FirePoints[index - 1].Distance), shape.Name);
                }

                for (int row = 0; row < shape.Rows; row++, index++)
                {
                    AssertFirePoint(layout.FirePoints[index], FireSide.Right, row, -1, shape.Name);
                    Assert.That(layout.FirePoints[index].Distance, Is.GreaterThan(layout.FirePoints[index - 1].Distance), shape.Name);
                }

                for (int col = shape.Cols - 1; col >= 0; col--, index++)
                {
                    AssertFirePoint(layout.FirePoints[index], FireSide.Bottom, -1, col, shape.Name);
                    Assert.That(layout.FirePoints[index].Distance, Is.GreaterThan(layout.FirePoints[index - 1].Distance), shape.Name);
                }

                for (int row = shape.Rows - 1; row >= 0; row--, index++)
                {
                    AssertFirePoint(layout.FirePoints[index], FireSide.Left, row, -1, shape.Name);
                    Assert.That(layout.FirePoints[index].Distance, Is.GreaterThan(layout.FirePoints[index - 1].Distance), shape.Name);
                }
            }
        }

        [Test]
        public void FirePointDistancesStayOnExpectedOrbitSideAndDirection()
        {
            BoardLayout layout = BoardLayout.Compute(9, 13, 860f);

            FirePoint firstTop = layout.FirePoints[0];
            FirePoint lastTop = layout.FirePoints[12];
            AssertLaneProjection(layout, firstTop);
            AssertLaneProjection(layout, lastTop);
            Assert.That(layout.PathPosition(lastTop.Distance).x, Is.GreaterThan(layout.PathPosition(firstTop.Distance).x));

            FirePoint firstRight = layout.FirePoints[13];
            FirePoint lastRight = layout.FirePoints[21];
            AssertLaneProjection(layout, firstRight);
            AssertLaneProjection(layout, lastRight);
            Assert.That(layout.PathPosition(lastRight.Distance).y, Is.GreaterThan(layout.PathPosition(firstRight.Distance).y));

            FirePoint firstBottom = layout.FirePoints[22];
            FirePoint lastBottom = layout.FirePoints[34];
            AssertLaneProjection(layout, firstBottom);
            AssertLaneProjection(layout, lastBottom);
            Assert.That(layout.PathPosition(lastBottom.Distance).x, Is.LessThan(layout.PathPosition(firstBottom.Distance).x));

            FirePoint firstLeft = layout.FirePoints[35];
            FirePoint lastLeft = layout.FirePoints[43];
            AssertLaneProjection(layout, firstLeft);
            AssertLaneProjection(layout, lastLeft);
            Assert.That(layout.PathPosition(lastLeft.Distance).y, Is.LessThan(layout.PathPosition(firstLeft.Distance).y));
        }

        [Test]
        public void PrismArcadeMetricsKeepOrbitLineShootersAndTileDepthProminent()
        {
            Assert.That(SquareFlowVisualMetrics.OrbitLineSegmentLengthScale, Is.GreaterThanOrEqualTo(0.55f));
            Assert.That(SquareFlowVisualMetrics.OrbitLineSegmentThicknessScale, Is.GreaterThanOrEqualTo(0.16f));
            Assert.That(SquareFlowVisualMetrics.OrbitLineSegmentSpacingMultiplier, Is.LessThanOrEqualTo(0.72f));
            Assert.That(SquareFlowVisualMetrics.ActiveOrbiterTokenScale, Is.GreaterThanOrEqualTo(0.95f));
            Assert.That(SquareFlowVisualMetrics.ActiveOrbiterGlowScale, Is.GreaterThanOrEqualTo(1.5f));
            Assert.That(SquareFlowVisualMetrics.ShooterButtonMinimumDiameter, Is.GreaterThanOrEqualTo(74f));
            Assert.That(SquareFlowVisualMetrics.CellDepthOffsetScale, Is.GreaterThanOrEqualTo(0.08f));
        }

        [Test]
        public void ReferenceGameplayLayoutPlacesHudBoardQueueAndDockLikeMockup()
        {
            BoardLayout board = BoardLayout.Compute(5, 5, 620f);
            SquareFlowGameplayScreenLayout layout = SquareFlowGameplayScreenLayout.Create(board);

            Assert.That(layout.HudSize.x, Is.GreaterThan(layout.BoardPosition.x + board.CanvasWidth * 0.5f));
            Assert.That(layout.HudSize.y, Is.EqualTo(122f));
            Assert.That(layout.UtilityButtonSize.x, Is.EqualTo(layout.UtilityButtonSize.y));
            Assert.That(layout.UtilityButtonSize.x, Is.EqualTo(66f));

            Assert.That(layout.BoardPosition.x, Is.LessThan(0f));
            Assert.That(layout.QueuePosition.x, Is.GreaterThan(layout.BoardPosition.x + board.CanvasWidth * 0.5f));
            Assert.That(layout.QueueSize.x, Is.EqualTo(154f));
            Assert.That(layout.QueueSize.y, Is.GreaterThan(board.GridHeight));

            float boardBottom = layout.BoardPosition.y - board.CanvasHeight * 0.5f;
            float dockTop = layout.DockPosition.y + layout.DockSize.y * 0.5f;
            Assert.That(dockTop, Is.LessThan(boardBottom));
            Assert.That(layout.DockSize.y, Is.EqualTo(128f));
        }

        private static void AssertPointOnOrbitEllipse(BoardLayout layout, Vector2 point)
        {
            float centerX = layout.OrbitX + layout.OrbitWidth * 0.5f;
            float centerY = layout.OrbitY + layout.OrbitHeight * 0.5f;
            float radiusX = layout.OrbitWidth * 0.5f;
            float radiusY = layout.OrbitHeight * 0.5f;
            float normalizedX = (point.x - centerX) / radiusX;
            float normalizedY = (point.y - centerY) / radiusY;

            Assert.That(normalizedX * normalizedX + normalizedY * normalizedY, Is.EqualTo(1f).Within(0.01f));
        }

        private static void AssertFirePoint(FirePoint point, FireSide side, int row, int col, string shapeName)
        {
            Assert.That(point.Side, Is.EqualTo(side), shapeName);
            Assert.That(point.Row, Is.EqualTo(row), shapeName);
            Assert.That(point.Col, Is.EqualTo(col), shapeName);
        }

        private static void AssertLaneProjection(BoardLayout layout, FirePoint point)
        {
            Vector2 position = layout.PathPosition(point.Distance);
            switch (point.Side)
            {
                case FireSide.Top:
                    Assert.That(position.y, Is.LessThan(layout.OrbitY + layout.OrbitHeight * 0.5f));
                    break;
                case FireSide.Right:
                    Assert.That(position.x, Is.GreaterThan(layout.OrbitX + layout.OrbitWidth * 0.5f));
                    break;
                case FireSide.Bottom:
                    Assert.That(position.y, Is.GreaterThan(layout.OrbitY + layout.OrbitHeight * 0.5f));
                    break;
                case FireSide.Left:
                    Assert.That(position.x, Is.LessThan(layout.OrbitX + layout.OrbitWidth * 0.5f));
                    break;
            }
        }
    }
}
