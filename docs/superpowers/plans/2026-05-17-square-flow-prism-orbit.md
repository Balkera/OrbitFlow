# Square Flow Prism Orbit Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make Square Flow look like the approved Prism Arcade mockup and move active shooters around a rounded orbit ring instead of the current square perimeter.

**Architecture:** Keep gameplay rules intact. Keep `BoardLayout` fire-point distances in their original top/right/bottom/left schedule, project distance-to-position onto an ellipse for rendering, then update runtime uGUI rendering to draw a rounded ring and polished arcade surfaces.

**Tech Stack:** Unity 6000.3 C#, uGUI, NUnit edit-mode tests, generated runtime sprites.

---

### Task 1: Rounded Orbit Layout

**Files:**
- Modify: `Assets/SquareFlow/Tests/EditMode/BoardLayoutTests.cs`
- Modify: `Assets/SquareFlow/Scripts/Core/BoardLayout.cs`

- [ ] **Step 1: Write failing tests**

Add tests proving the orbit path is rounded and fire-point order is preserved: `PathPosition` points must lie on the ellipse and must no longer return square corners, while every catalog shape still emits top, right, bottom, and left fire points in gameplay order.

- [ ] **Step 2: Run the layout test**

Run: `dotnet build SquareFlow.EditModeTests.csproj --no-restore`

Expected: build/test compile succeeds, but Unity runner or NUnit execution should show the new assertions fail against the old square path if run in Unity.

- [ ] **Step 3: Implement rounded layout**

Add orbit center/radius properties, keep the existing rectangular fire-point distance schedule, and change `PathPosition` to project each distance onto the ellipse.

- [ ] **Step 4: Verify**

Run: `dotnet build SquareFlow.EditModeTests.csproj --no-restore`

Expected: exit code 0.

### Task 2: Prism Arcade UI Polish

**Files:**
- Modify: `Assets/SquareFlow/Scripts/UI/SquareFlowTheme.cs`
- Modify: `Assets/SquareFlow/Scripts/UI/SquareFlowGameController.cs`

- [ ] **Step 1: Update theme palette**

Use darker prism background/panel colors, brighter block colors, and gold orbit/score accents.

- [ ] **Step 2: Add generated UI sprites**

Create runtime circle and rounded-rectangle sprites once, then apply them to panels, buttons, cells, and shooter tokens.

- [ ] **Step 3: Redraw orbit ring**

Replace the four straight orbit bars with small translucent ring segments/dots placed with `BoardLayout.PathPosition` around the full perimeter.

- [ ] **Step 4: Polish shooter visuals**

Render active orbiters as circular tokens with a larger translucent glow behind them. Preserve click behavior for waiting and column shooters.

- [ ] **Step 5: Verify**

Run:

```powershell
dotnet build SquareFlow.Runtime.csproj --no-restore
dotnet build SquareFlow.EditModeTests.csproj --no-restore
```

Expected: both commands exit 0.

### Task 3: Unity Smoke Check

**Files:**
- No source changes expected.

- [ ] **Step 1: Clear Unity console**

Use Unity MCP console clear.

- [ ] **Step 2: Enter Play Mode**

Use Unity MCP editor play mode and inspect console output.

- [ ] **Step 3: Confirm scene**

Confirm the scene still contains `SquareFlowRoot` and no new console errors are emitted.
