using System.Collections.Generic;
using System.Globalization;
using TMPro;
using SquareFlow.Core;
using SquareFlow.Effects;
using SquareFlow.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using UnityEngine.UI;

namespace SquareFlow.UI
{
    [RequireComponent(typeof(SquareFlowAudio))]
    public sealed class SquareFlowGameController : MonoBehaviour
    {
        private const float ReferenceCanvasWidth = 1080f;
        private const float ReferenceCanvasHeight = 1920f;
        private const float ShooterColumnSpacing = 350f;
        private const float ShooterRowSpacing = 116f;
        private const float WaitingQueueSpacing = 128f;
        private const float ResponsiveHorizontalMargin = 8f;
        private const float OrbiterStripItemScale = 2.94f;
        private const float OrbiterStripLabelScale = 1.62f;
        private const float OrbiterStripLabelX = -360f;
        private const float OrbiterStripFirstDotX = -230f;
        private const float OrbiterStripDotSpacing = 54f;
        private const float WaitingQueueLabelScale = 2.08f;
        private const float WaitingQueueSlotSize = 112f;
        private const float WaitingQueueTokenSize = 112f;
        private const float ShooterDockSlotSize = 112f;
        private const float ShooterDockTokenSize = 112f;

        private readonly List<GameObject> dynamicObjects = new List<GameObject>();
        private SaveDataService saveData;
        private SquareFlowAudio audioCue;
        private SquareFlowTheme theme;
        private RectTransform root;
        private GameState state;
        private GameRules rules;
        private BoardLayout layout;
        private TMP_Text hudText;
        private TMP_Text bestText;
        private TMP_Text comboText;
        private Sprite roundedRectSprite;
        private Sprite circleSprite;
        private Sprite shooterCircleSprite;
        private Sprite glassPanelSprite;
        private Sprite crownSprite;
        private Sprite gemSprite;
        private Sprite homeButtonSprite;
        private Sprite restartButtonSprite;
        private Sprite paletteButtonSprite;
        private Sprite muteButtonSprite;
        private TMP_FontAsset guiProFont;
        private Sprite guiProPanelSprite;
        private Sprite guiProInsetPanelSprite;
        private Sprite guiProPlayButtonSprite;
        private Sprite guiProPrimaryButtonSprite;
        private Sprite guiProSmallButtonSprite;
        private Sprite guiProDangerButtonSprite;
        private Sprite guiProConfirmButtonSprite;
        private Sprite guiProTitleRibbonSprite;
        private Sprite guiProActionButtonBlueSprite;
        private Sprite guiProActionButtonRedSprite;
        private Sprite guiProActionButtonGreenSprite;
        private Sprite guiProActionButtonYellowSprite;
        private int seenEventCount;
        private GameResult seenResult;
        private bool resultHandled;
        private MobileCameraController mobileCamera;
        private GameObject worldRoot;
        private BoardWorldView boardWorldView;
        private OrbitRingWorldView orbitRingWorldView;
        private OrbiterWorldView orbiterWorldView;
        private WorldEffectsController worldEffects;
        private MobileWorldLayout worldLayout;
        private SpriteRenderer worldBackground;
        private SpriteRenderer worldBoardPanelBorder;
        private SpriteRenderer worldBoardPanel;

        private void Awake()
        {
            saveData = new SaveDataService();
            audioCue = GetComponent<SquareFlowAudio>();
            theme = new SquareFlowTheme(saveData.DarkMode);
            EnsureRuntimeSprites();
            EnsureEventSystem();
            BuildCanvas();
            BuildWorldRenderer();
        }

        private void Start()
        {
            ShowMenu();
        }

        private void Update()
        {
            if (state == null || rules == null || state.Result != GameResult.None) return;

            rules.UpdateCombo(Time.deltaTime);
            List<GameEvent> events = rules.Advance(Time.deltaTime);
            UpdateHudTexts();

            if (events.Count == 0 && state.Events.Count == seenEventCount)
            {
                if (worldRoot != null && worldRoot.activeSelf)
                {
                    bool worldLayoutChanged = UpdateWorldLayout();
                    if (worldLayoutChanged)
                    {
                        RefreshWorldBackground();
                        RefreshWorldBoardPanel();
                        boardWorldView.Bind(state, layout, worldLayout, theme);
                        orbitRingWorldView.Bind(layout, worldLayout, theme);
                    }

                    orbiterWorldView.Refresh(state.ActiveOrbiters, worldLayout, theme);
                }
                return;
            }

            seenEventCount = state.Events.Count;
            PlayEvents(events);
            if (SquareFlowGameViewRefreshPolicy.NeedsFullRefresh(events))
                RefreshGameView();
            else
                RefreshWorldGameplay();

            if (state.Result != seenResult)
                ShowResultPanel();
        }

        private void ShowMenu()
        {
            state = null;
            rules = null;
            layout = null;
            seenEventCount = 0;
            seenResult = GameResult.None;
            resultHandled = false;
            theme = new SquareFlowTheme(saveData.DarkMode);
            StopAllCoroutines();
            ClearDynamicObjects();
            if (worldRoot != null)
            {
                worldRoot.SetActive(false);
                boardWorldView.Clear();
                orbitRingWorldView.Clear();
                orbiterWorldView.Clear();
                worldEffects.Clear();
            }

            Image background = root.GetComponent<Image>();
            ApplyCanvasBackground(background, true);

            RectTransform panel = AddPanel(root, "MenuPanel", Vector2.zero, Color.clear, null);
            SetStretch(panel);

            SquareFlowStartScreenLayout startLayout = SquareFlowStartScreenLayout.Create();
            RectTransform content = AddContainer(panel, "MenuContent", startLayout.ContentSize);
            SetAnchored(content, startLayout.ContentPosition);

            BoardShape shape = BoardShapeCatalog.GetShape(saveData.Level);

            RectTransform titleGlow = AddPanel(content, "MenuTitleGlow", new Vector2(360f, 66f), ColorWithAlpha(theme.TitleGlow, 0f));
            ApplyGuiProPanelSkin(titleGlow, guiProTitleRibbonSprite, ColorWithAlpha(theme.TitleGlow, 0.18f));
            SetAnchored(titleGlow, startLayout.TitlePosition + new Vector2(0f, -2f));
            SetRaycastTarget(titleGlow, false);
            AddText(content, "Square Flow", 84, FontStyle.Bold, theme.Text, startLayout.TitlePosition, new Vector2(820f, 116f));
            AddMenuSwatches(content, startLayout.SwatchPosition);
            AddThemeToggle(content, startLayout.ThemeTogglePosition);

            RectTransform stats = AddPanel(content, "MenuStatsCard", startLayout.StatsSize, ColorWithAlpha(theme.Panel, 0.94f));
            ApplyGuiProPanelSkin(stats, guiProPanelSprite, ColorWithAlpha(theme.Panel, 0.94f));
            SetAnchored(stats, startLayout.StatsPosition);
            ApplyOutline(stats, ColorWithAlpha(theme.Border, 0.58f), 1f);
            AddMenuStat(stats, "LEVEL", saveData.Level.ToString(CultureInfo.InvariantCulture), new Vector2(-360f, -2f), theme.Score);
            AddVerticalDivider(stats, -206f);
            AddMenuStat(stats, "BOARD", shape.Name, new Vector2(-20f, -2f), theme.Text);
            AddVerticalDivider(stats, 176f);
            AddMenuStat(stats, "MAX ORBS", SquareFlowConstants.MaxActiveOrbiters.ToString(CultureInfo.InvariantCulture), new Vector2(278f, -2f), theme.Green);
            AddButton(stats, "Reset All", new Vector2(424f, -48f), new Vector2(144f, 46f), ColorWithAlpha(theme.Red, 0.14f), theme.Red, ResetProgress, 15).gameObject.name = "ResetAllButton";

            RectTransform selector = AddContainer(content, "LevelSelector", startLayout.LevelSelectorSize);
            ApplyGuiProPanelSkin(selector, guiProInsetPanelSprite != null ? guiProInsetPanelSprite : guiProPanelSprite, ColorWithAlpha(theme.Panel, 0.62f));
            SetRaycastTarget(selector, false);
            SetAnchored(selector, startLayout.LevelSelectorPosition);
            RenderLevelSelector(selector);

            RectTransform instructions = AddPanel(content, "InstructionsCard", startLayout.InstructionsSize, ColorWithAlpha(theme.Panel, 0.74f));
            ApplyGuiProPanelSkin(instructions, guiProInsetPanelSprite != null ? guiProInsetPanelSprite : guiProPanelSprite, ColorWithAlpha(theme.Panel, 0.74f));
            SetAnchored(instructions, startLayout.InstructionsPosition);
            ApplyOutline(instructions, ColorWithAlpha(theme.Border, 0.52f), 1f);
            AddText(instructions, "Orbit shooters around shaped boards. Clear all blocks to win.", 25, FontStyle.Normal, theme.Text, new Vector2(0f, 80f), new Vector2(790f, 44f));
            AddText(instructions, "HP blocks", 25, FontStyle.Bold, new Color32(255, 150, 44, 255), new Vector2(-340f, 22f), new Vector2(220f, 38f), TextAnchor.MiddleRight);
            AddText(instructions, "need multiple hits. Bomb blasts 3x3.", 24, FontStyle.Normal, theme.Text, new Vector2(140f, 22f), new Vector2(520f, 38f), TextAnchor.MiddleLeft);
            AddText(instructions, "Wild", 25, FontStyle.Bold, theme.Text, new Vector2(-340f, -48f), new Vector2(220f, 38f), TextAnchor.MiddleRight);
            AddText(instructions, "hits any color. Max 5 orbiters at once.", 24, FontStyle.Normal, theme.Text, new Vector2(140f, -48f), new Vector2(520f, 38f), TextAnchor.MiddleLeft);

            Button play = AddButton(content, "Play", startLayout.PlayButtonPosition, startLayout.PlayButtonSize, theme.PlayButton, theme.Text, StartLevel, 44);
            play.gameObject.name = "PlayButton";
            RectTransform shine = AddPanel(play.GetComponent<RectTransform>(), "PlayButtonShine", new Vector2(startLayout.PlayButtonSize.x * 0.46f, startLayout.PlayButtonSize.y), ColorWithAlpha(theme.PlayButtonAlt, 0f));
            SetAnchored(shine, new Vector2(-startLayout.PlayButtonSize.x * 0.24f, 0f));
            SetRaycastTarget(shine, false);
            shine.SetAsFirstSibling();
        }

        private void StartLevel()
        {
            int level = saveData.Level;
            BoardShape shape = BoardShapeCatalog.GetShape(level);
            IFlowRandom random = new SystemFlowRandom();
            BoardCell[,] grid = BoardGenerator.Generate(shape, level, random);
            List<Shooter>[] columns = ShooterGenerator.BuildColumns(grid, shape, level, random);

            layout = BoardLayout.Compute(shape.Rows, shape.Cols, ReferenceCanvasWidth);
            if (mobileCamera != null)
                mobileCamera.Configure(theme.Background);
            UpdateWorldLayout();
            if (worldRoot != null)
                worldRoot.SetActive(true);
            state = GameState.Create(shape, grid, columns, level);
            rules = new GameRules(state, layout);
            seenEventCount = 0;
            seenResult = GameResult.None;
            resultHandled = false;
            StopAllCoroutines();
            if (worldEffects != null)
                worldEffects.Clear();
            PlayTone(523f, 0.08f, 0.18f);
            RefreshGameView();
        }

        private void RefreshGameView()
        {
            ClearDynamicObjects();

            Image background = root.GetComponent<Image>();
            ApplyCanvasBackground(background, false);

            Vector4 safeArea = CurrentSafeAreaPadding();
            SquareFlowGameplayScreenLayout screen = CreateGameplayScreenLayout(safeArea);

            RectTransform hud = AddContainer(root, "GameHeader", screen.HudSize);
            SetTopStretch(hud, safeArea.w, screen.HudSize.y, 0f);
            Vector2 headerCardSize = new Vector2(300f, 104f);
            hudText = AddHeaderStatCard(hud, "ScoreCard", new Vector2(-320f, 0f), headerCardSize, "SCORE", 52, gemSprite);
            bestText = AddHeaderStatCard(hud, "BestCard", Vector2.zero, headerCardSize, "BEST", 46, crownSprite);

            RectTransform levelBadge = AddGlassPanel(hud, "LevelBadge", headerCardSize);
            SetAnchored(levelBadge, new Vector2(320f, 0f));
            AddText(levelBadge, "LEVEL", 22, FontStyle.Bold, HeaderLabelColor(), new Vector2(0f, 25f), new Vector2(210f, 30f));
            AddText(levelBadge, state.Level.ToString(CultureInfo.InvariantCulture), 52, FontStyle.Bold, HeaderNumberColor(), new Vector2(0f, -17f), new Vector2(210f, 62f));

            RectTransform status = AddGlassPanel(root, "GameStatusBar", screen.StatusBarSize);
            SetTopStretch(status, safeArea.w + screen.StatusBarTopOffset, screen.StatusBarSize.y, 0f);
            comboText = AddText(status, string.Empty, 30, FontStyle.Bold, HeaderLabelColor(), new Vector2(-360f, 0f), new Vector2(300f, 44f), TextAnchor.MiddleLeft);

            RectTransform actions = AddContainer(status, "HudActions", screen.ActionSize);
            SetAnchored(actions, screen.ActionPosition);
            Color headerButton = HeaderButtonColor();
            if (homeButtonSprite != null)
                AddSpriteButton(actions, "HomeButton", new Vector2(-126f, 0f), Vector2.one * 78f, homeButtonSprite, ShowMenu);
            else
                AddButton(actions, "Menu", new Vector2(-126f, 0f), new Vector2(78f, 64f), headerButton, Color.white, ShowMenu, 19);
            if (restartButtonSprite != null)
                AddSpriteButton(actions, "RestartButton", new Vector2(-42f, 0f), Vector2.one * 78f, restartButtonSprite, StartLevel);
            else
                AddButton(actions, "R", new Vector2(-42f, 0f), screen.UtilityButtonSize, headerButton, Color.white, StartLevel, 22);
            if (paletteButtonSprite != null)
                AddSpriteButton(actions, "PaletteButton", new Vector2(42f, 0f), Vector2.one * 78f, paletteButtonSprite, ToggleThemeInGame);
            else
                AddButton(actions, "T", new Vector2(42f, 0f), screen.UtilityButtonSize, headerButton, Color.white, ToggleThemeInGame, 22);
            if (muteButtonSprite != null)
                AddSpriteButton(actions, "MuteButton", new Vector2(126f, 0f), Vector2.one * 78f, muteButtonSprite, ToggleMuteInGame);
            else
                AddButton(actions, "M", new Vector2(126f, 0f), screen.UtilityButtonSize, headerButton, Color.white, ToggleMuteInGame, 22);

            RenderOrbiterStrip(screen);
            RenderBoardFrame(screen);

            RefreshWorldGameplay();

            RectTransform queue = AddGlassPanel(root, "WaitingQueue", screen.QueueSize);
            SetBottomStretch(queue, safeArea.y + screen.QueueBottomOffset, screen.QueueSize.y, ResponsiveHorizontalMargin);
            RenderWaiting(queue);

            RectTransform columns = AddContainer(root, "ShooterColumns", screen.DockSize);
            SetBottomStretch(columns, safeArea.y + screen.DockBottomOffset, screen.DockSize.y, ResponsiveHorizontalMargin);
            RenderColumns(columns);

            UpdateHudTexts();
            if (state.Result != GameResult.None)
                ShowResultPanel();
        }

        private void RefreshWorldGameplay()
        {
            if (state == null || layout == null || worldRoot == null) return;

            UpdateWorldLayout();
            if (!worldLayout.IsValid) return;
            worldRoot.SetActive(true);
            RefreshWorldBackground();
            RefreshWorldBoardPanel();
            boardWorldView.Bind(state, layout, worldLayout, theme);
            orbitRingWorldView.Bind(layout, worldLayout, theme);
            orbiterWorldView.Refresh(state.ActiveOrbiters, worldLayout, theme);
        }

        private void RenderOrbiterStrip(SquareFlowGameplayScreenLayout screen)
        {
            Vector4 safeArea = CurrentSafeAreaPadding();
            RectTransform strip = AddGlassPanel(root, "OrbiterStrip", screen.OrbiterStripSize);
            SetTopStretch(strip, safeArea.w + screen.OrbiterStripTopOffset, screen.OrbiterStripSize.y, ResponsiveHorizontalMargin);
            TMP_Text label = AddText(strip, "ORBITERS", 18, FontStyle.Bold, HeaderLabelColor(), new Vector2(OrbiterStripLabelX, -1f), new Vector2(180f, 34f), TextAnchor.MiddleLeft);
            SetUniformScale(label.rectTransform, OrbiterStripLabelScale);

            int active = state != null ? state.ActiveOrbiters.Count : 0;
            for (int i = 0; i < SquareFlowConstants.MaxActiveOrbiters; i++)
            {
                Color fill = i < active ? HeaderLabelColor() : ColorWithAlpha(theme.InactiveSlot, 0.92f);
                RectTransform dot = AddPanel(strip, "OrbiterDot", Vector2.one * 28f, fill, circleSprite);
                SetAnchored(dot, new Vector2(OrbiterStripFirstDotX + i * OrbiterStripDotSpacing, 0f));
                SetUniformScale(dot, OrbiterStripItemScale);
                ApplyOutline(dot, ColorWithAlpha(Color.white, 0.68f), 1f);
            }

            TMP_Text count = AddText(strip, active.ToString(CultureInfo.InvariantCulture) + "/" + SquareFlowConstants.MaxActiveOrbiters, 18, FontStyle.Bold, HeaderLabelColor(), new Vector2(367f, -1f), new Vector2(90f, 30f), TextAnchor.MiddleRight);
            SetUniformScale(count.rectTransform, OrbiterStripLabelScale);
        }

        private void RenderBoardFrame(SquareFlowGameplayScreenLayout screen)
        {
            RectTransform frame = AddPanel(root, "BoardFrame", screen.BoardPanelSize, Color.clear, roundedRectSprite);
            SetAnchored(frame, screen.BoardPanelPosition);
            ApplyOutline(frame, ColorWithAlpha(Color.white, 0.58f), 2f);
            SetRaycastTarget(frame, false);

            RectTransform inset = AddPanel(frame, "BoardInset", new Vector2(screen.BoardPanelSize.x - 58f, screen.BoardPanelSize.y - 72f), Color.clear);
            SetAnchored(inset, new Vector2(0f, -5f));
            ApplyOutline(inset, ColorWithAlpha(Color.white, 0.12f), 1f);
            SetRaycastTarget(inset, false);
        }

        private bool UpdateWorldLayout()
        {
            if (layout == null)
            {
                bool hadLayout = worldLayout.IsValid;
                worldLayout = default;
                return hadLayout;
            }

            MobileWorldLayout next = mobileCamera != null
                ? CreateWorldLayoutForReferenceCanvas(layout, mobileCamera.VisibleWorldRect)
                : MobileWorldLayout.Create(layout);
            bool changed = !worldLayout.IsValid
                || (worldLayout.BoardCenter - next.BoardCenter).sqrMagnitude > 0.0001f
                || Mathf.Abs(worldLayout.WorldUnitsPerLayoutPixel - next.WorldUnitsPerLayoutPixel) > 0.0001f;
            worldLayout = next;
            return changed;
        }

        private MobileWorldLayout CreateWorldLayoutForReferenceCanvas(BoardLayout board, Rect visibleWorldRect)
        {
            Vector2 canvasSize = CurrentCanvasSize();
            Vector4 safeArea = CurrentSafeAreaPadding();
            SquareFlowGameplayScreenLayout screen = SquareFlowGameplayScreenLayout.Create(board, CurrentLayoutCanvasHeight(), safeArea.w, safeArea.y);
            float referencePixelToWorld = Mathf.Min(
                visibleWorldRect.width / canvasSize.x,
                visibleWorldRect.height / canvasSize.y);
            float panelFit = Mathf.Min(
                screen.BoardPanelSize.x / board.CanvasWidth,
                screen.BoardPanelSize.y / board.CanvasHeight);
            return new MobileWorldLayout(board, ReferenceCanvasToWorld(screen.BoardPanelPosition), referencePixelToWorld * panelFit);
        }

        private SquareFlowGameplayScreenLayout CreateGameplayScreenLayout(Vector4 safeArea)
        {
            return SquareFlowGameplayScreenLayout.Create(layout, CurrentLayoutCanvasHeight(), safeArea.w, safeArea.y);
        }

        private void RenderWaiting(RectTransform queue)
        {
            int capacity = SquareFlowConstants.WaitQueueLimit;
            float startX = WaitingQueueStartX(capacity);
            TMP_Text waitingLabel = AddText(queue, "WAITING " + state.WaitingQueue.Count + "/" + capacity, 20, FontStyle.Bold, HeaderLabelColor(), new Vector2(-232f, 61f), new Vector2(270f, 36f), TextAnchor.MiddleLeft);
            SetUniformScale(waitingLabel.rectTransform, WaitingQueueLabelScale);

            for (int i = 0; i < capacity; i++)
            {
                Vector2 position = new Vector2(startX + i * WaitingQueueSpacing, -22f);
                RectTransform slot = AddPanel(queue, "WaitingSlot", Vector2.one * WaitingQueueSlotSize, ColorWithAlpha(theme.InactiveSlot, 0.62f), circleSprite);
                SetAnchored(slot, position);
                ApplyOutline(slot, ColorWithAlpha(Color.white, 0.66f), 1f);
            }

            for (int i = 0; i < state.WaitingQueue.Count; i++)
            {
                int index = i;
                Shooter shooter = state.WaitingQueue[i];
                AddShooterButton(queue, shooter, new Vector2(startX + i * WaitingQueueSpacing, -22f), Vector2.one * WaitingQueueTokenSize, () => FireWaiting(index));
            }
        }

        private void RenderColumns(RectTransform columns)
        {
            const float slotSize = ShooterDockSlotSize;
            int visibleRows = SquareFlowGameplayScreenLayout.ShooterColumnVisibleRows;
            float startX = ShooterColumnsStartX(state.ShooterColumns.Length);
            float startY = ShooterColumnsStartY(visibleRows);

            for (int i = 0; i < state.ShooterColumns.Length; i++)
            {
                int column = i;
                float x = startX + i * ShooterColumnSpacing;
                List<Shooter> shooterColumn = state.ShooterColumns[i];
                RectTransform card = AddGlassPanel(columns, "ShooterColumnCard", new Vector2(340f, 468f));
                SetAnchored(card, new Vector2(x, 0f));
                AddText(card, ((char)('A' + i)).ToString(), 21, FontStyle.Bold, HeaderLabelColor(), new Vector2(-126f, 206f), new Vector2(80f, 32f));

                for (int row = 0; row < visibleRows; row++)
                {
                    bool frontRow = row == 0;
                    Vector2 position = new Vector2(0f, startY - row * ShooterRowSpacing);
                    RectTransform slot = AddPanel(card, frontRow ? "DockSlotFront" : "DockSlotQueued", Vector2.one * slotSize, ColorWithAlpha(theme.InactiveSlot, frontRow ? 0.78f : 0.44f), circleSprite);
                    SetAnchored(slot, position);
                    ApplyOutline(slot, ColorWithAlpha(Color.white, frontRow ? 0.68f : 0.42f), 1f);

                    if (row >= shooterColumn.Count)
                    {
                        if (frontRow)
                            AddText(slot, "-", 18, FontStyle.Bold, theme.SubtleText, Vector2.zero, Vector2.one * slotSize);
                        continue;
                    }

                    Shooter shooter = shooterColumn[row];
                    if (frontRow)
                        AddShooterButton(slot, shooter, Vector2.zero, Vector2.one * ShooterDockTokenSize, () => FireColumn(column));
                    else
                        AddShooterToken(slot, shooter, Vector2.zero, Vector2.one * ShooterDockTokenSize, false, null);
                }
            }
        }

        private void FireColumn(int column)
        {
            if (rules == null) return;
            string orbiterId = TryGetColumnShooterId(column);
            bool fired = rules.FireFromColumn(column);
            if (fired)
                RegisterColumnLaunch(orbiterId, column);
            PlayTone(fired ? 660f : 140f, fired ? 0.06f : 0.1f, 0.16f);
            RefreshGameView();
            if (state != null)
                seenEventCount = state.Events.Count;
        }

        private void FireWaiting(int index)
        {
            if (rules == null) return;
            string orbiterId = TryGetWaitingShooterId(index);
            bool fired = rules.FireFromWaiting(index);
            if (fired)
                RegisterWaitingLaunch(orbiterId, index);
            PlayTone(fired ? 740f : 140f, fired ? 0.06f : 0.1f, 0.16f);
            RefreshGameView();
            if (state != null)
                seenEventCount = state.Events.Count;
        }

        private string TryGetColumnShooterId(int column)
        {
            if (state == null || column < 0 || column >= state.ShooterColumns.Length) return null;
            List<Shooter> shooters = state.ShooterColumns[column];
            return shooters.Count > 0 ? shooters[0].Id : null;
        }

        private string TryGetWaitingShooterId(int index)
        {
            if (state == null || index < 0 || index >= state.WaitingQueue.Count) return null;
            return state.WaitingQueue[index].Id;
        }

        private void RegisterColumnLaunch(string orbiterId, int column)
        {
            if (string.IsNullOrEmpty(orbiterId) || orbiterWorldView == null || layout == null || state == null) return;
            SquareFlowGameplayScreenLayout screen = CreateGameplayScreenLayout(CurrentSafeAreaPadding());
            Vector2 columnOffset = new Vector2(
                ShooterColumnsStartX(state.ShooterColumns.Length) + column * ShooterColumnSpacing,
                ShooterColumnsStartY(screen.DockVisibleRows));
            Vector2 referencePosition = screen.DockPosition + columnOffset;
            orbiterWorldView.RegisterLaunchSource(orbiterId, ReferenceCanvasToWorld(referencePosition));
        }

        private void RegisterWaitingLaunch(string orbiterId, int index)
        {
            if (string.IsNullOrEmpty(orbiterId) || orbiterWorldView == null || layout == null) return;
            SquareFlowGameplayScreenLayout screen = CreateGameplayScreenLayout(CurrentSafeAreaPadding());
            Vector2 queueOffset = new Vector2(
                WaitingQueueStartX(SquareFlowConstants.WaitQueueLimit) + index * WaitingQueueSpacing,
                -22f);
            Vector2 referencePosition = screen.QueuePosition + queueOffset;
            orbiterWorldView.RegisterLaunchSource(orbiterId, ReferenceCanvasToWorld(referencePosition));
        }

        private Vector2 ReferenceCanvasToWorld(Vector2 anchoredPosition)
        {
            Rect visible = mobileCamera != null
                ? mobileCamera.VisibleWorldRect
                : new Rect(-5.4f, -9.6f, ReferenceCanvasWidth * 0.01f, ReferenceCanvasHeight * 0.01f);
            Vector2 canvasSize = CurrentCanvasSize();
            float viewportX = Mathf.Clamp01(0.5f + anchoredPosition.x / canvasSize.x);
            float viewportY = Mathf.Clamp01(0.5f + anchoredPosition.y / canvasSize.y);
            return new Vector2(
                Mathf.Lerp(visible.xMin, visible.xMax, viewportX),
                Mathf.Lerp(visible.yMin, visible.yMax, viewportY));
        }

        private Vector2 CurrentCanvasSize()
        {
            if (root == null || root.rect.width <= 0f || root.rect.height <= 0f)
                return new Vector2(ReferenceCanvasWidth, ReferenceCanvasHeight);

            return root.rect.size;
        }

        private float CurrentLayoutCanvasHeight()
        {
            return root != null && root.rect.height > 0f ? root.rect.height : 0f;
        }

        private static float ShooterColumnsStartX(int columnCount)
        {
            return -ShooterColumnSpacing * (columnCount - 1) * 0.5f;
        }

        private static float ShooterColumnsStartY(int visibleRows)
        {
            return (visibleRows - 1) * ShooterRowSpacing * 0.5f;
        }

        private static float WaitingQueueStartY(int capacity)
        {
            return (capacity - 1) * WaitingQueueSpacing * 0.5f;
        }

        private static float WaitingQueueStartX(int capacity)
        {
            return -WaitingQueueSpacing * (capacity - 1) * 0.5f;
        }

        private void ShowResultPanel()
        {
            if (state == null || state.Result == GameResult.None) return;

            if (!resultHandled)
            {
                resultHandled = true;
                seenResult = state.Result;
                if (state.Result == GameResult.Won)
                {
                    saveData.MarkCompleted(state.Level);
                    saveData.AddScore(state.Level, state.Moves, state.Score);
                    saveData.Level = state.Level + 1;
                    PlayTone(880f, 0.14f, 0.2f);
                }
                else
                {
                    PlayTone(196f, 0.18f, 0.2f);
                }
            }

            RectTransform panel = AddPanel(root, "ResultPanel", new Vector2(600f, 330f), theme.Panel);
            ApplyGuiProPanelSkin(panel, guiProPanelSprite, theme.Panel);
            SetAnchored(panel, new Vector2(0f, 72f));
            ApplyOutline(panel, ColorWithAlpha(theme.Score, 0.32f), 2f);
            AddText(panel, ResultTitle(), 42, FontStyle.Bold, theme.Text, new Vector2(0f, 100f), new Vector2(520f, 64f));
            AddText(panel, "Score " + state.Score + " - Moves " + state.Moves, 26, FontStyle.Bold, theme.Score, new Vector2(0f, 34f), new Vector2(520f, 44f));
            AddButton(panel, state.Result == GameResult.Won ? "Next Level" : "Try Again", new Vector2(0f, -52f), new Vector2(300f, 64f), theme.Green, theme.Text, StartLevel, 24);
            AddButton(panel, "Menu", new Vector2(0f, -126f), new Vector2(300f, 54f), theme.Blue, Color.white, ShowMenu, 21);
        }

        private string ResultTitle()
        {
            if (state.Result == GameResult.Won) return "Level Clear";
            if (state.Result == GameResult.LostWait) return "Queue Full";
            return "Out of Shooters";
        }

        private void AddMenuSwatches(RectTransform parent, Vector2 position)
        {
            RectTransform row = AddContainer(parent, "MenuSwatches", new Vector2(196f, 44f));
            SetAnchored(row, position);
            Color[] colors = { theme.Red, theme.Blue, theme.Yellow, theme.Green };
            for (int i = 0; i < colors.Length; i++)
            {
                RectTransform swatch = AddPanel(row, "Swatch", Vector2.one * 34f, colors[i]);
                SetAnchored(swatch, new Vector2(-63f + i * 42f, 0f));
                ApplyOutline(swatch, ColorWithAlpha(Color.white, 0.14f), 1f);
            }
        }

        private void AddThemeToggle(RectTransform parent, Vector2 position)
        {
            RectTransform toggle = AddPanel(parent, "ThemeToggle", new Vector2(286f, 60f), ColorWithAlpha(theme.Chip, 0.88f));
            SetAnchored(toggle, position);
            ApplyOutline(toggle, ColorWithAlpha(theme.Border, 0.56f), 1f);
            RectTransform moon = AddPanel(toggle, "MoonDot", Vector2.one * 24f, theme.Score, circleSprite);
            SetAnchored(moon, new Vector2(-88f, 0f));

            RectTransform pill = AddPanel(toggle, "ThemePill", new Vector2(92f, 42f), ColorWithAlpha(theme.Button, 0.86f));
            SetAnchored(pill, Vector2.zero);
            ApplyOutline(pill, ColorWithAlpha(Color.white, 0.22f), 1f);
            RectTransform knob = AddPanel(pill, "ThemeKnob", Vector2.one * 34f, saveData.DarkMode ? Color.white : theme.Score, circleSprite);
            SetAnchored(knob, new Vector2(saveData.DarkMode ? -23f : 23f, 0f));

            Button themeButton = AddButton(toggle, string.Empty, new Vector2(88f, 0f), new Vector2(54f, 48f), Color.clear, theme.Score, ToggleTheme, 1);
            themeButton.gameObject.name = "ThemeButton";
            RectTransform sun = AddPanel(themeButton.GetComponent<RectTransform>(), "SunDot", Vector2.one * 28f, theme.Score, circleSprite);
            SetAnchored(sun, Vector2.zero);
        }

        private void AddMenuStat(RectTransform parent, string label, string value, Vector2 position, Color valueColor)
        {
            AddText(parent, label, 18, FontStyle.Bold, theme.SubtleText, position + new Vector2(0f, 36f), new Vector2(180f, 30f));
            AddText(parent, value, 52, FontStyle.Bold, valueColor, position + new Vector2(0f, -18f), new Vector2(240f, 66f));
        }

        private void AddVerticalDivider(RectTransform parent, float x)
        {
            RectTransform divider = AddPanel(parent, "Divider", new Vector2(2f, 108f), ColorWithAlpha(theme.Border, 0.72f), null);
            SetAnchored(divider, new Vector2(x, 0f));
        }

        private void RenderLevelSelector(RectTransform panel)
        {
            HashSet<int> completed = saveData.CompletedLevels();
            for (int i = 0; i < BoardShapeCatalog.Count; i++)
            {
                int level = i + 1;
                int col = i % BoardShapeCatalog.Count;
                float x = -342f + col * 76f;
                bool selected = level == saveData.Level;
                Color fill = selected ? ColorWithAlpha(theme.SelectedLevel, 0.16f) : completed.Contains(level) ? ColorWithAlpha(theme.Score, 0.18f) : ColorWithAlpha(theme.Blue, 0.34f);
                Color text = selected ? theme.Score : theme.Blue;
                Button button = AddButton(panel, level.ToString(), new Vector2(x, 0f), new Vector2(66f, 62f), fill, text, () => SelectLevel(level), 22);
                button.gameObject.name = "LevelButton";
                Outline outline = button.GetComponent<Outline>();
                if (outline != null)
                    outline.effectColor = selected ? theme.SelectedLevel : ColorWithAlpha(theme.Blue, 0.52f);
            }
        }

        private void RenderLeaderboard(RectTransform panel)
        {
            SaveDataService.ScoreEntry[] scores = saveData.Scores();
            if (scores.Length == 0)
            {
                AddText(panel, "No scores yet", 22, FontStyle.Normal, theme.SubtleText, new Vector2(0f, -365f), new Vector2(760f, 36f));
                return;
            }

            int count = Mathf.Min(scores.Length, 10);
            for (int i = 0; i < count; i++)
            {
                SaveDataService.ScoreEntry entry = scores[i];
                string label = (i + 1) + ". L" + entry.Level + "  " + entry.Score + " pts  " + entry.Moves + " moves";
                AddText(panel, label, 20, FontStyle.Normal, theme.SubtleText, new Vector2(0f, -360f - i * 26f), new Vector2(760f, 28f));
            }
        }

        private void SelectLevel(int level)
        {
            saveData.Level = level;
            ShowMenu();
        }

        private void UpdateHudTexts()
        {
            if (state == null) return;

            if (hudText != null)
                hudText.text = state.Score.ToString("N0", CultureInfo.InvariantCulture);
            if (bestText != null)
                bestText.text = Mathf.Max(state.Score, HighestSavedScore()).ToString("N0", CultureInfo.InvariantCulture);
            if (comboText != null)
                comboText.text = state.Combo > 1f ? "combo x" + state.Combo.ToString("0.0", CultureInfo.InvariantCulture) : state.Moves + " moves";
        }

        private void PlayEvents(List<GameEvent> events)
        {
            for (int i = 0; i < events.Count; i++)
            {
                GameEvent gameEvent = events[i];
                switch (gameEvent.Type)
                {
                    case GameEventType.BlockDamaged:
                        SpawnShotEffect(gameEvent, false);
                        PlayTone(620f, 0.035f, 0.1f);
                        break;
                    case GameEventType.BlockDestroyed:
                        SpawnShotEffect(gameEvent, true);
                        PlayTone(784f, 0.05f, 0.12f);
                        break;
                    case GameEventType.BombDetonated:
                        SpawnShotEffect(gameEvent, true);
                        PlayTone(110f, 0.14f, 0.18f);
                        break;
                    case GameEventType.Blocked:
                        PlayTone(120f, 0.1f, 0.16f);
                        break;
                }
            }
        }

        private void SpawnShotEffect(GameEvent gameEvent, bool heavyImpact)
        {
            if (layout == null || !worldLayout.IsValid || worldEffects == null || !gameEvent.HasFirePoint) return;
            if (!worldLayout.TryFirePoint(gameEvent, out Vector2 start)) return;

            Vector2 end = worldLayout.EventTarget(gameEvent);
            Color color = ShotColor(gameEvent);
            bool heavyCellImpact = heavyImpact || gameEvent.Type == GameEventType.BombDetonated;
            worldEffects.PlayShot(start, end, color, heavyCellImpact);
            if (boardWorldView != null)
                boardWorldView.PlayHitFeedback(gameEvent.Row, gameEvent.Col, heavyCellImpact);
        }

        private Color ShotColor(GameEvent gameEvent)
        {
            if (gameEvent.Type == GameEventType.BombDetonated) return theme.Bomb;

            if (!string.IsNullOrEmpty(gameEvent.OrbiterId) && orbiterWorldView != null && orbiterWorldView.TryGetColor(gameEvent.OrbiterId, out Color orbiterColor))
                return orbiterColor;

            if (state != null && state.Shape.IsActive(gameEvent.Row, gameEvent.Col) && state.Grid[gameEvent.Row, gameEvent.Col].IsOccupied)
                return ColorForCell(state.Grid[gameEvent.Row, gameEvent.Col]);

            return theme.Score;
        }

        private void ToggleTheme()
        {
            saveData.DarkMode = !saveData.DarkMode;
            theme = new SquareFlowTheme(saveData.DarkMode);
            ShowMenu();
        }

        private void ToggleMute()
        {
            saveData.Muted = !saveData.Muted;
            ShowMenu();
        }

        private void ToggleThemeInGame()
        {
            saveData.DarkMode = !saveData.DarkMode;
            theme = new SquareFlowTheme(saveData.DarkMode);
            if (mobileCamera != null)
                mobileCamera.Configure(theme.Background);
            RefreshGameView();
        }

        private void ToggleMuteInGame()
        {
            saveData.Muted = !saveData.Muted;
            RefreshGameView();
        }

        private void ResetProgress()
        {
            saveData.ClearProgress();
            ShowMenu();
        }

        private void PlayTone(float frequency, float duration, float volume)
        {
            if (saveData.Muted || audioCue == null) return;
            audioCue.PlayTone(frequency, duration, volume);
        }

        private Color ColorForCell(BoardCell cell)
        {
            return cell.Type == BoardCellType.Bomb ? theme.Bomb : ColorForFlowColor(cell.Color);
        }

        private Color ColorForShooter(FlowColor color, bool wild)
        {
            return wild || color == FlowColor.Wild ? theme.Wild : ColorForFlowColor(color);
        }

        private Color ColorForFlowColor(FlowColor color)
        {
            switch (color)
            {
                case FlowColor.Blue:
                    return theme.Blue;
                case FlowColor.Yellow:
                    return theme.Yellow;
                case FlowColor.Green:
                    return theme.Green;
                case FlowColor.Wild:
                    return theme.Wild;
                default:
                    return theme.Red;
            }
        }

        private void BuildCanvas()
        {
            GameObject canvasObject = new GameObject("SquareFlowCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(Image));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            scaler.matchWidthOrHeight = 0f;

            root = canvasObject.GetComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
            canvasObject.GetComponent<Image>().color = theme.Background;
        }

        private Vector4 CurrentSafeAreaPadding()
        {
            if (!Application.isPlaying || root == null)
                return Vector4.zero;

            return SquareFlowSafeArea.PaddingForCanvas(
                Screen.safeArea,
                new Vector2(Screen.width, Screen.height),
                root.rect.size);
        }

        private void BuildWorldRenderer()
        {
            Camera existingCamera = Camera.main;
            bool createdCamera = existingCamera == null;
            GameObject cameraObject = createdCamera
                ? new GameObject("SquareFlowWorldCamera", typeof(Camera))
                : existingCamera.gameObject;

            if (createdCamera)
            {
                cameraObject.tag = "MainCamera";
                cameraObject.transform.SetParent(transform, false);
            }

            if (cameraObject.GetComponent<Camera>() == null)
                cameraObject.AddComponent<Camera>();

            mobileCamera = cameraObject.GetComponent<MobileCameraController>();
            if (mobileCamera == null)
                mobileCamera = cameraObject.AddComponent<MobileCameraController>();

            worldRoot = new GameObject("SquareFlowWorld");
            worldRoot.transform.SetParent(transform, false);

            worldBackground = new GameObject("WorldBackground").AddComponent<SpriteRenderer>();
            worldBackground.transform.SetParent(worldRoot.transform, false);
            worldBackground.sortingOrder = -100;

            worldBoardPanelBorder = new GameObject("WorldBoardPanelBorder").AddComponent<SpriteRenderer>();
            worldBoardPanelBorder.transform.SetParent(worldRoot.transform, false);
            worldBoardPanelBorder.sortingOrder = -11;

            worldBoardPanel = new GameObject("WorldBoardPanel").AddComponent<SpriteRenderer>();
            worldBoardPanel.transform.SetParent(worldRoot.transform, false);
            worldBoardPanel.sortingOrder = -10;

            boardWorldView = new GameObject("BoardWorldView").AddComponent<BoardWorldView>();
            boardWorldView.transform.SetParent(worldRoot.transform, false);

            orbitRingWorldView = new GameObject("OrbitRingWorldView").AddComponent<OrbitRingWorldView>();
            orbitRingWorldView.transform.SetParent(worldRoot.transform, false);

            orbiterWorldView = new GameObject("OrbiterWorldView").AddComponent<OrbiterWorldView>();
            orbiterWorldView.transform.SetParent(worldRoot.transform, false);

            worldEffects = new GameObject("WorldEffects").AddComponent<WorldEffectsController>();
            worldEffects.transform.SetParent(worldRoot.transform, false);

            worldRoot.SetActive(false);
            mobileCamera.Configure(theme.Background);
        }

        private void ApplyCanvasBackground(Image background, bool visible)
        {
            if (background == null) return;

            Sprite sky = SquareFlowWorldSprites.SkyBackground;
            background.sprite = sky;
            background.type = Image.Type.Simple;
            background.preserveAspect = false;
            background.color = visible ? sky != null ? Color.white : theme.Background : Color.clear;
        }

        private void RefreshWorldBackground()
        {
            if (worldBackground == null || mobileCamera == null) return;

            Sprite sky = SquareFlowWorldSprites.SkyBackground;
            if (sky == null)
            {
                worldBackground.gameObject.SetActive(false);
                return;
            }

            Rect visible = mobileCamera.VisibleWorldRect;
            worldBackground.gameObject.SetActive(true);
            worldBackground.sprite = sky;
            worldBackground.color = Color.white;
            worldBackground.transform.position = new Vector3(visible.center.x, visible.center.y, 5f);
            float coverScale = Mathf.Max(visible.width / sky.bounds.size.x, visible.height / sky.bounds.size.y);
            worldBackground.transform.localScale = Vector3.one * coverScale;
        }

        private void RefreshWorldBoardPanel()
        {
            if (worldBoardPanel == null || worldBoardPanelBorder == null || mobileCamera == null || layout == null || !worldLayout.IsValid)
            {
                SetWorldBoardPanelVisible(false);
                return;
            }

            SquareFlowGameplayScreenLayout screen = SquareFlowGameplayScreenLayout.Create(layout);
            Rect visible = mobileCamera.VisibleWorldRect;
            float referencePixelToWorld = Mathf.Min(
                visible.width / ReferenceCanvasWidth,
                visible.height / ReferenceCanvasHeight);
            Vector2 panelSize = screen.BoardPanelSize * referencePixelToWorld;
            Vector2 panelCenter = ReferenceCanvasToWorld(screen.BoardPanelPosition);

            ConfigureWorldPanelRenderer(
                worldBoardPanelBorder,
                panelCenter,
                panelSize + Vector2.one * Mathf.Max(0.04f, referencePixelToWorld * 10f),
                ColorWithAlpha(Color.white, 0.34f));
            ConfigureWorldPanelRenderer(worldBoardPanel, panelCenter, panelSize, BoardPanelColor());
        }

        private void SetWorldBoardPanelVisible(bool visible)
        {
            if (worldBoardPanelBorder != null)
                worldBoardPanelBorder.gameObject.SetActive(visible);
            if (worldBoardPanel != null)
                worldBoardPanel.gameObject.SetActive(visible);
        }

        private static void ConfigureWorldPanelRenderer(SpriteRenderer renderer, Vector2 center, Vector2 size, Color color)
        {
            renderer.gameObject.SetActive(true);
            renderer.sprite = SquareFlowWorldSprites.RoundedRect;
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = size;
            renderer.color = color;
            renderer.transform.position = new Vector3(center.x, center.y, 0.36f);
            renderer.transform.localScale = Vector3.one;
        }

        private TMP_Text AddHeaderStatCard(RectTransform parent, string objectName, Vector2 position, Vector2 size, string label, int valueFontSize, Sprite icon)
        {
            RectTransform card = AddGlassPanel(parent, objectName, size);
            SetAnchored(card, position);

            float contentOffset = size.x > 300f ? 56f : 38f;
            float textWidth = size.x - 116f;
            if (icon != null)
            {
                AddHeaderIcon(card, "HeaderIcon", icon, new Vector2(-104f, -2f), new Vector2(82f, 66f));
                contentOffset = 54f;
                textWidth = size.x - 140f;
            }

            AddText(card, label, 27, FontStyle.Bold, HeaderLabelColor(), new Vector2(contentOffset, 24f), new Vector2(textWidth, 34f));
            return AddText(card, string.Empty, valueFontSize, FontStyle.Bold, HeaderNumberColor(), new Vector2(contentOffset, -20f), new Vector2(textWidth, 62f));
        }

        private RectTransform AddHeaderIcon(RectTransform parent, string objectName, Sprite icon, Vector2 position, Vector2 size)
        {
            RectTransform rect = AddPanel(parent, objectName, size, Color.white, icon);
            SetAnchored(rect, position);
            Image image = rect.GetComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            Shadow shadow = rect.gameObject.AddComponent<Shadow>();
            shadow.effectColor = ColorWithAlpha(new Color32(113, 83, 0, 255), 0.24f);
            shadow.effectDistance = new Vector2(0f, -3f);
            return rect;
        }

        private RectTransform AddGlassPanel(RectTransform parent, string objectName, Vector2 size)
        {
            Sprite sprite = guiProPanelSprite != null ? guiProPanelSprite : glassPanelSprite != null ? glassPanelSprite : roundedRectSprite;
            RectTransform panel = AddPanel(parent, objectName, size, Color.white, sprite);
            ApplyGuiProPanelSkin(panel, sprite, Color.white);
            ApplyOutline(panel, ColorWithAlpha(Color.white, 0.50f), 1f);
            SetRaycastTarget(panel, false);
            return panel;
        }

        private RectTransform AddPanel(RectTransform parent, string objectName, Vector2 size, Color color)
        {
            return AddPanel(parent, objectName, size, color, roundedRectSprite);
        }

        private RectTransform AddPanel(RectTransform parent, string objectName, Vector2 size, Color color, Sprite sprite)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            dynamicObjects.Add(go);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            Image image = go.GetComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            image.type = HasSpriteBorder(sprite) ? Image.Type.Sliced : Image.Type.Simple;
            return rect;
        }

        private RectTransform AddContainer(RectTransform parent, string objectName, Vector2 size)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            dynamicObjects.Add(go);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            return rect;
        }

        private static void SetRaycastTarget(RectTransform rect, bool enabled)
        {
            Image image = rect != null ? rect.GetComponent<Image>() : null;
            if (image != null)
                image.raycastTarget = enabled;
        }

        private TMP_Text AddText(RectTransform parent, string value, int size, FontStyle style, Color color, Vector2 position, Vector2 dimensions, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            GameObject go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            dynamicObjects.Add(go);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = dimensions;
            SetAnchored(rect, position);

            TMP_Text text = go.GetComponent<TMP_Text>();
            text.text = value;
            ApplyGuiProTextSkin(text, size, style, color);
            text.alignment = ToTmpAlignment(alignment);
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Truncate;
            text.raycastTarget = false;
            Shadow shadow = go.AddComponent<Shadow>();
            shadow.effectColor = ColorWithAlpha(Color.black, 0.45f);
            shadow.effectDistance = new Vector2(1.2f, -1.2f);
            return text;
        }

        private Button AddButton(RectTransform parent, string label, Vector2 position, Vector2 size, Color color, Color textColor, UnityEngine.Events.UnityAction action)
        {
            return AddButton(parent, label, position, size, color, textColor, action, 18);
        }

        private Sprite ButtonSpriteForLabel(string label, Vector2 size)
        {
            if (string.IsNullOrEmpty(label))
                return null;

            if (label == "Play" || label == "Next Level" || label == "Try Again")
                return guiProPlayButtonSprite != null ? guiProPlayButtonSprite : guiProConfirmButtonSprite;

            if (label == "Reset All")
                return guiProDangerButtonSprite;

            if (size.x <= 100f && size.y <= 70f)
                return GuiProActionButtonSprite();

            if (size.x <= 180f || size.y <= 70f)
                return guiProSmallButtonSprite != null ? guiProSmallButtonSprite : GuiProActionButtonSprite();

            return guiProPrimaryButtonSprite;
        }

        private void ApplyGuiProPanelSkin(RectTransform rect, Sprite sprite, Color fallbackColor)
        {
            if (rect == null) return;

            Image image = rect.GetComponent<Image>();
            if (image == null)
            {
                image = rect.gameObject.AddComponent<Image>();
                image.raycastTarget = false;
            }

            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = HasSpriteBorder(sprite) ? Image.Type.Sliced : Image.Type.Simple;
                image.color = Color.white;
            }
            else
            {
                image.color = fallbackColor;
            }

            EnsureSoftPanelDepth(rect, 0.28f, 8f);
        }

        private Sprite GuiProActionButtonSprite()
        {
            if (guiProActionButtonBlueSprite != null) return guiProActionButtonBlueSprite;
            if (guiProActionButtonGreenSprite != null) return guiProActionButtonGreenSprite;
            if (guiProActionButtonYellowSprite != null) return guiProActionButtonYellowSprite;
            if (guiProActionButtonRedSprite != null) return guiProActionButtonRedSprite;
            return null;
        }

        private Button AddButton(RectTransform parent, string label, Vector2 position, Vector2 size, Color color, Color textColor, UnityEngine.Events.UnityAction action, int fontSize)
        {
            Sprite sprite = ButtonSpriteForLabel(label, size);
            Color buttonColor = sprite != null ? Color.white : color;
            RectTransform rect = AddPanel(parent, "Button", size, buttonColor, sprite != null ? sprite : roundedRectSprite);
            SetAnchored(rect, position);

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            button.onClick.AddListener(action);
            button.colors = ButtonColors(buttonColor);
            ApplyOutline(rect, ColorWithAlpha(Color.white, 0.26f), 1f);

            TMP_Text text = AddText(rect, label, fontSize, FontStyle.Bold, textColor, Vector2.zero, size);
            text.raycastTarget = false;
            return button;
        }

        private Button AddSpriteButton(RectTransform parent, string objectName, Vector2 position, Vector2 size, Sprite sprite, UnityEngine.Events.UnityAction action)
        {
            Sprite frameSprite = GuiProActionButtonSprite();
            RectTransform rect = AddPanel(parent, objectName, size, Color.white, frameSprite != null ? frameSprite : sprite);
            SetAnchored(rect, position);

            Image image = rect.GetComponent<Image>();
            image.preserveAspect = frameSprite == null;

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);
            button.colors = SpriteButtonColors();

            if (frameSprite != null && sprite != null)
            {
                RectTransform icon = AddPanel(rect, SemanticActionIconName(objectName, sprite), size * 0.58f, Color.white, sprite);
                SetAnchored(icon, Vector2.zero);
                Image iconImage = icon.GetComponent<Image>();
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;
            }

            return button;
        }

        private static string SemanticActionIconName(string objectName, Sprite sprite)
        {
            if (sprite != null && sprite.texture != null && !string.IsNullOrEmpty(sprite.texture.name))
                return sprite.texture.name + "Icon";

            return objectName + "Icon";
        }

        private RectTransform AddShooterButton(RectTransform parent, Shooter shooter, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
        {
            return AddShooterToken(parent, shooter, position, size, true, action);
        }

        private RectTransform AddShooterToken(RectTransform parent, Shooter shooter, Vector2 position, Vector2 size, bool selectable, UnityEngine.Events.UnityAction action)
        {
            Color fill = shooter.Hidden ? theme.SubtleText : ColorForShooter(shooter.Color, shooter.Wild);
            if (!selectable)
                fill = ColorWithAlpha(fill, shooter.Hidden ? 0.76f : 0.68f);

            Color textColor = shooter.Hidden || shooter.Wild || shooter.Color == FlowColor.Yellow ? new Color32(26, 23, 64, 255) : Color.white;
            string label = shooter.Hidden ? "?" : Mathf.Max(0, shooter.Ammo).ToString(CultureInfo.InvariantCulture);
            Sprite tokenSprite = shooter.Hidden ? shooterCircleSprite : SquareFlowWorldSprites.OrbitForShooter(shooter.Color, shooter.Wild);
            bool usesTextureSprite = tokenSprite != null && tokenSprite.texture != null && tokenSprite.texture.name.StartsWith("FlowOrbit");
            Color tokenColor = usesTextureSprite ? ColorWithAlpha(Color.white, selectable ? 1f : 0.78f) : fill;
            RectTransform rect = AddPanel(parent, selectable ? "ShooterButton" : "ShooterPreview", size, tokenColor, tokenSprite);
            SetAnchored(rect, position);

            Image image = rect.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = tokenSprite;
                image.type = Image.Type.Simple;
                image.raycastTarget = selectable;
            }

            float diameter = Mathf.Min(size.x, size.y);
            rect.sizeDelta = new Vector2(diameter, diameter);
            ApplyOutline(rect, ColorWithAlpha(Color.white, shooter.Hidden ? 0.22f : selectable ? 0.26f : 0.14f), 1f);
            AddShooterAmmoLabel(rect, label, diameter, textColor, selectable);

            if (selectable)
            {
                Button button = rect.gameObject.AddComponent<Button>();
                button.targetGraphic = image;
                button.onClick.AddListener(action);
                button.colors = ButtonColors(tokenColor);
            }

            return rect;
        }

        private TMP_Text AddShooterAmmoLabel(RectTransform token, string label, float diameter, Color textColor, bool selectable)
        {
            TMP_Text text = AddText(
                token,
                label,
                selectable ? SquareFlowVisualMetrics.ShooterAmmoLabelFontSize : SquareFlowVisualMetrics.ShooterAmmoLabelQueuedFontSize,
                FontStyle.Bold,
                textColor,
                Vector2.zero,
                new Vector2(diameter, diameter));
            text.gameObject.name = "AmmoLabel";
            text.raycastTarget = false;
            return text;
        }

        private void AddShooterAmmoDots(RectTransform token, Shooter shooter, float diameter, Color fill, bool selectable)
        {
            int count = Mathf.Max(0, shooter.Ammo);
            if (shooter.Hidden || count == 0) return;

            float rowWidth = Mathf.Max(diameter, SquareFlowVisualMetrics.ShooterAmmoDotDiameter + (count - 1) * SquareFlowVisualMetrics.ShooterAmmoDotSpacing);
            RectTransform row = AddContainer(token, "AmmoDots", new Vector2(rowWidth, SquareFlowVisualMetrics.ShooterAmmoDotDiameter));
            SetAnchored(row, new Vector2(0f, diameter * 0.5f + SquareFlowVisualMetrics.ShooterAmmoDotTopOffset));

            float spacing = count > 1
                ? Mathf.Min(SquareFlowVisualMetrics.ShooterAmmoDotSpacing, diameter * 0.82f / (count - 1))
                : 0f;
            float startX = -spacing * (count - 1) * 0.5f;
            Color dotColor = ColorWithAlpha(Color.Lerp(fill, Color.white, 0.72f), selectable ? 0.96f : 0.72f);

            for (int i = 0; i < count; i++)
            {
                RectTransform dot = AddPanel(row, "AmmoDot", Vector2.one * SquareFlowVisualMetrics.ShooterAmmoDotDiameter, dotColor, circleSprite);
                SetAnchored(dot, new Vector2(startX + i * spacing, 0f));
                Image image = dot.GetComponent<Image>();
                if (image != null)
                    image.raycastTarget = false;
                ApplyOutline(dot, ColorWithAlpha(Color.black, selectable ? 0.22f : 0.14f), 0.7f);
            }
        }

        private void ApplyOutline(RectTransform rect, Color color, float distance)
        {
            Outline outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(distance, -distance);
        }

        private void ApplySoftPanelDepth(RectTransform rect, float alpha, float distance)
        {
            Shadow shadow = rect.gameObject.AddComponent<Shadow>();
            shadow.effectColor = ColorWithAlpha(new Color32(12, 28, 84, 255), alpha);
            shadow.effectDistance = new Vector2(0f, -distance);
        }

        private void EnsureSoftPanelDepth(RectTransform rect, float alpha, float distance)
        {
            Shadow[] shadows = rect.GetComponents<Shadow>();
            for (int i = 0; i < shadows.Length; i++)
            {
                if (shadows[i].GetType() == typeof(Shadow))
                    return;
            }

            ApplySoftPanelDepth(rect, alpha, distance);
        }

        private ColorBlock ButtonColors(Color baseColor)
        {
            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.normalColor = baseColor;
            colors.highlightedColor = LerpColor(baseColor, Color.white, 0.16f);
            colors.pressedColor = LerpColor(baseColor, Color.black, 0.18f);
            colors.selectedColor = LerpColor(baseColor, Color.white, 0.08f);
            colors.disabledColor = ColorWithAlpha(baseColor, 0.36f);
            colors.colorMultiplier = 1f;
            return colors;
        }

        private static ColorBlock SpriteButtonColors()
        {
            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
            colors.pressedColor = new Color(0.86f, 0.92f, 1f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.36f);
            colors.colorMultiplier = 1f;
            return colors;
        }

        private static Color LerpColor(Color from, Color to, float t)
        {
            return new Color(
                Mathf.Lerp(from.r, to.r, t),
                Mathf.Lerp(from.g, to.g, t),
                Mathf.Lerp(from.b, to.b, t),
                from.a);
        }

        private static Color ColorWithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private static Color SkyBarColor(float alpha)
        {
            return ColorWithAlpha(new Color32(45, 91, 177, 255), alpha);
        }

        private static Color SkyCardColor(float alpha)
        {
            return ColorWithAlpha(new Color32(61, 74, 165, 255), alpha);
        }

        private static Color SkyBorderColor(float alpha)
        {
            return ColorWithAlpha(new Color32(207, 231, 255, 255), alpha);
        }

        private static Color BoardPanelColor()
        {
            return ColorWithAlpha(new Color32(53, 132, 233, 255), 0.42f);
        }

        private static Color HeaderLabelColor()
        {
            return new Color32(43, 99, 224, 255);
        }

        private static Color HeaderNumberColor()
        {
            return new Color32(63, 53, 188, 255);
        }

        private static Color HeaderButtonColor()
        {
            return new Color32(34, 91, 224, 245);
        }

        private void EnsureRuntimeSprites()
        {
            if (roundedRectSprite != null) return;
            roundedRectSprite = CreateRoundedRectSprite(96, 22);
            circleSprite = CreateCircleSprite(64, 0.5f, 0.5f, "SquareFlowCircle");
            shooterCircleSprite = CreateCircleSprite(64, 0.88f, 0.64f, "SquareFlowShooterCircle");
            glassPanelSprite = LoadSlicedUiSprite("SquareFlow/UI/FlowPanel", "FlowPanelSprite", new Vector4(150f, 150f, 150f, 150f), 300f);
            crownSprite = LoadSimpleUiSprite("SquareFlow/UI/FlowCrown", "FlowCrownSprite", 300f);
            gemSprite = LoadSimpleUiSprite("SquareFlow/UI/FlowGem", "FlowGemSprite", 300f);
            homeButtonSprite = LoadSimpleUiSprite("SquareFlow/UI/FlowHomeButton", "FlowHomeButtonSprite", 300f);
            restartButtonSprite = LoadSimpleUiSprite("SquareFlow/UI/FlowRestartButton", "FlowRestartButtonSprite", 300f);
            paletteButtonSprite = LoadSimpleUiSprite("SquareFlow/UI/FlowPaletteButton", "FlowPaletteButtonSprite", 300f);
            muteButtonSprite = LoadSimpleUiSprite("SquareFlow/UI/FlowMuteButton", "FlowMuteButtonSprite", 300f);

            guiProFont = Resources.Load<TMP_FontAsset>("SquareFlow/GUIPro/LilitaOne-Regular SDF");
            guiProPanelSprite = LoadSlicedUiSprite("SquareFlow/GUIPro/BasicFrame_Round20", "BasicFrame_Round20", new Vector4(25f, 25f, 25f, 25f), 180f);
            guiProInsetPanelSprite = LoadSlicedUiSprite("SquareFlow/GUIPro/BasicFrame_Round12", "BasicFrame_Round12", new Vector4(18f, 18f, 18f, 18f), 180f);
            guiProPlayButtonSprite = LoadSlicedUiSprite("SquareFlow/GUIPro/Button01_225_Yellow", "Button01_225_Yellow", new Vector4(32f, 199f, 32f, 26f), 220f);
            guiProPrimaryButtonSprite = LoadSlicedUiSprite("SquareFlow/GUIPro/Button01_225_Blue", "Button01_225_Blue", new Vector4(32f, 197f, 32f, 28f), 220f);
            guiProSmallButtonSprite = LoadSlicedUiSprite("SquareFlow/GUIPro/Button01_175_Blue", "Button01_175_Blue", new Vector4(33f, 144f, 31f, 31f), 190f);
            guiProDangerButtonSprite = LoadSlicedUiSprite("SquareFlow/GUIPro/Button01_175_Red", "Button01_175_Red", new Vector4(32f, 143f, 32f, 32f), 190f);
            guiProConfirmButtonSprite = LoadSlicedUiSprite("SquareFlow/GUIPro/Button01_175_Green", "Button01_175_Green", new Vector4(32f, 147f, 32f, 28f), 190f);
            guiProTitleRibbonSprite = LoadSlicedUiSprite("SquareFlow/GUIPro/Label_Ribbon_Single_Orange", "Label_Ribbon_Single_Orange", new Vector4(44f, 0f, 30f, 0f), 210f);
            guiProActionButtonBlueSprite = LoadSlicedUiSprite("SquareFlow/GUIPro/Button01_100_Blue", "Button01_100_Blue", new Vector4(27f, 113f, 28f, 32f), 145f);
            guiProActionButtonRedSprite = LoadSlicedUiSprite("SquareFlow/GUIPro/Button01_100_Red", "Button01_100_Red", new Vector4(26f, 116f, 29f, 29f), 145f);
            guiProActionButtonGreenSprite = LoadSlicedUiSprite("SquareFlow/GUIPro/Button01_100_Green", "Button01_100_Green", new Vector4(27f, 117f, 28f, 28f), 145f);
            guiProActionButtonYellowSprite = LoadSlicedUiSprite("SquareFlow/GUIPro/Button01_100_Yellow", "Button01_100_Yellow", new Vector4(28f, 116f, 27f, 29f), 145f);
            if (guiProPanelSprite != null)
                glassPanelSprite = guiProPanelSprite;
        }

        private int HighestSavedScore()
        {
            SaveDataService.ScoreEntry[] scores = saveData.Scores();
            return scores.Length > 0 ? scores[0].Score : 0;
        }

        private static bool HasSpriteBorder(Sprite sprite)
        {
            return sprite != null && (sprite.border.x > 0f || sprite.border.y > 0f || sprite.border.z > 0f || sprite.border.w > 0f);
        }

        private static Vector4 ClampSlicedSpriteBorder(Texture2D texture, Vector4 border)
        {
            if (texture == null) return Vector4.zero;

            Vector2 horizontal = ClampBorderPair(border.x, border.z, texture.width);
            Vector2 vertical = ClampBorderPair(border.y, border.w, texture.height);
            return new Vector4(horizontal.x, vertical.x, horizontal.y, vertical.y);
        }

        private static Vector2 ClampBorderPair(float leading, float trailing, int textureSize)
        {
            leading = Mathf.Max(0f, leading);
            trailing = Mathf.Max(0f, trailing);

            if (textureSize <= 0)
                return Vector2.zero;

            float maxBorderTotal = Mathf.Max(0f, textureSize - 1f);
            float total = leading + trailing;
            if (total <= maxBorderTotal)
                return new Vector2(leading, trailing);

            if (total <= 0f || maxBorderTotal <= 0f)
                return Vector2.zero;

            float scale = maxBorderTotal / total;
            return new Vector2(leading * scale, trailing * scale);
        }

        private static Sprite LoadSlicedUiSprite(string resourcePath, string spriteName, Vector4 border, float pixelsPerUnit)
        {
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null) return null;

            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            border = ClampSlicedSpriteBorder(texture, border);

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit,
                0,
                SpriteMeshType.FullRect,
                border);
            sprite.name = spriteName;
            return sprite;
        }

        private static Sprite LoadSimpleUiSprite(string resourcePath, string spriteName, float pixelsPerUnit)
        {
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null) return null;

            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit,
                0,
                SpriteMeshType.FullRect);
            sprite.name = spriteName;
            return sprite;
        }

        private static Sprite CreateRoundedRectSprite(int size, int radius)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "SquareFlowRoundedRect";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            float r = radius - 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float px = x < radius ? radius - x - 0.5f : x >= size - radius ? x - (size - radius) + 0.5f : 0f;
                float py = y < radius ? radius - y - 0.5f : y >= size - radius ? y - (size - radius) + 0.5f : 0f;
                float distance = Mathf.Sqrt(px * px + py * py);
                float alpha = px == 0f && py == 0f ? 1f : Mathf.Clamp01(r + 1f - distance);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }

        private static Sprite CreateCircleSprite(int size, float solidRadius, float edgeAlpha, string textureName)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = textureName;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            float center = (size - 1f) * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center) / center;
                float dy = (y - center) / center;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = edgeAlpha > 0f
                    ? Mathf.Clamp01((solidRadius - distance) * size * 0.28f + edgeAlpha)
                    : Mathf.Pow(Mathf.Clamp01(1f - distance), 2f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static void SetAnchored(RectTransform rect, Vector2 position)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
        }

        private static void SetUniformScale(RectTransform rect, float scale)
        {
            rect.localScale = Vector3.one * scale;
        }

        private static void SetTopStretch(RectTransform rect, float top, float height, float horizontalMargin)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(horizontalMargin, -top - height);
            rect.offsetMax = new Vector2(-horizontalMargin, -top);
            rect.anchoredPosition = new Vector2(0f, -top);
            rect.sizeDelta = new Vector2(-horizontalMargin * 2f, height);
        }

        private static void SetBottomStretch(RectTransform rect, float bottom, float height, float horizontalMargin)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(horizontalMargin, bottom);
            rect.offsetMax = new Vector2(-horizontalMargin, bottom + height);
            rect.anchoredPosition = new Vector2(0f, bottom);
            rect.sizeDelta = new Vector2(-horizontalMargin * 2f, height);
        }

        private static void SetStretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }

        private static FontStyles ToTmpFontStyle(FontStyle style)
        {
            switch (style)
            {
                case FontStyle.Bold:
                    return FontStyles.Bold;
                case FontStyle.Italic:
                    return FontStyles.Italic;
                case FontStyle.BoldAndItalic:
                    return FontStyles.Bold | FontStyles.Italic;
                default:
                    return FontStyles.Normal;
            }
        }

        private void ApplyGuiProTextSkin(TMP_Text text, int size, FontStyle style, Color color)
        {
            if (guiProFont != null)
                text.font = guiProFont;

            text.fontSize = size;
            text.fontStyle = ToTmpFontStyle(style);
            text.color = color;
            text.outlineWidth = TextOutlineWidth(size, style);
            text.outlineColor = TextOutlineColor(color);
            text.characterSpacing = size >= 40 ? 1.5f : 0.5f;
            text.wordSpacing = 0f;
        }

        private static float TextOutlineWidth(int size, FontStyle style)
        {
            if (size >= 52) return 0.18f;
            if (size >= 32) return 0.13f;
            return style == FontStyle.Bold || style == FontStyle.BoldAndItalic ? 0.09f : 0.055f;
        }

        private static Color32 TextOutlineColor(Color color)
        {
            Color dark = Color.Lerp(new Color32(42, 36, 104, 255), Color.black, 0.12f);
            if (color.r + color.g + color.b < 1.5f)
                return new Color32(255, 255, 255, 190);

            return ColorWithAlpha(dark, 0.86f);
        }

        private static TextAlignmentOptions ToTmpAlignment(TextAnchor alignment)
        {
            switch (alignment)
            {
                case TextAnchor.MiddleLeft:
                    return TextAlignmentOptions.MidlineLeft;
                case TextAnchor.MiddleRight:
                    return TextAlignmentOptions.MidlineRight;
                case TextAnchor.UpperLeft:
                    return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperCenter:
                    return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight:
                    return TextAlignmentOptions.TopRight;
                case TextAnchor.LowerLeft:
                    return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter:
                    return TextAlignmentOptions.Bottom;
                case TextAnchor.LowerRight:
                    return TextAlignmentOptions.BottomRight;
                default:
                    return TextAlignmentOptions.Center;
            }
        }

        private void ClearDynamicObjects()
        {
            for (int i = dynamicObjects.Count - 1; i >= 0; i--)
            {
                GameObject go = dynamicObjects[i];
                if (go != null)
                {
                    go.SetActive(false);
                    if (Application.isPlaying)
                        Destroy(go);
                    else
                        DestroyImmediate(go);
                }
            }
            dynamicObjects.Clear();
            hudText = null;
            bestText = null;
            comboText = null;
        }

        private static void EnsureEventSystem()
        {
            EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
                eventSystem = eventSystemObject.GetComponent<EventSystem>();
            }

#if ENABLE_INPUT_SYSTEM
            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();

            BaseInputModule[] modules = eventSystem.GetComponents<BaseInputModule>();
            for (int i = 0; i < modules.Length; i++)
            {
                if (modules[i] is InputSystemUIInputModule) continue;
                modules[i].enabled = false;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (eventSystem.GetComponent<BaseInputModule>() == null)
                eventSystem.gameObject.AddComponent<StandaloneInputModule>();
#else
            Debug.LogWarning("Square Flow UI needs an EventSystem input module, but neither Input System nor legacy input is enabled.");
#endif
        }

    }

    public static class SquareFlowGameViewRefreshPolicy
    {
        public static bool NeedsFullRefresh(IReadOnlyList<GameEvent> events)
        {
            if (events == null) return false;

            for (int i = 0; i < events.Count; i++)
                if (!IsWorldOnlyEvent(events[i].Type))
                    return true;

            return false;
        }

        private static bool IsWorldOnlyEvent(GameEventType type)
        {
            switch (type)
            {
                case GameEventType.BlockDamaged:
                case GameEventType.BlockDestroyed:
                case GameEventType.BombDetonated:
                case GameEventType.OrbiterRemoved:
                case GameEventType.ResultChanged:
                    return true;
                default:
                    return false;
            }
        }
    }

    public static class SquareFlowSafeArea
    {
        public static Vector4 PaddingForCanvas(Rect safeArea, Vector2 screenSize, Vector2 canvasSize)
        {
            if (screenSize.x <= 0f || screenSize.y <= 0f || canvasSize.x <= 0f || canvasSize.y <= 0f)
                return Vector4.zero;
            if (safeArea.width <= 0f || safeArea.height <= 0f)
                return Vector4.zero;

            float scaleX = canvasSize.x / screenSize.x;
            float scaleY = canvasSize.y / screenSize.y;
            float left = Mathf.Max(0f, safeArea.xMin * scaleX);
            float bottom = Mathf.Max(0f, safeArea.yMin * scaleY);
            float right = Mathf.Max(0f, (screenSize.x - safeArea.xMax) * scaleX);
            float top = Mathf.Max(0f, (screenSize.y - safeArea.yMax) * scaleY);
            return new Vector4(left, bottom, right, top);
        }
    }

    public static class SquareFlowVisualMetrics
    {
        public const float OrbitRingThicknessScale = 0.035f;
        public const int OrbitRingPointCount = 128;
        public const float ActiveOrbiterHolderScale = 1.62f;
        public const float ActiveOrbiterGlowScale = 1.52f;
        public const float ActiveOrbiterTokenScale = 0.98f;
        public const float ActiveOrbiterWorldScale = 1f;
        public const float ActiveOrbiterLaunchDurationSeconds = 0.5f;
        public const float ActiveOrbiterAmmoLabelFontSize = 1f;
        public const float ActiveOrbiterAmmoParticleScale = 0.1f;
        public const float ActiveOrbiterAmmoParticleOrbitRadiusScale = 0.72f;
        public const float ActiveOrbiterAmmoParticleRotationDegreesPerSecond = 120f;
        public const float ShooterButtonMinimumDiameter = 100f;
        public const int ShooterAmmoLabelFontSize = 26;
        public const int ShooterAmmoLabelQueuedFontSize = 20;
        public const float ShooterAmmoDotDiameter = 9f;
        public const float ShooterAmmoDotSpacing = 12f;
        public const float ShooterAmmoDotTopOffset = 8f;
        public const float DockSlotFrontScale = 1.4f;
        public const float CellDepthOffsetScale = 0.09f;
        public const float TileFaceScale = 0.96f;
        public const float TileDepthDropScale = 0.16f;
        public const float TileDepthDarkenAmount = 0.28f;
        public const float TileTopHighlightAlpha = 0.18f;
        public const float CellLabelFontSize = 2f;
        public const float CellHitFeedbackDurationSeconds = 0.22f;
        public const float CellHitShakeAmplitudeScale = 0.11f;
        public const float CellHitHeavyShakeMultiplier = 1.45f;
        public const float CellHitShakeFrequency = 58f;
        public const float CellHitFlashAlpha = 0.62f;
        public const float CellHitFaceFlashAmount = 0.72f;
        public const float ShotBulletTrailLength = 0.56f;
    }

    public readonly struct SquareFlowStartScreenLayout
    {
        private SquareFlowStartScreenLayout(
            Vector2 contentSize,
            Vector2 contentPosition,
            Vector2 titlePosition,
            Vector2 swatchPosition,
            Vector2 themeTogglePosition,
            Vector2 statsPosition,
            Vector2 statsSize,
            Vector2 levelSelectorPosition,
            Vector2 levelSelectorSize,
            Vector2 instructionsPosition,
            Vector2 instructionsSize,
            Vector2 playButtonPosition,
            Vector2 playButtonSize)
        {
            ContentSize = contentSize;
            ContentPosition = contentPosition;
            TitlePosition = titlePosition;
            SwatchPosition = swatchPosition;
            ThemeTogglePosition = themeTogglePosition;
            StatsPosition = statsPosition;
            StatsSize = statsSize;
            LevelSelectorPosition = levelSelectorPosition;
            LevelSelectorSize = levelSelectorSize;
            InstructionsPosition = instructionsPosition;
            InstructionsSize = instructionsSize;
            PlayButtonPosition = playButtonPosition;
            PlayButtonSize = playButtonSize;
        }

        public Vector2 ContentSize { get; }
        public Vector2 ContentPosition { get; }
        public Vector2 TitlePosition { get; }
        public Vector2 SwatchPosition { get; }
        public Vector2 ThemeTogglePosition { get; }
        public Vector2 StatsPosition { get; }
        public Vector2 StatsSize { get; }
        public Vector2 LevelSelectorPosition { get; }
        public Vector2 LevelSelectorSize { get; }
        public Vector2 InstructionsPosition { get; }
        public Vector2 InstructionsSize { get; }
        public Vector2 PlayButtonPosition { get; }
        public Vector2 PlayButtonSize { get; }

        public static SquareFlowStartScreenLayout Create()
        {
            return new SquareFlowStartScreenLayout(
                new Vector2(1030f, 1260f),
                new Vector2(0f, 40f),
                new Vector2(0f, 470f),
                new Vector2(0f, 365f),
                new Vector2(0f, 280f),
                new Vector2(0f, 110f),
                new Vector2(1000f, 170f),
                new Vector2(0f, -70f),
                new Vector2(820f, 72f),
                new Vector2(0f, -285f),
                new Vector2(840f, 286f),
                new Vector2(0f, -560f),
                new Vector2(410f, 128f));
        }
    }

    public readonly struct SquareFlowGameplayScreenLayout
    {
        public const int ShooterColumnVisibleRows = 4;

        private SquareFlowGameplayScreenLayout(
            Vector2 hudPosition,
            Vector2 hudSize,
            Vector2 statusBarSize,
            float statusBarTopOffset,
            Vector2 actionPosition,
            Vector2 actionSize,
            Vector2 boardPosition,
            Vector2 orbiterStripPosition,
            Vector2 orbiterStripSize,
            float orbiterStripTopOffset,
            Vector2 boardPanelPosition,
            Vector2 boardPanelSize,
            Vector2 queuePosition,
            Vector2 queueSize,
            float queueBottomOffset,
            Vector2 dockPosition,
            Vector2 dockSize,
            float dockBottomOffset,
            Vector2 utilityButtonSize)
        {
            HudPosition = hudPosition;
            HudSize = hudSize;
            StatusBarSize = statusBarSize;
            StatusBarTopOffset = statusBarTopOffset;
            ActionPosition = actionPosition;
            ActionSize = actionSize;
            BoardPosition = boardPosition;
            OrbiterStripPosition = orbiterStripPosition;
            OrbiterStripSize = orbiterStripSize;
            OrbiterStripTopOffset = orbiterStripTopOffset;
            BoardPanelPosition = boardPanelPosition;
            BoardPanelSize = boardPanelSize;
            QueuePosition = queuePosition;
            QueueSize = queueSize;
            QueueBottomOffset = queueBottomOffset;
            DockPosition = dockPosition;
            DockSize = dockSize;
            DockBottomOffset = dockBottomOffset;
            UtilityButtonSize = utilityButtonSize;
        }

        public Vector2 HudPosition { get; }
        public Vector2 HudSize { get; }
        public Vector2 StatusBarSize { get; }
        public float StatusBarTopOffset { get; }
        public Vector2 ActionPosition { get; }
        public Vector2 ActionSize { get; }
        public Vector2 BoardPosition { get; }
        public Vector2 OrbiterStripPosition { get; }
        public Vector2 OrbiterStripSize { get; }
        public float OrbiterStripTopOffset { get; }
        public Vector2 BoardPanelPosition { get; }
        public Vector2 BoardPanelSize { get; }
        public Vector2 QueuePosition { get; }
        public Vector2 QueueSize { get; }
        public float QueueBottomOffset { get; }
        public Vector2 DockPosition { get; }
        public Vector2 DockSize { get; }
        public float DockBottomOffset { get; }
        public Vector2 UtilityButtonSize { get; }
        public int DockVisibleRows => ShooterColumnVisibleRows;

        public static SquareFlowGameplayScreenLayout Create(BoardLayout board)
        {
            return Create(board, 0f);
        }

        public static SquareFlowGameplayScreenLayout Create(BoardLayout board, float canvasHeight)
        {
            return Create(board, canvasHeight, 0f, 0f);
        }

        public static SquareFlowGameplayScreenLayout Create(BoardLayout board, float canvasHeight, float safeAreaTop, float safeAreaBottom)
        {
            Vector2 hudSize = new Vector2(1080f, 128f);
            Vector2 statusBarSize = new Vector2(1080f, 86f);
            Vector2 actionSize = new Vector2(360f, 76f);
            Vector2 orbiterStripSize = new Vector2(1064f, 92f);
            Vector2 boardPanelSize = new Vector2(1080f, 1080f);
            Vector2 queueSize = new Vector2(1064f, 164f);
            Vector2 dockSize = new Vector2(1064f, 480f);
            float statusBarTopOffset = 136f;
            float orbiterStripTopOffset = 236f;
            float queueBottomOffset = 500f;
            float dockBottomOffset = 12f;
            Vector2 orbiterStripPosition = new Vector2(0f, 678f);
            Vector2 boardPanelPosition = new Vector2(0f, 130f);
            Vector2 queuePosition = new Vector2(0f, -378f);
            Vector2 dockPosition = new Vector2(0f, -708f);

            if (canvasHeight > 0f)
            {
                const float requiredGap = 8f;
                float topLimit = canvasHeight * 0.5f - (safeAreaTop + orbiterStripTopOffset + orbiterStripSize.y) - requiredGap;
                float queueTop = -canvasHeight * 0.5f + safeAreaBottom + queueBottomOffset + queueSize.y;
                float bottomLimit = queueTop + requiredGap;
                float availableBoardHeight = topLimit - bottomLimit;

                orbiterStripPosition = new Vector2(0f, canvasHeight * 0.5f - safeAreaTop - orbiterStripTopOffset - orbiterStripSize.y * 0.5f);
                queuePosition = new Vector2(0f, -canvasHeight * 0.5f + safeAreaBottom + queueBottomOffset + queueSize.y * 0.5f);
                dockPosition = new Vector2(0f, -canvasHeight * 0.5f + safeAreaBottom + dockBottomOffset + dockSize.y * 0.5f);

                if (availableBoardHeight < boardPanelSize.y)
                {
                    float fittedBoardSize = Mathf.Max(640f, availableBoardHeight);
                    fittedBoardSize = Mathf.Min(boardPanelSize.x, fittedBoardSize);
                    boardPanelSize = new Vector2(fittedBoardSize, fittedBoardSize);
                    boardPanelPosition = new Vector2(0f, (topLimit + bottomLimit) * 0.5f);
                }
            }

            return new SquareFlowGameplayScreenLayout(
                new Vector2(0f, 896f),
                hudSize,
                statusBarSize,
                statusBarTopOffset,
                new Vector2(344f, 0f),
                actionSize,
                Vector2.zero,
                orbiterStripPosition,
                orbiterStripSize,
                orbiterStripTopOffset,
                boardPanelPosition,
                boardPanelSize,
                queuePosition,
                queueSize,
                queueBottomOffset,
                dockPosition,
                dockSize,
                dockBottomOffset,
                new Vector2(78f, 78f));
        }
    }
}
