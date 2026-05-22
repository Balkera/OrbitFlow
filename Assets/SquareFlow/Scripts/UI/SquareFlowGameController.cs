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
        private const float ShooterColumnSpacing = 118f;
        private const float ShooterRowSpacing = 96f;
        private const float WaitingQueueSpacing = 96f;

        private readonly List<GameObject> dynamicObjects = new List<GameObject>();
        private SaveDataService saveData;
        private SquareFlowAudio audioCue;
        private SquareFlowTheme theme;
        private RectTransform root;
        private GameState state;
        private GameRules rules;
        private BoardLayout layout;
        private TMP_Text hudText;
        private TMP_Text comboText;
        private Sprite roundedRectSprite;
        private Sprite circleSprite;
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
            background.color = theme.Background;

            RectTransform panel = AddPanel(root, "MenuPanel", Vector2.zero, theme.Panel, null);
            SetStretch(panel);

            RectTransform content = AddContainer(panel, "MenuContent", new Vector2(900f, 1500f));
            SetAnchored(content, Vector2.zero);

            AddText(content, "Square Flow", 78, FontStyle.Bold, theme.Text, new Vector2(0f, 650f), new Vector2(840f, 112f));
            BoardShape shape = BoardShapeCatalog.GetShape(saveData.Level);
            AddText(content, "Level " + saveData.Level + " - " + shape.Name, 36, FontStyle.Bold, theme.Score, new Vector2(0f, 550f), new Vector2(820f, 62f));
            AddText(content, "Clear matching color blocks before your shooters run out. Bombs clear nearby cells, wild shooters hit anything, and unused ammo moves into the waiting queue.", 25, FontStyle.Normal, theme.SubtleText, new Vector2(0f, 430f), new Vector2(820f, 128f));

            AddButton(content, "Play", new Vector2(0f, 260f), new Vector2(540f, 96f), theme.Green, theme.Text, StartLevel, 34);
            AddButton(content, saveData.DarkMode ? "Light" : "Dark", new Vector2(-150f, 130f), new Vector2(260f, 76f), theme.Blue, Color.white, ToggleTheme, 28);
            AddButton(content, saveData.Muted ? "Sound" : "Mute", new Vector2(150f, 130f), new Vector2(260f, 76f), theme.Yellow, new Color32(26, 23, 64, 255), ToggleMute, 28);

            AddText(content, "Select Level", 30, FontStyle.Bold, theme.Text, new Vector2(0f, 12f), new Vector2(360f, 50f));
            RenderLevelSelector(content);

            AddText(content, "Leaderboard", 30, FontStyle.Bold, theme.Text, new Vector2(0f, -310f), new Vector2(360f, 50f));
            RenderLeaderboard(content);

            AddButton(content, "Reset Progress", new Vector2(0f, -650f), new Vector2(500f, 66f), theme.Red, Color.white, ResetProgress, 24);
            AddText(content, "Click a front shooter column or a waiting shooter to fire. Combo rises as blocks break quickly.", 20, FontStyle.Normal, theme.SubtleText, new Vector2(0f, -712f), new Vector2(820f, 44f));
        }

        private void StartLevel()
        {
            int level = saveData.Level;
            BoardShape shape = BoardShapeCatalog.GetShape(level);
            IFlowRandom random = new SystemFlowRandom();
            BoardCell[,] grid = BoardGenerator.Generate(shape, level, random);
            List<Shooter>[] columns = ShooterGenerator.BuildColumns(grid, shape, level, random);

            layout = BoardLayout.Compute(shape.Rows, shape.Cols, 860f);
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
            background.color = Color.clear;

            SquareFlowGameplayScreenLayout screen = SquareFlowGameplayScreenLayout.Create(layout);

            RectTransform hud = AddPanel(root, "Hud", screen.HudSize, ColorWithAlpha(theme.Panel, 0.94f));
            SetAnchored(hud, screen.HudPosition);
            ApplyOutline(hud, ColorWithAlpha(theme.Border, 0.55f), 1f);
            AddText(hud, "LEVEL " + state.Level.ToString("00", CultureInfo.InvariantCulture), 14, FontStyle.Bold, theme.SubtleText, new Vector2(-130f, 30f), new Vector2(220f, 26f), TextAnchor.MiddleLeft);
            hudText = AddText(hud, string.Empty, 38, FontStyle.Bold, theme.Score, new Vector2(-130f, -16f), new Vector2(220f, 58f), TextAnchor.MiddleLeft);
            comboText = AddText(hud, string.Empty, 16, FontStyle.Bold, theme.SubtleText, new Vector2(136f, -8f), new Vector2(220f, 42f), TextAnchor.MiddleRight);

            RectTransform actions = AddPanel(root, "HudActions", screen.ActionSize, ColorWithAlpha(theme.Panel, 0.9f));
            SetAnchored(actions, screen.ActionPosition);
            ApplyOutline(actions, ColorWithAlpha(theme.Border, 0.5f), 1f);
            AddButton(actions, "II", new Vector2(-84f, 0f), screen.UtilityButtonSize, theme.Button, theme.Text, ShowMenu);
            AddButton(actions, "R", Vector2.zero, screen.UtilityButtonSize, theme.Button, theme.Text, StartLevel);
            AddButton(actions, "S", new Vector2(84f, 0f), screen.UtilityButtonSize, theme.Button, theme.Text, ToggleMuteInGame);

            RefreshWorldGameplay();

            RectTransform queue = AddPanel(root, "WaitingQueue", screen.QueueSize, theme.Panel);
            SetAnchored(queue, screen.QueuePosition);
            ApplyOutline(queue, ColorWithAlpha(theme.Border, 0.55f), 1f);
            RenderWaiting(queue);

            RectTransform columns = AddPanel(root, "ShooterColumns", screen.DockSize, theme.Panel);
            SetAnchored(columns, screen.DockPosition);
            ApplyOutline(columns, ColorWithAlpha(theme.Border, 0.55f), 1f);
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
            boardWorldView.Bind(state, layout, worldLayout, theme);
            orbitRingWorldView.Bind(layout, worldLayout, theme);
            orbiterWorldView.Refresh(state.ActiveOrbiters, worldLayout, theme);
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
                ? MobileWorldLayout.Create(layout, mobileCamera.VisibleWorldRect)
                : MobileWorldLayout.Create(layout);
            bool changed = !worldLayout.IsValid
                || (worldLayout.BoardCenter - next.BoardCenter).sqrMagnitude > 0.0001f
                || Mathf.Abs(worldLayout.WorldUnitsPerLayoutPixel - next.WorldUnitsPerLayoutPixel) > 0.0001f;
            worldLayout = next;
            return changed;
        }

        private void RenderWaiting(RectTransform queue)
        {
            int capacity = SquareFlowConstants.WaitQueueLimit;
            float startY = WaitingQueueStartY(capacity);

            for (int i = 0; i < capacity; i++)
            {
                Vector2 position = new Vector2(0f, startY - i * WaitingQueueSpacing);
                RectTransform slot = AddPanel(queue, "WaitingSlot", Vector2.one * 78f, ColorWithAlpha(theme.DockSlot, 0.78f), circleSprite);
                SetAnchored(slot, position);
                ApplyOutline(slot, ColorWithAlpha(theme.Border, 0.42f), 1f);
            }

            for (int i = 0; i < state.WaitingQueue.Count; i++)
            {
                int index = i;
                Shooter shooter = state.WaitingQueue[i];
                AddShooterButton(queue, shooter, new Vector2(0f, startY - i * WaitingQueueSpacing), Vector2.one * 70f, () => FireWaiting(index));
            }
        }

        private void RenderColumns(RectTransform columns)
        {
            const float slotSize = 78f;
            int visibleRows = SquareFlowGameplayScreenLayout.ShooterColumnVisibleRows;
            float startX = ShooterColumnsStartX(state.ShooterColumns.Length);
            float startY = ShooterColumnsStartY(visibleRows);

            for (int i = 0; i < state.ShooterColumns.Length; i++)
            {
                int column = i;
                float x = startX + i * ShooterColumnSpacing;
                List<Shooter> shooterColumn = state.ShooterColumns[i];

                for (int row = 0; row < visibleRows; row++)
                {
                    bool frontRow = row == 0;
                    Vector2 position = new Vector2(x, startY - row * ShooterRowSpacing);
                    RectTransform slot = AddPanel(columns, frontRow ? "DockSlotFront" : "DockSlotQueued", Vector2.one * slotSize, ColorWithAlpha(theme.DockSlot, frontRow ? 0.94f : 0.58f));
                    SetAnchored(slot, position);
                    if (frontRow)
                        slot.localScale = Vector3.one * SquareFlowVisualMetrics.DockSlotFrontScale;
                    ApplyOutline(slot, ColorWithAlpha(theme.Border, frontRow ? 0.55f : 0.32f), 1f);

                    if (row >= shooterColumn.Count)
                    {
                        if (frontRow)
                            AddText(slot, "-", 18, FontStyle.Bold, theme.SubtleText, Vector2.zero, Vector2.one * slotSize);
                        continue;
                    }

                    Shooter shooter = shooterColumn[row];
                    if (frontRow)
                        AddShooterButton(slot, shooter, Vector2.zero, Vector2.one * 68f, () => FireColumn(column));
                    else
                        AddShooterToken(slot, shooter, Vector2.zero, Vector2.one * 58f, false, null);
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
            SquareFlowGameplayScreenLayout screen = SquareFlowGameplayScreenLayout.Create(layout);
            Vector2 columnOffset = new Vector2(
                ShooterColumnsStartX(state.ShooterColumns.Length) + column * ShooterColumnSpacing,
                ShooterColumnsStartY(screen.DockVisibleRows));
            Vector2 referencePosition = screen.DockPosition + columnOffset;
            orbiterWorldView.RegisterLaunchSource(orbiterId, ReferenceCanvasToWorld(referencePosition));
        }

        private void RegisterWaitingLaunch(string orbiterId, int index)
        {
            if (string.IsNullOrEmpty(orbiterId) || orbiterWorldView == null || layout == null) return;
            SquareFlowGameplayScreenLayout screen = SquareFlowGameplayScreenLayout.Create(layout);
            Vector2 queueOffset = new Vector2(
                0f,
                WaitingQueueStartY(SquareFlowConstants.WaitQueueLimit) - index * WaitingQueueSpacing);
            Vector2 referencePosition = screen.QueuePosition + queueOffset;
            orbiterWorldView.RegisterLaunchSource(orbiterId, ReferenceCanvasToWorld(referencePosition));
        }

        private Vector2 ReferenceCanvasToWorld(Vector2 anchoredPosition)
        {
            Rect visible = mobileCamera != null
                ? mobileCamera.VisibleWorldRect
                : new Rect(-5.4f, -9.6f, ReferenceCanvasWidth * 0.01f, ReferenceCanvasHeight * 0.01f);
            float viewportX = Mathf.Clamp01(0.5f + anchoredPosition.x / ReferenceCanvasWidth);
            float viewportY = Mathf.Clamp01(0.5f + anchoredPosition.y / ReferenceCanvasHeight);
            return new Vector2(
                Mathf.Lerp(visible.xMin, visible.xMax, viewportX),
                Mathf.Lerp(visible.yMin, visible.yMax, viewportY));
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

            RectTransform panel = AddPanel(root, "ResultPanel", new Vector2(480f, 270f), theme.Panel);
            SetAnchored(panel, new Vector2(0f, 72f));
            ApplyOutline(panel, ColorWithAlpha(theme.Score, 0.32f), 2f);
            AddText(panel, ResultTitle(), 34, FontStyle.Bold, theme.Text, new Vector2(0f, 78f), new Vector2(420f, 52f));
            AddText(panel, "Score " + state.Score + " - Moves " + state.Moves, 21, FontStyle.Bold, theme.Score, new Vector2(0f, 22f), new Vector2(420f, 38f));
            AddButton(panel, state.Result == GameResult.Won ? "Next Level" : "Try Again", new Vector2(0f, -42f), new Vector2(240f, 50f), theme.Green, theme.Text, StartLevel);
            AddButton(panel, "Menu", new Vector2(0f, -104f), new Vector2(240f, 42f), theme.Blue, Color.white, ShowMenu);
        }

        private string ResultTitle()
        {
            if (state.Result == GameResult.Won) return "Level Clear";
            if (state.Result == GameResult.LostWait) return "Queue Full";
            return "Out of Shooters";
        }

        private void RenderLevelSelector(RectTransform panel)
        {
            HashSet<int> completed = saveData.CompletedLevels();
            for (int i = 0; i < BoardShapeCatalog.Count; i++)
            {
                int level = i + 1;
                int row = i / 5;
                int col = i % 5;
                float x = -320f + col * 160f;
                float y = -92f - row * 88f;
                bool selected = level == saveData.Level;
                Color fill = selected ? theme.Green : completed.Contains(level) ? theme.Score : theme.Blue;
                Color text = selected || completed.Contains(level) ? new Color32(26, 23, 64, 255) : Color.white;
                AddButton(panel, level.ToString(), new Vector2(x, y), new Vector2(120f, 68f), fill, text, () => SelectLevel(level), 30);
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
            if (state == null || hudText == null || comboText == null) return;

            hudText.text = state.Score.ToString("N0", CultureInfo.InvariantCulture);
            comboText.text = state.Combo > 1f ? "COMBO x" + state.Combo.ToString("0.0", CultureInfo.InvariantCulture) : "MOVES " + state.Moves;
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
            return cell.Type == BoardCellType.Bomb ? theme.CellEmpty : ColorForFlowColor(cell.Color);
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
            scaler.matchWidthOrHeight = 0.35f;

            root = canvasObject.GetComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
            canvasObject.GetComponent<Image>().color = theme.Background;
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
            image.type = sprite == roundedRectSprite ? Image.Type.Sliced : Image.Type.Simple;
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
            text.fontSize = size;
            text.fontStyle = ToTmpFontStyle(style);
            text.color = color;
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

        private Button AddButton(RectTransform parent, string label, Vector2 position, Vector2 size, Color color, Color textColor, UnityEngine.Events.UnityAction action, int fontSize)
        {
            RectTransform rect = AddPanel(parent, "Button", size, color);
            SetAnchored(rect, position);

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            button.onClick.AddListener(action);
            button.colors = ButtonColors(color);
            ApplyOutline(rect, ColorWithAlpha(Color.white, 0.26f), 1f);

            TMP_Text text = AddText(rect, label, fontSize, FontStyle.Bold, textColor, Vector2.zero, size);
            text.raycastTarget = false;
            return button;
        }

        private void AddShooterButton(RectTransform parent, Shooter shooter, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
        {
            AddShooterToken(parent, shooter, position, size, true, action);
        }

        private void AddShooterToken(RectTransform parent, Shooter shooter, Vector2 position, Vector2 size, bool selectable, UnityEngine.Events.UnityAction action)
        {
            Color fill = shooter.Hidden ? theme.SubtleText : ColorForShooter(shooter.Color, shooter.Wild);
            if (!selectable)
                fill = ColorWithAlpha(fill, shooter.Hidden ? 0.48f : 0.68f);

            Color textColor = shooter.Hidden || shooter.Wild || shooter.Color == FlowColor.Yellow ? new Color32(26, 23, 64, 255) : Color.white;
            string label = shooter.Hidden ? "?" : Mathf.Max(0, shooter.Ammo).ToString(CultureInfo.InvariantCulture);
            RectTransform rect = AddPanel(parent, selectable ? "ShooterButton" : "ShooterPreview", size, fill, circleSprite);
            SetAnchored(rect, position);

            Image image = rect.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = circleSprite;
                image.type = Image.Type.Simple;
                image.raycastTarget = selectable;
            }

            float diameter = Mathf.Min(size.x, size.y);
            rect.sizeDelta = new Vector2(diameter, diameter);
            ApplyOutline(rect, ColorWithAlpha(Color.white, selectable ? 0.26f : 0.14f), 1f);
            AddShooterAmmoDots(rect, shooter, diameter, fill, selectable);
            AddShooterAmmoLabel(rect, label, diameter, textColor, selectable);

            if (selectable)
            {
                Button button = rect.gameObject.AddComponent<Button>();
                button.targetGraphic = image;
                button.onClick.AddListener(action);
                button.colors = ButtonColors(fill);
            }
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

        private void EnsureRuntimeSprites()
        {
            if (roundedRectSprite != null) return;
            roundedRectSprite = CreateRoundedRectSprite(96, 22);
            circleSprite = CreateCircleSprite(64, 0.5f, 0.5f);
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

        private static Sprite CreateCircleSprite(int size, float solidRadius, float edgeAlpha)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = edgeAlpha > 0f ? "SquareFlowCircle" : "SquareFlowGlow";
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
                    Destroy(go);
                }
            }
            dynamicObjects.Clear();
            hudText = null;
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

    public static class SquareFlowVisualMetrics
    {
        public const float OrbitRingThicknessScale = 0.16f;
        public const int OrbitRingPointCount = 128;
        public const float ActiveOrbiterHolderScale = 1.62f;
        public const float ActiveOrbiterGlowScale = 1.52f;
        public const float ActiveOrbiterTokenScale = 0.98f;
        public const float ActiveOrbiterWorldScale = 2f;
        public const float ActiveOrbiterLaunchDurationSeconds = 0.5f;
        public const float ActiveOrbiterAmmoLabelFontSize = 1f;
        public const float ActiveOrbiterAmmoParticleScale = 0.1f;
        public const float ActiveOrbiterAmmoParticleOrbitRadiusScale = 0.72f;
        public const float ActiveOrbiterAmmoParticleRotationDegreesPerSecond = 120f;
        public const float ShooterButtonMinimumDiameter = 74f;
        public const int ShooterAmmoLabelFontSize = 18;
        public const int ShooterAmmoLabelQueuedFontSize = 15;
        public const float ShooterAmmoDotDiameter = 8f;
        public const float ShooterAmmoDotSpacing = 11f;
        public const float ShooterAmmoDotTopOffset = 8f;
        public const float DockSlotFrontScale = 1.4f;
        public const float CellDepthOffsetScale = 0.09f;
        public const float TileFaceScale = 0.92f;
        public const float TileDepthDropScale = 0.14f;
        public const float TileDepthDarkenAmount = 0.32f;
        public const float TileTopHighlightAlpha = 0.14f;
        public const float CellLabelFontSize = 2f;
        public const float CellHitFeedbackDurationSeconds = 0.22f;
        public const float CellHitShakeAmplitudeScale = 0.11f;
        public const float CellHitHeavyShakeMultiplier = 1.45f;
        public const float CellHitShakeFrequency = 58f;
        public const float CellHitFlashAlpha = 0.62f;
        public const float CellHitFaceFlashAmount = 0.72f;
        public const float ShotBulletTrailLength = 0.56f;
    }

    public readonly struct SquareFlowGameplayScreenLayout
    {
        public const int ShooterColumnVisibleRows = 5;

        private SquareFlowGameplayScreenLayout(
            Vector2 hudPosition,
            Vector2 hudSize,
            Vector2 actionPosition,
            Vector2 actionSize,
            Vector2 boardPosition,
            Vector2 queuePosition,
            Vector2 queueSize,
            Vector2 dockPosition,
            Vector2 dockSize,
            Vector2 utilityButtonSize)
        {
            HudPosition = hudPosition;
            HudSize = hudSize;
            ActionPosition = actionPosition;
            ActionSize = actionSize;
            BoardPosition = boardPosition;
            QueuePosition = queuePosition;
            QueueSize = queueSize;
            DockPosition = dockPosition;
            DockSize = dockSize;
            UtilityButtonSize = utilityButtonSize;
        }

        public Vector2 HudPosition { get; }
        public Vector2 HudSize { get; }
        public Vector2 ActionPosition { get; }
        public Vector2 ActionSize { get; }
        public Vector2 BoardPosition { get; }
        public Vector2 QueuePosition { get; }
        public Vector2 QueueSize { get; }
        public Vector2 DockPosition { get; }
        public Vector2 DockSize { get; }
        public Vector2 UtilityButtonSize { get; }
        public int DockVisibleRows => ShooterColumnVisibleRows;

        public static SquareFlowGameplayScreenLayout Create(BoardLayout board)
        {
            Vector2 hudSize = new Vector2(520f, 112f);
            Vector2 actionSize = new Vector2(274f, 88f);
            Vector2 queueSize = new Vector2(176f, 540f);
            Vector2 dockSize = new Vector2(520f, 540f);
            return new SquareFlowGameplayScreenLayout(
                new Vector2(-214f, 900f),
                hudSize,
                new Vector2(358f, 900f),
                actionSize,
                Vector2.zero,
                new Vector2(318f, -750f),
                queueSize,
                new Vector2(-118f, -750f),
                dockSize,
                new Vector2(64f, 64f));
        }
    }
}
