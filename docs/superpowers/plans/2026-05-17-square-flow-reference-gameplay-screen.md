# Square Flow Reference Gameplay Screen Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Restyle only the active Square Flow gameplay screen to match the provided dark arcade reference.

**Architecture:** Keep the runtime-generated uGUI approach. Add a small testable layout helper for gameplay screen panel geometry, then update `SquareFlowGameController.RefreshGameView` and its rendering helpers to use the reference-style top HUD, centered board, right queue, and bottom dock.

**Tech Stack:** Unity C#, uGUI, NUnit edit mode tests, existing `SquareFlow.Core` and `SquareFlow.UI` assemblies.

---

## File Structure

- Modify: `Assets/SquareFlow/Tests/EditMode/BoardLayoutTests.cs`
  - Adds a focused test that describes the gameplay-only reference layout contract.
- Modify: `Assets/SquareFlow/Scripts/UI/SquareFlowGameController.cs`
  - Adds the layout helper and applies the new gameplay screen rendering.
  - Keeps menu, save, audio, and rules code unchanged.
- Modify: `Assets/SquareFlow/Scripts/UI/SquareFlowTheme.cs`
  - Adjusts dark gameplay palette values to match the provided reference while keeping existing light mode functional.

## Task 1: Add Reference Gameplay Layout Contract

**Files:**
- Modify: `Assets/SquareFlow/Tests/EditMode/BoardLayoutTests.cs`
- Modify: `Assets/SquareFlow/Scripts/UI/SquareFlowGameController.cs`

- [ ] **Step 1: Write the failing test**

Add this test to `BoardLayoutTests` after `PrismArcadeMetricsKeepOrbitLineShootersAndTileDepthProminent`:

```csharp
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
```

- [ ] **Step 2: Run the test to verify it fails**

Run:

```powershell
dotnet test SquareFlow.EditModeTests.csproj --filter ReferenceGameplayLayoutPlacesHudBoardQueueAndDockLikeMockup
```

Expected: fail because `SquareFlowGameplayScreenLayout` does not exist.

- [ ] **Step 3: Add the minimal layout helper**

Add this public readonly struct near `SquareFlowVisualMetrics` in `SquareFlowGameController.cs`:

```csharp
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
            new Vector2(0f, 764f),
            hudSize,
            boardPosition,
            new Vector2(boardPosition.x + board.CanvasWidth * 0.5f + 178f, boardPosition.y),
            queueSize,
            new Vector2(0f, boardPosition.y - board.CanvasHeight * 0.5f - 112f),
            dockSize,
            new Vector2(66f, 66f));
    }
}
```

- [ ] **Step 4: Run the focused test to verify it passes**

Run:

```powershell
dotnet test SquareFlow.EditModeTests.csproj --filter ReferenceGameplayLayoutPlacesHudBoardQueueAndDockLikeMockup
```

Expected: pass.

## Task 2: Apply the Reference Gameplay Screen Layout

**Files:**
- Modify: `Assets/SquareFlow/Scripts/UI/SquareFlowGameController.cs`

- [ ] **Step 1: Update `RefreshGameView`**

Use `SquareFlowGameplayScreenLayout.Create(layout)` after `state` and `layout` are initialized. Replace the current top HUD, centered queue, and stacked shooter columns with:

```csharp
SquareFlowGameplayScreenLayout screen = SquareFlowGameplayScreenLayout.Create(layout);

RectTransform hud = AddPanel(root, "Hud", screen.HudSize, theme.Panel);
SetAnchored(hud, screen.HudPosition);
ApplyOutline(hud, ColorWithAlpha(theme.SubtleText, 0.26f), 1f);
AddText(hud, "LEVEL " + state.Level.ToString("00"), 17, FontStyle.Bold, theme.SubtleText, new Vector2(-456f, 30f), new Vector2(180f, 30f), TextAnchor.MiddleLeft);
hudText = AddText(hud, string.Empty, 40, FontStyle.Bold, theme.Score, new Vector2(-456f, -18f), new Vector2(220f, 56f), TextAnchor.MiddleLeft);
comboText = AddText(hud, string.Empty, 15, FontStyle.Bold, theme.SubtleText, new Vector2(-250f, -18f), new Vector2(360f, 34f), TextAnchor.MiddleLeft);
AddButton(hud, "II", new Vector2(300f, 0f), screen.UtilityButtonSize, theme.Button, theme.Text, ShowMenu);
AddButton(hud, "R", new Vector2(382f, 0f), screen.UtilityButtonSize, theme.Button, theme.Text, StartLevel);
AddButton(hud, saveData.Muted ? "S" : "S", new Vector2(464f, 0f), screen.UtilityButtonSize, theme.Button, theme.Text, ToggleMuteInGame);
```

Then anchor board, queue, and dock using `screen.BoardPosition`, `screen.QueuePosition`, and `screen.DockPosition`.

- [ ] **Step 2: Update `UpdateHudTexts`**

Set the main HUD text to score only:

```csharp
hudText.text = state.Score.ToString("N0");
comboText.text = state.Combo > 1f ? "COMBO x" + state.Combo.ToString("0.0") : "MOVES " + state.Moves;
```

- [ ] **Step 3: Run the focused test**

Run:

```powershell
dotnet test SquareFlow.EditModeTests.csproj --filter ReferenceGameplayLayoutPlacesHudBoardQueueAndDockLikeMockup
```

Expected: pass.

## Task 3: Restyle Board, Queue, Dock, and Tokens

**Files:**
- Modify: `Assets/SquareFlow/Scripts/UI/SquareFlowGameController.cs`
- Modify: `Assets/SquareFlow/Scripts/UI/SquareFlowTheme.cs`

- [ ] **Step 1: Extend theme colors**

Add gameplay-friendly color properties to `SquareFlowTheme`:

```csharp
public Color CellEmpty { get; }
public Color Border { get; }
public Color Button { get; }
public Color DockSlot { get; }
```

Initialize them with dark slate values in the constructor. Keep light mode values readable.

- [ ] **Step 2: Add text alignment overload**

Change `AddText` to accept an optional `TextAnchor alignment = TextAnchor.MiddleCenter` and assign `text.alignment = alignment`.

- [ ] **Step 3: Render waiting queue vertically**

Update `RenderWaiting` so waiting shooters are spaced down the right queue panel:

```csharp
float startY = Mathf.Min(150f, state.WaitingQueue.Count * 46f);
for (int i = 0; i < state.WaitingQueue.Count; i++)
{
    int index = i;
    Shooter shooter = state.WaitingQueue[i];
    AddShooterButton(queue, shooter, new Vector2(0f, startY - i * 88f), Vector2.one * 62f, () => FireWaiting(index));
}
```

- [ ] **Step 4: Render front shooters as bottom dock slots**

Update `RenderColumns` so it draws four compact rounded slots without column labels:

```csharp
float spacing = 104f;
float startX = -spacing * (state.ShooterColumns.Length - 1) * 0.5f;
for (int i = 0; i < state.ShooterColumns.Length; i++)
{
    int column = i;
    float x = startX + i * spacing;
    RectTransform slot = AddPanel(columns, "DockSlot", new Vector2(80f, 80f), theme.DockSlot);
    SetAnchored(slot, new Vector2(x, 0f));
    ApplyOutline(slot, ColorWithAlpha(theme.SubtleText, 0.24f), 1f);
    if (state.ShooterColumns[i].Count == 0)
    {
        AddText(slot, "-", 20, FontStyle.Bold, theme.SubtleText, Vector2.zero, new Vector2(80f, 80f));
        continue;
    }

    Shooter front = state.ShooterColumns[i][0];
    AddShooterButton(slot, front, Vector2.zero, Vector2.one * 58f, () => FireColumn(column));
}
```

- [ ] **Step 5: Add wild tile band rendering**

In `RenderBoard`, after `AddTileDepth(tile)`, draw a diagonal band for wild cells by adding a narrow panel rotated `-45` degrees and clipped visually by tile layering.

- [ ] **Step 6: Run tests**

Run:

```powershell
dotnet test SquareFlow.EditModeTests.csproj
```

Expected: all tests pass.

## Task 4: Verify Compile and Manual UI Risk

**Files:**
- No new files.

- [ ] **Step 1: Run C# build**

Run:

```powershell
dotnet build SquareFlow.Runtime.csproj
```

Expected: exit code 0.

- [ ] **Step 2: Run edit mode tests if Unity is available**

Run:

```powershell
dotnet test SquareFlow.EditModeTests.csproj
```

Expected: exit code 0.

- [ ] **Step 3: Inspect diff**

Run:

```powershell
git diff -- Assets/SquareFlow/Scripts/UI/SquareFlowGameController.cs Assets/SquareFlow/Scripts/UI/SquareFlowTheme.cs Assets/SquareFlow/Tests/EditMode/BoardLayoutTests.cs
```

Expected: only gameplay screen layout/styling, theme colors, and focused layout tests changed.
