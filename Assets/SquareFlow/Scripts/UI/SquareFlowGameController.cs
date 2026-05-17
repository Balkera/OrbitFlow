using System.Collections.Generic;
using System.Globalization;
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
        private readonly List<GameObject> dynamicObjects = new List<GameObject>();
        private readonly Dictionary<string, RectTransform> orbiterRects = new Dictionary<string, RectTransform>();
        private SaveDataService saveData;
        private SquareFlowAudio audioCue;
        private SquareFlowTheme theme;
        private Canvas canvas;
        private RectTransform root;
        private Font font;
        private GameState state;
        private GameRules rules;
        private BoardLayout layout;
        private Text hudText;
        private Text comboText;
        private Sprite roundedRectSprite;
        private Sprite circleSprite;
        private Sprite glowSprite;
        private int seenEventCount;
        private GameResult seenResult;
        private bool resultHandled;

        private void Awake()
        {
            saveData = new SaveDataService();
            audioCue = GetComponent<SquareFlowAudio>();
            theme = new SquareFlowTheme(saveData.DarkMode);
            font = LoadFont();
            EnsureRuntimeSprites();
            EnsureEventSystem();
            BuildCanvas();
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
            UpdateOrbiterVisuals();

            if (events.Count == 0 && state.Events.Count == seenEventCount) return;

            seenEventCount = state.Events.Count;
            PlayEvents(events);
            RefreshGameView();

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
            ClearDynamicObjects();

            Image background = root.GetComponent<Image>();
            background.color = theme.Background;

            RectTransform panel = AddPanel(root, "MenuPanel", new Vector2(620f, 820f), theme.Panel);
            SetAnchored(panel, Vector2.zero);

            AddText(panel, "Square Flow", 44, FontStyle.Bold, theme.Text, new Vector2(0f, 350f), new Vector2(540f, 64f));
            BoardShape shape = BoardShapeCatalog.GetShape(saveData.Level);
            AddText(panel, "Level " + saveData.Level + " - " + shape.Name, 24, FontStyle.Bold, theme.Score, new Vector2(0f, 292f), new Vector2(540f, 44f));
            AddText(panel, "Clear matching color blocks before your shooters run out. Bombs clear nearby cells, wild shooters hit anything, and unused ammo moves into the waiting queue.", 18, FontStyle.Normal, theme.SubtleText, new Vector2(0f, 228f), new Vector2(510f, 82f));

            AddButton(panel, "Play", new Vector2(0f, 152f), new Vector2(300f, 54f), theme.Green, theme.Text, StartLevel);
            AddButton(panel, saveData.DarkMode ? "Light" : "Dark", new Vector2(-82f, 82f), new Vector2(140f, 44f), theme.Blue, Color.white, ToggleTheme);
            AddButton(panel, saveData.Muted ? "Sound" : "Mute", new Vector2(82f, 82f), new Vector2(140f, 44f), theme.Yellow, new Color32(26, 23, 64, 255), ToggleMute);

            AddText(panel, "Select Level", 18, FontStyle.Bold, theme.Text, new Vector2(0f, 26f), new Vector2(220f, 30f));
            RenderLevelSelector(panel);

            AddText(panel, "Leaderboard", 18, FontStyle.Bold, theme.Text, new Vector2(0f, -162f), new Vector2(220f, 30f));
            RenderLeaderboard(panel);

            AddButton(panel, "Reset Progress", new Vector2(0f, -364f), new Vector2(300f, 36f), theme.Red, Color.white, ResetProgress);
            AddText(panel, "Click a front shooter column or a waiting shooter to fire. Combo rises as blocks break quickly.", 14, FontStyle.Normal, theme.SubtleText, new Vector2(0f, -398f), new Vector2(510f, 24f));
        }

        private void StartLevel()
        {
            int level = saveData.Level;
            BoardShape shape = BoardShapeCatalog.GetShape(level);
            IFlowRandom random = new SystemFlowRandom();
            BoardCell[,] grid = BoardGenerator.Generate(shape, level, random);
            List<Shooter>[] columns = ShooterGenerator.BuildColumns(grid, shape, level, random);

            layout = BoardLayout.Compute(shape.Rows, shape.Cols, 860f);
            state = GameState.Create(shape, grid, columns, level);
            rules = new GameRules(state, layout);
            seenEventCount = 0;
            seenResult = GameResult.None;
            resultHandled = false;
            PlayTone(523f, 0.08f, 0.18f);
            RefreshGameView();
        }

        private void RefreshGameView()
        {
            ClearDynamicObjects();

            Image background = root.GetComponent<Image>();
            background.color = theme.Background;

            SquareFlowGameplayScreenLayout screen = SquareFlowGameplayScreenLayout.Create(layout);

            RectTransform hud = AddPanel(root, "Hud", screen.HudSize, theme.Panel);
            SetAnchored(hud, screen.HudPosition);
            ApplyOutline(hud, ColorWithAlpha(theme.Border, 0.55f), 1f);
            AddText(hud, "LEVEL " + state.Level.ToString("00", CultureInfo.InvariantCulture), 17, FontStyle.Bold, theme.SubtleText, new Vector2(-456f, 30f), new Vector2(180f, 30f), TextAnchor.MiddleLeft);
            hudText = AddText(hud, string.Empty, 40, FontStyle.Bold, theme.Score, new Vector2(-456f, -18f), new Vector2(220f, 56f), TextAnchor.MiddleLeft);
            comboText = AddText(hud, string.Empty, 15, FontStyle.Bold, theme.SubtleText, new Vector2(-246f, -18f), new Vector2(360f, 34f), TextAnchor.MiddleLeft);
            AddButton(hud, "II", new Vector2(300f, 0f), screen.UtilityButtonSize, theme.Button, theme.Text, ShowMenu);
            AddButton(hud, "R", new Vector2(382f, 0f), screen.UtilityButtonSize, theme.Button, theme.Text, StartLevel);
            AddButton(hud, "S", new Vector2(464f, 0f), screen.UtilityButtonSize, theme.Button, theme.Text, ToggleMuteInGame);

            RectTransform board = AddPanel(root, "Board", new Vector2(layout.CanvasWidth, layout.CanvasHeight), new Color(0f, 0f, 0f, 0f));
            SetAnchored(board, screen.BoardPosition);
            RenderOrbitRing(board);
            RenderBoard(board);
            RenderOrbiters(board);

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

        private void RenderBoard(RectTransform board)
        {
            for (int r = 0; r < state.Shape.Rows; r++)
            for (int c = 0; c < state.Shape.Cols; c++)
            {
                if (!state.Shape.IsActive(r, c)) continue;

                BoardCell cell = state.Grid[r, c];
                Color cellColor = cell.IsOccupied ? ColorForCell(cell) : theme.CellEmpty;
                if (cell.IsOccupied)
                {
                    float depthOffset = layout.Cell * SquareFlowVisualMetrics.CellDepthOffsetScale;
                    RectTransform shadow = AddPanel(board, "CellDepth", new Vector2(layout.Cell, layout.Cell), ColorWithAlpha(Color.black, 0.32f));
                    shadow.GetComponent<Image>().raycastTarget = false;
                    SetAnchored(shadow, BoardPoint(c, r) + new Vector2(depthOffset, -depthOffset));
                }

                RectTransform tile = AddPanel(board, "Cell", new Vector2(layout.Cell, layout.Cell), cellColor);
                SetAnchored(tile, BoardPoint(c, r));
                ApplyOutline(tile, cell.IsOccupied ? ColorWithAlpha(Color.white, 0.28f) : ColorWithAlpha(theme.SubtleText, 0.24f), 1f);

                if (cell.IsOccupied)
                    AddTileDepth(tile);

                if (cell.Type == BoardCellType.Bomb)
                    AddWildBand(tile);

                if (cell.Type == BoardCellType.Normal && cell.Hp > 1)
                    AddText(tile, cell.Hp.ToString(), 16, FontStyle.Bold, Color.white, Vector2.zero, new Vector2(layout.Cell, layout.Cell));
                else if (cell.Type == BoardCellType.Bomb)
                    AddText(tile, "*", 18, FontStyle.Bold, theme.Score, Vector2.zero, new Vector2(layout.Cell, layout.Cell));
            }
        }

        private void RenderOrbiters(RectTransform board)
        {
            for (int i = 0; i < state.ActiveOrbiters.Count; i++)
            {
                ActiveOrbiter orbiter = state.ActiveOrbiters[i];
                Vector2 position = layout.PathPosition(orbiter.Distance);
                Color tokenColor = ColorForShooter(orbiter.Color, orbiter.Wild);
                Vector2 anchored = new Vector2(position.x - layout.CanvasWidth * 0.5f, layout.CanvasHeight * 0.5f - position.y);

                RectTransform holder = AddContainer(board, "OrbiterRoot", Vector2.one * (layout.Cell * 1.25f));
                SetAnchored(holder, anchored);

                RectTransform glow = AddPanel(holder, "OrbiterGlow", Vector2.one * (layout.Cell * 1.15f), ColorWithAlpha(tokenColor, 0.64f), glowSprite);
                glow.GetComponent<Image>().raycastTarget = false;
                SetAnchored(glow, Vector2.zero);

                float tokenSize = layout.Cell * 0.68f;
                RectTransform dot = AddPanel(holder, "Orbiter", Vector2.one * tokenSize, tokenColor, circleSprite);
                dot.GetComponent<Image>().raycastTarget = false;
                SetAnchored(dot, Vector2.zero);
                ApplyOutline(dot, ColorWithAlpha(Color.white, 0.5f), 2f);
                orbiterRects[orbiter.Id] = holder;
            }
        }

        private void UpdateOrbiterVisuals()
        {
            if (layout == null || state == null || orbiterRects.Count == 0) return;

            for (int i = 0; i < state.ActiveOrbiters.Count; i++)
            {
                ActiveOrbiter orbiter = state.ActiveOrbiters[i];
                if (!orbiterRects.TryGetValue(orbiter.Id, out RectTransform rect) || rect == null) continue;

                Vector2 position = layout.PathPosition(orbiter.Distance);
                rect.anchoredPosition = new Vector2(position.x - layout.CanvasWidth * 0.5f, layout.CanvasHeight * 0.5f - position.y);
            }
        }

        private void RenderWaiting(RectTransform queue)
        {
            float startY = Mathf.Min(150f, state.WaitingQueue.Count * 46f);
            for (int i = 0; i < state.WaitingQueue.Count; i++)
            {
                int index = i;
                Shooter shooter = state.WaitingQueue[i];
                AddShooterButton(queue, shooter, new Vector2(0f, startY - i * 88f), Vector2.one * 62f, () => FireWaiting(index));
            }
        }

        private void RenderColumns(RectTransform columns)
        {
            float spacing = 104f;
            float startX = -spacing * (state.ShooterColumns.Length - 1) * 0.5f;
            for (int i = 0; i < state.ShooterColumns.Length; i++)
            {
                int column = i;
                float x = startX + i * spacing;
                RectTransform slot = AddPanel(columns, "DockSlot", new Vector2(80f, 80f), theme.DockSlot);
                SetAnchored(slot, new Vector2(x, 0f));
                ApplyOutline(slot, ColorWithAlpha(theme.Border, 0.5f), 1f);

                if (state.ShooterColumns[i].Count == 0)
                {
                    AddText(slot, "-", 20, FontStyle.Bold, theme.SubtleText, Vector2.zero, new Vector2(80f, 80f));
                    continue;
                }

                Shooter front = state.ShooterColumns[i][0];
                AddShooterButton(slot, front, Vector2.zero, Vector2.one * 58f, () => FireColumn(column));
            }
        }

        private void FireColumn(int column)
        {
            if (rules == null) return;
            bool fired = rules.FireFromColumn(column);
            PlayTone(fired ? 660f : 140f, fired ? 0.06f : 0.1f, 0.16f);
            RefreshGameView();
        }

        private void FireWaiting(int index)
        {
            if (rules == null) return;
            bool fired = rules.FireFromWaiting(index);
            PlayTone(fired ? 740f : 140f, fired ? 0.06f : 0.1f, 0.16f);
            RefreshGameView();
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
                float x = -208f + col * 104f;
                float y = -22f - row * 54f;
                bool selected = level == saveData.Level;
                Color fill = selected ? theme.Green : completed.Contains(level) ? theme.Score : theme.Blue;
                Color text = selected || completed.Contains(level) ? new Color32(26, 23, 64, 255) : Color.white;
                AddButton(panel, level.ToString(), new Vector2(x, y), new Vector2(74f, 42f), fill, text, () => SelectLevel(level));
            }
        }

        private void RenderLeaderboard(RectTransform panel)
        {
            SaveDataService.ScoreEntry[] scores = saveData.Scores();
            if (scores.Length == 0)
            {
                AddText(panel, "No scores yet", 15, FontStyle.Normal, theme.SubtleText, new Vector2(0f, -204f), new Vector2(500f, 26f));
                return;
            }

            int count = Mathf.Min(scores.Length, 10);
            for (int i = 0; i < count; i++)
            {
                SaveDataService.ScoreEntry entry = scores[i];
                string label = (i + 1) + ". L" + entry.Level + "  " + entry.Score + " pts  " + entry.Moves + " moves";
                AddText(panel, label, 13, FontStyle.Normal, theme.SubtleText, new Vector2(0f, -184f - i * 17f), new Vector2(500f, 18f));
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
                switch (events[i].Type)
                {
                    case GameEventType.BlockDestroyed:
                        PlayTone(784f, 0.05f, 0.12f);
                        break;
                    case GameEventType.BombDetonated:
                        PlayTone(110f, 0.14f, 0.18f);
                        break;
                    case GameEventType.Blocked:
                        PlayTone(120f, 0.1f, 0.16f);
                        break;
                }
            }
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

        private void RenderOrbitRing(RectTransform board)
        {
            float segmentLength = Mathf.Max(12f, layout.Cell * 0.32f);
            float thickness = Mathf.Max(3.5f, layout.Cell * 0.06f);
            float spacing = segmentLength * 0.52f;
            int count = Mathf.Max(112, Mathf.CeilToInt(layout.Perimeter / spacing));
            Color glow = ColorWithAlpha(theme.Score, 0.12f);
            Color ring = ColorWithAlpha(theme.Score, 0.62f);

            for (int i = 0; i < count; i++)
            {
                float distance = layout.Perimeter * i / count;
                Vector2 anchored = BoardAnchored(layout.PathPosition(distance));
                Vector2 before = BoardAnchored(layout.PathPosition(distance - spacing * 0.45f));
                Vector2 after = BoardAnchored(layout.PathPosition(distance + spacing * 0.45f));
                float angle = Mathf.Atan2(after.y - before.y, after.x - before.x) * Mathf.Rad2Deg;

                RectTransform halo = AddPanel(board, "OrbitLineGlow", new Vector2(segmentLength * 1.6f, thickness * 4.4f), glow);
                halo.GetComponent<Image>().raycastTarget = false;
                halo.localRotation = Quaternion.Euler(0f, 0f, angle);
                SetAnchored(halo, anchored);

                RectTransform segment = AddPanel(board, "OrbitLine", new Vector2(segmentLength, thickness), ring);
                segment.GetComponent<Image>().raycastTarget = false;
                segment.localRotation = Quaternion.Euler(0f, 0f, angle);
                SetAnchored(segment, anchored);
            }
        }

        private void BuildCanvas()
        {
            GameObject canvasObject = new GameObject("SquareFlowCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(Image));
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            root = canvasObject.GetComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
            canvasObject.GetComponent<Image>().color = theme.Background;
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

        private Text AddText(RectTransform parent, string value, int size, FontStyle style, Color color, Vector2 position, Vector2 dimensions, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            GameObject go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            dynamicObjects.Add(go);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = dimensions;
            SetAnchored(rect, position);

            Text text = go.GetComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            Shadow shadow = go.AddComponent<Shadow>();
            shadow.effectColor = ColorWithAlpha(Color.black, 0.45f);
            shadow.effectDistance = new Vector2(1.2f, -1.2f);
            return text;
        }

        private Button AddButton(RectTransform parent, string label, Vector2 position, Vector2 size, Color color, Color textColor, UnityEngine.Events.UnityAction action)
        {
            RectTransform rect = AddPanel(parent, "Button", size, color);
            SetAnchored(rect, position);

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            button.onClick.AddListener(action);
            button.colors = ButtonColors(color);
            ApplyOutline(rect, ColorWithAlpha(Color.white, 0.26f), 1f);

            Text text = AddText(rect, label, 18, FontStyle.Bold, textColor, Vector2.zero, size);
            text.raycastTarget = false;
            return button;
        }

        private void AddShooterButton(RectTransform parent, Shooter shooter, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
        {
            Color fill = shooter.Hidden ? theme.SubtleText : ColorForShooter(shooter.Color, shooter.Wild);
            Color textColor = shooter.Hidden || shooter.Wild || shooter.Color == FlowColor.Yellow ? new Color32(26, 23, 64, 255) : Color.white;
            string label = shooter.Hidden ? "?" : shooter.Wild ? "*" : string.Empty;
            Button button = AddButton(parent, label, position, size, fill, textColor, action);
            Image image = button.targetGraphic as Image;
            if (image != null)
            {
                image.sprite = circleSprite;
                image.type = Image.Type.Simple;
            }
            float diameter = Mathf.Min(size.x, size.y);
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(diameter, diameter);
            Text text = button.GetComponentInChildren<Text>();
            if (text != null)
                text.rectTransform.sizeDelta = new Vector2(diameter, diameter);
        }

        private void AddWildBand(RectTransform tile)
        {
            RectTransform band = AddContainer(tile, "WildBand", new Vector2(layout.Cell * 1.45f, layout.Cell * 0.42f));
            band.GetComponent<RectTransform>().localRotation = Quaternion.Euler(0f, 0f, -45f);
            SetAnchored(band, Vector2.zero);

            Color[] colors = { theme.Red, theme.Yellow, theme.Green, theme.Blue };
            float stripWidth = band.sizeDelta.x / colors.Length;
            for (int i = 0; i < colors.Length; i++)
            {
                RectTransform strip = AddPanel(band, "WildBandStrip", new Vector2(stripWidth + 1f, band.sizeDelta.y), colors[i]);
                strip.GetComponent<Image>().raycastTarget = false;
                SetAnchored(strip, new Vector2(-band.sizeDelta.x * 0.5f + stripWidth * (i + 0.5f), 0f));
            }
        }

        private void AddTileDepth(RectTransform tile)
        {
            float highlightWidth = layout.Cell * 0.72f;
            float highlightHeight = Mathf.Max(5f, layout.Cell * 0.16f);
            RectTransform topLight = AddPanel(tile, "CellTopLight", new Vector2(highlightWidth, highlightHeight), ColorWithAlpha(Color.white, 0.22f));
            topLight.GetComponent<Image>().raycastTarget = false;
            SetAnchored(topLight, new Vector2(0f, layout.Cell * 0.28f));

            RectTransform bottomShade = AddPanel(tile, "CellBottomShade", new Vector2(layout.Cell * 0.8f, highlightHeight), ColorWithAlpha(Color.black, 0.18f));
            bottomShade.GetComponent<Image>().raycastTarget = false;
            SetAnchored(bottomShade, new Vector2(0f, -layout.Cell * 0.28f));
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
            roundedRectSprite = CreateRoundedRectSprite(40, 12);
            circleSprite = CreateCircleSprite(64, 0.5f, 0.5f);
            glowSprite = CreateCircleSprite(96, 0.5f, 0f);
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

        private Vector2 BoardPoint(int col, int row)
        {
            return new Vector2(layout.CellCenterX(col) - layout.CanvasWidth * 0.5f, layout.CanvasHeight * 0.5f - layout.CellCenterY(row));
        }

        private Vector2 BoardAnchored(Vector2 point)
        {
            return new Vector2(point.x - layout.CanvasWidth * 0.5f, layout.CanvasHeight * 0.5f - point.y);
        }

        private static void SetAnchored(RectTransform rect, Vector2 position)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
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
            orbiterRects.Clear();
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

        private static Font LoadFont()
        {
            Font loaded = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (loaded != null) return loaded;
            loaded = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return loaded;
        }
    }

    public static class SquareFlowVisualMetrics
    {
        public const float OrbitLineSegmentLengthScale = 0.58f;
        public const float OrbitLineSegmentThicknessScale = 0.16f;
        public const float OrbitLineSegmentSpacingMultiplier = 0.68f;
        public const float ActiveOrbiterHolderScale = 1.62f;
        public const float ActiveOrbiterGlowScale = 1.52f;
        public const float ActiveOrbiterTokenScale = 0.98f;
        public const float ShooterButtonMinimumDiameter = 76f;
        public const float CellDepthOffsetScale = 0.09f;
    }

    public readonly struct SquareFlowGameplayScreenLayout
    {
        private SquareFlowGameplayScreenLayout(
            Vector2 hudPosition,
            Vector2 hudSize,
            Vector2 boardPosition,
            Vector2 queuePosition,
            Vector2 queueSize,
            Vector2 dockPosition,
            Vector2 dockSize,
            Vector2 utilityButtonSize)
        {
            HudPosition = hudPosition;
            HudSize = hudSize;
            BoardPosition = boardPosition;
            QueuePosition = queuePosition;
            QueueSize = queueSize;
            DockPosition = dockPosition;
            DockSize = dockSize;
            UtilityButtonSize = utilityButtonSize;
        }

        public Vector2 HudPosition { get; }
        public Vector2 HudSize { get; }
        public Vector2 BoardPosition { get; }
        public Vector2 QueuePosition { get; }
        public Vector2 QueueSize { get; }
        public Vector2 DockPosition { get; }
        public Vector2 DockSize { get; }
        public Vector2 UtilityButtonSize { get; }

        public static SquareFlowGameplayScreenLayout Create(BoardLayout board)
        {
            Vector2 boardPosition = new Vector2(-96f, 76f);
            Vector2 hudSize = new Vector2(1036f, 122f);
            Vector2 queueSize = new Vector2(154f, 500f);
            Vector2 dockSize = new Vector2(1036f, 128f);
            return new SquareFlowGameplayScreenLayout(
                new Vector2(0f, 520f),
                hudSize,
                boardPosition,
                new Vector2(boardPosition.x + board.CanvasWidth * 0.5f + 178f, boardPosition.y),
                queueSize,
                new Vector2(0f, boardPosition.y - board.CanvasHeight * 0.5f - 112f),
                dockSize,
                new Vector2(66f, 66f));
        }
    }
}
