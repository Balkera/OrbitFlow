using System.Collections.Generic;
using System.Reflection;
using TMPro;
using NUnit.Framework;
using SquareFlow.Core;
using SquareFlow.Runtime;
using SquareFlow.UI;
using UnityEditor;
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
            Assert.That(SquareFlowVisualMetrics.OrbitRingThicknessScale, Is.EqualTo(0.175f));
            Assert.That(SquareFlowVisualMetrics.OrbitRingPointCount, Is.GreaterThanOrEqualTo(96));
            Assert.That(SquareFlowVisualMetrics.ActiveOrbiterTokenScale, Is.GreaterThanOrEqualTo(0.95f));
            Assert.That(SquareFlowVisualMetrics.ActiveOrbiterGlowScale, Is.GreaterThanOrEqualTo(1.5f));
            Assert.That(SquareFlowVisualMetrics.ActiveOrbiterWorldScale, Is.EqualTo(1f));
            Assert.That(SquareFlowVisualMetrics.ActiveOrbiterTokenScale * SquareFlowVisualMetrics.ActiveOrbiterWorldScale, Is.InRange(0.9f, 1.05f));
            Assert.That(SquareFlowVisualMetrics.ActiveOrbiterLaunchDurationSeconds, Is.EqualTo(0.5f));
            Assert.That(SquareFlowVisualMetrics.ActiveOrbiterAmmoLabelFontSize, Is.EqualTo(3.5f));
            Assert.That(SquareFlowVisualMetrics.ActiveOrbiterAmmoParticleScale, Is.EqualTo(0.5f));
            Assert.That(SquareFlowVisualMetrics.ShooterButtonMinimumDiameter, Is.GreaterThanOrEqualTo(74f));
            Assert.That(SquareFlowVisualMetrics.CellDepthOffsetScale, Is.GreaterThanOrEqualTo(0.08f));
            Assert.That(SquareFlowVisualMetrics.CellLabelFontSize, Is.EqualTo(3.5f));
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
        public void BoardWorldViewTintsOnlyGridCellTrayFaces()
        {
            GameObject host = new GameObject("BoardWorldViewTintTest");

            try
            {
                BoardShape shape = new BoardShape("TintTest", BoardShape.Mask(new[]{1, 1}));
                BoardCell[,] grid = new BoardCell[1, 2];
                grid[0, 0] = BoardCell.Empty;
                grid[0, 1] = BoardCell.Normal(FlowColor.Red, 2);
                GameState state = GameState.Create(shape, grid, new List<Shooter>[0], 1);
                BoardLayout board = BoardLayout.Compute(shape.Rows, shape.Cols, 620f);
                BoardWorldView view = host.AddComponent<BoardWorldView>();

                view.Bind(state, board, MobileWorldLayout.Create(board), new SquareFlowTheme(false));

                SpriteRenderer trayFace = host.transform.Find("WorldCell_0_0/Face").GetComponent<SpriteRenderer>();
                SpriteRenderer blockFace = host.transform.Find("WorldCell_0_1/Face").GetComponent<SpriteRenderer>();
                Assert.That(trayFace.sprite.texture.name, Is.EqualTo("FlowGridCellTray"));
                Assert.That(trayFace.color, Is.EqualTo((Color)new Color32(220, 220, 220, 175)));
                Assert.That(blockFace.sprite.texture.name, Is.EqualTo("FlowBlockRed"));
                Assert.That(blockFace.color, Is.EqualTo(Color.white));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void TmpExamplePrefabsDoNotKeepObsoleteCanvasRenderer()
        {
            string[] paths =
            {
                "Assets/TextMesh Pro/Examples & Extras/Prefabs/TextMeshPro - Prefab 1.prefab",
                "Assets/TextMesh Pro/Examples & Extras/Prefabs/TextMeshPro - Prefab 2.prefab"
            };

            for (int i = 0; i < paths.Length; i++)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(paths[i]);

                Assert.That(prefab, Is.Not.Null, paths[i]);
                Assert.That(prefab.GetComponent<TMPro.TextMeshPro>(), Is.Not.Null, paths[i]);
                Assert.That(prefab.GetComponent<CanvasRenderer>(), Is.Null, paths[i]);
            }
        }

        [Test]
        public void ReferenceGameplayLayoutKeepsCanvasForHudQueueAndDock()
        {
            BoardLayout board = BoardLayout.Compute(5, 5, 620f);
            SquareFlowGameplayScreenLayout layout = SquareFlowGameplayScreenLayout.Create(board);

            Assert.That(layout.HudSize.x, Is.EqualTo(1080f));
            Assert.That(layout.HudSize.y, Is.EqualTo(128f));
            Assert.That(layout.HudPosition, Is.EqualTo(new Vector2(0f, 896f)));
            Assert.That(layout.StatusBarSize, Is.EqualTo(new Vector2(1080f, 86f)));
            Assert.That(layout.StatusBarTopOffset, Is.EqualTo(136f));
            Assert.That(layout.ActionSize.x, Is.EqualTo(276f));
            Assert.That(layout.ActionPosition.x, Is.EqualTo(180f));
            Assert.That(layout.ActionPosition.y, Is.EqualTo(0f));
            Assert.That(layout.UtilityButtonSize, Is.EqualTo(new Vector2(78f, 78f)));
            Assert.That(layout.OrbiterStripSize, Is.EqualTo(new Vector2(1064f, 92f)));
            Assert.That(layout.OrbiterStripPosition, Is.EqualTo(new Vector2(0f, 678f)));
            Assert.That(layout.OrbiterStripTopOffset, Is.EqualTo(236f));
            Assert.That(layout.BoardPanelSize, Is.EqualTo(new Vector2(1080f, 1080f)));
            Assert.That(layout.BoardPanelPosition, Is.EqualTo(new Vector2(0f, 130f)));
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
            GameObject hiddenParent = new GameObject("HiddenShooterParent", typeof(RectTransform));

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
                AssertReadableAutosizedText(selectableLabel, SquareFlowVisualMetrics.ShooterAmmoLabelFontSize);

                InvokeAddShooterToken(controller, queuedParent.GetComponent<RectTransform>(), new Shooter("queued", FlowColor.Red, 2, false), false);
                Transform queuedToken = queuedParent.transform.Find("ShooterPreview");
                Transform queuedDots = queuedToken.Find("AmmoDots");
                TMP_Text queuedLabel = queuedToken.Find("AmmoLabel").GetComponent<TMP_Text>();

                Assert.That(queuedDots, Is.Null);
                Assert.That(queuedLabel.text, Is.EqualTo("2"));
                AssertReadableAutosizedText(queuedLabel, SquareFlowVisualMetrics.ShooterAmmoLabelQueuedFontSize);

                InvokeAddShooterToken(controller, hiddenParent.GetComponent<RectTransform>(), new Shooter("hidden", FlowColor.Green, 6, false, true), false);
                Transform hiddenToken = hiddenParent.transform.Find("ShooterPreview");
                Image hiddenImage = hiddenToken.GetComponent<Image>();
                TMP_Text hiddenLabel = hiddenToken.Find("AmmoLabel").GetComponent<TMP_Text>();
                Assert.That(hiddenImage.sprite.texture.name, Is.EqualTo("SquareFlowShooterCircle"));
                Assert.That(hiddenImage.color.a, Is.GreaterThanOrEqualTo(0.76f));
                Assert.That(hiddenLabel.text, Is.EqualTo("?"));
                AssertReadableAutosizedText(hiddenLabel, SquareFlowVisualMetrics.ShooterAmmoLabelQueuedFontSize);
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(selectableParent);
                Object.DestroyImmediate(queuedParent);
                Object.DestroyImmediate(hiddenParent);
            }
        }

        [Test]
        public void WaitingQueueUsesFixedFullSizeShooterTokensWithoutInheritedScale()
        {
            GameObject host = new GameObject("SquareFlowControllerHost");
            GameObject queueObject = new GameObject("WaitingQueueTest", typeof(RectTransform));

            try
            {
                SquareFlowGameController controller = host.AddComponent<SquareFlowGameController>();
                if (host.transform.Find("SquareFlowCanvas") == null)
                    InvokePrivate(controller, "Awake");

                BoardShape shape = BoardShapeCatalog.GetShape(1);
                GameState state = GameState.Create(shape, new BoardCell[shape.Rows, shape.Cols], new List<Shooter>[0], 1);
                state.WaitingQueue.Add(new Shooter("wait-a", FlowColor.Green, 2, false));
                state.WaitingQueue.Add(new Shooter("wait-b", FlowColor.Blue, 1, false));
                SetPrivateField(controller, "state", state);

                RectTransform queue = queueObject.GetComponent<RectTransform>();
                InvokePrivate(controller, "RenderWaiting", queue);

                RectTransform[] waitingSlots = NamedChildren(queue, "WaitingSlot");
                Assert.That(waitingSlots.Length, Is.EqualTo(SquareFlowConstants.WaitQueueLimit));
                Assert.That(waitingSlots[0].sizeDelta, Is.EqualTo(Vector2.one * 80f));
                Assert.That(waitingSlots[0].localScale, Is.EqualTo(Vector3.one));
                Assert.That(waitingSlots[0].GetComponent<Image>().color.a, Is.EqualTo(1f).Within(0.001f));

                RectTransform[] waitingButtons = NamedChildren(queue, "ShooterButton");
                Assert.That(waitingButtons.Length, Is.EqualTo(2));
                Assert.That(waitingButtons[0].sizeDelta, Is.EqualTo(Vector2.one * 80f));
                Assert.That(waitingButtons[0].localScale, Is.EqualTo(Vector3.one));
                Assert.That(waitingButtons[1].anchoredPosition.x - waitingButtons[0].anchoredPosition.x, Is.EqualTo(86f).Within(0.001f));
                Assert.That(RightEdge(waitingButtons[0]), Is.LessThan(LeftEdge(waitingButtons[1])));
                Assert.That(waitingSlots[0].anchoredPosition.y, Is.EqualTo(0f).Within(0.001f));
                Assert.That(waitingButtons[0].anchoredPosition.y, Is.EqualTo(0f).Within(0.001f));
                TMP_Text benchLabel = FindText(queue, "BENCH");
                TMP_Text countLabel = FindText(queue, "2/5");
                AssertReadableAutosizedText(benchLabel, 24f);
                AssertReadableAutosizedText(countLabel, 31f);
                Assert.That(benchLabel.rectTransform.anchoredPosition.x, Is.EqualTo(50f).Within(0.001f));
                Assert.That(countLabel.rectTransform.anchoredPosition.x, Is.EqualTo(-50f).Within(0.001f));
                Assert.That(benchLabel.rectTransform.anchorMin, Is.EqualTo(new Vector2(0f, 0.5f)));
                Assert.That(benchLabel.rectTransform.anchorMax, Is.EqualTo(new Vector2(0f, 0.5f)));
                Assert.That(benchLabel.rectTransform.pivot, Is.EqualTo(new Vector2(0f, 0.5f)));
                Assert.That(countLabel.rectTransform.anchorMin, Is.EqualTo(new Vector2(1f, 0.5f)));
                Assert.That(countLabel.rectTransform.anchorMax, Is.EqualTo(new Vector2(1f, 0.5f)));
                Assert.That(countLabel.rectTransform.pivot, Is.EqualTo(new Vector2(1f, 0.5f)));
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(queueObject);
            }
        }

        [Test]
        public void ShooterTokensUseTextureSpritesByColor()
        {
            GameObject host = new GameObject("SquareFlowControllerHost");
            GameObject selectableParent = new GameObject("SelectableShooterParent", typeof(RectTransform));
            GameObject wildParent = new GameObject("WildShooterParent", typeof(RectTransform));

            try
            {
                SquareFlowGameController controller = host.AddComponent<SquareFlowGameController>();
                SetPrivateField(controller, "theme", new SquareFlowTheme(true));
                InvokePrivate(controller, "EnsureRuntimeSprites");

                InvokeAddShooterToken(controller, selectableParent.GetComponent<RectTransform>(), new Shooter("blue", FlowColor.Blue, 3, false), true);
                Image blueImage = selectableParent.transform.Find("ShooterButton").GetComponent<Image>();
                Assert.That(blueImage.sprite.texture.name, Is.EqualTo("FlowOrbitBlue"));
                Assert.That(blueImage.color, Is.EqualTo(Color.white));

                InvokeAddShooterToken(controller, wildParent.GetComponent<RectTransform>(), new Shooter("wild", FlowColor.Wild, 2, true), true);
                Image wildImage = wildParent.transform.Find("ShooterButton").GetComponent<Image>();
                Assert.That(wildImage.sprite.texture.name, Is.EqualTo("FlowOrbitWild"));
                Assert.That(wildImage.sprite.texture.name, Is.Not.EqualTo("FlowOrbitOrange"));
                Assert.That(wildImage.color, Is.EqualTo(Color.white));
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(selectableParent);
                Object.DestroyImmediate(wildParent);
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

                InvokePrivate(controller, "SelectLevel", 2);

                Transform canvas = host.transform.Find("SquareFlowCanvas");
                Assert.That(canvas, Is.Not.Null);
                Image canvasBackground = canvas.GetComponent<Image>();
                Assert.That(canvasBackground.sprite.texture.name, Is.EqualTo("FlowSkyBackground"));
                Assert.That(canvasBackground.color, Is.EqualTo(Color.white));
                CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
                Assert.That(scaler, Is.Not.Null);
                Assert.That(scaler.screenMatchMode, Is.EqualTo(CanvasScaler.ScreenMatchMode.Expand));
                Transform panel = canvas.Find("MenuPanel");
                Assert.That(panel, Is.Not.Null);
                Assert.That(panel.GetComponent<Image>().color.a, Is.EqualTo(0f));
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

                Transform logo = content.Find("PixelFlowLogo");
                Assert.That(logo, Is.Not.Null);
                RectTransform logoRect = logo.GetComponent<RectTransform>();
                Assert.That(logoRect.anchoredPosition, Is.EqualTo(new Vector2(0f, 465f)));
                Assert.That(logoRect.sizeDelta, Is.EqualTo(new Vector2(920f, 435f)));
                Image logoImage = logo.GetComponent<Image>();
                Assert.That(logoImage, Is.Not.Null);
                Assert.That(logoImage.sprite, Is.Not.Null);
                Assert.That(logoImage.sprite.texture.name, Is.EqualTo("PixelFlowLogo"));
                Assert.That(logoImage.preserveAspect, Is.True);
                Assert.That(logoImage.raycastTarget, Is.False);
                Assert.That(FindText(content, "Square Flow"), Is.Null);
                Assert.That(content.Find("MenuTitleGlow"), Is.Null);
                Assert.That(content.Find("MenuSwatches"), Is.Null);
                Assert.That(content.Find("ThemeToggle"), Is.Null);
                Assert.That(content.Find("MenuStatsCard"), Is.Not.Null);
                Assert.That(content.Find("InstructionsCard"), Is.Null);
                Assert.That(content.Find("LevelSelector"), Is.Not.Null);
                Assert.That(content.Find("MenuStatsCard").GetComponent<RectTransform>().sizeDelta.x, Is.EqualTo(800f));
                Assert.That(content.Find("MenuStatsCard").GetComponent<RectTransform>().anchoredPosition.y, Is.EqualTo(25f));
                Assert.That(content.Find("LevelSelector").GetComponent<RectTransform>().anchoredPosition.y, Is.EqualTo(-210f));
                Transform statsCard = content.Find("MenuStatsCard");
                TMP_Text levelLabel = FindText(statsCard, "LEVEL");
                TMP_Text boardLabel = FindText(statsCard, "BOARD");
                TMP_Text levelValue = FindText(statsCard, "2");
                TMP_Text boardValue = FindText(statsCard, "Dino");
                AssertReadableAutosizedText(levelLabel, 55f);
                AssertReadableAutosizedText(boardLabel, 55f);
                AssertReadableAutosizedText(levelValue, 80f);
                Assert.That(boardValue.enableAutoSizing, Is.True);
                Assert.That(boardValue.fontSizeMin, Is.EqualTo(50f).Within(0.001f));
                Assert.That(boardValue.fontSizeMax, Is.EqualTo(160f).Within(0.001f));
                Assert.That(levelLabel.rectTransform.sizeDelta, Is.EqualTo(new Vector2(360f, 68f)));
                Assert.That(boardLabel.rectTransform.sizeDelta, Is.EqualTo(new Vector2(360f, 68f)));
                Assert.That(levelValue.rectTransform.sizeDelta, Is.EqualTo(new Vector2(360f, 95f)));
                Assert.That(boardValue.rectTransform.sizeDelta, Is.EqualTo(new Vector2(300f, 95f)));
                Assert.That(TopEdge(levelLabel.rectTransform), Is.LessThanOrEqualTo(74f));
                Assert.That(TopEdge(boardLabel.rectTransform), Is.LessThanOrEqualTo(74f));
                Assert.That(BottomEdge(levelValue.rectTransform), Is.GreaterThanOrEqualTo(-77f));
                Assert.That(BottomEdge(boardValue.rectTransform), Is.GreaterThanOrEqualTo(-77f));
                Assert.That(levelValue.color, Is.EqualTo((Color)new Color32(255, 220, 54, 255)));
                Assert.That(boardValue.color, Is.EqualTo((Color)new Color32(255, 220, 54, 255)));
                Assert.That(levelLabel.color, Is.EqualTo((Color)new Color32(173, 165, 255, 255)));
                Assert.That(boardLabel.color, Is.EqualTo((Color)new Color32(173, 165, 255, 255)));

                TMP_Text playLabel = FindText(content, "PLAY");
                RectTransform playButton = playLabel.transform.parent.GetComponent<RectTransform>();
                Assert.That(playButton.gameObject.name, Is.EqualTo("PlayButton"));
                Assert.That(playButton.sizeDelta.x, Is.EqualTo(410f));
                Assert.That(playButton.sizeDelta.y, Is.EqualTo(128f));
                Assert.That(playButton.anchoredPosition.y, Is.EqualTo(-485f));
                AssertReadableAutosizedText(playLabel, 44f);
                Assert.That(playLabel.color, Is.EqualTo(Color.white));
                Transform shine = playButton.Find("PlayButtonShine");
                Assert.That(shine, Is.Not.Null);
                Assert.That(shine.GetComponent<Image>().raycastTarget, Is.False);
                Transform resetButton = content.Find("ResetAllButton");
                Assert.That(resetButton, Is.Not.Null);
                Assert.That(resetButton.GetComponent<RectTransform>().anchoredPosition.y, Is.EqualTo(-615f));
                Assert.That(resetButton.GetComponent<RectTransform>().anchoredPosition.y, Is.LessThan(playButton.anchoredPosition.y));
                TMP_Text resetLabel = FindText(resetButton, "Reset All");
                AssertReadableAutosizedText(resetLabel, 18f);
                Assert.That(resetLabel.color, Is.EqualTo(Color.white));
                Assert.That(FindText(content.Find("MenuStatsCard"), "MAX ORBS"), Is.Null);
                Assert.That(FindText(content, "HP blocks"), Is.Null);
                AssertAllTextsUseReadableAutosizing(content);
                AssertAllTextsUseOutlineWidth(content, 0.5f);

                Assert.That(content.GetComponentsInChildren<Text>().Length, Is.EqualTo(0));

                Transform selector = content.Find("LevelSelector");
                int levelButtonCount = 0;
                int topRowCount = 0;
                int bottomRowCount = 0;
                TMP_Text[] labels = selector.GetComponentsInChildren<TMP_Text>();
                for (int i = 0; i < labels.Length; i++)
                {
                    int level;
                    if (!int.TryParse(labels[i].text, out level)) continue;

                    RectTransform button = labels[i].transform.parent.GetComponent<RectTransform>();
                    Assert.That(button.sizeDelta.x, Is.EqualTo(126f));
                    Assert.That(button.sizeDelta.y, Is.EqualTo(92f));
                    AssertReadableAutosizedText(labels[i], 32f);
                    Assert.That(labels[i].color, Is.EqualTo(level == 2 ? (Color)new Color32(255, 220, 54, 255) : Color.white));
                    if (button.anchoredPosition.y > 0f)
                        topRowCount++;
                    else
                        bottomRowCount++;
                    levelButtonCount++;
                }

                Assert.That(levelButtonCount, Is.EqualTo(BoardShapeCatalog.Count));
                Assert.That(topRowCount, Is.EqualTo(5));
                Assert.That(bottomRowCount, Is.EqualTo(5));
                Assert.That(FindText(content, "Leaderboard"), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void MainMenuUsesGuiProSkinWithoutAddingControls()
        {
            GameObject host = new GameObject("SquareFlowControllerHost");

            try
            {
                SquareFlowGameController controller = host.AddComponent<SquareFlowGameController>();
                InvokePrivate(controller, "Awake");
                InvokePrivate(controller, "ShowMenu");

                Transform canvas = host.transform.Find("SquareFlowCanvas");
                Assert.That(canvas, Is.Not.Null);
                Transform content = canvas.Find("MenuPanel/MenuContent");
                Assert.That(content, Is.Not.Null);

                Transform logo = content.Find("PixelFlowLogo");
                Assert.That(logo, Is.Not.Null);
                Assert.That(logo.GetComponent<Image>().sprite.texture.name, Is.EqualTo("PixelFlowLogo"));
                Assert.That(FindText(content, "Square Flow"), Is.Null);
                AssertGuiProPanel(content.Find("MenuStatsCard"));
                Assert.That(content.Find("InstructionsCard"), Is.Null);
                AssertGuiProPanel(content.Find("LevelSelector"), "BasicFrame_Round12");

                Transform playButton = content.Find("PlayButton");
                AssertGuiProButton(playButton, "Button01_225_Yellow");
                AssertGuiProFont(FindText(playButton, "PLAY"));

                Transform resetButton = content.Find("ResetAllButton");
                AssertGuiProButton(resetButton, "Button01_175_Red");
                AssertGuiProFont(FindText(resetButton, "Reset All"));
                Assert.That(resetButton.GetComponent<RectTransform>().anchoredPosition.y, Is.LessThan(playButton.GetComponent<RectTransform>().anchoredPosition.y));

                Transform statsCard = content.Find("MenuStatsCard");
                Assert.That(FindText(statsCard, "LEVEL"), Is.Not.Null);
                Assert.That(FindText(statsCard, "BOARD"), Is.Not.Null);
                Assert.That(FindText(statsCard, "MAX ORBS"), Is.Null);
                AssertAllGuiProFonts(statsCard);
                Assert.That(FindText(content, "HP blocks"), Is.Null);
                Assert.That(FindText(content, "Wild"), Is.Null);

                Transform levelSelector = content.Find("LevelSelector");
                AssertAllGuiProFonts(levelSelector);
                AssertAllTextsUseOutlineWidth(content, 0.5f);

                Button[] buttons = content.GetComponentsInChildren<Button>(true);
                Assert.That(buttons.Length, Is.EqualTo(BoardShapeCatalog.Count + 2));
                RectTransform[] levelButtons = NamedChildren(levelSelector, "LevelButton");
                Assert.That(levelButtons.Length, Is.EqualTo(BoardShapeCatalog.Count));
                for (int i = 0; i < levelButtons.Length; i++)
                {
                    AssertGuiProButtonTextureStartsWith(levelButtons[i], "Button01_175_");
                    AssertGuiProFont(FindText(levelButtons[i], (i + 1).ToString()));
                }

                Assert.That(content.Find("ThemeToggle"), Is.Null);
                Assert.That(FindText(content, "Shop"), Is.Null);
                Assert.That(FindText(content, "Inventory"), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void ExpandedCanvasKeepsGameplayBandsSeparatedOnPhonesAndTablets()
        {
            BoardLayout board = BoardLayout.Compute(5, 5, 620f);
            Vector2 referenceResolution = new Vector2(1080f, 1920f);

            AssertGameplayBandsDoNotOverlap("iPhone 14 Pro Max", new Vector2(1290f, 2796f), referenceResolution, board);
            AssertGameplayBandsDoNotOverlap("iPad Pro 12.9", new Vector2(2048f, 2732f), referenceResolution, board);
            AssertGameplayBandsDoNotOverlap("iPad 10.9", new Vector2(1640f, 2360f), referenceResolution, board);
            AssertGameplayBandsDoNotOverlap("Galaxy Tab portrait", new Vector2(1600f, 2560f), referenceResolution, board);
            AssertGameplayBandsDoNotOverlap("simulator tablet safe area", new Vector2(1768f, 2208f), referenceResolution, board, 76.52f, 0f);
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
                Image canvasBackground = canvas.GetComponent<Image>();
                Assert.That(canvasBackground.color.a, Is.EqualTo(0f));
                Transform worldBackground = host.transform.Find("SquareFlowWorld/WorldBackground");
                Assert.That(worldBackground, Is.Not.Null);
                SpriteRenderer worldBackgroundRenderer = worldBackground.GetComponent<SpriteRenderer>();
                Assert.That(worldBackgroundRenderer.sprite.texture.name, Is.EqualTo("FlowSkyBackground"));
                Assert.That(worldBackgroundRenderer.sortingOrder, Is.LessThan(0));
                Assert.That(host.transform.Find("SquareFlowWorld/WorldBoardPanelBorder"), Is.Null);
                Assert.That(host.transform.Find("SquareFlowWorld/WorldBoardPanel"), Is.Null);
                Transform boardWorldView = host.transform.Find("SquareFlowWorld/BoardWorldView");
                Assert.That(boardWorldView, Is.Not.Null);
                Assert.That(boardWorldView.Find("GridBackdrop"), Is.Null);
                TextMeshPro[] worldCellLabels = boardWorldView.GetComponentsInChildren<TextMeshPro>(true);
                Assert.That(worldCellLabels.Length, Is.GreaterThan(0));
                for (int i = 0; i < worldCellLabels.Length; i++)
                    Assert.That(worldCellLabels[i].rectTransform.localPosition.y, Is.EqualTo(0.05f).Within(0.001f), worldCellLabels[i].name);
                SpriteRenderer[] worldCellRenderers = boardWorldView.GetComponentsInChildren<SpriteRenderer>(true);
                int faceRendererCount = 0;
                int blockFaceRendererCount = 0;
                Color expectedGridCellTrayColor = new Color32(220, 220, 220, 175);
                for (int i = 0; i < worldCellRenderers.Length; i++)
                {
                    if (worldCellRenderers[i].name != "Face") continue;
                    faceRendererCount++;
                    string textureName = worldCellRenderers[i].sprite.texture.name;
                    if (textureName == "FlowGridCellTray")
                        Assert.That(worldCellRenderers[i].color, Is.EqualTo(expectedGridCellTrayColor), worldCellRenderers[i].transform.parent.name);
                    else if (textureName.StartsWith("FlowBlock"))
                    {
                        blockFaceRendererCount++;
                        Assert.That(worldCellRenderers[i].color, Is.EqualTo(Color.white), worldCellRenderers[i].transform.parent.name);
                    }
                }

                Assert.That(faceRendererCount, Is.EqualTo(worldCellLabels.Length));
                Assert.That(blockFaceRendererCount, Is.GreaterThan(0));
                Transform header = canvas.Find("GameHeader");
                Assert.That(header, Is.Not.Null);
                Assert.That(header.GetComponent<Image>(), Is.Null);
                Transform scoreCard = header.Find("ScoreCard");
                Transform bestCard = header.Find("BestCard");
                AssertPanelImage(scoreCard);
                AssertPanelImage(bestCard);
                Assert.That(FindText(scoreCard, "SCORE"), Is.Not.Null);
                Assert.That(FindText(bestCard, "BEST"), Is.Not.Null);
                AssertHeaderIcon(scoreCard, "FlowGem", new Vector2(-90f, 0f), new Vector2(102.5f, 82.5f));
                AssertHeaderIcon(bestCard, "FlowCrown", new Vector2(-90f, 0f), new Vector2(90.2f, 72.6f));
                AssertPanelImage(header.Find("LevelBadge"));
                Assert.That(FindText(header.Find("LevelBadge"), "LEVEL"), Is.Not.Null);
                Assert.That(FindText(scoreCard, "0"), Is.Not.Null);
                AssertNoShapeNameText(header);

                Transform status = canvas.Find("GameStatusBar");
                Assert.That(status, Is.Not.Null);
                AssertPanelImage(status);
                Transform hudActions = status.Find("HudActions");
                Assert.That(hudActions, Is.Not.Null);
                Assert.That(hudActions.Find("HomeButton").GetComponent<Button>(), Is.Not.Null);
                Assert.That(hudActions.Find("RestartButton").GetComponent<Button>(), Is.Not.Null);
                Assert.That(hudActions.Find("MuteButton").GetComponent<Button>(), Is.Not.Null);

                Transform orbiterStrip = canvas.Find("OrbiterStrip");
                Assert.That(orbiterStrip, Is.Not.Null);
                AssertPanelImage(orbiterStrip);
                Transform boardFrame = canvas.Find("BoardFrame");
                Assert.That(boardFrame, Is.Not.Null);
                RectTransform boardFrameRect = boardFrame.GetComponent<RectTransform>();
                Assert.That(boardFrameRect.anchoredPosition, Is.EqualTo(new Vector2(0f, 130f)));
                Image boardFrameImage = boardFrame.GetComponent<Image>();
                Assert.That(boardFrameImage, Is.Not.Null);
                Assert.That(boardFrameImage.color.a, Is.EqualTo(0f));
                Assert.That(boardFrameImage.raycastTarget, Is.False);

                RectTransform titleBarRect = canvas.Find("GameHeader").GetComponent<RectTransform>();
                RectTransform statusBarRect = status.GetComponent<RectTransform>();
                Assert.That(titleBarRect.sizeDelta.y, Is.EqualTo(128f));
                Assert.That(statusBarRect.sizeDelta.y, Is.EqualTo(86f));
                Assert.That(statusBarRect.anchoredPosition.y, Is.EqualTo(-136f));

                TMP_Text moves = FindText(status, "0 MOVES");
                Assert.That(moves, Is.Not.Null);
                AssertReadableAutosizedText(moves, 36f);
                AssertTextOutlineDarkerThanFill(moves);
                Assert.That(RightEdge(moves.rectTransform), Is.LessThan(LeftEdge(hudActions.GetComponent<RectTransform>())));

                Transform strip = canvas.Find("OrbiterStrip");
                TMP_Text orbiterLabel = FindText(strip, "ORBITERS");
                Assert.That(orbiterLabel, Is.Not.Null);
                Assert.That(orbiterLabel.fontSize * orbiterLabel.rectTransform.localScale.x, Is.EqualTo(moves.fontSize).Within(0.001f));
                AssertTextOutlineDarkerThanFill(orbiterLabel);
                Assert.That(orbiterLabel.rectTransform.localScale.x, Is.EqualTo(1.5f).Within(0.001f));
                Assert.That(orbiterLabel.rectTransform.anchoredPosition.x, Is.EqualTo(-240f));
                TMP_Text orbiterCount = FindText(strip, "0/5");
                Assert.That(orbiterCount, Is.Not.Null);
                Assert.That(orbiterCount.rectTransform.localScale.x, Is.EqualTo(1.5f).Within(0.001f));
                Assert.That(orbiterCount.rectTransform.anchoredPosition.x, Is.EqualTo(285f));
                RectTransform[] orbiterDots = NamedChildren(strip, "OrbiterDot");
                Assert.That(orbiterDots.Length, Is.EqualTo(SquareFlowConstants.MaxActiveOrbiters));
                Assert.That(orbiterDots[0].anchoredPosition.x, Is.EqualTo(-70f));
                Assert.That(orbiterDots[2].anchoredPosition.x, Is.EqualTo(40f).Within(0.001f));
                Assert.That(orbiterDots[1].anchoredPosition.x - orbiterDots[0].anchoredPosition.x, Is.EqualTo(55f).Within(0.001f));
                Assert.That(orbiterDots[0].localScale.x, Is.EqualTo(2.94f).Within(0.001f));
                Assert.That(RightEdge(orbiterLabel.rectTransform), Is.LessThan(LeftEdge(orbiterDots[0])));
                Assert.That(RightEdge(orbiterDots[orbiterDots.Length - 1]), Is.LessThan(LeftEdge(orbiterCount.rectTransform)));

                Transform waiting = canvas.Find("WaitingQueue");
                Assert.That(waiting, Is.Not.Null);
                AssertPanelImage(waiting);
                Assert.That(waiting.GetComponent<Image>().color.a, Is.EqualTo(100f / 255f).Within(0.001f));
                RectTransform waitingRect = waiting.GetComponent<RectTransform>();
                Assert.That(waitingRect.anchorMin, Is.EqualTo(new Vector2(0f, 0.5f)));
                Assert.That(waitingRect.anchorMax, Is.EqualTo(new Vector2(1f, 0.5f)));
                Assert.That(waitingRect.pivot, Is.EqualTo(new Vector2(0.5f, 0.5f)));
                Assert.That(waitingRect.anchoredPosition, Is.EqualTo(new Vector2(0f, -336f)));
                Assert.That(waitingRect.sizeDelta, Is.EqualTo(new Vector2(-160f, 80f)));
                Assert.That(waitingRect.offsetMin, Is.EqualTo(new Vector2(80f, -376f)));
                Assert.That(waitingRect.offsetMax, Is.EqualTo(new Vector2(-80f, -296f)));
                Assert.That(waitingRect.localPosition.z, Is.EqualTo(0f).Within(0.001f));
                Assert.That(waitingRect.localEulerAngles, Is.EqualTo(Vector3.zero));
                Assert.That(waitingRect.localScale, Is.EqualTo(Vector3.one));

                RectTransform[] waitingSlots = NamedChildren(waiting, "WaitingSlot");
                Assert.That(waitingSlots.Length, Is.EqualTo(SquareFlowConstants.WaitQueueLimit));
                Assert.That(waitingSlots[0].sizeDelta, Is.EqualTo(Vector2.one * 80f));
                Assert.That(waitingSlots[0].localScale, Is.EqualTo(Vector3.one));
                Assert.That(waitingSlots[0].GetComponent<Image>().color.a, Is.EqualTo(1f).Within(0.001f));
                TMP_Text waitingLabel = FindText(waiting, "BENCH");
                Assert.That(waitingLabel, Is.Not.Null);
                AssertReadableAutosizedText(waitingLabel, 24f);
                TMP_Text waitingCount = FindText(waiting, "0/5");
                Assert.That(waitingCount, Is.Not.Null);
                AssertReadableAutosizedText(waitingCount, 31f);
                Assert.That(waitingLabel.rectTransform.anchoredPosition.x, Is.EqualTo(50f).Within(0.001f));
                Assert.That(waitingCount.rectTransform.anchoredPosition.x, Is.EqualTo(-50f).Within(0.001f));
                Assert.That(waitingLabel.rectTransform.anchorMin, Is.EqualTo(new Vector2(0f, 0.5f)));
                Assert.That(waitingLabel.rectTransform.anchorMax, Is.EqualTo(new Vector2(0f, 0.5f)));
                Assert.That(waitingLabel.rectTransform.pivot, Is.EqualTo(new Vector2(0f, 0.5f)));
                Assert.That(waitingCount.rectTransform.anchorMin, Is.EqualTo(new Vector2(1f, 0.5f)));
                Assert.That(waitingCount.rectTransform.anchorMax, Is.EqualTo(new Vector2(1f, 0.5f)));
                Assert.That(waitingCount.rectTransform.pivot, Is.EqualTo(new Vector2(1f, 0.5f)));
                Assert.That(waitingLabel.rectTransform.anchoredPosition.y, Is.EqualTo(0f).Within(0.001f));
                Assert.That(waitingCount.rectTransform.anchoredPosition.y, Is.EqualTo(0f).Within(0.001f));
                AssertAllTextsUseReadableAutosizing(canvas);
                float waitingY = waitingSlots[0].anchoredPosition.y;
                Assert.That(waitingY, Is.EqualTo(0f).Within(0.001f));
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
                Assert.That(columnsRect.pivot, Is.EqualTo(new Vector2(0.5f, 0.5f)));
                Assert.That(columnsRect.anchoredPosition, Is.EqualTo(new Vector2(0f, 70f)));
                Assert.That(columnsRect.offsetMin, Is.EqualTo(new Vector2(40f, -170f)));
                Assert.That(columnsRect.offsetMax, Is.EqualTo(new Vector2(-40f, 310f)));
                Assert.That(columnsRect.localPosition.z, Is.EqualTo(0f).Within(0.001f));
                Assert.That(columnsRect.localEulerAngles, Is.EqualTo(Vector3.zero));
                Assert.That(columnsRect.localScale, Is.EqualTo(Vector3.one));
                Assert.That(columnsRect.sizeDelta, Is.EqualTo(new Vector2(-80f, 480f)));
                RectTransform[] cards = NamedChildren(columns, "ShooterColumnCard");
                Assert.That(cards.Length, Is.EqualTo(3));
                Assert.That(cards[0].sizeDelta, Is.EqualTo(new Vector2(288f, 500f)));
                for (int i = 0; i < cards.Length; i++)
                {
                    Assert.That(cards[i].anchoredPosition.y, Is.EqualTo(240f).Within(0.001f));
                    AssertPanelImage(cards[i]);
                    Assert.That(cards[i].GetComponent<Image>().color.a, Is.EqualTo(100f / 255f).Within(0.001f));
                }
                Assert.That(LeftEdge(cards[1]) - RightEdge(cards[0]), Is.GreaterThanOrEqualTo(60f));
                Assert.That(LeftEdge(cards[2]) - RightEdge(cards[1]), Is.GreaterThanOrEqualTo(60f));
                float cardTopInCanvas = -960f + columnsRect.anchoredPosition.y + cards[0].anchoredPosition.y + cards[0].sizeDelta.y * 0.5f;
                float queueBottomInCanvas = waitingRect.anchoredPosition.y - waitingRect.sizeDelta.y * 0.5f;
                Assert.That(cardTopInCanvas, Is.LessThanOrEqualTo(queueBottomInCanvas - 23.999f));

                RectTransform firstDockSlot = cards[0].Find("DockSlotFront").GetComponent<RectTransform>();
                Assert.That(firstDockSlot.sizeDelta, Is.EqualTo(Vector2.one * 112f));
                Assert.That(firstDockSlot.localScale, Is.EqualTo(Vector3.one));
                RectTransform firstDockToken = firstDockSlot.Find("ShooterButton").GetComponent<RectTransform>();
                Assert.That(firstDockToken.sizeDelta, Is.EqualTo(Vector2.one * 112f));

                RectTransform[] queuedDockSlots = NamedChildren(cards[0], "DockSlotQueued");
                Assert.That(queuedDockSlots.Length, Is.EqualTo(SquareFlowGameplayScreenLayout.ShooterColumnVisibleRows - 1));
                RectTransform firstQueuedDockSlot = queuedDockSlots[0];
                Assert.That(firstQueuedDockSlot.sizeDelta, Is.EqualTo(Vector2.one * 112f));
                Assert.That(firstQueuedDockSlot.localScale, Is.EqualTo(Vector3.one));
                Assert.That(firstDockSlot.anchoredPosition.y - firstQueuedDockSlot.anchoredPosition.y, Is.EqualTo(116f).Within(0.001f));
                Assert.That(BottomEdge(firstDockSlot), Is.GreaterThan(TopEdge(firstQueuedDockSlot)));
                RectTransform firstQueuedToken = firstQueuedDockSlot.Find("ShooterPreview").GetComponent<RectTransform>();
                Assert.That(firstQueuedToken.sizeDelta, Is.EqualTo(Vector2.one * 112f));
                AssertReadableAutosizedText(FindText(cards[0], "A"), 52f);
                AssertReadableAutosizedText(FindText(cards[1], "B"), 52f);
                AssertReadableAutosizedText(FindText(cards[2], "C"), 52f);
                Assert.That(columns.GetComponentsInChildren<Button>().Length, Is.EqualTo(3));

                GameState gameState = GetPrivateField<GameState>(controller, "state");
                gameState.Result = GameResult.LostOutOfShooters;
                InvokePrivate(controller, "ShowResultPanel");
                Transform resultPanel = canvas.Find("ResultPanel");
                Assert.That(resultPanel, Is.Not.Null);
                AssertReadableAutosizedText(FindText(resultPanel, "Out of Shooters"), 42f);
                AssertReadableAutosizedText(FindText(resultPanel, "Try Again"), 24f);
                AssertAllTextsUseReadableAutosizing(resultPanel);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void GameplayUsesGuiProSkinWithoutMovingControls()
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

                Transform header = canvas.Find("GameHeader");
                Transform status = canvas.Find("GameStatusBar");
                Transform orbiterStrip = canvas.Find("OrbiterStrip");
                Transform waiting = canvas.Find("WaitingQueue");
                Transform columns = canvas.Find("ShooterColumns");

                Assert.That(header, Is.Not.Null);
                AssertRectTransformLayout(
                    header,
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(0f, 128f),
                    new Vector2(0f, -128f),
                    Vector2.zero);
                AssertRectTransformLayout(
                    header.Find("ScoreCard"),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(300f, 104f),
                    new Vector2(-470f, -52f),
                    new Vector2(-170f, 52f));
                AssertGuiProPanel(header.Find("ScoreCard"));
                AssertHeaderIcon(header.Find("ScoreCard"), "FlowGem", new Vector2(-90f, 0f), new Vector2(102.5f, 82.5f));
                AssertRectTransformLayout(
                    header.Find("BestCard"),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(300f, 104f),
                    new Vector2(-150f, -52f),
                    new Vector2(150f, 52f));
                AssertGuiProPanel(header.Find("BestCard"));
                AssertHeaderIcon(header.Find("BestCard"), "FlowCrown", new Vector2(-90f, 0f), new Vector2(90.2f, 72.6f));
                AssertRectTransformLayout(
                    header.Find("LevelBadge"),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(300f, 104f),
                    new Vector2(170f, -52f),
                    new Vector2(470f, 52f));
                AssertGuiProPanel(header.Find("LevelBadge"));
                Assert.That(FindText(header.Find("LevelBadge"), "LEVEL"), Is.Not.Null);
                AssertNoShapeNameText(header);
                AssertGuiProPanel(status);
                AssertGuiProPanel(orbiterStrip);
                AssertGuiProPanel(waiting, "BasicFrame_Round20", 100f / 255f);

                AssertRectTransformLayout(
                    orbiterStrip,
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(-160f, 92f),
                    new Vector2(80f, -328f),
                    new Vector2(-80f, -236f));

                RectTransform statusRect = status.GetComponent<RectTransform>();
                AssertRectTransformLayout(
                    status,
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(-160f, 86f),
                    new Vector2(80f, -222f),
                    new Vector2(-80f, -136f));
                Assert.That(statusRect.sizeDelta.y, Is.EqualTo(86f));
                Assert.That(statusRect.anchoredPosition.y, Is.EqualTo(-136f));

                Transform hudActions = status.Find("HudActions");
                Assert.That(hudActions, Is.Not.Null);
                AssertRectTransformLayout(
                    hudActions,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(276f, 76f),
                    new Vector2(42f, -38f),
                    new Vector2(318f, 38f));
                Assert.That(hudActions.GetComponentsInChildren<Button>().Length, Is.EqualTo(3));
                AssertRectTransformLayout(
                    hudActions.Find("HomeButton"),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(78f, 78f),
                    new Vector2(-123f, -39f),
                    new Vector2(-45f, 39f));
                AssertGuiProIconButton(hudActions.Find("HomeButton"), "FlowHomeButton");
                AssertRectTransformLayout(
                    hudActions.Find("RestartButton"),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(78f, 78f),
                    new Vector2(-39f, -39f),
                    new Vector2(39f, 39f));
                AssertGuiProIconButton(hudActions.Find("RestartButton"), "FlowRestartButton");
                AssertRectTransformLayout(
                    hudActions.Find("MuteButton"),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(78f, 78f),
                    new Vector2(45f, -39f),
                    new Vector2(123f, 39f));
                AssertGuiProIconButton(hudActions.Find("MuteButton"), "FlowMuteButton");

                AssertAllGuiProFonts(header);
                AssertAllGuiProFonts(status);
                AssertAllGuiProFonts(orbiterStrip);
                AssertAllGuiProFonts(waiting);

                AssertRectTransformLayout(
                    waiting,
                    new Vector2(0f, 0.5f),
                    new Vector2(1f, 0.5f),
                    new Vector2(-160f, 80f),
                    new Vector2(80f, -376f),
                    new Vector2(-80f, -296f));
                Assert.That(waiting.GetComponent<RectTransform>().pivot, Is.EqualTo(new Vector2(0.5f, 0.5f)));
                Assert.That(waiting.GetComponent<RectTransform>().anchoredPosition, Is.EqualTo(new Vector2(0f, -336f)));

                AssertRectTransformLayout(
                    columns,
                    new Vector2(0f, 0f),
                    new Vector2(1f, 0f),
                    new Vector2(-80f, 480f),
                    new Vector2(40f, -170f),
                    new Vector2(-40f, 310f));
                Assert.That(columns.GetComponent<RectTransform>().pivot, Is.EqualTo(new Vector2(0.5f, 0.5f)));
                Assert.That(columns.GetComponent<RectTransform>().anchoredPosition.y, Is.EqualTo(70f).Within(0.001f));

                RectTransform[] cards = NamedChildren(columns, "ShooterColumnCard");
                Assert.That(cards.Length, Is.EqualTo(3));
                for (int i = 0; i < cards.Length; i++)
                {
                    AssertRectTransformLayout(
                        cards[i],
                        new Vector2(0.5f, 0.5f),
                        new Vector2(0.5f, 0.5f),
                        new Vector2(288f, 500f),
                        new Vector2(-496f + i * 352f, -10f),
                        new Vector2(-208f + i * 352f, 490f));
                    Assert.That(cards[i].anchoredPosition, Is.EqualTo(new Vector2(-352f + i * 352f, 240f)));
                    AssertGuiProPanel(cards[i], "BasicFrame_Round20", 100f / 255f);
                    AssertAllGuiProFonts(cards[i]);
                }

                Assert.That(columns.GetComponentsInChildren<Button>().Length, Is.EqualTo(3));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        private static void AssertSkyPanel(Transform transform, float expectedAlpha)
        {
            Image image = transform.GetComponent<Image>();
            Assert.That(image, Is.Not.Null);
            Assert.That(image.color.b, Is.GreaterThan(image.color.g));
            Assert.That(image.color.g, Is.GreaterThan(image.color.r));
            Assert.That(image.color.a, Is.EqualTo(expectedAlpha).Within(0.001f));
        }

        private static void AssertGlassPanel(Transform transform)
        {
            Assert.That(transform, Is.Not.Null);
            Image image = transform.GetComponent<Image>();
            Assert.That(image, Is.Not.Null);
            Assert.That(image.sprite, Is.Not.Null);
            Assert.That(image.sprite.texture.name, Is.EqualTo("FlowPanel"));
            Assert.That(image.type, Is.EqualTo(Image.Type.Sliced));
            Assert.That(image.color, Is.EqualTo(Color.white));
            AssertSoftPanelShadow(transform);
        }

        private static void AssertGuiProFont(TMP_Text text)
        {
            Assert.That(text, Is.Not.Null);
            Assert.That(text.font, Is.Not.Null);
            Assert.That(text.font.name, Does.Contain("LilitaOne"));
            Assert.That(text.outlineWidth, Is.GreaterThan(0f));
            AssertTextOutlineDarkerThanFill(text);
        }

        private static void AssertTextOutlineDarkerThanFill(TMP_Text text)
        {
            Assert.That(text, Is.Not.Null);
            Color fill = text.color;
            Color outline = text.outlineColor;
            Assert.That(outline.a, Is.GreaterThan(0f));
            if (Luminance(fill) > 0.02f)
                Assert.That(Luminance(outline), Is.LessThan(Luminance(fill)));
        }

        private static float Luminance(Color color)
        {
            return color.r * 0.2126f + color.g * 0.7152f + color.b * 0.0722f;
        }

        private static void AssertAllGuiProFonts(Transform parent)
        {
            Assert.That(parent, Is.Not.Null);
            TMP_Text[] labels = parent.GetComponentsInChildren<TMP_Text>(true);
            Assert.That(labels.Length, Is.GreaterThan(0));
            for (int i = 0; i < labels.Length; i++)
                AssertGuiProFont(labels[i]);
        }

        private static void AssertAllTextsUseReadableAutosizing(Transform parent)
        {
            Assert.That(parent, Is.Not.Null);
            TMP_Text[] labels = parent.GetComponentsInChildren<TMP_Text>(true);
            Assert.That(labels.Length, Is.GreaterThan(0));
            for (int i = 0; i < labels.Length; i++)
            {
                Assert.That(labels[i].enableAutoSizing, Is.True, labels[i].text);
                Assert.That(labels[i].fontSizeMax, Is.EqualTo(labels[i].fontSizeMin * 2f).Within(0.001f), labels[i].text);
                Assert.That(labels[i].overflowMode, Is.EqualTo(TextOverflowModes.Truncate), labels[i].text);
            }
        }

        private static void AssertAllTextsUseOutlineWidth(Transform parent, float expectedOutlineWidth)
        {
            Assert.That(parent, Is.Not.Null);
            TMP_Text[] labels = parent.GetComponentsInChildren<TMP_Text>(true);
            Assert.That(labels.Length, Is.GreaterThan(0));
            for (int i = 0; i < labels.Length; i++)
                Assert.That(labels[i].outlineWidth, Is.EqualTo(expectedOutlineWidth).Within(0.001f), labels[i].text);
        }

        private static void AssertReadableAutosizedText(TMP_Text text, float previousSize)
        {
            Assert.That(text, Is.Not.Null);
            Assert.That(text.enableAutoSizing, Is.True, text.text);
            Assert.That(text.fontSizeMin, Is.EqualTo(previousSize).Within(0.001f), text.text);
            Assert.That(text.fontSizeMax, Is.EqualTo(previousSize * 2f).Within(0.001f), text.text);
            Assert.That(text.fontSize, Is.EqualTo(previousSize * 2f).Within(0.001f), text.text);
            Assert.That(text.overflowMode, Is.EqualTo(TextOverflowModes.Truncate), text.text);
        }

        private static void AssertNoShapeNameText(Transform parent)
        {
            foreach (BoardShape shape in BoardShapeCatalog.All)
                Assert.That(FindText(parent, shape.Name), Is.Null, shape.Name + " should not render in the gameplay HUD.");
        }

        private static void AssertGuiProPanel(Transform transform, string expectedTextureName = "BasicFrame_Round20", float expectedAlpha = 1f)
        {
            Assert.That(transform, Is.Not.Null);
            Image image = transform.GetComponent<Image>();
            Assert.That(image, Is.Not.Null);
            Assert.That(image.sprite, Is.Not.Null);
            Assert.That(image.sprite.texture.name, Is.EqualTo(expectedTextureName));
            Assert.That(image.type, Is.EqualTo(Image.Type.Sliced));
            Assert.That(image.color.r, Is.EqualTo(1f).Within(0.001f));
            Assert.That(image.color.g, Is.EqualTo(1f).Within(0.001f));
            Assert.That(image.color.b, Is.EqualTo(1f).Within(0.001f));
            Assert.That(image.color.a, Is.EqualTo(expectedAlpha).Within(0.001f));
            Assert.That(image.pixelsPerUnitMultiplier, Is.EqualTo(0.25f).Within(0.001f));
            AssertSoftPanelShadow(transform);
        }

        private static void AssertGuiProButton(Transform button, string expectedTextureName)
        {
            Assert.That(button, Is.Not.Null);
            Image image = button.GetComponent<Image>();
            Assert.That(image, Is.Not.Null);
            Assert.That(image.sprite, Is.Not.Null);
            Assert.That(image.sprite.texture.name, Is.EqualTo(expectedTextureName));
            Assert.That(image.type, Is.EqualTo(Image.Type.Sliced));
            Assert.That(image.color, Is.EqualTo(Color.white));
            Assert.That(button.GetComponent<Button>(), Is.Not.Null);
        }

        private static void AssertGuiProIconButton(Transform button, string expectedIconTextureName)
        {
            AssertGuiProButtonTextureStartsWith(button, "Button01_100_");
            Image icon = FindChildImage(button, expectedIconTextureName);
            Assert.That(icon, Is.Not.Null);
            Assert.That(icon.sprite, Is.Not.Null);
            Assert.That(icon.sprite.texture.name, Is.EqualTo(expectedIconTextureName));
            Assert.That(icon.preserveAspect, Is.True);
            Assert.That(icon.raycastTarget, Is.False);
        }

        private static void AssertGuiProButtonTextureStartsWith(Transform button, string expectedTextureNamePrefix)
        {
            Assert.That(button, Is.Not.Null);
            Image image = button.GetComponent<Image>();
            Assert.That(image, Is.Not.Null);
            Assert.That(image.sprite, Is.Not.Null);
            Assert.That(image.sprite.texture.name, Does.StartWith(expectedTextureNamePrefix));
            Assert.That(image.type, Is.EqualTo(Image.Type.Sliced));
            Assert.That(image.color, Is.EqualTo(Color.white));
            Assert.That(button.GetComponent<Button>(), Is.Not.Null);
        }

        private static void AssertPanelImage(Transform transform)
        {
            Assert.That(transform, Is.Not.Null);
            Image image = transform.GetComponent<Image>();
            Assert.That(image, Is.Not.Null);
            Assert.That(image.sprite, Is.Not.Null);
            Assert.That(image.type, Is.EqualTo(Image.Type.Sliced));
            Assert.That(image.pixelsPerUnitMultiplier, Is.EqualTo(0.25f).Within(0.001f));
            AssertSoftPanelShadow(transform);
        }

        private static Image FindChildImage(Transform parent, string expectedTextureName)
        {
            Assert.That(parent, Is.Not.Null);
            Image[] images = parent.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i].transform == parent) continue;
                if (images[i].sprite != null && images[i].sprite.texture.name == expectedTextureName)
                    return images[i];
            }

            return null;
        }

        private static void AssertRectTransformLayout(Transform transform, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Vector2 offsetMin, Vector2 offsetMax)
        {
            Assert.That(transform, Is.Not.Null);
            RectTransform rect = transform.GetComponent<RectTransform>();
            Assert.That(rect, Is.Not.Null);
            Assert.That(rect.anchorMin, Is.EqualTo(anchorMin));
            Assert.That(rect.anchorMax, Is.EqualTo(anchorMax));
            Assert.That(rect.sizeDelta, Is.EqualTo(sizeDelta));
            Assert.That(rect.offsetMin, Is.EqualTo(offsetMin));
            Assert.That(rect.offsetMax, Is.EqualTo(offsetMax));
        }

        private static void AssertHeaderIcon(Transform card, string expectedTextureName, Vector2? expectedPosition = null, Vector2? expectedSize = null)
        {
            Transform icon = card.Find("HeaderIcon");
            Assert.That(icon, Is.Not.Null);
            RectTransform rect = icon.GetComponent<RectTransform>();
            Assert.That(rect, Is.Not.Null);
            if (expectedPosition.HasValue)
            {
                Assert.That(rect.anchoredPosition.x, Is.EqualTo(expectedPosition.Value.x).Within(0.001f));
                Assert.That(rect.anchoredPosition.y, Is.EqualTo(expectedPosition.Value.y).Within(0.001f));
            }

            if (expectedSize.HasValue)
            {
                Assert.That(rect.sizeDelta.x, Is.EqualTo(expectedSize.Value.x).Within(0.001f));
                Assert.That(rect.sizeDelta.y, Is.EqualTo(expectedSize.Value.y).Within(0.001f));
            }

            Image image = icon.GetComponent<Image>();
            Assert.That(image, Is.Not.Null);
            Assert.That(image.sprite, Is.Not.Null);
            Assert.That(image.sprite.texture.name, Is.EqualTo(expectedTextureName));
            Assert.That(image.preserveAspect, Is.True);
            Assert.That(image.raycastTarget, Is.False);
        }

        private static void AssertHeaderIconImage(Transform card)
        {
            Transform icon = card.Find("HeaderIcon");
            Assert.That(icon, Is.Not.Null);
            Image image = icon.GetComponent<Image>();
            Assert.That(image, Is.Not.Null);
            Assert.That(image.sprite, Is.Not.Null);
            Assert.That(image.preserveAspect, Is.True);
            Assert.That(image.raycastTarget, Is.False);
        }

        private static void AssertSpriteButton(Transform button, string expectedTextureName)
        {
            Assert.That(button, Is.Not.Null);
            Image image = button.GetComponent<Image>();
            Assert.That(image, Is.Not.Null);
            Assert.That(image.sprite, Is.Not.Null);
            Assert.That(image.sprite.texture.name, Is.EqualTo(expectedTextureName));
            Assert.That(image.preserveAspect, Is.True);
            Assert.That(button.GetComponent<Button>(), Is.Not.Null);
        }

        private static void AssertSoftPanelShadow(Transform transform)
        {
            Shadow[] shadows = transform.GetComponents<Shadow>();
            Shadow softShadow = null;
            for (int i = 0; i < shadows.Length; i++)
            {
                if (shadows[i].GetType() == typeof(Shadow))
                {
                    softShadow = shadows[i];
                    break;
                }
            }

            Assert.That(softShadow, Is.Not.Null);
            Assert.That(softShadow.effectColor.a, Is.GreaterThan(0.2f));
            Assert.That(softShadow.effectDistance.y, Is.LessThan(0f));
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

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            return (T)field.GetValue(target);
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

        private static void AssertGameplayBandsDoNotOverlap(string deviceName, Vector2 screenSize, Vector2 referenceResolution, BoardLayout board, float safeTop = 0f, float safeBottom = 0f)
        {
            Vector2 canvasSize = ExpandCanvasSize(screenSize, referenceResolution);
            SquareFlowGameplayScreenLayout layout = SquareFlowGameplayScreenLayout.Create(board, canvasSize.y, safeTop, safeBottom);
            const float requiredGap = 8f;

            Rect statusBar = TopBand(canvasSize.y, safeTop + layout.StatusBarTopOffset, layout.StatusBarSize.y);
            Rect orbiterStrip = TopBand(canvasSize.y, safeTop + layout.OrbiterStripTopOffset, layout.OrbiterStripSize.y);
            Rect boardBand = CenterBand(layout.BoardPanelPosition.y, layout.BoardPanelSize.y);
            float canvasBottom = canvasSize.y * -0.5f;
            float columnsY = safeBottom + 70f;
            Rect waiting = CenterBand(canvasBottom + columnsY + 240f + 250f + 24f + 40f, 80f);
            Rect dock = CenterBand(canvasBottom + columnsY + 240f, 500f);

            Assert.That(orbiterStrip.yMax, Is.LessThanOrEqualTo(statusBar.yMin - requiredGap), deviceName + " status/orbiter overlap");
            Assert.That(boardBand.yMax, Is.LessThanOrEqualTo(orbiterStrip.yMin - requiredGap), deviceName + " orbiter/board overlap");
            Assert.That(waiting.yMax, Is.LessThanOrEqualTo(boardBand.yMin - requiredGap), deviceName + " board/waiting overlap");
            Assert.That(dock.yMax, Is.LessThanOrEqualTo(waiting.yMin - requiredGap), deviceName + " waiting/dock overlap");
        }

        private static Vector2 ExpandCanvasSize(Vector2 screenSize, Vector2 referenceResolution)
        {
            float scale = Mathf.Min(screenSize.x / referenceResolution.x, screenSize.y / referenceResolution.y);
            return screenSize / scale;
        }

        private static Rect TopBand(float canvasHeight, float topOffset, float height)
        {
            float top = canvasHeight * 0.5f - topOffset;
            return new Rect(0f, top - height, 1f, height);
        }

        private static Rect BottomBand(float canvasHeight, float bottomOffset, float height)
        {
            float bottom = -canvasHeight * 0.5f + bottomOffset;
            return new Rect(0f, bottom, 1f, height);
        }

        private static Rect CenterBand(float centerY, float height)
        {
            return new Rect(0f, centerY - height * 0.5f, 1f, height);
        }

        private static float LeftEdge(RectTransform rect)
        {
            return rect.anchoredPosition.x - rect.sizeDelta.x * rect.localScale.x * 0.5f;
        }

        private static float RightEdge(RectTransform rect)
        {
            return rect.anchoredPosition.x + rect.sizeDelta.x * rect.localScale.x * 0.5f;
        }

        private static float TopEdge(RectTransform rect)
        {
            return rect.anchoredPosition.y + rect.sizeDelta.y * rect.localScale.y * 0.5f;
        }

        private static float BottomEdge(RectTransform rect)
        {
            return rect.anchoredPosition.y - rect.sizeDelta.y * rect.localScale.y * 0.5f;
        }
    }
}
