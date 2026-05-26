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
        public void ComputeBuildsHtmlRectangularOrbitGoldenValues()
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
            Assert.That(layout.OrbitRadiusX, Is.EqualTo(236f));
            Assert.That(layout.OrbitRadiusY, Is.EqualTo(193f));
            Assert.That(layout.OrbitCenterX, Is.EqualTo(layout.CanvasWidth * 0.5f));
            Assert.That(layout.OrbitCenterY, Is.EqualTo(layout.CanvasHeight * 0.5f));
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
            Assert.That((layout.CellCenterX(0) + layout.CellCenterX(8)) * 0.5f, Is.EqualTo(layout.OrbitCenterX));
            Assert.That((layout.CellCenterY(0) + layout.CellCenterY(6)) * 0.5f, Is.EqualTo(layout.OrbitCenterY));
        }

        [Test]
        public void PathPositionReturnsHtmlRectanglePoints()
        {
            BoardLayout layout = BoardLayout.Compute(7, 9, 900f);

            Vector2 start = layout.PathPosition(0f);
            Assert.That(start.x, Is.EqualTo(layout.OrbitX).Within(0.001f));
            Assert.That(start.y, Is.EqualTo(layout.OrbitY).Within(0.001f));

            Vector2 topRight = layout.PathPosition(layout.OrbitWidth);
            Assert.That(topRight.x, Is.EqualTo(layout.OrbitX + layout.OrbitWidth).Within(0.001f));
            Assert.That(topRight.y, Is.EqualTo(layout.OrbitY).Within(0.001f));

            Vector2 bottomRight = layout.PathPosition(layout.OrbitWidth + layout.OrbitHeight);
            Assert.That(bottomRight.x, Is.EqualTo(layout.OrbitX + layout.OrbitWidth).Within(0.001f));
            Assert.That(bottomRight.y, Is.EqualTo(layout.OrbitY + layout.OrbitHeight).Within(0.001f));
        }

        [Test]
        public void FirePointsHaveExpectedCountOrderAndExtremes()
        {
            BoardLayout layout = BoardLayout.Compute(7, 9, 900f);

            Assert.That(layout.FirePoints.Count, Is.EqualTo(32));
            Assert.That(layout.FirePoints[0].Side, Is.EqualTo(FireSide.Top));
            Assert.That(layout.FirePoints[0].Row, Is.EqualTo(-1));
            Assert.That(layout.FirePoints[0].Col, Is.EqualTo(0));
            Assert.That(layout.FirePoints[0].Distance, Is.EqualTo(64f).Within(0.001f));

            for (int i = 1; i < layout.FirePoints.Count; i++)
                Assert.That(layout.FirePoints[i].Distance, Is.GreaterThan(layout.FirePoints[i - 1].Distance));

            FirePoint last = layout.FirePoints[layout.FirePoints.Count - 1];
            Assert.That(last.Side, Is.EqualTo(FireSide.Left));
            Assert.That(last.Row, Is.EqualTo(0));
            Assert.That(last.Col, Is.EqualTo(-1));
            Assert.That(last.Distance, Is.EqualTo(1652f));
        }

        [Test]
        public void CatalogShapesKeepEveryCellInsideRectangularOrbitAndCentered()
        {
            foreach (BoardShape shape in BoardShapeCatalog.All)
            {
                BoardLayout layout = BoardLayout.Compute(shape.Rows, shape.Cols, 860f);

                Assert.That(layout.GridX, Is.EqualTo(layout.Pad), shape.Name);
                Assert.That(layout.GridY, Is.EqualTo(layout.Pad), shape.Name);
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

            Assert.That(layout.FirePoints[0].Side, Is.EqualTo(FireSide.Top), shape.Name);
            AssertTopStartSideSequence(layout, shape.Name);
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
            Assert.That(SquareFlowVisualMetrics.OrbitRingThicknessScale, Is.LessThanOrEqualTo(0.06f));
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

            Assert.That(layout.HudSize.x, Is.EqualTo(1080f));
            Assert.That(layout.HudSize.y, Is.EqualTo(112f));
            Assert.That(layout.HudPosition, Is.EqualTo(new Vector2(0f, 904f)));
            Assert.That(layout.ActionSize.x, Is.EqualTo(430f));
            Assert.That(layout.ActionPosition.x, Is.EqualTo(330f));
            Assert.That(layout.ActionPosition.y, Is.EqualTo(0f));
            Assert.That(layout.UtilityButtonSize, Is.EqualTo(new Vector2(72f, 62f)));
            Assert.That(layout.OrbiterStripSize, Is.EqualTo(new Vector2(1064f, 70f)));
            Assert.That(layout.OrbiterStripPosition, Is.EqualTo(new Vector2(0f, 797f)));
            Assert.That(layout.OrbiterStripTopOffset, Is.EqualTo(128f));
            Assert.That(layout.BoardPanelSize, Is.EqualTo(new Vector2(1080f, 1080f)));
            Assert.That(layout.BoardPanelPosition, Is.EqualTo(new Vector2(0f, 260f)));
            Assert.That(layout.QueueSize.x, Is.EqualTo(1064f));
            Assert.That(layout.QueueSize.y, Is.EqualTo(164f));
            Assert.That(layout.QueuePosition.x, Is.EqualTo(0f));
            Assert.That(layout.QueuePosition.y, Is.EqualTo(-378f));
            Assert.That(layout.QueueBottomOffset, Is.EqualTo(500f));
            Assert.That(layout.DockVisibleRows, Is.EqualTo(4));
            Assert.That(layout.DockSize.x, Is.EqualTo(1064f));
            Assert.That(layout.DockSize.y, Is.EqualTo(480f));
            Assert.That(layout.DockPosition.x, Is.EqualTo(0f));
            Assert.That(layout.DockPosition.y, Is.EqualTo(-708f));
            Assert.That(layout.DockBottomOffset, Is.EqualTo(12f));
        }

        [Test]
        public void IPhoneSafeAreaConvertsToCanvasPadding()
        {
            Rect safeArea = new Rect(0f, 102f, 1284f, 2535f);
            Vector2 screenSize = new Vector2(1284f, 2778f);
            Vector2 canvasSize = new Vector2(1080f, 2778f * 1080f / 1284f);

            Vector4 padding = SquareFlowSafeArea.PaddingForCanvas(safeArea, screenSize, canvasSize);

            Assert.That(padding.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(padding.z, Is.EqualTo(0f).Within(0.001f));
            Assert.That(padding.y, Is.EqualTo(102f * canvasSize.y / screenSize.y).Within(0.001f));
            Assert.That(padding.w, Is.EqualTo(141f * canvasSize.y / screenSize.y).Within(0.001f));
            Assert.That(padding.w, Is.GreaterThan(padding.y));
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
        public void ShooterTokensShowHtmlStyleCountsWithoutAmmoDotRows()
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

                Assert.That(selectableDots, Is.Null);
                Assert.That(selectableLabel.text, Is.EqualTo("3"));
                Assert.That(selectableLabel.fontSize, Is.EqualTo(SquareFlowVisualMetrics.ShooterAmmoLabelFontSize));

                InvokeAddShooterToken(controller, queuedParent.GetComponent<RectTransform>(), new Shooter("queued", FlowColor.Red, 2, false), false);
                Transform queuedToken = queuedParent.transform.Find("ShooterPreview");
                Transform queuedDots = queuedToken.Find("AmmoDots");
                TMP_Text queuedLabel = queuedToken.Find("AmmoLabel").GetComponent<TMP_Text>();

                Assert.That(queuedDots, Is.Null);
                Assert.That(queuedLabel.text, Is.EqualTo("2"));
                Assert.That(queuedLabel.fontSize, Is.EqualTo(SquareFlowVisualMetrics.ShooterAmmoLabelQueuedFontSize));
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
                CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
                Assert.That(scaler, Is.Not.Null);
                Assert.That(scaler.matchWidthOrHeight, Is.EqualTo(0f));
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
                Assert.That(contentRect.anchoredPosition, Is.EqualTo(new Vector2(0f, 40f)));
                Assert.That(contentRect.sizeDelta.x, Is.EqualTo(1030f));
                Assert.That(contentRect.sizeDelta.y, Is.EqualTo(1260f));

                TMP_Text title = FindText(content, "Square Flow");
                Assert.That(title.fontSize, Is.GreaterThanOrEqualTo(84));
                Assert.That(content.Find("MenuTitleGlow"), Is.Not.Null);
                Assert.That(content.Find("MenuSwatches"), Is.Not.Null);
                Assert.That(content.Find("ThemeToggle"), Is.Not.Null);
                Assert.That(content.Find("MenuStatsCard"), Is.Not.Null);
                Assert.That(content.Find("InstructionsCard"), Is.Not.Null);
                Assert.That(content.Find("LevelSelector"), Is.Not.Null);
                Assert.That(content.Find("MenuStatsCard").GetComponent<RectTransform>().sizeDelta.x, Is.EqualTo(1000f));
                Assert.That(content.Find("MenuTitleGlow").GetComponent<Image>().raycastTarget, Is.False);

                TMP_Text playLabel = FindText(content, "Play");
                RectTransform playButton = playLabel.transform.parent.GetComponent<RectTransform>();
                Assert.That(playButton.gameObject.name, Is.EqualTo("PlayButton"));
                Assert.That(playButton.sizeDelta.x, Is.EqualTo(410f));
                Assert.That(playButton.sizeDelta.y, Is.EqualTo(128f));
                Assert.That(playLabel.fontSize, Is.GreaterThanOrEqualTo(40));
                Transform shine = playButton.Find("PlayButtonShine");
                Assert.That(shine, Is.Not.Null);
                Assert.That(shine.GetComponent<Image>().raycastTarget, Is.False);
                Assert.That(FindText(content, "Reset All"), Is.Not.Null);
                Assert.That(FindText(content, "MAX ORBS"), Is.Not.Null);
                Assert.That(FindText(content, "5"), Is.Not.Null);
                Assert.That(FindText(content, "HP blocks"), Is.Not.Null);

                Assert.That(content.GetComponentsInChildren<Text>().Length, Is.EqualTo(0));

                Transform selector = content.Find("LevelSelector");
                int levelButtonCount = 0;
                float levelButtonY = float.NaN;
                TMP_Text[] labels = selector.GetComponentsInChildren<TMP_Text>();
                for (int i = 0; i < labels.Length; i++)
                {
                    int level;
                    if (!int.TryParse(labels[i].text, out level)) continue;

                    RectTransform button = labels[i].transform.parent.GetComponent<RectTransform>();
                    Assert.That(button.sizeDelta.x, Is.EqualTo(66f));
                    Assert.That(button.sizeDelta.y, Is.EqualTo(62f));
                    Assert.That(labels[i].fontSize, Is.EqualTo(22));
                    if (float.IsNaN(levelButtonY))
                        levelButtonY = button.anchoredPosition.y;
                    Assert.That(button.anchoredPosition.y, Is.EqualTo(levelButtonY).Within(0.001f));
                    levelButtonCount++;
                }

                Assert.That(levelButtonCount, Is.EqualTo(BoardShapeCatalog.Count));
                Assert.That(FindText(content, "Leaderboard"), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void GameplayViewBuildsFigmaReferencePanelsAndHorizontalQueues()
        {
            GameObject host = new GameObject("SquareFlowControllerHost");

            try
            {
                SquareFlowGameController controller = host.AddComponent<SquareFlowGameController>();
                InvokePrivate(controller, "Awake");
                InvokePrivate(controller, "SelectLevel", 5);
                InvokePrivate(controller, "StartLevel");

                Transform canvas = host.transform.Find("SquareFlowCanvas");
                Assert.That(canvas, Is.Not.Null);
                Assert.That(canvas.Find("GameHeader"), Is.Not.Null);
                Assert.That(canvas.Find("OrbiterStrip"), Is.Not.Null);
                Transform boardFrame = canvas.Find("BoardFrame");
                Assert.That(boardFrame, Is.Not.Null);
                RectTransform boardFrameRect = boardFrame.GetComponent<RectTransform>();
                Assert.That(boardFrameRect.anchoredPosition, Is.EqualTo(new Vector2(0f, 260f)));
                Image boardFrameImage = boardFrame.GetComponent<Image>();
                Assert.That(boardFrameImage.color.a, Is.EqualTo(0f));
                Assert.That(boardFrameImage.raycastTarget, Is.False);

                Transform waiting = canvas.Find("WaitingQueue");
                Assert.That(waiting, Is.Not.Null);
                RectTransform waitingRect = waiting.GetComponent<RectTransform>();
                Assert.That(waitingRect.anchorMin, Is.EqualTo(new Vector2(0f, 0f)));
                Assert.That(waitingRect.anchorMax, Is.EqualTo(new Vector2(1f, 0f)));
                Assert.That(waitingRect.sizeDelta, Is.EqualTo(new Vector2(-16f, 164f)));
                Assert.That(waitingRect.offsetMin.x, Is.EqualTo(8f));
                Assert.That(waitingRect.offsetMax.x, Is.EqualTo(-8f));

                RectTransform[] waitingSlots = NamedChildren(waiting, "WaitingSlot");
                Assert.That(waitingSlots.Length, Is.EqualTo(SquareFlowConstants.WaitQueueLimit));
                float waitingY = waitingSlots[0].anchoredPosition.y;
                for (int i = 0; i < waitingSlots.Length; i++)
                {
                    Assert.That(waitingSlots[i].anchoredPosition.y, Is.EqualTo(waitingY).Within(0.001f));
                    if (i > 0)
                        Assert.That(waitingSlots[i].anchoredPosition.x, Is.GreaterThan(waitingSlots[i - 1].anchoredPosition.x));
                }

                Transform columns = canvas.Find("ShooterColumns");
                Assert.That(columns, Is.Not.Null);
                RectTransform columnsRect = columns.GetComponent<RectTransform>();
                Assert.That(columnsRect.anchorMin, Is.EqualTo(new Vector2(0f, 0f)));
                Assert.That(columnsRect.anchorMax, Is.EqualTo(new Vector2(1f, 0f)));
                Assert.That(columnsRect.sizeDelta, Is.EqualTo(new Vector2(-16f, 480f)));
                RectTransform[] cards = NamedChildren(columns, "ShooterColumnCard");
                Assert.That(cards.Length, Is.EqualTo(3));
                Assert.That(cards[0].sizeDelta, Is.EqualTo(new Vector2(340f, 468f)));
                Assert.That(FindText(cards[0], "A"), Is.Not.Null);
                Assert.That(FindText(cards[1], "B"), Is.Not.Null);
                Assert.That(FindText(cards[2], "C"), Is.Not.Null);
                Assert.That(columns.GetComponentsInChildren<Button>().Length, Is.EqualTo(3));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        private static void AssertTopStartSideSequence(BoardLayout layout, string shapeName)
        {
            FireSide[] expectedPhases =
            {
                FireSide.Top,
                FireSide.Right,
                FireSide.Bottom,
                FireSide.Left
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

        private static void AssertPointOnOrbitCircle(BoardLayout layout, Vector2 point)
        {
            float dx = point.x - layout.OrbitCenterX;
            float dy = point.y - layout.OrbitCenterY;

            Assert.That(Mathf.Sqrt(dx * dx + dy * dy), Is.EqualTo(layout.OrbitRadiusX).Within(0.01f));
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

            Assert.That(x, Is.GreaterThan(layout.OrbitX), shapeName + " cell " + row + "," + col);
            Assert.That(x, Is.LessThan(layout.OrbitX + layout.OrbitWidth), shapeName + " cell " + row + "," + col);
            Assert.That(y, Is.GreaterThan(layout.OrbitY), shapeName + " cell " + row + "," + col);
            Assert.That(y, Is.LessThan(layout.OrbitY + layout.OrbitHeight), shapeName + " cell " + row + "," + col);
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

        private static void InvokePrivate(object target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(target, args);
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

            return null;
        }

        private static RectTransform[] NamedChildren(Transform parent, string name)
        {
            List<RectTransform> matches = new List<RectTransform>();
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == name)
                    matches.Add(child.GetComponent<RectTransform>());
            }

            return matches.ToArray();
        }
    }
}
