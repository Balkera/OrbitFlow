# Square Flow Mobile World Renderer Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a phone-shaped, touch-first Unity gameplay presentation where moving gameplay renders with world-space 2D objects and Canvas is limited to HUD, menus, text, and touch UI.

**Architecture:** Keep `SquareFlow.Core` as the source of truth. Add a tested world-layout adapter, then add world-space SpriteRenderer views for board cells, orbit ring, active orbiters, and effects. Refactor `SquareFlowGameController` so Canvas rebuilds only UI surfaces while gameplay visuals update in place.

**Tech Stack:** Unity 6000.3.15f1, C#, SpriteRenderer, Unity UI/uGUI, Unity Test Framework/NUnit, URP project settings already present.

---

## Scope Check

This plan implements one coherent subsystem: the mobile-first world-space gameplay renderer. It does not package Android builds, add store services, or change Square Flow rules. Android packaging remains a future phase after the renderer is stable.

## File Structure

- Create: `Assets/SquareFlow/Scripts/Runtime/MobileWorldLayout.cs`
  - Converts existing `BoardLayout` logical pixel coordinates into world-space coordinates.
  - Provides fire-point and cell target positions for views and effects.
- Create: `Assets/SquareFlow/Tests/EditMode/MobileWorldLayoutTests.cs`
  - Locks down coordinate conversion, y-axis orientation, and fire-point lookup.
- Create: `Assets/SquareFlow/Scripts/Runtime/SquareFlowWorldSprites.cs`
  - Owns generated white sprites shared by SpriteRenderer-based world views.
- Create: `Assets/SquareFlow/Scripts/Runtime/MobileCameraController.cs`
  - Configures the gameplay camera for portrait-first 2D rendering.
- Create: `Assets/SquareFlow/Scripts/Runtime/BoardWorldView.cs`
  - Creates and updates world-space board cell sprites.
- Create: `Assets/SquareFlow/Scripts/Runtime/OrbitRingWorldView.cs`
  - Creates and updates reusable world-space orbit ring segments.
- Create: `Assets/SquareFlow/Scripts/Runtime/OrbiterWorldView.cs`
  - Pools and updates active orbiter sprites.
- Create: `Assets/SquareFlow/Scripts/Runtime/WorldEffectsController.cs`
  - Pools shot, glow, and impact sprites for hit feedback.
- Create: `Assets/SquareFlow/Tests/EditMode/WorldViewReuseTests.cs`
  - Verifies board and orbiter views reuse existing GameObjects across refreshes.
- Modify: `Assets/SquareFlow/Scripts/UI/SquareFlowGameController.cs`
  - Builds world renderer components.
  - Keeps Canvas for menu, HUD, queue, shooter dock, result panel, and touch controls.
  - Stops rendering board, orbiters, and shot effects as Canvas elements.
- Modify: `Assets/SquareFlow/Tests/EditMode/BoardLayoutTests.cs`
  - Updates layout assertions that are no longer tied to Canvas gameplay board placement.

## Task 1: Add Tested Mobile World Layout

**Files:**
- Create: `Assets/SquareFlow/Scripts/Runtime/MobileWorldLayout.cs`
- Create: `Assets/SquareFlow/Tests/EditMode/MobileWorldLayoutTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `Assets/SquareFlow/Tests/EditMode/MobileWorldLayoutTests.cs`:

```csharp
using NUnit.Framework;
using SquareFlow.Core;
using SquareFlow.Runtime;
using UnityEngine;

namespace SquareFlow.Tests
{
    public sealed class MobileWorldLayoutTests
    {
        [Test]
        public void SingleCellCenterMapsToConfiguredBoardCenter()
        {
            BoardLayout board = BoardLayout.Compute(1, 1, 320f);
            MobileWorldLayout world = new MobileWorldLayout(board, new Vector2(1.25f, -0.5f), 0.01f);

            Vector2 center = world.CellCenter(0, 0);

            Assert.That(center.x, Is.EqualTo(1.25f).Within(0.001f));
            Assert.That(center.y, Is.EqualTo(-0.5f).Within(0.001f));
            Assert.That(world.CellSize, Is.EqualTo(board.Cell * 0.01f).Within(0.001f));
        }

        [Test]
        public void RowZeroIsAboveLaterRowsInWorldSpace()
        {
            BoardLayout board = BoardLayout.Compute(2, 1, 320f);
            MobileWorldLayout world = new MobileWorldLayout(board, Vector2.zero, 0.01f);

            Vector2 top = world.CellCenter(0, 0);
            Vector2 bottom = world.CellCenter(0, 1);

            Assert.That(top.y, Is.GreaterThan(bottom.y));
        }

        [Test]
        public void FirePointLookupUsesEventFireLane()
        {
            BoardLayout board = BoardLayout.Compute(3, 3, 420f);
            MobileWorldLayout world = new MobileWorldLayout(board, Vector2.zero, 0.01f);
            GameEvent hit = new GameEvent(
                GameEventType.BlockDestroyed,
                row: 1,
                col: 2,
                orbiterId: "o1",
                score: 100,
                fireSide: FireSide.Right,
                fireRow: 1,
                fireCol: -1);

            bool found = world.TryFirePoint(hit, out Vector2 firePoint);

            Assert.That(found, Is.True);
            Assert.That(firePoint.x, Is.GreaterThan(world.CellCenter(2, 1).x));
        }

        [Test]
        public void EventTargetUsesHitCellCenter()
        {
            BoardLayout board = BoardLayout.Compute(3, 3, 420f);
            MobileWorldLayout world = new MobileWorldLayout(board, new Vector2(0.5f, 0.25f), 0.01f);
            GameEvent hit = new GameEvent(GameEventType.BlockDamaged, row: 2, col: 1);

            Assert.That(world.EventTarget(hit), Is.EqualTo(world.CellCenter(1, 2)));
        }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run:

```powershell
dotnet test SquareFlow.EditModeTests.csproj --filter MobileWorldLayoutTests
```

Expected: fail with compile errors because `MobileWorldLayout` does not exist.

- [ ] **Step 3: Add the world layout implementation**

Create `Assets/SquareFlow/Scripts/Runtime/MobileWorldLayout.cs`:

```csharp
using SquareFlow.Core;
using UnityEngine;

namespace SquareFlow.Runtime
{
    public readonly struct MobileWorldLayout
    {
        public const float DefaultWorldUnitsPerLayoutPixel = 0.01f;
        public static readonly Vector2 DefaultBoardCenter = new Vector2(0f, 0.85f);

        private readonly BoardLayout board;

        public MobileWorldLayout(BoardLayout board, Vector2 boardCenter, float worldUnitsPerLayoutPixel)
        {
            this.board = board;
            BoardCenter = boardCenter;
            WorldUnitsPerLayoutPixel = Mathf.Max(0.001f, worldUnitsPerLayoutPixel);
        }

        public bool IsValid => board != null;
        public Vector2 BoardCenter { get; }
        public float WorldUnitsPerLayoutPixel { get; }
        public float CellSize => IsValid ? board.Cell * WorldUnitsPerLayoutPixel : 0f;
        public float CanvasWidth => IsValid ? board.CanvasWidth * WorldUnitsPerLayoutPixel : 0f;
        public float CanvasHeight => IsValid ? board.CanvasHeight * WorldUnitsPerLayoutPixel : 0f;

        public static MobileWorldLayout Create(BoardLayout board)
        {
            return new MobileWorldLayout(board, DefaultBoardCenter, DefaultWorldUnitsPerLayoutPixel);
        }

        public Vector2 CellCenter(int col, int row)
        {
            if (!IsValid) return BoardCenter;
            return ToWorld(new Vector2(
                board.CellCenterX(col) - board.CanvasWidth * 0.5f,
                board.CanvasHeight * 0.5f - board.CellCenterY(row)));
        }

        public Vector2 PathPosition(float distance)
        {
            if (!IsValid) return BoardCenter;
            Vector2 point = board.PathPosition(distance);
            return ToWorld(new Vector2(
                point.x - board.CanvasWidth * 0.5f,
                board.CanvasHeight * 0.5f - point.y));
        }

        public Vector2 EventTarget(GameEvent gameEvent)
        {
            return CellCenter(gameEvent.Col, gameEvent.Row);
        }

        public bool TryFirePoint(GameEvent gameEvent, out Vector2 worldPosition)
        {
            if (!IsValid || !gameEvent.HasFirePoint)
            {
                worldPosition = BoardCenter;
                return false;
            }

            for (int i = 0; i < board.FirePoints.Count; i++)
            {
                FirePoint point = board.FirePoints[i];
                if (point.Side != gameEvent.FireSide || point.Row != gameEvent.FireRow || point.Col != gameEvent.FireCol)
                    continue;

                worldPosition = PathPosition(point.Distance);
                return true;
            }

            worldPosition = BoardCenter;
            return false;
        }

        private Vector2 ToWorld(Vector2 boardAnchoredPoint)
        {
            return BoardCenter + boardAnchoredPoint * WorldUnitsPerLayoutPixel;
        }
    }
}
```

- [ ] **Step 4: Run focused tests to verify they pass**

Run:

```powershell
dotnet test SquareFlow.EditModeTests.csproj --filter MobileWorldLayoutTests
```

Expected: pass.

- [ ] **Step 5: Commit**

Run:

```powershell
git add -- Assets/SquareFlow/Scripts/Runtime/MobileWorldLayout.cs Assets/SquareFlow/Tests/EditMode/MobileWorldLayoutTests.cs
git commit -m "Add mobile world layout mapping"
```

Expected: commit succeeds with only the layout and test files staged.

## Task 2: Add World Sprite And Camera Utilities

**Files:**
- Create: `Assets/SquareFlow/Scripts/Runtime/SquareFlowWorldSprites.cs`
- Create: `Assets/SquareFlow/Scripts/Runtime/MobileCameraController.cs`

- [ ] **Step 1: Add generated world sprites**

Create `Assets/SquareFlow/Scripts/Runtime/SquareFlowWorldSprites.cs`:

```csharp
using UnityEngine;

namespace SquareFlow.Runtime
{
    public static class SquareFlowWorldSprites
    {
        private static Sprite roundedRect;
        private static Sprite circle;
        private static Sprite glow;
        private static Sprite square;

        public static Sprite RoundedRect
        {
            get
            {
                Ensure();
                return roundedRect;
            }
        }

        public static Sprite Circle
        {
            get
            {
                Ensure();
                return circle;
            }
        }

        public static Sprite Glow
        {
            get
            {
                Ensure();
                return glow;
            }
        }

        public static Sprite Square
        {
            get
            {
                Ensure();
                return square;
            }
        }

        public static void Ensure()
        {
            if (roundedRect != null) return;

            roundedRect = CreateRoundedRectSprite(96, 22);
            circle = CreateCircleSprite(64, 0.5f, 0.5f, "SquareFlowWorldCircle");
            glow = CreateCircleSprite(96, 0.5f, 0f, "SquareFlowWorldGlow");
            square = CreateSolidSprite(8, "SquareFlowWorldSquare");
        }

        private static Sprite CreateSolidSprite(int size, string name)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = name;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                texture.SetPixel(x, y, Color.white);

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static Sprite CreateRoundedRectSprite(int size, int radius)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "SquareFlowWorldRoundedRect";
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
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }

        private static Sprite CreateCircleSprite(int size, float solidRadius, float edgeAlpha, string name)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = name;
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
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
```

- [ ] **Step 2: Add mobile camera controller**

Create `Assets/SquareFlow/Scripts/Runtime/MobileCameraController.cs`:

```csharp
using UnityEngine;

namespace SquareFlow.Runtime
{
    [RequireComponent(typeof(Camera))]
    public sealed class MobileCameraController : MonoBehaviour
    {
        public const float PortraitReferenceHeightWorldUnits = 19.2f;

        private Camera targetCamera;

        public Camera Camera
        {
            get
            {
                if (targetCamera == null)
                    targetCamera = GetComponent<Camera>();
                return targetCamera;
            }
        }

        public void Configure(Color background)
        {
            Camera.orthographic = true;
            Camera.orthographicSize = PortraitReferenceHeightWorldUnits * 0.5f;
            Camera.clearFlags = CameraClearFlags.SolidColor;
            Camera.backgroundColor = background;
            Transform cameraTransform = Camera.transform;
            cameraTransform.position = new Vector3(0f, 0f, -10f);
            cameraTransform.rotation = Quaternion.identity;
        }
    }
}
```

- [ ] **Step 3: Run compile check**

Run:

```powershell
dotnet build SquareFlow.Runtime.csproj
```

Expected: build succeeds.

- [ ] **Step 4: Commit**

Run:

```powershell
git add -- Assets/SquareFlow/Scripts/Runtime/SquareFlowWorldSprites.cs Assets/SquareFlow/Scripts/Runtime/MobileCameraController.cs
git commit -m "Add world sprite and camera utilities"
```

Expected: commit succeeds with only the two runtime utility files staged.

## Task 3: Add Board And Orbit Ring World Views

**Files:**
- Create: `Assets/SquareFlow/Scripts/Runtime/BoardWorldView.cs`
- Create: `Assets/SquareFlow/Scripts/Runtime/OrbitRingWorldView.cs`
- Create: `Assets/SquareFlow/Tests/EditMode/WorldViewReuseTests.cs`

- [ ] **Step 1: Write failing board reuse test**

Create `Assets/SquareFlow/Tests/EditMode/WorldViewReuseTests.cs`:

```csharp
using System.Collections.Generic;
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

        private static List<Shooter>[] EmptyColumns()
        {
            return new[] { new List<Shooter>(), new List<Shooter>(), new List<Shooter>() };
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet test SquareFlow.EditModeTests.csproj --filter BoardWorldViewRefreshesCellsWithoutCreatingMoreObjects
```

Expected: fail with compile errors because `BoardWorldView` does not exist.

- [ ] **Step 3: Add board world view**

Create `Assets/SquareFlow/Scripts/Runtime/BoardWorldView.cs`:

```csharp
using System.Collections.Generic;
using SquareFlow.Core;
using SquareFlow.UI;
using UnityEngine;

namespace SquareFlow.Runtime
{
    public sealed class BoardWorldView : MonoBehaviour
    {
        private readonly List<CellView> cells = new List<CellView>();
        private BoardShape boundShape;
        private BoardLayout boundBoard;
        private MobileWorldLayout boundWorld;

        public void Bind(GameState state, BoardLayout board, MobileWorldLayout world, SquareFlowTheme theme)
        {
            if (state == null || board == null || !world.IsValid)
            {
                Clear();
                return;
            }

            bool needsRebuild = boundShape == null
                || boundShape.Rows != state.Shape.Rows
                || boundShape.Cols != state.Shape.Cols
                || cells.Count == 0;

            if (needsRebuild)
                Rebuild(state);

            boundShape = state.Shape;
            boundBoard = board;
            boundWorld = world;
            RefreshCells(state, theme);
        }

        public void RefreshCells(GameState state, SquareFlowTheme theme)
        {
            if (state == null || boundBoard == null || !boundWorld.IsValid) return;

            for (int i = 0; i < cells.Count; i++)
            {
                CellView cell = cells[i];
                BoardCell boardCell = state.Grid[cell.Row, cell.Col];
                bool visible = state.Shape.IsActive(cell.Row, cell.Col);
                cell.Root.SetActive(visible);
                if (!visible) continue;

                Vector2 center = boundWorld.CellCenter(cell.Col, cell.Row);
                cell.Root.transform.position = new Vector3(center.x, center.y, 0f);
                float tileSize = boundWorld.CellSize * SquareFlowVisualMetrics.TileFaceScale;
                cell.Face.transform.localScale = Vector3.one * tileSize;
                cell.Depth.transform.localScale = Vector3.one * tileSize;
                cell.Depth.transform.localPosition = new Vector3(0f, -boundWorld.CellSize * SquareFlowVisualMetrics.TileDepthDropScale, 0.04f);
                cell.Highlight.transform.localScale = new Vector3(tileSize * 0.72f, Mathf.Max(0.03f, tileSize * 0.08f), 1f);
                cell.Highlight.transform.localPosition = new Vector3(0f, tileSize * 0.3f, -0.04f);
                cell.Label.transform.localPosition = new Vector3(0f, 0f, -0.08f);
                cell.Label.characterSize = Mathf.Max(0.16f, boundWorld.CellSize * 0.32f);
                cell.Face.color = CellColor(boardCell, theme);
                cell.Depth.color = boardCell.IsOccupied ? LerpColor(cell.Face.color, Color.black, SquareFlowVisualMetrics.TileDepthDarkenAmount) : Color.clear;
                cell.Highlight.color = boardCell.IsOccupied ? ColorWithAlpha(Color.white, SquareFlowVisualMetrics.TileTopHighlightAlpha) : Color.clear;
                cell.Label.text = LabelForCell(boardCell);
                cell.Label.color = boardCell.Type == BoardCellType.Bomb || boardCell.Color == FlowColor.Yellow ? new Color32(26, 23, 64, 255) : Color.white;
            }
        }

        public void Clear()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                DestroyImmediateOrRuntime(transform.GetChild(i).gameObject);

            cells.Clear();
            boundShape = null;
            boundBoard = null;
            boundWorld = default;
        }

        private void Rebuild(GameState state)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                DestroyImmediateOrRuntime(transform.GetChild(i).gameObject);

            cells.Clear();
            boundShape = state.Shape;

            for (int r = 0; r < state.Shape.Rows; r++)
            for (int c = 0; c < state.Shape.Cols; c++)
            {
                if (!state.Shape.IsActive(r, c)) continue;
                cells.Add(CreateCell(r, c));
            }
        }

        private CellView CreateCell(int row, int col)
        {
            GameObject root = new GameObject("WorldCell_" + row + "_" + col);
            root.transform.SetParent(transform, false);

            SpriteRenderer depth = CreateRenderer(root.transform, "Depth", SquareFlowWorldSprites.RoundedRect, 2);
            SpriteRenderer face = CreateRenderer(root.transform, "Face", SquareFlowWorldSprites.RoundedRect, 1);
            SpriteRenderer highlight = CreateRenderer(root.transform, "Highlight", SquareFlowWorldSprites.Square, 0);
            TextMesh label = CreateLabel(root.transform);

            return new CellView(row, col, root, depth, face, highlight, label);
        }

        private static SpriteRenderer CreateRenderer(Transform parent, string name, Sprite sprite, int order)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return renderer;
        }

        private static TextMesh CreateLabel(Transform parent)
        {
            GameObject go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            TextMesh label = go.AddComponent<TextMesh>();
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = 64;
            MeshRenderer renderer = label.GetComponent<MeshRenderer>();
            renderer.sortingOrder = 7;
            return label;
        }

        private static Color CellColor(BoardCell cell, SquareFlowTheme theme)
        {
            if (!cell.IsOccupied) return theme.CellEmpty;
            if (cell.Type == BoardCellType.Bomb) return theme.Bomb;

            switch (cell.Color)
            {
                case FlowColor.Blue:
                    return theme.Blue;
                case FlowColor.Yellow:
                    return theme.Yellow;
                case FlowColor.Green:
                    return theme.Green;
                default:
                    return theme.Red;
            }
        }

        private static string LabelForCell(BoardCell cell)
        {
            if (cell.Type == BoardCellType.Bomb) return "*";
            if (cell.Type == BoardCellType.Normal && cell.Hp > 1) return cell.Hp.ToString();
            return string.Empty;
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

        private static void DestroyImmediateOrRuntime(GameObject go)
        {
            if (Application.isPlaying)
                Destroy(go);
            else
                DestroyImmediate(go);
        }

        private readonly struct CellView
        {
            public CellView(int row, int col, GameObject root, SpriteRenderer depth, SpriteRenderer face, SpriteRenderer highlight, TextMesh label)
            {
                Row = row;
                Col = col;
                Root = root;
                Depth = depth;
                Face = face;
                Highlight = highlight;
                Label = label;
            }

            public int Row { get; }
            public int Col { get; }
            public GameObject Root { get; }
            public SpriteRenderer Depth { get; }
            public SpriteRenderer Face { get; }
            public SpriteRenderer Highlight { get; }
            public TextMesh Label { get; }
        }
    }
}
```

- [ ] **Step 4: Add orbit ring world view**

Create `Assets/SquareFlow/Scripts/Runtime/OrbitRingWorldView.cs`:

```csharp
using System.Collections.Generic;
using SquareFlow.Core;
using SquareFlow.UI;
using UnityEngine;

namespace SquareFlow.Runtime
{
    public sealed class OrbitRingWorldView : MonoBehaviour
    {
        private readonly List<SpriteRenderer> segments = new List<SpriteRenderer>();

        public void Bind(BoardLayout board, MobileWorldLayout world, SquareFlowTheme theme)
        {
            if (board == null || !world.IsValid)
            {
                SetActiveCount(0);
                return;
            }

            float segmentLengthLayout = Mathf.Max(12f, board.Cell * SquareFlowVisualMetrics.OrbitLineSegmentLengthScale);
            float spacingLayout = segmentLengthLayout * SquareFlowVisualMetrics.OrbitLineSegmentSpacingMultiplier;
            float segmentLengthWorld = segmentLengthLayout * world.WorldUnitsPerLayoutPixel;
            int count = Mathf.Max(96, Mathf.CeilToInt(board.Perimeter / Mathf.Max(1f, spacingLayout)));
            SetActiveCount(count);

            for (int i = 0; i < count; i++)
            {
                float distance = board.Perimeter * i / count;
                Vector2 position = world.PathPosition(distance);
                Vector2 before = world.PathPosition(distance - spacingLayout * 0.45f);
                Vector2 after = world.PathPosition(distance + spacingLayout * 0.45f);
                float angle = Mathf.Atan2(after.y - before.y, after.x - before.x) * Mathf.Rad2Deg;

                SpriteRenderer renderer = segments[i];
                renderer.color = ColorWithAlpha(theme.Score, 0.62f);
                renderer.transform.position = new Vector3(position.x, position.y, 0.2f);
                renderer.transform.rotation = Quaternion.Euler(0f, 0f, angle);
                renderer.transform.localScale = new Vector3(segmentLengthWorld, Mathf.Max(0.035f, board.Cell * SquareFlowVisualMetrics.OrbitLineSegmentThicknessScale * world.WorldUnitsPerLayoutPixel), 1f);
            }
        }

        public void Clear()
        {
            SetActiveCount(0);
        }

        private void SetActiveCount(int count)
        {
            while (segments.Count < count)
                segments.Add(CreateSegment());

            for (int i = 0; i < segments.Count; i++)
                segments[i].gameObject.SetActive(i < count);
        }

        private SpriteRenderer CreateSegment()
        {
            GameObject go = new GameObject("OrbitSegment");
            go.transform.SetParent(transform, false);
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = SquareFlowWorldSprites.Square;
            renderer.sortingOrder = -1;
            return renderer;
        }

        private static Color ColorWithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
```

- [ ] **Step 5: Run focused board reuse test**

Run:

```powershell
dotnet test SquareFlow.EditModeTests.csproj --filter BoardWorldViewRefreshesCellsWithoutCreatingMoreObjects
```

Expected: pass.

- [ ] **Step 6: Commit**

Run:

```powershell
git add -- Assets/SquareFlow/Scripts/Runtime/BoardWorldView.cs Assets/SquareFlow/Scripts/Runtime/OrbitRingWorldView.cs Assets/SquareFlow/Tests/EditMode/WorldViewReuseTests.cs
git commit -m "Add world board and orbit views"
```

Expected: commit succeeds with board/ring view files and the reuse test staged.

## Task 4: Add Pooled Orbiter And World Effects Views

**Files:**
- Modify: `Assets/SquareFlow/Tests/EditMode/WorldViewReuseTests.cs`
- Create: `Assets/SquareFlow/Scripts/Runtime/OrbiterWorldView.cs`
- Create: `Assets/SquareFlow/Scripts/Runtime/WorldEffectsController.cs`

- [ ] **Step 1: Add failing orbiter reuse test**

Append this test to `WorldViewReuseTests`:

```csharp
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
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet test SquareFlow.EditModeTests.csproj --filter OrbiterWorldViewReusesObjectForSameOrbiterId
```

Expected: fail with compile errors because `OrbiterWorldView` does not exist.

- [ ] **Step 3: Add orbiter world view**

Create `Assets/SquareFlow/Scripts/Runtime/OrbiterWorldView.cs`:

```csharp
using System.Collections.Generic;
using SquareFlow.Core;
using SquareFlow.UI;
using UnityEngine;

namespace SquareFlow.Runtime
{
    public sealed class OrbiterWorldView : MonoBehaviour
    {
        private readonly Dictionary<string, OrbiterView> active = new Dictionary<string, OrbiterView>();
        private readonly List<string> missing = new List<string>();

        public void Refresh(List<ActiveOrbiter> orbiters, MobileWorldLayout world, SquareFlowTheme theme)
        {
            missing.Clear();
            foreach (string id in active.Keys)
                missing.Add(id);

            for (int i = 0; i < orbiters.Count; i++)
            {
                ActiveOrbiter orbiter = orbiters[i];
                missing.Remove(orbiter.Id);

                if (!active.TryGetValue(orbiter.Id, out OrbiterView view))
                {
                    view = CreateOrbiter(orbiter.Id);
                    active.Add(orbiter.Id, view);
                }

                Vector2 position = world.PathPosition(orbiter.Distance);
                view.Root.SetActive(true);
                view.Root.transform.position = new Vector3(position.x, position.y, -0.25f);
                view.Glow.color = ColorWithAlpha(ColorForShooter(orbiter.Color, orbiter.Wild, theme), 0.64f);
                view.Token.color = ColorForShooter(orbiter.Color, orbiter.Wild, theme);
                float tokenSize = world.CellSize * SquareFlowVisualMetrics.ActiveOrbiterTokenScale;
                view.Token.transform.localScale = Vector3.one * tokenSize;
                view.Glow.transform.localScale = Vector3.one * (world.CellSize * SquareFlowVisualMetrics.ActiveOrbiterGlowScale);
            }

            for (int i = 0; i < missing.Count; i++)
                active[missing[i]].Root.SetActive(false);
        }

        public bool TryGetColor(string orbiterId, out Color color)
        {
            if (active.TryGetValue(orbiterId, out OrbiterView view))
            {
                color = view.Token.color;
                return true;
            }

            color = Color.white;
            return false;
        }

        public void Clear()
        {
            foreach (OrbiterView view in active.Values)
                view.Root.SetActive(false);
        }

        private OrbiterView CreateOrbiter(string id)
        {
            GameObject root = new GameObject("WorldOrbiter_" + id);
            root.transform.SetParent(transform, false);

            SpriteRenderer glow = CreateRenderer(root.transform, "Glow", SquareFlowWorldSprites.Glow, 5);
            SpriteRenderer token = CreateRenderer(root.transform, "Token", SquareFlowWorldSprites.Circle, 6);
            return new OrbiterView(root, glow, token);
        }

        private static SpriteRenderer CreateRenderer(Transform parent, string name, Sprite sprite, int order)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return renderer;
        }

        private static Color ColorForShooter(FlowColor color, bool wild, SquareFlowTheme theme)
        {
            if (wild || color == FlowColor.Wild) return theme.Wild;

            switch (color)
            {
                case FlowColor.Blue:
                    return theme.Blue;
                case FlowColor.Yellow:
                    return theme.Yellow;
                case FlowColor.Green:
                    return theme.Green;
                default:
                    return theme.Red;
            }
        }

        private static Color ColorWithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private readonly struct OrbiterView
        {
            public OrbiterView(GameObject root, SpriteRenderer glow, SpriteRenderer token)
            {
                Root = root;
                Glow = glow;
                Token = token;
            }

            public GameObject Root { get; }
            public SpriteRenderer Glow { get; }
            public SpriteRenderer Token { get; }
        }
    }
}
```

- [ ] **Step 4: Add pooled world effects controller**

Create `Assets/SquareFlow/Scripts/Runtime/WorldEffectsController.cs`:

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SquareFlow.Runtime
{
    public sealed class WorldEffectsController : MonoBehaviour
    {
        private readonly Queue<SpriteRenderer> linePool = new Queue<SpriteRenderer>();
        private readonly Queue<SpriteRenderer> circlePool = new Queue<SpriteRenderer>();

        public void PlayShot(Vector2 start, Vector2 end, Color color, bool heavyImpact)
        {
            if (!gameObject.activeInHierarchy) return;
            StartCoroutine(AnimateShot(start, end, color, heavyImpact));
        }

        public void Clear()
        {
            StopAllCoroutines();
            for (int i = 0; i < transform.childCount; i++)
                transform.GetChild(i).gameObject.SetActive(false);
        }

        private IEnumerator AnimateShot(Vector2 start, Vector2 end, Color color, bool heavyImpact)
        {
            float distance = Vector2.Distance(start, end);
            if (distance <= 0.01f) yield break;

            SpriteRenderer streak = Take(linePool, "WorldShotStreak", SquareFlowWorldSprites.Square, 10);
            SpriteRenderer glow = Take(circlePool, "WorldShotGlow", SquareFlowWorldSprites.Glow, 11);
            SpriteRenderer core = Take(circlePool, "WorldShotCore", SquareFlowWorldSprites.Circle, 12);
            float angle = Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg;
            float duration = heavyImpact ? 0.16f : 0.12f;

            streak.transform.position = new Vector3((start.x + end.x) * 0.5f, (start.y + end.y) * 0.5f, -0.45f);
            streak.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            streak.transform.localScale = new Vector3(distance, heavyImpact ? 0.08f : 0.055f, 1f);

            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = EaseOut(t);
                Vector2 position = Vector2.Lerp(start, end, eased);
                streak.color = ColorWithAlpha(color, Mathf.Lerp(0.55f, 0.12f, t));
                glow.color = ColorWithAlpha(color, Mathf.Lerp(0.68f, 0.1f, t));
                core.color = ColorWithAlpha(Color.white, Mathf.Lerp(1f, 0.36f, t));
                glow.transform.position = new Vector3(position.x, position.y, -0.5f);
                core.transform.position = new Vector3(position.x, position.y, -0.55f);
                glow.transform.localScale = Vector3.one * (heavyImpact ? 0.48f : 0.36f);
                core.transform.localScale = Vector3.one * 0.12f;
                yield return null;
            }

            Release(streak);
            Release(glow);
            Release(core);
            yield return AnimateImpact(end, color, heavyImpact);
        }

        private IEnumerator AnimateImpact(Vector2 position, Color color, bool heavyImpact)
        {
            SpriteRenderer pulse = Take(circlePool, "WorldImpactPulse", SquareFlowWorldSprites.Glow, 13);
            float duration = heavyImpact ? 0.28f : 0.2f;

            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = EaseOut(t);
                pulse.transform.position = new Vector3(position.x, position.y, -0.5f);
                pulse.transform.localScale = Vector3.one * Mathf.Lerp(0.32f, heavyImpact ? 1.0f : 0.72f, eased);
                pulse.color = ColorWithAlpha(color, Mathf.Lerp(0.42f, 0f, t));
                yield return null;
            }

            Release(pulse);
        }

        private SpriteRenderer Take(Queue<SpriteRenderer> pool, string name, Sprite sprite, int order)
        {
            SpriteRenderer renderer = pool.Count > 0 ? pool.Dequeue() : CreateRenderer(name, sprite, order);
            renderer.gameObject.SetActive(true);
            return renderer;
        }

        private SpriteRenderer CreateRenderer(string name, Sprite sprite, int order)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform, false);
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return renderer;
        }

        private void Release(SpriteRenderer renderer)
        {
            renderer.gameObject.SetActive(false);
            if (renderer.sprite == SquareFlowWorldSprites.Square)
                linePool.Enqueue(renderer);
            else
                circlePool.Enqueue(renderer);
        }

        private static float EaseOut(float t)
        {
            float inverse = 1f - Mathf.Clamp01(t);
            return 1f - inverse * inverse;
        }

        private static Color ColorWithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
```

- [ ] **Step 5: Run focused orbiter test**

Run:

```powershell
dotnet test SquareFlow.EditModeTests.csproj --filter OrbiterWorldViewReusesObjectForSameOrbiterId
```

Expected: pass.

- [ ] **Step 6: Run all edit-mode tests**

Run:

```powershell
dotnet test SquareFlow.EditModeTests.csproj
```

Expected: all tests pass.

- [ ] **Step 7: Commit**

Run:

```powershell
git add -- Assets/SquareFlow/Scripts/Runtime/OrbiterWorldView.cs Assets/SquareFlow/Scripts/Runtime/WorldEffectsController.cs Assets/SquareFlow/Tests/EditMode/WorldViewReuseTests.cs
git commit -m "Add pooled world orbiter and effects views"
```

Expected: commit succeeds with orbiter/effects code and reuse tests staged.

## Task 5: Integrate World Renderer Into Game Controller

**Files:**
- Modify: `Assets/SquareFlow/Scripts/UI/SquareFlowGameController.cs`
- Modify: `Assets/SquareFlow/Tests/EditMode/BoardLayoutTests.cs`

- [ ] **Step 1: Update layout test that assumed Canvas board rendering**

In `Assets/SquareFlow/Tests/EditMode/BoardLayoutTests.cs`, replace `ReferenceGameplayLayoutPlacesHudBoardQueueAndDockLikeMockup` with this Canvas-only assertion:

```csharp
[Test]
public void ReferenceGameplayLayoutKeepsCanvasForHudQueueAndDock()
{
    BoardLayout board = BoardLayout.Compute(5, 5, 620f);
    SquareFlowGameplayScreenLayout layout = SquareFlowGameplayScreenLayout.Create(board);

    Assert.That(layout.HudSize.y, Is.EqualTo(122f));
    Assert.That(layout.UtilityButtonSize.x, Is.EqualTo(layout.UtilityButtonSize.y));
    Assert.That(layout.UtilityButtonSize.x, Is.EqualTo(66f));
    Assert.That(layout.QueueSize.x, Is.EqualTo(154f));
    Assert.That(layout.QueueSize.y, Is.GreaterThan(board.GridHeight));
    Assert.That(layout.DockSize.y, Is.EqualTo(128f));
}
```

- [ ] **Step 2: Add world renderer fields**

In `SquareFlowGameController`, add these fields beside the existing runtime fields:

```csharp
private Camera gameplayCamera;
private MobileCameraController mobileCamera;
private GameObject worldRoot;
private BoardWorldView boardWorldView;
private OrbitRingWorldView orbitRingWorldView;
private OrbiterWorldView orbiterWorldView;
private WorldEffectsController worldEffects;
private MobileWorldLayout worldLayout;
```

- [ ] **Step 3: Build world renderer during `Awake`**

In `Awake`, call `BuildWorldRenderer();` immediately after `BuildCanvas();`:

```csharp
BuildCanvas();
BuildWorldRenderer();
```

Add this method near `BuildCanvas`:

```csharp
private void BuildWorldRenderer()
{
    GameObject cameraObject = new GameObject("SquareFlowWorldCamera", typeof(Camera), typeof(MobileCameraController));
    cameraObject.transform.SetParent(transform, false);
    gameplayCamera = cameraObject.GetComponent<Camera>();
    mobileCamera = cameraObject.GetComponent<MobileCameraController>();

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
```

- [ ] **Step 4: Hide world gameplay on menu**

At the end of `ShowMenu`, before adding menu UI, clear and disable world gameplay:

```csharp
if (worldRoot != null)
{
    worldRoot.SetActive(false);
    boardWorldView.Clear();
    orbitRingWorldView.Clear();
    orbiterWorldView.Clear();
    worldEffects.Clear();
}
```

- [ ] **Step 5: Create mobile world layout when starting a level**

In `StartLevel`, after `layout = BoardLayout.Compute(shape.Rows, shape.Cols, 860f);`, add:

```csharp
worldLayout = MobileWorldLayout.Create(layout);
if (worldRoot != null)
    worldRoot.SetActive(true);
if (mobileCamera != null)
    mobileCamera.Configure(theme.Background);
```

- [ ] **Step 6: Replace Canvas board rendering in `RefreshGameView`**

In `RefreshGameView`, remove this Canvas board block:

```csharp
RectTransform board = AddPanel(root, "Board", new Vector2(layout.CanvasWidth, layout.CanvasHeight), new Color(0f, 0f, 0f, 0f));
SetAnchored(board, screen.BoardPosition);
RenderOrbitRing(board);
RenderBoard(board);
RenderOrbiters(board);
```

Insert this call in its place:

```csharp
RefreshWorldGameplay();
```

Add this method near `RefreshGameView`:

```csharp
private void RefreshWorldGameplay()
{
    if (state == null || layout == null || !worldLayout.IsValid || worldRoot == null) return;

    worldRoot.SetActive(true);
    boardWorldView.Bind(state, layout, worldLayout, theme);
    orbitRingWorldView.Bind(layout, worldLayout, theme);
    orbiterWorldView.Refresh(state.ActiveOrbiters, worldLayout, theme);
}
```

- [ ] **Step 7: Update per-frame orbiter refresh**

In `Update`, replace:

```csharp
UpdateOrbiterVisuals();
```

with:

```csharp
if (state != null && worldRoot != null && worldRoot.activeSelf)
    orbiterWorldView.Refresh(state.ActiveOrbiters, worldLayout, theme);
```

- [ ] **Step 8: Update shot effects to use world positions**

Replace `SpawnShotEffect` with:

```csharp
private void SpawnShotEffect(GameEvent gameEvent, bool heavyImpact)
{
    if (layout == null || !worldLayout.IsValid || worldEffects == null || !gameEvent.HasFirePoint) return;
    if (!worldLayout.TryFirePoint(gameEvent, out Vector2 start)) return;

    Vector2 end = worldLayout.EventTarget(gameEvent);
    Color color = ShotColor(gameEvent);
    worldEffects.PlayShot(start, end, color, heavyImpact || gameEvent.Type == GameEventType.BombDetonated);
}
```

Replace `ShotColor` with:

```csharp
private Color ShotColor(GameEvent gameEvent)
{
    if (gameEvent.Type == GameEventType.BombDetonated) return theme.Bomb;

    if (!string.IsNullOrEmpty(gameEvent.OrbiterId) && orbiterWorldView != null && orbiterWorldView.TryGetColor(gameEvent.OrbiterId, out Color orbiterColor))
        return orbiterColor;

    if (state != null && state.Shape.IsActive(gameEvent.Row, gameEvent.Col) && state.Grid[gameEvent.Row, gameEvent.Col].IsOccupied)
        return ColorForCell(state.Grid[gameEvent.Row, gameEvent.Col]);

    return theme.Score;
}
```

- [ ] **Step 9: Refresh world views after firing**

In both `FireColumn` and `FireWaiting`, replace the final `RefreshGameView();` call with:

```csharp
RefreshGameView();
RefreshWorldGameplay();
```

- [ ] **Step 10: Remove obsolete Canvas gameplay helpers**

Delete these methods from `SquareFlowGameController` after the world renderer compiles:

```csharp
private void RenderBoard(RectTransform board)
private void RenderOrbiters(RectTransform board)
private void UpdateOrbiterVisuals()
private bool TryGetFirePointPosition(GameEvent gameEvent, out Vector2 position)
private IEnumerator AnimateShotEffect(Vector2 start, Vector2 end, Color color, bool heavyImpact)
private IEnumerator AnimateImpactBurst(Vector2 position, Color color, bool heavyImpact)
private void RenderOrbitRing(RectTransform board)
private Vector2 BoardPoint(int col, int row)
private Vector2 BoardAnchored(Vector2 point)
```

- [ ] **Step 11: Run compile check**

Run:

```powershell
dotnet build SquareFlow.Runtime.csproj
```

Expected: build succeeds with no missing method or namespace errors.

- [ ] **Step 12: Run edit-mode tests**

Run:

```powershell
dotnet test SquareFlow.EditModeTests.csproj
```

Expected: all tests pass.

- [ ] **Step 13: Commit**

Run:

```powershell
git add -- Assets/SquareFlow/Scripts/UI/SquareFlowGameController.cs Assets/SquareFlow/Tests/EditMode/BoardLayoutTests.cs
git commit -m "Render gameplay with world-space views"
```

Expected: commit succeeds with controller integration and adjusted layout test staged.

## Task 6: Mobile Touch Layout Pass

**Files:**
- Modify: `Assets/SquareFlow/Scripts/UI/SquareFlowGameController.cs`

- [ ] **Step 1: Update Canvas scaler for portrait touch**

In `BuildCanvas`, keep the reference resolution at 1080 by 1920 and bias scaling toward width for phone portrait:

```csharp
CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
scaler.referenceResolution = new Vector2(1080f, 1920f);
scaler.matchWidthOrHeight = 0.35f;
```

- [ ] **Step 2: Adjust gameplay Canvas layout helper**

Replace `SquareFlowGameplayScreenLayout.Create` with:

```csharp
public static SquareFlowGameplayScreenLayout Create(BoardLayout board)
{
    Vector2 hudSize = new Vector2(1000f, 122f);
    Vector2 queueSize = new Vector2(154f, 430f);
    Vector2 dockSize = new Vector2(1000f, 150f);
    return new SquareFlowGameplayScreenLayout(
        new Vector2(0f, 804f),
        hudSize,
        Vector2.zero,
        new Vector2(392f, 64f),
        queueSize,
        new Vector2(0f, -704f),
        dockSize,
        new Vector2(74f, 74f));
}
```

- [ ] **Step 3: Increase shooter touch targets**

In `RenderWaiting`, set waiting shooter size to 74:

```csharp
AddShooterButton(queue, shooter, new Vector2(0f, startY - i * 88f), Vector2.one * 74f, () => FireWaiting(index));
```

In `RenderColumns`, set dock slot size to 96 and shooter size to 74:

```csharp
RectTransform slot = AddPanel(columns, "DockSlot", new Vector2(96f, 96f), theme.DockSlot);
SetAnchored(slot, new Vector2(x, 0f));
ApplyOutline(slot, ColorWithAlpha(theme.Border, 0.5f), 1f);

if (state.ShooterColumns[i].Count == 0)
{
    AddText(slot, "-", 20, FontStyle.Bold, theme.SubtleText, Vector2.zero, new Vector2(96f, 96f));
    continue;
}

Shooter front = state.ShooterColumns[i][0];
AddShooterButton(slot, front, Vector2.zero, Vector2.one * 74f, () => FireColumn(column));
```

- [ ] **Step 4: Run focused layout test**

Run:

```powershell
dotnet test SquareFlow.EditModeTests.csproj --filter ReferenceGameplayLayoutKeepsCanvasForHudQueueAndDock
```

Expected: pass.

- [ ] **Step 5: Run compile check**

Run:

```powershell
dotnet build SquareFlow.Runtime.csproj
```

Expected: build succeeds.

- [ ] **Step 6: Commit**

Run:

```powershell
git add -- Assets/SquareFlow/Scripts/UI/SquareFlowGameController.cs
git commit -m "Tune gameplay UI for portrait touch"
```

Expected: commit succeeds with only controller layout changes staged.

## Task 7: Verification And Unity Smoke Check

**Files:**
- No source files.

- [ ] **Step 1: Run full C# build**

Run:

```powershell
dotnet build SquareFlow.Runtime.csproj
```

Expected: exit code 0.

- [ ] **Step 2: Run all edit-mode tests through dotnet**

Run:

```powershell
dotnet test SquareFlow.EditModeTests.csproj
```

Expected: exit code 0.

- [ ] **Step 3: Run Unity edit-mode test runner**

Run:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.15f1\Editor\Unity.exe" -batchmode -projectPath "C:\Users\samet\My project" -runTests -testPlatform EditMode -testResults "C:\Users\samet\My project\TestResults.xml" -quit -logFile "C:\Users\samet\My project\Logs\editmode-tests-mobile-world-renderer.log"
```

Expected: Unity exits with code 0 and writes passing results to `TestResults.xml`.

- [ ] **Step 4: Run Unity compile check**

Run:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.15f1\Editor\Unity.exe" -batchmode -projectPath "C:\Users\samet\My project" -quit -logFile "C:\Users\samet\My project\Logs\compile-check-mobile-world-renderer.log"
```

Expected: Unity exits with code 0 and the log has no `error CS` compile errors.

- [ ] **Step 5: Inspect final diff**

Run:

```powershell
git status --short
git diff --stat
```

Expected: no unstaged implementation changes remain after task commits. Existing unrelated workspace changes may still appear because the repository already had untracked/generated Unity files before this plan.

## Final Verification Checklist

- [ ] Board cells render through `BoardWorldView` using `SpriteRenderer`.
- [ ] Orbit ring renders through `OrbitRingWorldView`.
- [ ] Active orbiters render and move through `OrbiterWorldView`.
- [ ] Shot and impact effects play through `WorldEffectsController`.
- [ ] Canvas remains responsible for menu, HUD, queue, shooter dock, and result panel.
- [ ] Normal firing does not rebuild the board GameObject hierarchy.
- [ ] Edit-mode tests pass.
- [ ] Unity compile check passes.
