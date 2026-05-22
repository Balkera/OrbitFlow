using System.Collections.Generic;
using System.Reflection;
using TMPro;
using NUnit.Framework;
using SquareFlow.Core;
using SquareFlow.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SquareFlow.Tests
{
    public sealed class BoardLayoutTests
    {
        [Test]
        public void ComputeBuildsCenteredCircularOrbitGoldenValues()
        {
            BoardLayout layout = BoardLayout.Compute(7, 9, 900f);

            Assert.That(layout.Cell, Is.EqualTo(40f));
            Assert.That(layout.Pad, Is.EqualTo(69f));
            Assert.That(layout.Inset, Is.EqualTo(25f));
            Assert.That(layout.GridWidth, Is.EqualTo(384f));
            Assert.That(layout.GridHeight, Is.EqualTo(298f));
            Assert.That(layout.CanvasWidth, Is.EqualTo(626f));
            Assert.That(layout.CanvasHeight, Is.EqualTo(626f));
            Assert.That(layout.OrbitX, Is.EqualTo(25f));
            Assert.That(layout.OrbitY, Is.EqualTo(25f));
            Assert.That(layout.OrbitWidth, Is.EqualTo(576f));
            Assert.That(layout.OrbitHeight, Is.EqualTo(576f));
            Assert.That(layout.OrbitRadiusX, Is.EqualTo(layout.OrbitRadiusY));
            Assert.That(layout.OrbitCenterX, Is.EqualTo(layout.CanvasWidth * 0.5f));
            Assert.That(layout.OrbitCenterY, Is.EqualTo(layout.CanvasHeight * 0.5f));
            Assert.That(layout.Perimeter, Is.EqualTo(2304f));
        }

        [Test]
        public void CellCentersMatchHtmlGoldenValues()
        {
            BoardLayout layout = BoardLayout.Compute(7, 9, 900f);

            Assert.That(layout.CellCenterX(0), Is.EqualTo(141f));
            Assert.That(layout.CellCenterX(8), Is.EqualTo(485f));
            Assert.That(layout.CellCenterY(0), Is.EqualTo(184f));
            Assert.That(layout.CellCenterY(6), Is.EqualTo(442f));
            Assert.That((layout.CellCenterX(0) + layout.CellCenterX(8)) * 0.5f, Is.EqualTo(layout.OrbitCenterX));
            Assert.That((layout.CellCenterY(0) + layout.CellCenterY(6)) * 0.5f, Is.EqualTo(layout.OrbitCenterY));
        }

        [Test]
        public void PathPositionReturnsRoundedOrbitPoints()
        {
            BoardLayout layout = BoardLayout.Compute(7, 9, 900f);

            Vector2 start = layout.PathPosition(0f);
            Assert.That(start.x, Is.EqualTo(layout.OrbitCenterX).Within(0.001f));
            Assert.That(start.y, Is.EqualTo(layout.OrbitY + layout.OrbitHeight).Within(0.001f));
            AssertPointOnOrbitCircle(layout, start);

            Vector2 quarter = layout.PathPosition(layout.Perimeter * 0.25f);
            Assert.That(quarter.x, Is.EqualTo(layout.OrbitX).Within(0.001f));
            Assert.That(quarter.y, Is.EqualTo(layout.OrbitCenterY).Within(0.001f));
            AssertPointOnOrbitCircle(layout, quarter);

            Vector2 half = layout.PathPosition(layout.Perimeter * 0.5f);
            Assert.That(half.x, Is.EqualTo(layout.OrbitCenterX).Within(0.001f));
            Assert.That(half.y, Is.EqualTo(layout.OrbitY).Within(0.001f));
            AssertPointOnOrbitCircle(layout, half);
        }

        [Test]
        public void FirePointsHaveExpectedCountOrderAndExtremes()
        {
            BoardLayout layout = BoardLayout.Compute(7, 9, 900f);

            Assert.That(layout.FirePoints.Count, Is.EqualTo(32));
            Assert.That(layout.FirePoints[0].Side, Is.EqualTo(FireSide.Bottom));
            Assert.That(layout.FirePoints[0].Row, Is.EqualTo(-1));
            Assert.That(layout.FirePoints[0].Col, Is.EqualTo(4));
            Assert.That(layout.FirePoints[0].Distance, Is.EqualTo(0f).Within(0.001f));

            for (int i = 1; i < layout.FirePoints.Count; i++)
                Assert.That(layout.FirePoints[i].Distance, Is.GreaterThan(layout.FirePoints[i - 1].Distance));

            FirePoint last = layout.FirePoints[layout.FirePoints.Count - 1];
            Assert.That(last.Side, Is.EqualTo(FireSide.Bottom));
            Assert.That(last.Row, Is.EqualTo(-1));
            Assert.That(last.Col, Is.EqualTo(5));
            Assert.That(last.Distance, Is.EqualTo(2261f));
        }

        [Test]
        public void CatalogShapesKeepEveryCellInsideCircularOrbitAndCentered()
        {
            foreach (BoardShape shape in BoardShapeCatalog.All)
            {
                BoardLayout layout = BoardLayout.Compute(shape.Rows, shape.Cols, 860f);

                Assert.That(layout.OrbitWidth, Is.EqualTo(layout.OrbitHeight), shape.Name);
                Assert.That(layout.OrbitRadiusX, Is.EqualTo(layout.OrbitRadiusY), shape.Name);
                Assert.That(GridCenter(layout, shape.Cols, true), Is.EqualTo(layout.OrbitCenterX).Within(0.001f), shape.Name);
                Assert.That(GridCenter(layout, shape.Rows, false), Is.EqualTo(layout.OrbitCenterY).Within(0.001f), shape.Name);

                for (int row = 0; row < shape.Rows; row++)
                for (int col = 0; col < shape.Cols; col++)
                {
                    if (!shape.IsActive(row, col)) continue;

                    AssertCellCornerInsideOrbit(layout, col, row, -0.5f, -0.5f, shape.Name);
                    AssertCellCornerInsideOrbit(layout, col, row, 0.5f, -0.5f, shape.Name);
                    AssertCellCornerInsideOrbit(layout, col, row, -0.5f, 0.5f, shape.Name);
                    AssertCellCornerInsideOrbit(layout, col, row, 0.5f, 0.5f, shape.Name);
                }
            }
        }

        [Test]
        public void CatalogShapesKeepClockwiseFirePointOrder()
        {
            foreach (BoardShape shape in BoardShapeCatalog.All)
            {
                BoardLayout layout = BoardLayout.Compute(shape.Rows, shape.Cols, 860f);
                Assert.That(layout.FirePoints.Count, Is.EqualTo(shape.Rows * 2 + shape.Cols * 2), shape.Name);

                Assert.That(layout.FirePoints[0].Side, Is.EqualTo(FireSide.Bottom), shape.Name);
                AssertBottomStartSideSequence(layout, shape.Name);
            }
        }

        [Test]
        public void FirePointDistancesStayOnExpectedOrbitSideAndDirection()
        {
            BoardLayout layout = BoardLayout.Compute(9, 13, 860f);

            FirePoint firstTop = FirstPoint(layout, FireSide.Top);
            FirePoint lastTop = LastPoint(layout, FireSide.Top);
            AssertLaneProjection(layout, firstTop);
            AssertLaneProjection(layout, lastTop);
            Assert.That(layout.PathPosition(lastTop.Distance).x, Is.GreaterThan(layout.PathPosition(firstTop.Distance).x));

            FirePoint firstRight = FirstPoint(layout, FireSide.Right);
            FirePoint lastRight = LastPoint(layout, FireSide.Right);
            AssertLaneProjection(layout, firstRight);
            AssertLaneProjection(layout, lastRight);
            Assert.That(layout.PathPosition(lastRight.Distance).y, Is.GreaterThan(layout.PathPosition(firstRight.Distance).y));

            AssertBottomRunMovesLeft(layout, LeadingPoints(layout, FireSide.Bottom));
            AssertBottomRunMovesLeft(layout, TrailingPoints(layout, FireSide.Bottom));

            FirePoint firstLeft = FirstPoint(layout, FireSide.Left);
            FirePoint lastLeft = LastPoint(layout, FireSide.Left);
            AssertLaneProjection(layout, firstLeft);
            AssertLaneProjection(layout, lastLeft);
            Assert.That(layout.PathPosition(lastLeft.Distance).y, Is.LessThan(layout.PathPosition(firstLeft.Distance).y));
        }

        [Test]
        public void PrismArcadeMetricsKeepOrbitLineShootersAndTileDepthProminent()
        {
            Assert.That(SquareFlowVisualMetrics.OrbitRingThicknessScale, Is.GreaterThanOrEqualTo(0.16f));
            Assert.That(SquareFlowVisualMetrics.OrbitRingPointCount, Is.GreaterThanOrEqualTo(96));
            Assert.That(SquareFlowVisualMetrics.ActiveOrbiterTokenScale, Is.GreaterThanOrEqualTo(0.95f));
            Assert.That(SquareFlowVisualMetrics.ActiveOrbiterGlowScale, Is.GreaterThanOrEqualTo(1.5f));
            Assert.That(SquareFlowVisualMetrics.ActiveOrbiterWorldScale, Is.EqualTo(2f));
            Assert.That(SquareFlowVisualMetrics.ActiveOrbiterLaunchDurationSeconds, Is.EqualTo(0.5f));
            Assert.That(SquareFlowVisualMetrics.ActiveOrbiterAmmoLabelFontSize, Is.EqualTo(1f));
            Assert.That(SquareFlowVisualMetrics.ActiveOrbiterAmmoParticleScale, Is.EqualTo(0.1f));
            Assert.That(SquareFlowVisualMetrics.ShooterButtonMinimumDiameter, Is.GreaterThanOrEqualTo(74f));
            Assert.That(SquareFlowVisualMetrics.CellDepthOffsetScale, Is.GreaterThanOrEqualTo(0.08f));
            Assert.That(SquareFlowVisualMetrics.CellLabelFontSize, Is.EqualTo(2f));
            Assert.That(SquareFlowVisualMetrics.DockSlotFrontScale, Is.EqualTo(1.4f));
            Assert.That(SquareFlowVisualMetrics.CellHitFeedbackDurationSeconds, Is.InRange(0.18f, 0.3f));
            Assert.That(SquareFlowVisualMetrics.CellHitShakeAmplitudeScale, Is.InRange(0.08f, 0.16f));
            Assert.That(SquareFlowVisualMetrics.CellHitFlashAlpha, Is.InRange(0.5f, 0.75f));
            Assert.That(SquareFlowVisualMetrics.ShotBulletTrailLength, Is.InRange(0.35f, 0.8f));
        }

        [Test]
        public void ReferenceTileMetricsCreateRoundedRaisedBlocks()
        {
            Assert.That(SquareFlowVisualMetrics.TileFaceScale, Is.GreaterThanOrEqualTo(0.9f));
            Assert.That(SquareFlowVisualMetrics.TileDepthDropScale, Is.GreaterThanOrEqualTo(0.12f));
            Assert.That(SquareFlowVisualMetrics.TileDepthDarkenAmount, Is.InRange(0.22f, 0.42f));
            Assert.That(SquareFlowVisualMetrics.TileTopHighlightAlpha, Is.InRange(0.08f, 0.22f));
        }

        [Test]
        public void ReferenceGameplayLayoutKeepsCanvasForHudQueueAndDock()
        {
            BoardLayout board = BoardLayout.Compute(5, 5, 620f);
            SquareFlowGameplayScreenLayout layout = SquareFlowGameplayScreenLayout.Create(board);

            Assert.That(layout.HudSize.x, Is.EqualTo(520f));
            Assert.That(layout.HudSize.y, Is.EqualTo(112f));
            Assert.That(layout.HudPosition.x, Is.LessThan(0f));
            Assert.That(layout.HudPosition.y, Is.EqualTo(900f));
            Assert.That(layout.ActionSize.x, Is.EqualTo(274f));
            Assert.That(layout.ActionPosition.x, Is.GreaterThan(0f));
            Assert.That(layout.ActionPosition.y, Is.EqualTo(layout.HudPosition.y));
            Assert.That(layout.UtilityButtonSize.x, Is.EqualTo(layout.UtilityButtonSize.y));
            Assert.That(layout.UtilityButtonSize.x, Is.EqualTo(64f));
            Assert.That(layout.QueueSize.x, Is.EqualTo(176f));
            Assert.That(layout.QueueSize.y, Is.EqualTo(540f));
            Assert.That(layout.QueueSize.y, Is.GreaterThan(board.GridHeight));
            Assert.That(layout.QueuePosition.x, Is.EqualTo(318f));
            Assert.That(layout.QueuePosition.y, Is.EqualTo(layout.DockPosition.y));
            Assert.That(layout.QueuePosition.y, Is.EqualTo(-750f));
            Assert.That(layout.DockVisibleRows, Is.EqualTo(5));
            Assert.That(layout.DockSize.x, Is.EqualTo(520f));
            Assert.That(layout.DockSize.y, Is.EqualTo(540f));
            Assert.That(layout.QueueSize.y, Is.EqualTo(layout.DockSize.y));
            Assert.That(layout.QueuePosition.x - layout.QueueSize.x * 0.5f - (layout.DockPosition.x + layout.DockSize.x * 0.5f), Is.InRange(40f, 60f));
        }

        [Test]
        public void HitEventsDoNotNeedFullGameViewRefresh()
        {
            List<GameEvent> events = new List<GameEvent>
            {
                new GameEvent(GameEventType.BlockDamaged),
                new GameEvent(GameEventType.BlockDestroyed),
                new GameEvent(GameEventType.BombDetonated)
            };

            Assert.That(SquareFlowGameViewRefreshPolicy.NeedsFullRefresh(events), Is.False);
        }

        [Test]
        public void QueueChangesNeedFullGameViewRefresh()
        {
            List<GameEvent> events = new List<GameEvent>
            {
                new GameEvent(GameEventType.BlockDestroyed),
                new GameEvent(GameEventType.OrbiterQueued)
            };

            Assert.That(SquareFlowGameViewRefreshPolicy.NeedsFullRefresh(events), Is.True);
        }

        [Test]
        public void ShooterTokensShowAmmoDotsAndCountsAboveSelectableAndQueuedTokens()
        {
            GameObject host = new GameObject("SquareFlowControllerHost");
            GameObject selectableParent = new GameObject("SelectableShooterParent", typeof(RectTransform));
            GameObject queuedParent = new GameObject("QueuedShooterParent", typeof(RectTransform));

            try
            {
                SquareFlowGameController controller = host.AddComponent<SquareFlowGameController>();
                SetPrivateField(controller, "theme", new SquareFlowTheme(true));
                InvokePrivate(controller, "EnsureRuntimeSprites");

                InvokeAddShooterToken(controller, selectableParent.GetComponent<RectTransform>(), new Shooter("front", FlowColor.Blue, 3, false), true);
                Transform selectableToken = selectableParent.transform.Find("ShooterButton");
                Transform selectableDots = selectableToken.Find("AmmoDots");
                TMP_Text selectableLabel = selectableToken.Find("AmmoLabel").GetComponent<TMP_Text>();

                Assert.That(selectableDots, Is.Not.Null);
                Assert.That(selectableLabel.text, Is.EqualTo("3"));
                Assert.That(selectableLabel.fontSize, Is.EqualTo(SquareFlowVisualMetrics.ShooterAmmoLabelFontSize));
                Assert.That(AmmoDotCount(selectableDots), Is.EqualTo(3));
                Assert.That(selectableDots.GetComponent<RectTransform>().anchoredPosition.y, Is.EqualTo(35f + SquareFlowVisualMetrics.ShooterAmmoDotTopOffset).Within(0.001f));
                Assert.That(selectableDots.GetChild(0).GetComponent<RectTransform>().sizeDelta.x, Is.EqualTo(SquareFlowVisualMetrics.ShooterAmmoDotDiameter));
                Assert.That(selectableDots.GetChild(0).GetComponent<Image>().raycastTarget, Is.False);

                InvokeAddShooterToken(controller, queuedParent.GetComponent<RectTransform>(), new Shooter("queued", FlowColor.Red, 2, false), false);
                Transform queuedToken = queuedParent.transform.Find("ShooterPreview");
                Transform queuedDots = queuedToken.Find("AmmoDots");
                TMP_Text queuedLabel = queuedToken.Find("AmmoLabel").GetComponent<TMP_Text>();

                Assert.That(queuedDots, Is.Not.Null);
                Assert.That(queuedLabel.text, Is.EqualTo("2"));
                Assert.That(queuedLabel.fontSize, Is.EqualTo(SquareFlowVisualMetrics.ShooterAmmoLabelQueuedFontSize));
                Assert.That(AmmoDotCount(queuedDots), Is.EqualTo(2));
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(selectableParent);
                Object.DestroyImmediate(queuedParent);
            }
        }

        [Test]
        public void MainMenuPanelStretchesToFillCanvas()
        {
            GameObject host = new GameObject("SquareFlowControllerHost");

            try
            {
                SquareFlowGameController controller = host.AddComponent<SquareFlowGameController>();
                if (host.transform.Find("SquareFlowCanvas") == null)
                    InvokePrivate(controller, "Awake");

                InvokePrivate(controller, "ShowMenu");

                Transform canvas = host.transform.Find("SquareFlowCanvas");
                Assert.That(canvas, Is.Not.Null);
                Transform panel = canvas.Find("MenuPanel");
                Assert.That(panel, Is.Not.Null);
                RectTransform panelRect = panel.GetComponent<RectTransform>();

                Assert.That(panelRect.anchorMin, Is.EqualTo(Vector2.zero));
                Assert.That(panelRect.anchorMax, Is.EqualTo(Vector2.one));
                Assert.That(panelRect.offsetMin, Is.EqualTo(Vector2.zero));
                Assert.That(panelRect.offsetMax, Is.EqualTo(Vector2.zero));

                Transform content = panel.Find("MenuContent");
                Assert.That(content, Is.Not.Null);
                RectTransform contentRect = content.GetComponent<RectTransform>();
                Assert.That(contentRect.anchorMin, Is.EqualTo(new Vector2(0.5f, 0.5f)));
                Assert.That(contentRect.anchorMax, Is.EqualTo(new Vector2(0.5f, 0.5f)));
                Assert.That(contentRect.sizeDelta.x, Is.GreaterThanOrEqualTo(860f));
                Assert.That(contentRect.sizeDelta.y, Is.GreaterThanOrEqualTo(1320f));

                TMP_Text title = FindText(content, "Square Flow");
                Assert.That(title.fontSize, Is.GreaterThanOrEqualTo(70));

                TMP_Text playLabel = FindText(content, "Play");
                RectTransform playButton = playLabel.transform.parent.GetComponent<RectTransform>();
                Assert.That(playButton.sizeDelta.x, Is.GreaterThanOrEqualTo(500f));
                Assert.That(playButton.sizeDelta.y, Is.GreaterThanOrEqualTo(90f));
                Assert.That(playLabel.fontSize, Is.GreaterThanOrEqualTo(30));

                Assert.That(content.GetComponentsInChildren<Text>().Length, Is.EqualTo(0));

                int levelButtonCount = 0;
                TMP_Text[] labels = content.GetComponentsInChildren<TMP_Text>();
                for (int i = 0; i < labels.Length; i++)
                {
                    int level;
                    if (!int.TryParse(labels[i].text, out level)) continue;

                    RectTransform button = labels[i].transform.parent.GetComponent<RectTransform>();
                    Assert.That(button.sizeDelta.x, Is.GreaterThanOrEqualTo(108f));
                    Assert.That(button.sizeDelta.y, Is.GreaterThanOrEqualTo(64f));
                    Assert.That(labels[i].fontSize, Is.GreaterThanOrEqualTo(28));
                    levelButtonCount++;
                }

                Assert.That(levelButtonCount, Is.EqualTo(BoardShapeCatalog.Count));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        private static void AssertBottomStartSideSequence(BoardLayout layout, string shapeName)
        {
            FireSide[] expectedPhases =
            {
                FireSide.Bottom,
                FireSide.Left,
                FireSide.Top,
                FireSide.Right,
                FireSide.Bottom
            };
            int phase = 0;

            for (int i = 0; i < layout.FirePoints.Count; i++)
            {
                if (i > 0)
                    Assert.That(layout.FirePoints[i].Distance, Is.GreaterThan(layout.FirePoints[i - 1].Distance), shapeName);

                while (phase < expectedPhases.Length - 1 && layout.FirePoints[i].Side != expectedPhases[phase])
                    phase++;

                Assert.That(layout.FirePoints[i].Side, Is.EqualTo(expectedPhases[phase]), shapeName + " fire point " + i);
            }
        }

        private static FirePoint FirstPoint(BoardLayout layout, FireSide side)
        {
            for (int i = 0; i < layout.FirePoints.Count; i++)
                if (layout.FirePoints[i].Side == side)
                    return layout.FirePoints[i];

            Assert.Fail("Missing fire point for " + side);
            return default;
        }

        private static FirePoint LastPoint(BoardLayout layout, FireSide side)
        {
            for (int i = layout.FirePoints.Count - 1; i >= 0; i--)
                if (layout.FirePoints[i].Side == side)
                    return layout.FirePoints[i];

            Assert.Fail("Missing fire point for " + side);
            return default;
        }

        private static List<FirePoint> LeadingPoints(BoardLayout layout, FireSide side)
        {
            List<FirePoint> points = new List<FirePoint>();
            for (int i = 0; i < layout.FirePoints.Count; i++)
            {
                if (layout.FirePoints[i].Side != side) break;
                points.Add(layout.FirePoints[i]);
            }

            return points;
        }

        private static List<FirePoint> TrailingPoints(BoardLayout layout, FireSide side)
        {
            List<FirePoint> points = new List<FirePoint>();
            for (int i = layout.FirePoints.Count - 1; i >= 0; i--)
            {
                if (layout.FirePoints[i].Side != side) break;
                points.Insert(0, layout.FirePoints[i]);
            }

            return points;
        }

        private static void AssertBottomRunMovesLeft(BoardLayout layout, List<FirePoint> points)
        {
            for (int i = 0; i < points.Count; i++)
            {
                AssertLaneProjection(layout, points[i]);
                if (i > 0)
                    Assert.That(layout.PathPosition(points[i].Distance).x, Is.LessThan(layout.PathPosition(points[i - 1].Distance).x));
            }
        }

        private static void AssertPointOnOrbitCircle(BoardLayout layout, Vector2 point)
        {
            float dx = point.x - layout.OrbitCenterX;
            float dy = point.y - layout.OrbitCenterY;

            Assert.That(Mathf.Sqrt(dx * dx + dy * dy), Is.EqualTo(layout.OrbitRadiusX).Within(0.01f));
        }

        private static float GridCenter(BoardLayout layout, int count, bool horizontal)
        {
            float first = horizontal ? layout.CellCenterX(0) : layout.CellCenterY(0);
            float last = horizontal ? layout.CellCenterX(count - 1) : layout.CellCenterY(count - 1);
            return (first + last) * 0.5f;
        }

        private static void AssertCellCornerInsideOrbit(BoardLayout layout, int col, int row, float xOffset, float yOffset, string shapeName)
        {
            float x = layout.CellCenterX(col) + xOffset * layout.Cell;
            float y = layout.CellCenterY(row) + yOffset * layout.Cell;
            float dx = x - layout.OrbitCenterX;
            float dy = y - layout.OrbitCenterY;
            float distance = Mathf.Sqrt(dx * dx + dy * dy);

            Assert.That(distance, Is.LessThan(layout.OrbitRadiusX - 1f), shapeName + " cell " + row + "," + col);
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

        private static void InvokeAddShooterToken(SquareFlowGameController controller, RectTransform parent, Shooter shooter, bool selectable)
        {
            MethodInfo method = typeof(SquareFlowGameController).GetMethod("AddShooterToken", BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(controller, new object[] { parent, shooter, Vector2.zero, Vector2.one * 70f, selectable, new UnityAction(() => { }) });
        }

        private static void InvokePrivate(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(target, null);
        }

        private static void SetPrivateField<T>(object target, string fieldName, T value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }

        private static int AmmoDotCount(Transform parent)
        {
            int count = 0;
            for (int i = 0; i < parent.childCount; i++)
                if (parent.GetChild(i).name == "AmmoDot")
                    count++;

            return count;
        }

        private static TMP_Text FindText(Transform parent, string value)
        {
            TMP_Text[] labels = parent.GetComponentsInChildren<TMP_Text>();
            for (int i = 0; i < labels.Length; i++)
                if (labels[i].text == value)
                    return labels[i];

            Assert.Fail("Missing text: " + value);
            return null;
        }
    }
}
