# Square Flow Unity Remake Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a native Unity 2D/UI remake of the HTML Square Flow game in the existing Unity project.

**Architecture:** Core game behavior lives in small, testable C# model/rules classes under `Assets/SquareFlow/Scripts/Core`. Runtime Unity scripts under `Assets/SquareFlow/Scripts/Runtime`, `UI`, and `Effects` bind that model to a generated scene UI. The existing `SampleScene` becomes a thin host for a generated `SquareFlowRoot`.

**Tech Stack:** Unity 6000.3.15f1, C#, Unity UI/uGUI, Unity Test Framework/NUnit, PlayerPrefs, URP project settings already present.

---

## File Map

- Create `Assets/SquareFlow/Scripts/SquareFlow.Runtime.asmdef`: runtime assembly definition.
- Create `Assets/SquareFlow/Tests/EditMode/SquareFlow.EditModeTests.asmdef`: edit-mode test assembly.
- Create `Assets/SquareFlow/Scripts/Core/SquareFlowConstants.cs`: shared constants.
- Create `Assets/SquareFlow/Scripts/Core/FlowColor.cs`: color enum and helpers.
- Create `Assets/SquareFlow/Scripts/Core/BoardCell.cs`: cell type, HP, color state.
- Create `Assets/SquareFlow/Scripts/Core/Shooter.cs`: shooter data.
- Create `Assets/SquareFlow/Scripts/Core/ActiveOrbiter.cs`: active orbiter data.
- Create `Assets/SquareFlow/Scripts/Core/BoardShape.cs`: board shape model.
- Create `Assets/SquareFlow/Scripts/Core/BoardShapeCatalog.cs`: ten source shape masks.
- Create `Assets/SquareFlow/Scripts/Core/IFlowRandom.cs`: deterministic random abstraction.
- Create `Assets/SquareFlow/Scripts/Core/SystemFlowRandom.cs`: production random implementation.
- Create `Assets/SquareFlow/Scripts/Core/BoardGenerator.cs`: level grid generation.
- Create `Assets/SquareFlow/Scripts/Core/ShooterGenerator.cs`: shooter column generation.
- Create `Assets/SquareFlow/Scripts/Core/BoardLayout.cs`: orbit layout and fire-point calculations.
- Create `Assets/SquareFlow/Scripts/Core/TargetingSystem.cs`: hit target lookup.
- Create `Assets/SquareFlow/Scripts/Core/GameResult.cs`: result enum.
- Create `Assets/SquareFlow/Scripts/Core/GameEvent.cs`: rule event model for UI/effects.
- Create `Assets/SquareFlow/Scripts/Core/GameState.cs`: mutable level state.
- Create `Assets/SquareFlow/Scripts/Core/GameRules.cs`: fire, advance, hits, scoring, win/loss.
- Create `Assets/SquareFlow/Scripts/Runtime/SaveDataService.cs`: PlayerPrefs wrapper.
- Create `Assets/SquareFlow/Scripts/UI/SquareFlowTheme.cs`: dark/light palette.
- Create `Assets/SquareFlow/Scripts/UI/SquareFlowGameController.cs`: root UI and interaction coordinator.
- Create `Assets/SquareFlow/Scripts/Effects/SquareFlowAudio.cs`: generated audio cues.
- Create `Assets/SquareFlow/Editor/SquareFlowSceneBuilder.cs`: editor utility to rebuild SampleScene.
- Modify `Assets/Scenes/SampleScene.unity`: replace default cube scene with generated Square Flow host.
- Create tests under `Assets/SquareFlow/Tests/EditMode`.

Commit steps are included for teams that initialize git. This project currently is not a git repository, so each commit step can be skipped until `git init` exists.

---

## Task 1: Assembly Setup And Core Types

**Files:**
- Create: `Assets/SquareFlow/Scripts/SquareFlow.Runtime.asmdef`
- Create: `Assets/SquareFlow/Tests/EditMode/SquareFlow.EditModeTests.asmdef`
- Create: `Assets/SquareFlow/Scripts/Core/SquareFlowConstants.cs`
- Create: `Assets/SquareFlow/Scripts/Core/FlowColor.cs`
- Create: `Assets/SquareFlow/Scripts/Core/BoardCell.cs`
- Create: `Assets/SquareFlow/Scripts/Core/Shooter.cs`
- Create: `Assets/SquareFlow/Scripts/Core/ActiveOrbiter.cs`
- Create: `Assets/SquareFlow/Tests/EditMode/CoreTypesTests.cs`

- [ ] **Step 1: Create failing core type tests**

```csharp
using NUnit.Framework;
using SquareFlow.Core;

namespace SquareFlow.Tests
{
    public sealed class CoreTypesTests
    {
        [Test]
        public void BoardCellFactoryCreatesNormalBombAndEmptyCells()
        {
            BoardCell normal = BoardCell.Normal(FlowColor.Red, 3);
            BoardCell bomb = BoardCell.Bomb();
            BoardCell empty = BoardCell.Empty;

            Assert.That(normal.Type, Is.EqualTo(BoardCellType.Normal));
            Assert.That(normal.Color, Is.EqualTo(FlowColor.Red));
            Assert.That(normal.Hp, Is.EqualTo(3));
            Assert.That(bomb.Type, Is.EqualTo(BoardCellType.Bomb));
            Assert.That(bomb.Hp, Is.EqualTo(1));
            Assert.That(empty.Type, Is.EqualTo(BoardCellType.Empty));
            Assert.That(empty.IsOccupied, Is.False);
        }

        [Test]
        public void ShooterStoresColorAmmoWildAndHiddenFlags()
        {
            Shooter shooter = new Shooter("s1", FlowColor.Wild, 7, true, true);

            Assert.That(shooter.Id, Is.EqualTo("s1"));
            Assert.That(shooter.Color, Is.EqualTo(FlowColor.Wild));
            Assert.That(shooter.Ammo, Is.EqualTo(7));
            Assert.That(shooter.Wild, Is.True);
            Assert.That(shooter.Hidden, Is.True);
        }

        [Test]
        public void ConstantsMatchSourceGameLimits()
        {
            Assert.That(SquareFlowConstants.Speed, Is.EqualTo(320f));
            Assert.That(SquareFlowConstants.WaitQueueLimit, Is.EqualTo(5));
            Assert.That(SquareFlowConstants.MaxActiveOrbiters, Is.EqualTo(5));
            Assert.That(SquareFlowConstants.NormalColorCount, Is.EqualTo(4));
        }
    }
}
```

- [ ] **Step 2: Run tests and verify they fail because types do not exist**

Run:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.15f1\Editor\Unity.exe" -batchmode -projectPath "C:\Users\samet\My project" -runTests -testPlatform EditMode -testResults "C:\Users\samet\My project\TestResults.xml" -quit -logFile "C:\Users\samet\My project\Logs\editmode-tests.log"
```

Expected: non-zero exit code. The log names missing `SquareFlow.Core` types.

- [ ] **Step 3: Add assembly definitions**

`Assets/SquareFlow/Scripts/SquareFlow.Runtime.asmdef`:

```json
{
  "name": "SquareFlow.Runtime",
  "rootNamespace": "SquareFlow",
  "references": [],
  "includePlatforms": [],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": false,
  "precompiledReferences": [],
  "autoReferenced": true,
  "defineConstraints": [],
  "versionDefines": [],
  "noEngineReferences": false
}
```

`Assets/SquareFlow/Tests/EditMode/SquareFlow.EditModeTests.asmdef`:

```json
{
  "name": "SquareFlow.EditModeTests",
  "rootNamespace": "SquareFlow.Tests",
  "references": [
    "SquareFlow.Runtime"
  ],
  "includePlatforms": [
    "Editor"
  ],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": false,
  "precompiledReferences": [],
  "autoReferenced": true,
  "defineConstraints": [],
  "versionDefines": [],
  "noEngineReferences": false,
  "optionalUnityReferences": [
    "TestAssemblies"
  ]
}
```

- [ ] **Step 4: Add minimal core type implementation**

`Assets/SquareFlow/Scripts/Core/SquareFlowConstants.cs`:

```csharp
namespace SquareFlow.Core
{
    public static class SquareFlowConstants
    {
        public const float Speed = 320f;
        public const int WaitQueueLimit = 5;
        public const int MaxActiveOrbiters = 5;
        public const int NormalColorCount = 4;
        public const int ExtraShooterCount = 6;
        public const float ComboResetSeconds = 1.3f;
    }
}
```

`Assets/SquareFlow/Scripts/Core/FlowColor.cs`:

```csharp
namespace SquareFlow.Core
{
    public enum FlowColor
    {
        Wild = -1,
        Red = 0,
        Blue = 1,
        Yellow = 2,
        Green = 3
    }
}
```

`Assets/SquareFlow/Scripts/Core/BoardCell.cs`:

```csharp
using System;

namespace SquareFlow.Core
{
    public enum BoardCellType
    {
        Empty,
        Normal,
        Bomb
    }

    [Serializable]
    public struct BoardCell
    {
        public BoardCellType Type { get; private set; }
        public FlowColor Color { get; private set; }
        public int Hp { get; private set; }
        public bool IsOccupied => Type != BoardCellType.Empty;

        private BoardCell(BoardCellType type, FlowColor color, int hp)
        {
            Type = type;
            Color = color;
            Hp = hp;
        }

        public static BoardCell Empty => new BoardCell(BoardCellType.Empty, FlowColor.Red, 0);

        public static BoardCell Normal(FlowColor color, int hp)
        {
            if (color == FlowColor.Wild) throw new ArgumentException("Normal cells need a normal color.", nameof(color));
            if (hp < 1) throw new ArgumentOutOfRangeException(nameof(hp), "HP must be positive.");
            return new BoardCell(BoardCellType.Normal, color, hp);
        }

        public static BoardCell Bomb()
        {
            return new BoardCell(BoardCellType.Bomb, FlowColor.Wild, 1);
        }

        public BoardCell WithHp(int hp)
        {
            return Type == BoardCellType.Normal ? Normal(Color, hp) : this;
        }
    }
}
```

`Assets/SquareFlow/Scripts/Core/Shooter.cs`:

```csharp
namespace SquareFlow.Core
{
    public readonly struct Shooter
    {
        public Shooter(string id, FlowColor color, int ammo, bool wild, bool hidden = false)
        {
            Id = id;
            Color = color;
            Ammo = ammo;
            Wild = wild;
            Hidden = hidden;
        }

        public string Id { get; }
        public FlowColor Color { get; }
        public int Ammo { get; }
        public bool Wild { get; }
        public bool Hidden { get; }

        public Shooter Revealed()
        {
            return new Shooter(Id, Color, Ammo, Wild, false);
        }
    }
}
```

`Assets/SquareFlow/Scripts/Core/ActiveOrbiter.cs`:

```csharp
namespace SquareFlow.Core
{
    public sealed class ActiveOrbiter
    {
        public ActiveOrbiter(Shooter shooter)
        {
            Id = shooter.Id;
            Color = shooter.Color;
            Ammo = shooter.Ammo;
            Wild = shooter.Wild;
            Distance = 0f;
        }

        public string Id { get; }
        public FlowColor Color { get; }
        public int Ammo { get; set; }
        public bool Wild { get; }
        public float Distance { get; set; }
    }
}
```

- [ ] **Step 5: Run tests and verify they pass**

Run the Unity edit-mode command from Step 2.

Expected: exit code 0 and all tests in `CoreTypesTests` pass.

- [ ] **Step 6: Commit if git exists**

```powershell
git rev-parse --is-inside-work-tree
git add Assets/SquareFlow/Scripts Assets/SquareFlow/Tests/EditMode
git commit -m "feat: add square flow core types"
```

Expected in this project right now: first command reports this is not a git repository, so skip the add/commit commands.

---

## Task 2: Board Shapes And Board Generation

**Files:**
- Create: `Assets/SquareFlow/Scripts/Core/BoardShape.cs`
- Create: `Assets/SquareFlow/Scripts/Core/BoardShapeCatalog.cs`
- Create: `Assets/SquareFlow/Scripts/Core/IFlowRandom.cs`
- Create: `Assets/SquareFlow/Scripts/Core/SystemFlowRandom.cs`
- Create: `Assets/SquareFlow/Scripts/Core/BoardGenerator.cs`
- Create: `Assets/SquareFlow/Tests/EditMode/FixedFlowRandom.cs`
- Create: `Assets/SquareFlow/Tests/EditMode/BoardGenerationTests.cs`

- [ ] **Step 1: Write failing tests for shape catalog and generated grids**

```csharp
using System.Linq;
using NUnit.Framework;
using SquareFlow.Core;

namespace SquareFlow.Tests
{
    public sealed class BoardGenerationTests
    {
        [Test]
        public void CatalogContainsTheTenSourceShapes()
        {
            Assert.That(BoardShapeCatalog.Count, Is.EqualTo(10));
            Assert.That(BoardShapeCatalog.GetShape(1).Name, Is.EqualTo("Diamond"));
            Assert.That(BoardShapeCatalog.GetShape(2).Rows, Is.EqualTo(15));
            Assert.That(BoardShapeCatalog.GetShape(10).Cols, Is.EqualTo(13));
            Assert.That(BoardShapeCatalog.GetShape(11).Name, Is.EqualTo("Diamond"));
        }

        [Test]
        public void GeneratedGridOnlyFillsActiveShapeCells()
        {
            BoardShape shape = BoardShapeCatalog.GetShape(1);
            BoardCell[,] grid = BoardGenerator.Generate(shape, 1, new FixedFlowRandom(0.25));

            for (int r = 0; r < shape.Rows; r++)
            for (int c = 0; c < shape.Cols; c++)
            {
                Assert.That(grid[r, c].IsOccupied, Is.EqualTo(shape.IsActive(r, c)));
            }
        }

        [Test]
        public void GeneratedGridCreatesAtLeastOneBomb()
        {
            BoardShape shape = BoardShapeCatalog.GetShape(1);
            BoardCell[,] grid = BoardGenerator.Generate(shape, 1, new FixedFlowRandom(0.15));

            int bombs = grid.Cast<BoardCell>().Count(cell => cell.Type == BoardCellType.Bomb);

            Assert.That(bombs, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void GeneratedHpStaysWithinLevelScaledMaximum()
        {
            BoardShape shape = BoardShapeCatalog.GetShape(10);
            int level = 20;
            BoardCell[,] grid = BoardGenerator.Generate(shape, level, new FixedFlowRandom(0.99));
            int maxHp = BoardGenerator.GetMaxHp(level);

            foreach (BoardCell cell in grid)
            {
                if (cell.Type == BoardCellType.Normal)
                    Assert.That(cell.Hp, Is.InRange(1, maxHp));
            }
        }
    }
}
```

`Assets/SquareFlow/Tests/EditMode/FixedFlowRandom.cs`:

```csharp
using System.Collections.Generic;
using SquareFlow.Core;

namespace SquareFlow.Tests
{
    public sealed class FixedFlowRandom : IFlowRandom
    {
        private readonly Queue<double> values = new Queue<double>();
        private readonly double fallback;

        public FixedFlowRandom(double fallback, params double[] sequence)
        {
            this.fallback = fallback;
            foreach (double value in sequence) values.Enqueue(value);
        }

        public double Value()
        {
            return values.Count > 0 ? values.Dequeue() : fallback;
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            double raw = Value();
            int width = maxExclusive - minInclusive;
            return minInclusive + UnityEngine.Mathf.Clamp((int)(raw * width), 0, width - 1);
        }
    }
}
```

- [ ] **Step 2: Run tests and verify they fail because generation classes do not exist**

Run the Unity edit-mode command from Task 1.

Expected: non-zero exit code with missing `BoardShapeCatalog`, `BoardGenerator`, and `IFlowRandom`.

- [ ] **Step 3: Add board shape and random implementation**

`Assets/SquareFlow/Scripts/Core/BoardShape.cs`:

```csharp
using System;

namespace SquareFlow.Core
{
    public sealed class BoardShape
    {
        private readonly bool[,] cells;

        public BoardShape(string name, bool[,] cells)
        {
            Name = name;
            this.cells = cells;
            Rows = cells.GetLength(0);
            Cols = cells.GetLength(1);
        }

        public string Name { get; }
        public int Rows { get; }
        public int Cols { get; }

        public bool IsActive(int row, int col)
        {
            return row >= 0 && row < Rows && col >= 0 && col < Cols && cells[row, col];
        }

        public int ActiveCellCount()
        {
            int count = 0;
            for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Cols; c++)
                if (cells[r, c]) count++;
            return count;
        }

        public bool[,] CopyCells()
        {
            return (bool[,])cells.Clone();
        }

        public static bool[,] Mask(params int[][] rows)
        {
            if (rows.Length == 0) throw new ArgumentException("Shape needs at least one row.", nameof(rows));
            int cols = rows[0].Length;
            bool[,] mask = new bool[rows.Length, cols];
            for (int r = 0; r < rows.Length; r++)
            {
                if (rows[r].Length != cols) throw new ArgumentException("Shape rows need equal widths.", nameof(rows));
                for (int c = 0; c < cols; c++) mask[r, c] = rows[r][c] != 0;
            }
            return mask;
        }
    }
}
```

`Assets/SquareFlow/Scripts/Core/IFlowRandom.cs`:

```csharp
namespace SquareFlow.Core
{
    public interface IFlowRandom
    {
        double Value();
        int Range(int minInclusive, int maxExclusive);
    }
}
```

`Assets/SquareFlow/Scripts/Core/SystemFlowRandom.cs`:

```csharp
using System;

namespace SquareFlow.Core
{
    public sealed class SystemFlowRandom : IFlowRandom
    {
        private readonly Random random;

        public SystemFlowRandom(int? seed = null)
        {
            random = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        public double Value()
        {
            return random.NextDouble();
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            return random.Next(minInclusive, maxExclusive);
        }
    }
}
```

- [ ] **Step 4: Add the exact ten source board masks**

`Assets/SquareFlow/Scripts/Core/BoardShapeCatalog.cs`:

```csharp
using System.Collections.Generic;

namespace SquareFlow.Core
{
    public static class BoardShapeCatalog
    {
        private static readonly BoardShape[] Shapes =
        {
            new BoardShape("Diamond", BoardShape.Mask(
                new[]{0,0,1,1,1,1,1,0,0}, new[]{0,1,1,1,1,1,1,1,0}, new[]{1,1,1,1,1,1,1,1,1},
                new[]{0,1,1,1,1,1,1,1,0}, new[]{0,0,1,1,1,1,1,0,0}, new[]{0,0,0,1,1,1,0,0,0}, new[]{0,0,0,0,1,0,0,0,0})),
            new BoardShape("Dino", BoardShape.Mask(
                new[]{0,0,0,0,0,0,0,1,1,1,1,0,0}, new[]{0,0,0,0,0,0,1,1,1,1,1,1,0}, new[]{0,0,0,0,0,0,1,1,0,1,1,1,0},
                new[]{0,0,0,0,0,0,1,1,1,1,1,1,0}, new[]{0,0,0,0,0,0,1,1,1,1,0,0,0}, new[]{0,0,0,0,0,0,1,1,1,1,1,0,0},
                new[]{1,0,0,0,0,1,1,1,1,0,0,0,0}, new[]{1,1,0,0,1,1,1,1,0,0,0,0,0}, new[]{1,1,1,1,1,1,1,1,1,1,1,0,0},
                new[]{0,1,1,1,1,1,1,1,0,0,0,0,0}, new[]{0,0,1,1,1,1,1,0,0,0,0,0,0}, new[]{0,0,0,1,1,1,1,0,0,0,0,0,0},
                new[]{0,0,0,1,1,0,1,1,0,0,0,0,0}, new[]{0,0,0,1,1,0,0,1,1,0,0,0,0}, new[]{0,0,1,1,0,0,0,0,1,1,0,0,0})),
            new BoardShape("Heart", BoardShape.Mask(
                new[]{0,0,1,1,0,0,0,1,1,0,0}, new[]{0,1,1,1,1,0,1,1,1,1,0}, new[]{1,1,1,1,1,1,1,1,1,1,1},
                new[]{1,1,1,1,1,1,1,1,1,1,1}, new[]{1,1,1,1,1,1,1,1,1,1,1}, new[]{0,1,1,1,1,1,1,1,1,1,0},
                new[]{0,0,1,1,1,1,1,1,1,0,0}, new[]{0,0,0,1,1,1,1,1,0,0,0}, new[]{0,0,0,0,1,1,1,0,0,0,0}, new[]{0,0,0,0,0,1,0,0,0,0,0}, new[]{0,0,0,0,0,0,0,0,0,0,0})),
            new BoardShape("Pizza", BoardShape.Mask(
                new[]{0,0,0,1,1,1,1,1,0,0,0}, new[]{0,0,1,1,1,1,1,1,1,0,0}, new[]{0,1,1,1,1,1,1,1,1,1,0},
                new[]{1,1,1,1,1,1,1,1,0,0,0}, new[]{1,1,1,1,1,1,1,0,0,0,0}, new[]{1,1,1,1,1,1,0,0,0,0,0},
                new[]{1,1,1,1,1,1,1,0,0,0,0}, new[]{1,1,1,1,1,1,1,1,0,0,0}, new[]{0,1,1,1,1,1,1,1,1,1,0},
                new[]{0,0,1,1,1,1,1,1,1,0,0}, new[]{0,0,0,1,1,1,1,1,0,0,0})),
            new BoardShape("Smiley", BoardShape.Mask(
                new[]{0,0,0,1,1,1,1,1,0,0,0}, new[]{0,0,1,1,1,1,1,1,1,0,0}, new[]{0,1,1,1,1,1,1,1,1,1,0},
                new[]{1,1,1,0,1,1,1,0,1,1,1}, new[]{1,1,1,1,1,1,1,1,1,1,1}, new[]{1,1,1,1,1,1,1,1,1,1,1},
                new[]{1,1,0,1,1,1,1,1,0,1,1}, new[]{1,1,1,0,1,1,1,0,1,1,1}, new[]{0,1,1,1,0,0,0,1,1,1,0},
                new[]{0,0,1,1,1,1,1,1,1,0,0}, new[]{0,0,0,1,1,1,1,1,0,0,0})),
            new BoardShape("Fish", BoardShape.Mask(
                new[]{0,0,0,0,0,0,1,1,0,0,0,0,0}, new[]{1,0,0,0,0,1,1,1,1,1,0,0,0}, new[]{1,1,0,0,1,1,1,1,1,1,1,0,0},
                new[]{1,1,1,1,1,1,1,1,1,1,1,1,0}, new[]{1,1,1,1,1,1,1,1,1,0,1,1,1}, new[]{1,1,1,1,1,1,1,1,1,1,1,1,0},
                new[]{1,1,0,0,1,1,1,1,1,1,1,0,0}, new[]{1,0,0,0,0,1,1,1,1,1,0,0,0}, new[]{0,0,0,0,0,0,1,1,0,0,0,0,0})),
            new BoardShape("Skull", BoardShape.Mask(
                new[]{0,0,1,1,1,1,1,1,1,0,0}, new[]{0,1,1,1,1,1,1,1,1,1,0}, new[]{1,1,1,1,1,1,1,1,1,1,1},
                new[]{1,1,1,0,1,1,1,0,1,1,1}, new[]{1,1,1,1,1,1,1,1,1,1,1}, new[]{0,1,1,1,1,1,1,1,1,1,0},
                new[]{0,0,1,1,1,1,1,1,1,0,0}, new[]{0,0,0,1,0,1,0,1,0,0,0}, new[]{0,0,0,1,0,1,0,1,0,0,0}, new[]{0,0,0,0,0,0,0,0,0,0,0}, new[]{0,0,0,0,0,0,0,0,0,0,0})),
            new BoardShape("Tree", BoardShape.Mask(
                new[]{0,0,0,0,0,1,0,0,0,0,0}, new[]{0,0,0,0,1,1,1,0,0,0,0}, new[]{0,0,0,1,1,1,1,1,0,0,0},
                new[]{0,0,1,1,1,1,1,1,1,0,0}, new[]{0,0,0,1,1,1,1,1,0,0,0}, new[]{0,0,1,1,1,1,1,1,1,0,0},
                new[]{0,1,1,1,1,1,1,1,1,1,0}, new[]{0,0,1,1,1,1,1,1,1,0,0}, new[]{0,1,1,1,1,1,1,1,1,1,0},
                new[]{1,1,1,1,1,1,1,1,1,1,1}, new[]{0,0,0,0,1,1,1,0,0,0,0}, new[]{0,0,0,0,1,1,1,0,0,0,0}, new[]{0,0,0,0,1,1,1,0,0,0,0})),
            new BoardShape("Hourglass", BoardShape.Mask(
                new[]{1,1,1,1,1,1,1,1,1}, new[]{1,1,1,1,1,1,1,1,1}, new[]{0,1,1,1,1,1,1,1,0}, new[]{0,0,1,1,1,1,1,0,0},
                new[]{0,0,0,1,1,1,0,0,0}, new[]{0,0,0,0,1,0,0,0,0}, new[]{0,0,0,1,1,1,0,0,0}, new[]{0,0,1,1,1,1,1,0,0},
                new[]{0,1,1,1,1,1,1,1,0}, new[]{1,1,1,1,1,1,1,1,1}, new[]{1,1,1,1,1,1,1,1,1})),
            new BoardShape("Crown", BoardShape.Mask(
                new[]{1,0,0,0,0,0,1,0,0,0,0,0,1}, new[]{1,1,0,0,0,1,1,1,0,0,0,1,1}, new[]{1,1,1,0,0,1,1,1,0,0,1,1,1},
                new[]{1,1,1,0,1,1,1,1,1,0,1,1,1}, new[]{1,1,1,1,1,1,1,1,1,1,1,1,1}, new[]{1,1,1,1,1,1,1,1,1,1,1,1,1},
                new[]{1,1,1,1,1,1,1,1,1,1,1,1,1}, new[]{1,1,1,1,1,1,1,1,1,1,1,1,1}, new[]{1,1,1,1,1,1,1,1,1,1,1,1,1}))
        };

        public static int Count => Shapes.Length;

        public static BoardShape GetShape(int level)
        {
            int index = ((level - 1) % Shapes.Length + Shapes.Length) % Shapes.Length;
            return Shapes[index];
        }

        public static IReadOnlyList<BoardShape> All => Shapes;
    }
}
```

- [ ] **Step 5: Add board generator**

`Assets/SquareFlow/Scripts/Core/BoardGenerator.cs`:

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SquareFlow.Core
{
    public static class BoardGenerator
    {
        public static int GetMaxHp(int level)
        {
            int clamped = Mathf.Min(level, 20);
            return Mathf.Min(2 + Mathf.FloorToInt(clamped * 0.75f), 14);
        }

        public static BoardCell[,] Generate(BoardShape shape, int level, IFlowRandom random)
        {
            BoardCell[,] grid = new BoardCell[shape.Rows, shape.Cols];
            List<Vector2Int> active = new List<Vector2Int>();
            int maxHp = GetMaxHp(level);
            double power = Math.Max(0.25, Math.Min(level, 20) / 10.0);
            double[] thresholds = BuildThresholds(maxHp, power);

            for (int r = 0; r < shape.Rows; r++)
            for (int c = 0; c < shape.Cols; c++)
            {
                if (!shape.IsActive(r, c))
                {
                    grid[r, c] = BoardCell.Empty;
                    continue;
                }

                int hp = PickHp(thresholds, random.Value());
                FlowColor color = (FlowColor)random.Range(0, SquareFlowConstants.NormalColorCount);
                grid[r, c] = BoardCell.Normal(color, hp);
                active.Add(new Vector2Int(c, r));
            }

            PlaceBombs(grid, shape, active, random);
            return grid;
        }

        private static double[] BuildThresholds(int maxHp, double power)
        {
            double[] weights = new double[maxHp];
            double total = 0;
            for (int i = 0; i < maxHp; i++)
            {
                weights[i] = Math.Pow(i + 1, power);
                total += weights[i];
            }

            double[] thresholds = new double[maxHp];
            double cumulative = 0;
            for (int i = 0; i < maxHp; i++)
            {
                cumulative += weights[i] / total;
                thresholds[i] = cumulative;
            }
            return thresholds;
        }

        private static int PickHp(double[] thresholds, double value)
        {
            for (int i = 0; i < thresholds.Length; i++)
                if (value < thresholds[i])
                    return i + 1;
            return thresholds.Length;
        }

        private static void PlaceBombs(BoardCell[,] grid, BoardShape shape, List<Vector2Int> active, IFlowRandom random)
        {
            int bombCount = Mathf.Max(1, Mathf.FloorToInt(active.Count * 0.04f));
            float centerRow = shape.Rows / 2f;
            float centerCol = shape.Cols / 2f;
            active.Sort((a, b) =>
            {
                float da = Mathf.Abs(a.y - centerRow) + Mathf.Abs(a.x - centerCol);
                float db = Mathf.Abs(b.y - centerRow) + Mathf.Abs(b.x - centerCol);
                return da.CompareTo(db);
            });

            int candidateCount = Mathf.CeilToInt(active.Count * 0.4f);
            List<Vector2Int> candidates = active.GetRange(0, candidateCount);
            Shuffle(candidates, random);
            for (int i = 0; i < bombCount && i < candidates.Count; i++)
            {
                Vector2Int pos = candidates[i];
                grid[pos.y, pos.x] = BoardCell.Bomb();
            }
        }

        private static void Shuffle<T>(IList<T> list, IFlowRandom random)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
```

- [ ] **Step 6: Run tests and verify they pass**

Run the Unity edit-mode command from Task 1.

Expected: exit code 0 and all tests in `BoardGenerationTests` pass.

- [ ] **Step 7: Commit if git exists**

```powershell
git rev-parse --is-inside-work-tree
git add Assets/SquareFlow/Scripts/Core Assets/SquareFlow/Tests/EditMode
git commit -m "feat: generate square flow boards"
```

Expected in this project right now: first command reports this is not a git repository, so skip the add/commit commands.

---

## Task 3: Shooter Generation, Layout, And Targeting

**Files:**
- Create: `Assets/SquareFlow/Scripts/Core/ShooterGenerator.cs`
- Create: `Assets/SquareFlow/Scripts/Core/BoardLayout.cs`
- Create: `Assets/SquareFlow/Scripts/Core/TargetingSystem.cs`
- Create: `Assets/SquareFlow/Tests/EditMode/ShooterGenerationTests.cs`
- Create: `Assets/SquareFlow/Tests/EditMode/TargetingSystemTests.cs`

- [ ] **Step 1: Write failing tests for shooter generation**

```csharp
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SquareFlow.Core;

namespace SquareFlow.Tests
{
    public sealed class ShooterGenerationTests
    {
        [Test]
        public void ShooterGenerationCreatesThreeColumnsAndEnoughNormalAmmo()
        {
            BoardShape shape = BoardShapeCatalog.GetShape(1);
            BoardCell[,] grid = new BoardCell[shape.Rows, shape.Cols];
            for (int r = 0; r < shape.Rows; r++)
            for (int c = 0; c < shape.Cols; c++)
                grid[r, c] = shape.IsActive(r, c) ? BoardCell.Normal(FlowColor.Red, 1) : BoardCell.Empty;

            List<Shooter>[] columns = ShooterGenerator.BuildColumns(grid, shape, 1, new FixedFlowRandom(0.2));
            int redAmmo = columns.SelectMany(x => x).Where(s => !s.Wild && s.Color == FlowColor.Red).Sum(s => s.Ammo);

            Assert.That(columns.Length, Is.EqualTo(3));
            Assert.That(redAmmo, Is.GreaterThanOrEqualTo(shape.ActiveCellCount()));
            Assert.That(columns.SelectMany(x => x).Count(), Is.GreaterThan(3));
        }

        [Test]
        public void HiddenFlagOnlyAppearsAfterTheFrontShooter()
        {
            BoardShape shape = BoardShapeCatalog.GetShape(1);
            BoardCell[,] grid = BoardGenerator.Generate(shape, 5, new FixedFlowRandom(0.3));

            List<Shooter>[] columns = ShooterGenerator.BuildColumns(grid, shape, 5, new FixedFlowRandom(0.1));

            foreach (List<Shooter> column in columns)
                if (column.Count > 0)
                    Assert.That(column[0].Hidden, Is.False);
        }
    }
}
```

- [ ] **Step 2: Write failing tests for targeting from all four sides**

```csharp
using NUnit.Framework;
using SquareFlow.Core;

namespace SquareFlow.Tests
{
    public sealed class TargetingSystemTests
    {
        [Test]
        public void FindsFirstMatchingTargetFromTop()
        {
            BoardShape shape = new BoardShape("Test", BoardShape.Mask(new[]{1}, new[]{1}, new[]{1}));
            BoardCell[,] grid =
            {
                { BoardCell.Normal(FlowColor.Blue, 1) },
                { BoardCell.Normal(FlowColor.Red, 1) },
                { BoardCell.Normal(FlowColor.Red, 1) }
            };
            FirePoint point = new FirePoint(FireSide.Top, 0, -1, 0);

            TargetHit? hit = TargetingSystem.GetTarget(grid, shape, point, FlowColor.Red, false);

            Assert.That(hit.HasValue, Is.True);
            Assert.That(hit.Value.Row, Is.EqualTo(1));
            Assert.That(hit.Value.Col, Is.EqualTo(0));
        }

        [Test]
        public void WildTargetsFirstNormalCellRegardlessOfColor()
        {
            BoardShape shape = new BoardShape("Test", BoardShape.Mask(new[]{1,1,1}));
            BoardCell[,] grid = { { BoardCell.Normal(FlowColor.Blue, 1), BoardCell.Normal(FlowColor.Red, 1), BoardCell.Normal(FlowColor.Green, 1) } };
            FirePoint point = new FirePoint(FireSide.Left, 0, -1, 0);

            TargetHit? hit = TargetingSystem.GetTarget(grid, shape, point, FlowColor.Wild, true);

            Assert.That(hit.HasValue, Is.True);
            Assert.That(hit.Value.Col, Is.EqualTo(0));
        }

        [Test]
        public void BombIsAlwaysAValidTarget()
        {
            BoardShape shape = new BoardShape("Test", BoardShape.Mask(new[]{1,1}));
            BoardCell[,] grid = { { BoardCell.Bomb(), BoardCell.Normal(FlowColor.Green, 1) } };
            FirePoint point = new FirePoint(FireSide.Left, 0, -1, 0);

            TargetHit? hit = TargetingSystem.GetTarget(grid, shape, point, FlowColor.Red, false);

            Assert.That(hit.HasValue, Is.True);
            Assert.That(hit.Value.Special, Is.EqualTo(TargetSpecial.Bomb));
            Assert.That(hit.Value.Col, Is.EqualTo(0));
        }
    }
}
```

- [ ] **Step 3: Run tests and verify they fail because generator/layout/targeting do not exist**

Run the Unity edit-mode command from Task 1.

Expected: non-zero exit code with missing `ShooterGenerator`, `FirePoint`, and `TargetingSystem`.

- [ ] **Step 4: Add shooter generation**

`Assets/SquareFlow/Scripts/Core/ShooterGenerator.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace SquareFlow.Core
{
    public static class ShooterGenerator
    {
        public static List<Shooter>[] BuildColumns(BoardCell[,] grid, BoardShape shape, int level, IFlowRandom random)
        {
            int clamped = Mathf.Min(level, 20);
            int ammoBase = 2 + Mathf.FloorToInt(clamped * 0.7f);
            int ammoRange = 2 + Mathf.FloorToInt(clamped * 0.85f);
            float wildChance = Mathf.Max(0f, (clamped - 2) * 0.022f);
            int[] hpByColor = CountNormalHpByColor(grid, shape);
            List<Shooter> pool = new List<Shooter>();

            for (int color = 0; color < SquareFlowConstants.NormalColorCount; color++)
            {
                int remaining = hpByColor[color];
                while (remaining > 0)
                {
                    int ammo = Mathf.Min(random.Range(0, ammoRange) + ammoBase, remaining);
                    pool.Add(new Shooter(NewId(), (FlowColor)color, ammo, false));
                    remaining -= ammo;
                }
            }

            Shuffle(pool, random);
            for (int i = 0; i < SquareFlowConstants.ExtraShooterCount; i++)
            {
                bool wild = random.Value() < wildChance;
                FlowColor color = wild ? FlowColor.Wild : (FlowColor)random.Range(0, SquareFlowConstants.NormalColorCount);
                int ammo = random.Range(0, ammoRange) + ammoBase;
                pool.Add(new Shooter(NewId(), color, ammo, wild));
            }

            for (int i = 0; i < pool.Count; i++)
            {
                Shooter shooter = pool[i];
                if (!shooter.Wild && random.Value() < wildChance)
                    pool[i] = new Shooter(shooter.Id, FlowColor.Wild, shooter.Ammo, true, shooter.Hidden);
            }

            List<Shooter>[] columns = { new List<Shooter>(), new List<Shooter>(), new List<Shooter>() };
            for (int i = 0; i < pool.Count; i++)
            {
                int col = i % columns.Length;
                Shooter shooter = pool[i];
                bool hidden = columns[col].Count > 0 && random.Value() < 0.35f;
                columns[col].Add(new Shooter(shooter.Id, shooter.Color, shooter.Ammo, shooter.Wild, hidden));
            }
            return columns;
        }

        private static int[] CountNormalHpByColor(BoardCell[,] grid, BoardShape shape)
        {
            int[] counts = new int[SquareFlowConstants.NormalColorCount];
            for (int r = 0; r < shape.Rows; r++)
            for (int c = 0; c < shape.Cols; c++)
            {
                BoardCell cell = grid[r, c];
                if (shape.IsActive(r, c) && cell.Type == BoardCellType.Normal)
                    counts[(int)cell.Color] += cell.Hp;
            }
            return counts;
        }

        private static void Shuffle<T>(IList<T> list, IFlowRandom random)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private static string NewId()
        {
            return System.Guid.NewGuid().ToString("N");
        }
    }
}
```

- [ ] **Step 5: Add layout and targeting**

`Assets/SquareFlow/Scripts/Core/BoardLayout.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace SquareFlow.Core
{
    public enum FireSide { Top, Right, Bottom, Left }

    public readonly struct FirePoint
    {
        public FirePoint(FireSide side, int row, int col, float distance)
        {
            Side = side;
            Row = row;
            Col = col;
            Distance = distance;
        }

        public FireSide Side { get; }
        public int Row { get; }
        public int Col { get; }
        public float Distance { get; }
    }

    public sealed class BoardLayout
    {
        public const float Gap = 3f;

        private BoardLayout() {}

        public float Cell { get; private set; }
        public float Pad { get; private set; }
        public float Inset { get; private set; }
        public float GridWidth { get; private set; }
        public float GridHeight { get; private set; }
        public float CanvasWidth { get; private set; }
        public float CanvasHeight { get; private set; }
        public float OrbitX { get; private set; }
        public float OrbitY { get; private set; }
        public float OrbitWidth { get; private set; }
        public float OrbitHeight { get; private set; }
        public float Perimeter { get; private set; }
        public List<FirePoint> FirePoints { get; private set; }

        public static BoardLayout Compute(int rows, int cols, float availableWidth)
        {
            float usable = Mathf.Max(availableWidth - 24f, 200f);
            float rawCell = (usable - (cols - 1) * Gap) / (3.44f + cols);
            float capCell = cols <= 5 ? 52f : cols <= 8 ? 48f : cols <= 11 ? 40f : cols <= 13 ? 34f : 30f;
            BoardLayout layout = new BoardLayout();
            layout.Cell = Mathf.Min(capCell, Mathf.Max(12f, Mathf.Floor(rawCell)));
            layout.Pad = Mathf.Round(layout.Cell * 1.72f);
            layout.Inset = Mathf.Round(layout.Cell * 0.62f);
            layout.GridWidth = cols * (layout.Cell + Gap) - Gap;
            layout.GridHeight = rows * (layout.Cell + Gap) - Gap;
            layout.CanvasWidth = layout.Pad * 2f + layout.GridWidth;
            layout.CanvasHeight = layout.Pad * 2f + layout.GridHeight;
            layout.OrbitX = layout.Inset;
            layout.OrbitY = layout.Inset;
            layout.OrbitWidth = layout.CanvasWidth - 2f * layout.Inset;
            layout.OrbitHeight = layout.CanvasHeight - 2f * layout.Inset;
            layout.Perimeter = 2f * (layout.OrbitWidth + layout.OrbitHeight);
            layout.FirePoints = layout.BuildFirePoints(rows, cols);
            return layout;
        }

        public float CellCenterX(int col) => Pad + col * (Cell + Gap) + Cell / 2f;
        public float CellCenterY(int row) => Pad + row * (Cell + Gap) + Cell / 2f;

        public Vector2 PathPosition(float distance)
        {
            float d = Mathf.Repeat(distance, Perimeter);
            if (d < OrbitWidth) return new Vector2(OrbitX + d, OrbitY);
            d -= OrbitWidth;
            if (d < OrbitHeight) return new Vector2(OrbitX + OrbitWidth, OrbitY + d);
            d -= OrbitHeight;
            if (d < OrbitWidth) return new Vector2(OrbitX + OrbitWidth - d, OrbitY + OrbitHeight);
            d -= OrbitWidth;
            return new Vector2(OrbitX, OrbitY + OrbitHeight - d);
        }

        private List<FirePoint> BuildFirePoints(int rows, int cols)
        {
            List<FirePoint> points = new List<FirePoint>();
            for (int c = 0; c < cols; c++) points.Add(new FirePoint(FireSide.Top, -1, c, CellCenterX(c) - OrbitX));
            for (int r = 0; r < rows; r++) points.Add(new FirePoint(FireSide.Right, r, -1, OrbitWidth + (CellCenterY(r) - OrbitY)));
            for (int c = 0; c < cols; c++)
            {
                int col = cols - 1 - c;
                points.Add(new FirePoint(FireSide.Bottom, -1, col, OrbitWidth + OrbitHeight + (OrbitX + OrbitWidth - CellCenterX(col))));
            }
            for (int r = 0; r < rows; r++)
            {
                int row = rows - 1 - r;
                points.Add(new FirePoint(FireSide.Left, row, -1, 2f * OrbitWidth + OrbitHeight + (OrbitY + OrbitHeight - CellCenterY(row))));
            }
            points.Sort((a, b) => a.Distance.CompareTo(b.Distance));
            return points;
        }
    }
}
```

`Assets/SquareFlow/Scripts/Core/TargetingSystem.cs`:

```csharp
namespace SquareFlow.Core
{
    public enum TargetSpecial { None, Bomb }

    public readonly struct TargetHit
    {
        public TargetHit(int row, int col, TargetSpecial special)
        {
            Row = row;
            Col = col;
            Special = special;
        }

        public int Row { get; }
        public int Col { get; }
        public TargetSpecial Special { get; }
    }

    public static class TargetingSystem
    {
        public static TargetHit? GetTarget(BoardCell[,] grid, BoardShape shape, FirePoint point, FlowColor color, bool wild)
        {
            if (point.Side == FireSide.Top)
            {
                for (int r = 0; r < shape.Rows; r++)
                {
                    TargetHit? hit = Check(grid, shape, r, point.Col, color, wild);
                    if (hit.HasValue || BlocksLine(grid, shape, r, point.Col)) return hit;
                }
            }
            else if (point.Side == FireSide.Bottom)
            {
                for (int r = shape.Rows - 1; r >= 0; r--)
                {
                    TargetHit? hit = Check(grid, shape, r, point.Col, color, wild);
                    if (hit.HasValue || BlocksLine(grid, shape, r, point.Col)) return hit;
                }
            }
            else if (point.Side == FireSide.Right)
            {
                for (int c = shape.Cols - 1; c >= 0; c--)
                {
                    TargetHit? hit = Check(grid, shape, point.Row, c, color, wild);
                    if (hit.HasValue || BlocksLine(grid, shape, point.Row, c)) return hit;
                }
            }
            else
            {
                for (int c = 0; c < shape.Cols; c++)
                {
                    TargetHit? hit = Check(grid, shape, point.Row, c, color, wild);
                    if (hit.HasValue || BlocksLine(grid, shape, point.Row, c)) return hit;
                }
            }

            return null;
        }

        private static bool BlocksLine(BoardCell[,] grid, BoardShape shape, int row, int col)
        {
            return shape.IsActive(row, col) && grid[row, col].IsOccupied;
        }

        private static TargetHit? Check(BoardCell[,] grid, BoardShape shape, int row, int col, FlowColor color, bool wild)
        {
            if (!shape.IsActive(row, col)) return null;
            BoardCell cell = grid[row, col];
            if (!cell.IsOccupied) return null;
            if (cell.Type == BoardCellType.Bomb) return new TargetHit(row, col, TargetSpecial.Bomb);
            if (cell.Type == BoardCellType.Normal && (wild || cell.Color == color)) return new TargetHit(row, col, TargetSpecial.None);
            return null;
        }
    }
}
```

- [ ] **Step 6: Run tests and verify they pass**

Run the Unity edit-mode command from Task 1.

Expected: exit code 0 and all shooter/targeting tests pass.

- [ ] **Step 7: Commit if git exists**

```powershell
git rev-parse --is-inside-work-tree
git add Assets/SquareFlow/Scripts/Core Assets/SquareFlow/Tests/EditMode
git commit -m "feat: add shooter generation and targeting"
```

Expected in this project right now: first command reports this is not a git repository, so skip the add/commit commands.

---

## Task 4: Game State, Rules, Events, And Persistence

**Files:**
- Create: `Assets/SquareFlow/Scripts/Core/GameResult.cs`
- Create: `Assets/SquareFlow/Scripts/Core/GameEvent.cs`
- Create: `Assets/SquareFlow/Scripts/Core/GameState.cs`
- Create: `Assets/SquareFlow/Scripts/Core/GameRules.cs`
- Create: `Assets/SquareFlow/Scripts/Runtime/SaveDataService.cs`
- Create: `Assets/SquareFlow/Tests/EditMode/GameRulesTests.cs`

- [ ] **Step 1: Write failing tests for rules**

```csharp
using System.Collections.Generic;
using NUnit.Framework;
using SquareFlow.Core;

namespace SquareFlow.Tests
{
    public sealed class GameRulesTests
    {
        [Test]
        public void FireFromColumnRespectsMaxActiveLimit()
        {
            GameState state = MakeStateWithColumnShooters(6);
            GameRules rules = new GameRules(state, BoardLayout.Compute(1, 1, 320));

            for (int i = 0; i < SquareFlowConstants.MaxActiveOrbiters; i++)
                Assert.That(rules.FireFromColumn(0), Is.True);

            Assert.That(rules.FireFromColumn(0), Is.False);
            Assert.That(state.ActiveOrbiters.Count, Is.EqualTo(SquareFlowConstants.MaxActiveOrbiters));
        }

        [Test]
        public void BombClearsCenterAndNeighbors()
        {
            BoardShape shape = new BoardShape("Block", BoardShape.Mask(new[]{1,1,1}, new[]{1,1,1}, new[]{1,1,1}));
            BoardCell[,] grid = new BoardCell[3, 3];
            for (int r = 0; r < 3; r++)
            for (int c = 0; c < 3; c++)
                grid[r, c] = BoardCell.Normal(FlowColor.Red, 1);
            grid[1, 1] = BoardCell.Bomb();

            GameState state = GameState.Create(shape, grid, EmptyColumns(), 1);
            GameRules rules = new GameRules(state, BoardLayout.Compute(3, 3, 400));

            int cleared = rules.DetonateBomb(1, 1);

            Assert.That(cleared, Is.EqualTo(9));
            foreach (BoardCell cell in state.Grid) Assert.That(cell.IsOccupied, Is.False);
        }

        [Test]
        public void EmptyBoardWins()
        {
            BoardShape shape = new BoardShape("One", BoardShape.Mask(new[]{1}));
            GameState state = GameState.Create(shape, new[,] { { BoardCell.Empty } }, EmptyColumns(), 1);
            GameRules rules = new GameRules(state, BoardLayout.Compute(1, 1, 320));

            rules.CheckEndConditions();

            Assert.That(state.Result, Is.EqualTo(GameResult.Won));
        }

        [Test]
        public void FullWaitingQueueLoses()
        {
            GameState state = MakeStateWithColumnShooters(0);
            for (int i = 0; i < SquareFlowConstants.WaitQueueLimit; i++)
                state.WaitingQueue.Add(new Shooter("w" + i, FlowColor.Red, 1, false));
            GameRules rules = new GameRules(state, BoardLayout.Compute(1, 1, 320));

            rules.CheckEndConditions();

            Assert.That(state.Result, Is.EqualTo(GameResult.LostWait));
        }

        private static GameState MakeStateWithColumnShooters(int count)
        {
            BoardShape shape = new BoardShape("One", BoardShape.Mask(new[]{1}));
            BoardCell[,] grid = { { BoardCell.Normal(FlowColor.Red, 99) } };
            List<Shooter>[] columns = EmptyColumns();
            for (int i = 0; i < count; i++)
                columns[0].Add(new Shooter("s" + i, FlowColor.Red, 1, false));
            return GameState.Create(shape, grid, columns, 1);
        }

        private static List<Shooter>[] EmptyColumns()
        {
            return new[] { new List<Shooter>(), new List<Shooter>(), new List<Shooter>() };
        }
    }
}
```

- [ ] **Step 2: Run tests and verify they fail because rules do not exist**

Run the Unity edit-mode command from Task 1.

Expected: non-zero exit code with missing `GameState`, `GameRules`, and `GameResult`.

- [ ] **Step 3: Add state, events, and result types**

`Assets/SquareFlow/Scripts/Core/GameResult.cs`:

```csharp
namespace SquareFlow.Core
{
    public enum GameResult
    {
        None,
        Won,
        LostWait,
        LostOutOfShooters
    }
}
```

`Assets/SquareFlow/Scripts/Core/GameEvent.cs`:

```csharp
namespace SquareFlow.Core
{
    public enum GameEventType
    {
        Fired,
        BlockDamaged,
        BlockDestroyed,
        BombDetonated,
        OrbiterQueued,
        OrbiterRemoved,
        ResultChanged,
        Blocked
    }

    public readonly struct GameEvent
    {
        public GameEvent(GameEventType type, int row = -1, int col = -1, string orbiterId = null, int score = 0)
        {
            Type = type;
            Row = row;
            Col = col;
            OrbiterId = orbiterId;
            Score = score;
        }

        public GameEventType Type { get; }
        public int Row { get; }
        public int Col { get; }
        public string OrbiterId { get; }
        public int Score { get; }
    }
}
```

`Assets/SquareFlow/Scripts/Core/GameState.cs`:

```csharp
using System.Collections.Generic;
using System.Linq;

namespace SquareFlow.Core
{
    public sealed class GameState
    {
        private GameState() {}

        public BoardShape Shape { get; private set; }
        public BoardCell[,] Grid { get; private set; }
        public List<Shooter>[] ShooterColumns { get; private set; }
        public List<Shooter> WaitingQueue { get; } = new List<Shooter>();
        public List<ActiveOrbiter> ActiveOrbiters { get; } = new List<ActiveOrbiter>();
        public int Level { get; private set; }
        public int Moves { get; set; }
        public int Score { get; set; }
        public float Combo { get; set; } = 1f;
        public float ComboTimer { get; set; }
        public GameResult Result { get; set; }

        public static GameState Create(BoardShape shape, BoardCell[,] grid, List<Shooter>[] shooterColumns, int level)
        {
            return new GameState
            {
                Shape = shape,
                Grid = grid,
                ShooterColumns = shooterColumns,
                Level = level,
                Result = GameResult.None
            };
        }

        public bool AnyBlocksRemaining()
        {
            foreach (BoardCell cell in Grid)
                if (cell.IsOccupied)
                    return true;
            return false;
        }

        public bool HasAvailableShooters()
        {
            return ActiveOrbiters.Count > 0 || WaitingQueue.Count > 0 || ShooterColumns.Any(column => column.Count > 0);
        }
    }
}
```

- [ ] **Step 4: Add game rules**

`Assets/SquareFlow/Scripts/Core/GameRules.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace SquareFlow.Core
{
    public sealed class GameRules
    {
        private readonly GameState state;
        private readonly BoardLayout layout;

        public GameRules(GameState state, BoardLayout layout)
        {
            this.state = state;
            this.layout = layout;
        }

        public bool FireFromColumn(int columnIndex)
        {
            if (state.Result != GameResult.None || state.ActiveOrbiters.Count >= SquareFlowConstants.MaxActiveOrbiters) return false;
            if (columnIndex < 0 || columnIndex >= state.ShooterColumns.Length || state.ShooterColumns[columnIndex].Count == 0) return false;

            Shooter shooter = state.ShooterColumns[columnIndex][0];
            state.ShooterColumns[columnIndex].RemoveAt(0);
            RevealFrontShooter(columnIndex);
            FireShooter(shooter);
            return true;
        }

        public bool FireFromWaiting(int index)
        {
            if (state.Result != GameResult.None || state.ActiveOrbiters.Count >= SquareFlowConstants.MaxActiveOrbiters) return false;
            if (index < 0 || index >= state.WaitingQueue.Count) return false;

            Shooter shooter = state.WaitingQueue[index];
            state.WaitingQueue.RemoveAt(index);
            FireShooter(shooter);
            return true;
        }

        public List<GameEvent> Advance(float deltaSeconds)
        {
            List<GameEvent> events = new List<GameEvent>();
            if (state.Result != GameResult.None) return events;

            for (int i = state.ActiveOrbiters.Count - 1; i >= 0; i--)
            {
                ActiveOrbiter orbiter = state.ActiveOrbiters[i];
                float oldDistance = orbiter.Distance;
                orbiter.Distance += SquareFlowConstants.Speed * deltaSeconds;

                foreach (FirePoint point in layout.FirePoints)
                {
                    if (point.Distance <= oldDistance || point.Distance > orbiter.Distance || orbiter.Ammo <= 0) continue;
                    TargetHit? hit = TargetingSystem.GetTarget(state.Grid, state.Shape, point, orbiter.Color, orbiter.Wild);
                    if (!hit.HasValue) continue;
                    ApplyHit(orbiter, hit.Value, events);
                }

                if (orbiter.Distance >= layout.Perimeter || orbiter.Ammo <= 0)
                {
                    state.ActiveOrbiters.RemoveAt(i);
                    if (orbiter.Ammo > 0 && state.WaitingQueue.Count < SquareFlowConstants.WaitQueueLimit)
                        state.WaitingQueue.Add(new Shooter(System.Guid.NewGuid().ToString("N"), orbiter.Color, orbiter.Ammo, orbiter.Wild));
                    events.Add(new GameEvent(GameEventType.OrbiterRemoved, orbiterId: orbiter.Id));
                }
            }

            CheckEndConditions();
            return events;
        }

        public int DetonateBomb(int row, int col)
        {
            int cleared = 0;
            for (int dr = -1; dr <= 1; dr++)
            for (int dc = -1; dc <= 1; dc++)
            {
                int nr = row + dr;
                int nc = col + dc;
                if (!state.Shape.IsActive(nr, nc) || !state.Grid[nr, nc].IsOccupied) continue;
                state.Grid[nr, nc] = BoardCell.Empty;
                cleared++;
            }
            return cleared;
        }

        public void CheckEndConditions()
        {
            if (state.Result != GameResult.None) return;
            if (!state.AnyBlocksRemaining())
            {
                state.Result = GameResult.Won;
                return;
            }
            if (state.WaitingQueue.Count >= SquareFlowConstants.WaitQueueLimit)
            {
                state.Result = GameResult.LostWait;
                return;
            }
            if (!state.HasAvailableShooters())
            {
                state.Result = GameResult.LostOutOfShooters;
            }
        }

        public void UpdateCombo(float deltaSeconds)
        {
            if (state.ComboTimer <= 0f) return;
            state.ComboTimer -= deltaSeconds;
            if (state.ComboTimer <= 0f) state.Combo = 1f;
        }

        private void FireShooter(Shooter shooter)
        {
            state.Moves++;
            state.ActiveOrbiters.Add(new ActiveOrbiter(shooter));
        }

        private void RevealFrontShooter(int columnIndex)
        {
            if (state.ShooterColumns[columnIndex].Count == 0) return;
            Shooter front = state.ShooterColumns[columnIndex][0];
            state.ShooterColumns[columnIndex][0] = front.Revealed();
        }

        private void ApplyHit(ActiveOrbiter orbiter, TargetHit hit, List<GameEvent> events)
        {
            BoardCell cell = state.Grid[hit.Row, hit.Col];
            orbiter.Ammo--;
            if (hit.Special == TargetSpecial.Bomb)
            {
                int cleared = DetonateBomb(hit.Row, hit.Col);
                AddScore(150 + cleared * 50);
                events.Add(new GameEvent(GameEventType.BombDetonated, hit.Row, hit.Col));
                return;
            }

            int hp = cell.Hp - 1;
            if (hp <= 0)
            {
                state.Grid[hit.Row, hit.Col] = BoardCell.Empty;
                AddScore(Mathf.FloorToInt(100 * state.Level * (orbiter.Wild ? 1.5f : 1f)));
                events.Add(new GameEvent(GameEventType.BlockDestroyed, hit.Row, hit.Col));
            }
            else
            {
                state.Grid[hit.Row, hit.Col] = cell.WithHp(hp);
                events.Add(new GameEvent(GameEventType.BlockDamaged, hit.Row, hit.Col));
            }
        }

        private void AddScore(int basePoints)
        {
            float multiplier = state.Combo >= 2f ? state.Combo : 1f;
            state.Score += Mathf.FloorToInt(basePoints * multiplier);
            state.Combo = Mathf.Min(state.Combo + 0.5f, 7f);
            state.ComboTimer = SquareFlowConstants.ComboResetSeconds;
        }
    }
}
```

- [ ] **Step 5: Add PlayerPrefs persistence service**

`Assets/SquareFlow/Scripts/Runtime/SaveDataService.cs`:

```csharp
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SquareFlow.Runtime
{
    public sealed class SaveDataService
    {
        private const string LevelKey = "sf-unity-level";
        private const string CompletedKey = "sf-unity-completed";
        private const string ScoresKey = "sf-unity-scores";
        private const string DarkKey = "sf-unity-dark";
        private const string MutedKey = "sf-unity-muted";

        public int Level
        {
            get => Mathf.Max(1, PlayerPrefs.GetInt(LevelKey, 1));
            set => PlayerPrefs.SetInt(LevelKey, Mathf.Max(1, value));
        }

        public bool DarkMode
        {
            get => PlayerPrefs.GetInt(DarkKey, 1) == 1;
            set => PlayerPrefs.SetInt(DarkKey, value ? 1 : 0);
        }

        public bool Muted
        {
            get => PlayerPrefs.GetInt(MutedKey, 0) == 1;
            set => PlayerPrefs.SetInt(MutedKey, value ? 1 : 0);
        }

        public HashSet<int> CompletedLevels()
        {
            string data = PlayerPrefs.GetString(CompletedKey, string.Empty);
            return data.Split(',').Where(x => int.TryParse(x, out _)).Select(int.Parse).ToHashSet();
        }

        public void SaveCompletedLevels(HashSet<int> levels)
        {
            PlayerPrefs.SetString(CompletedKey, string.Join(",", levels.OrderBy(x => x)));
        }

        public void ClearProgress()
        {
            PlayerPrefs.DeleteKey(LevelKey);
            PlayerPrefs.DeleteKey(CompletedKey);
            PlayerPrefs.DeleteKey(ScoresKey);
        }
    }
}
```

- [ ] **Step 6: Run tests and verify they pass**

Run the Unity edit-mode command from Task 1.

Expected: exit code 0 and all `GameRulesTests` pass.

- [ ] **Step 7: Commit if git exists**

```powershell
git rev-parse --is-inside-work-tree
git add Assets/SquareFlow/Scripts Assets/SquareFlow/Tests/EditMode
git commit -m "feat: add square flow game rules"
```

Expected in this project right now: first command reports this is not a git repository, so skip the add/commit commands.

---

## Task 5: Native Unity UI Controller And Theme

**Files:**
- Create: `Assets/SquareFlow/Scripts/UI/SquareFlowTheme.cs`
- Create: `Assets/SquareFlow/Scripts/UI/SquareFlowGameController.cs`
- Create: `Assets/SquareFlow/Scripts/Effects/SquareFlowAudio.cs`

- [ ] **Step 1: Add a minimal theme palette**

`Assets/SquareFlow/Scripts/UI/SquareFlowTheme.cs`:

```csharp
using UnityEngine;

namespace SquareFlow.UI
{
    public readonly struct SquareFlowTheme
    {
        public SquareFlowTheme(bool dark)
        {
            Background = dark ? new Color32(15, 12, 41, 255) : new Color32(220, 232, 250, 255);
            Panel = dark ? new Color32(38, 34, 80, 230) : new Color32(255, 255, 255, 235);
            Text = dark ? Color.white : new Color32(26, 23, 64, 255);
            SubtleText = dark ? new Color32(180, 180, 210, 255) : new Color32(80, 70, 130, 255);
            Score = dark ? new Color32(236, 201, 75, 255) : new Color32(122, 85, 0, 255);
            Red = new Color32(255, 107, 107, 255);
            Blue = new Color32(66, 153, 225, 255);
            Yellow = new Color32(236, 201, 75, 255);
            Green = new Color32(72, 187, 120, 255);
            Wild = new Color32(226, 232, 240, 255);
            Bomb = new Color32(249, 202, 36, 255);
        }

        public Color Background { get; }
        public Color Panel { get; }
        public Color Text { get; }
        public Color SubtleText { get; }
        public Color Score { get; }
        public Color Red { get; }
        public Color Blue { get; }
        public Color Yellow { get; }
        public Color Green { get; }
        public Color Wild { get; }
        public Color Bomb { get; }
    }
}
```

- [ ] **Step 2: Add generated audio cue component**

`Assets/SquareFlow/Scripts/Effects/SquareFlowAudio.cs`:

```csharp
using UnityEngine;

namespace SquareFlow.Effects
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class SquareFlowAudio : MonoBehaviour
    {
        private AudioSource source;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
        }

        public void PlayTone(float frequency, float duration, float volume)
        {
            if (source == null) source = GetComponent<AudioSource>();
            source.PlayOneShot(CreateTone(frequency, duration), volume);
        }

        private static AudioClip CreateTone(float frequency, float duration)
        {
            int sampleRate = 44100;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)sampleRate;
                float envelope = 1f - i / (float)samples;
                data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope;
            }
            AudioClip clip = AudioClip.Create("SquareFlowTone", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
```

- [ ] **Step 3: Add UI controller that builds menu and game views at runtime**

`Assets/SquareFlow/Scripts/UI/SquareFlowGameController.cs`:

```csharp
using System.Collections.Generic;
using SquareFlow.Core;
using SquareFlow.Effects;
using SquareFlow.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace SquareFlow.UI
{
    public sealed class SquareFlowGameController : MonoBehaviour
    {
        private readonly List<GameObject> dynamicObjects = new List<GameObject>();
        private SaveDataService save;
        private SquareFlowAudio audioCue;
        private Canvas canvas;
        private RectTransform root;
        private GameState state;
        private GameRules rules;
        private BoardLayout layout;
        private bool inGame;
        private bool muted;
        private bool darkMode;

        private void Awake()
        {
            save = new SaveDataService();
            darkMode = save.DarkMode;
            muted = save.Muted;
            audioCue = gameObject.GetComponent<SquareFlowAudio>() ?? gameObject.AddComponent<SquareFlowAudio>();
            gameObject.AddComponent<AudioSource>();
            BuildCanvas();
            ShowMenu();
        }

        private void Update()
        {
            if (!inGame || state == null || rules == null) return;
            rules.UpdateCombo(Time.deltaTime);
            List<GameEvent> events = rules.Advance(Time.deltaTime);
            if (events.Count > 0) RefreshGame();
            if (state.Result != GameResult.None) ShowResult();
        }

        private void BuildCanvas()
        {
            GameObject canvasObject = new GameObject("SquareFlowCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(900, 1600);
            scaler.matchWidthOrHeight = 0.5f;
            root = canvasObject.GetComponent<RectTransform>();
        }

        private void Clear()
        {
            foreach (GameObject obj in dynamicObjects)
                if (obj != null) Destroy(obj);
            dynamicObjects.Clear();
        }

        private void ShowMenu()
        {
            inGame = false;
            Clear();
            SquareFlowTheme theme = new SquareFlowTheme(darkMode);
            Image bg = AddPanel("MenuBackground", root, theme.Background);
            Stretch(bg.rectTransform);
            VerticalLayoutGroup stack = AddStack("MenuStack", root, 14, TextAnchor.MiddleCenter);
            Stretch(stack.GetComponent<RectTransform>(), 36, 36, 36, 36);

            AddText(stack.transform, "Square Flow", 46, FontStyle.Bold, theme.Text);
            BoardShape shape = BoardShapeCatalog.GetShape(save.Level);
            AddText(stack.transform, $"Level {save.Level} - {shape.Name}", 24, FontStyle.Bold, theme.Score);
            AddButton(stack.transform, "Play", theme, StartGame);
            AddButton(stack.transform, darkMode ? "Light Mode" : "Dark Mode", theme, ToggleTheme);
            AddButton(stack.transform, muted ? "Unmute" : "Mute", theme, ToggleMute);
            AddButton(stack.transform, "Reset Progress", theme, ResetProgress);
            AddText(stack.transform, "Orbit color shooters around shaped boards. Clear every block before the queue fills.", 20, FontStyle.Normal, theme.SubtleText);
        }

        private void StartGame()
        {
            BoardShape shape = BoardShapeCatalog.GetShape(save.Level);
            BoardCell[,] grid = BoardGenerator.Generate(shape, save.Level, new SystemFlowRandom());
            List<Shooter>[] columns = ShooterGenerator.BuildColumns(grid, shape, save.Level, new SystemFlowRandom());
            state = GameState.Create(shape, grid, columns, save.Level);
            layout = BoardLayout.Compute(shape.Rows, shape.Cols, Mathf.Min(Screen.width, 900));
            rules = new GameRules(state, layout);
            inGame = true;
            RefreshGame();
        }

        private void RefreshGame()
        {
            Clear();
            SquareFlowTheme theme = new SquareFlowTheme(darkMode);
            Image bg = AddPanel("GameBackground", root, theme.Background);
            Stretch(bg.rectTransform);
            VerticalLayoutGroup stack = AddStack("GameStack", root, 8, TextAnchor.UpperCenter);
            Stretch(stack.GetComponent<RectTransform>(), 20, 20, 20, 20);

            AddHud(stack.transform, theme);
            AddBoard(stack.transform, theme);
            AddQueue(stack.transform, theme);
            AddColumns(stack.transform, theme);
        }

        private void AddHud(Transform parent, SquareFlowTheme theme)
        {
            AddText(parent, $"Square Flow  Lv {state.Level}  Score {state.Score}  Moves {state.Moves}", 22, FontStyle.Bold, theme.Text);
            AddText(parent, $"Orbiters {state.ActiveOrbiters.Count}/{SquareFlowConstants.MaxActiveOrbiters}  Waiting {state.WaitingQueue.Count}/{SquareFlowConstants.WaitQueueLimit}", 18, FontStyle.Bold, theme.SubtleText);
        }

        private void AddBoard(Transform parent, SquareFlowTheme theme)
        {
            GridLayoutGroup grid = AddGrid("Board", parent, state.Shape.Cols, 5, new Vector2(42, 42));
            for (int r = 0; r < state.Shape.Rows; r++)
            for (int c = 0; c < state.Shape.Cols; c++)
            {
                BoardCell cell = state.Grid[r, c];
                Image image = AddPanel($"Cell_{r}_{c}", grid.transform, CellColor(cell, theme));
                image.rectTransform.sizeDelta = new Vector2(42, 42);
                if (state.Shape.IsActive(r, c) && cell.IsOccupied)
                    AddText(image.transform, CellLabel(cell), 18, FontStyle.Bold, cell.Type == BoardCellType.Bomb ? Color.black : Color.white);
            }
        }

        private void AddQueue(Transform parent, SquareFlowTheme theme)
        {
            HorizontalLayoutGroup row = AddRow("WaitingQueue", parent, 6);
            AddText(row.transform, "Waiting", 18, FontStyle.Bold, theme.SubtleText);
            for (int i = 0; i < state.WaitingQueue.Count; i++)
            {
                int index = i;
                AddShooterButton(row.transform, state.WaitingQueue[i], theme, () =>
                {
                    if (rules.FireFromWaiting(index)) Play(520);
                    RefreshGame();
                });
            }
        }

        private void AddColumns(Transform parent, SquareFlowTheme theme)
        {
            HorizontalLayoutGroup columns = AddRow("ShooterColumns", parent, 12);
            for (int c = 0; c < state.ShooterColumns.Length; c++)
            {
                int column = c;
                VerticalLayoutGroup stack = AddStack("Column" + c, columns.transform, 4, TextAnchor.UpperCenter);
                AddText(stack.transform, ((char)('A' + c)).ToString(), 16, FontStyle.Bold, theme.SubtleText);
                foreach (Shooter shooter in state.ShooterColumns[c])
                {
                    if (shooter.Hidden) AddText(stack.transform, "?", 24, FontStyle.Bold, theme.SubtleText);
                    else AddShooterButton(stack.transform, shooter, theme, () =>
                    {
                        if (rules.FireFromColumn(column)) Play(shooter.Wild ? 900 : 340);
                        RefreshGame();
                    });
                }
            }
        }

        private void ShowResult()
        {
            if (state.Result == GameResult.Won)
            {
                save.Level = state.Level + 1;
                Play(784);
            }
            else
            {
                Play(180);
            }
            inGame = false;
            SquareFlowTheme theme = new SquareFlowTheme(darkMode);
            AddText(root, state.Result == GameResult.Won ? "Cleared!" : "Game Over", 36, FontStyle.Bold, theme.Score);
            AddButton(root, state.Result == GameResult.Won ? "Next Level" : "Play Again", theme, StartGame);
            AddButton(root, "Menu", theme, ShowMenu);
        }

        private void ToggleTheme()
        {
            darkMode = !darkMode;
            save.DarkMode = darkMode;
            ShowMenu();
        }

        private void ToggleMute()
        {
            muted = !muted;
            save.Muted = muted;
            ShowMenu();
        }

        private void ResetProgress()
        {
            save.ClearProgress();
            ShowMenu();
        }

        private void Play(float frequency)
        {
            if (!muted && audioCue != null) audioCue.PlayTone(frequency, 0.12f, 0.18f);
        }

        private Color CellColor(BoardCell cell, SquareFlowTheme theme)
        {
            if (!cell.IsOccupied) return new Color(0, 0, 0, 0.15f);
            if (cell.Type == BoardCellType.Bomb) return theme.Bomb;
            return cell.Color switch
            {
                FlowColor.Red => theme.Red,
                FlowColor.Blue => theme.Blue,
                FlowColor.Yellow => theme.Yellow,
                FlowColor.Green => theme.Green,
                _ => theme.Wild
            };
        }

        private static string CellLabel(BoardCell cell)
        {
            if (cell.Type == BoardCellType.Bomb) return "B";
            return cell.Hp > 1 ? cell.Hp.ToString() : string.Empty;
        }

        private void AddShooterButton(Transform parent, Shooter shooter, SquareFlowTheme theme, UnityEngine.Events.UnityAction action)
        {
            Button button = AddButton(parent, shooter.Wild ? $"* {shooter.Ammo}" : shooter.Ammo.ToString(), theme, action);
            button.image.color = shooter.Wild ? theme.Wild : CellColor(BoardCell.Normal(shooter.Color, 1), theme);
        }

        private Image AddPanel(string name, Transform parent, Color color)
        {
            GameObject obj = new GameObject(name, typeof(Image));
            obj.transform.SetParent(parent, false);
            dynamicObjects.Add(obj);
            Image image = obj.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private Text AddText(Transform parent, string value, int size, FontStyle style, Color color)
        {
            GameObject obj = new GameObject("Text", typeof(Text));
            obj.transform.SetParent(parent, false);
            dynamicObjects.Add(obj);
            Text text = obj.GetComponent<Text>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private Button AddButton(Transform parent, string label, SquareFlowTheme theme, UnityEngine.Events.UnityAction action)
        {
            GameObject obj = new GameObject("Button", typeof(Image), typeof(Button));
            obj.transform.SetParent(parent, false);
            dynamicObjects.Add(obj);
            Image image = obj.GetComponent<Image>();
            image.color = theme.Panel;
            Button button = obj.GetComponent<Button>();
            button.onClick.AddListener(action);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(220, 56);
            AddText(obj.transform, label, 22, FontStyle.Bold, theme.Text);
            return button;
        }

        private VerticalLayoutGroup AddStack(string name, Transform parent, float spacing, TextAnchor alignment)
        {
            GameObject obj = new GameObject(name, typeof(VerticalLayoutGroup));
            obj.transform.SetParent(parent, false);
            dynamicObjects.Add(obj);
            VerticalLayoutGroup group = obj.GetComponent<VerticalLayoutGroup>();
            group.spacing = spacing;
            group.childAlignment = alignment;
            group.childControlHeight = false;
            group.childControlWidth = true;
            return group;
        }

        private HorizontalLayoutGroup AddRow(string name, Transform parent, float spacing)
        {
            GameObject obj = new GameObject(name, typeof(HorizontalLayoutGroup));
            obj.transform.SetParent(parent, false);
            dynamicObjects.Add(obj);
            HorizontalLayoutGroup group = obj.GetComponent<HorizontalLayoutGroup>();
            group.spacing = spacing;
            group.childAlignment = TextAnchor.MiddleCenter;
            group.childControlHeight = false;
            group.childControlWidth = false;
            return group;
        }

        private GridLayoutGroup AddGrid(string name, Transform parent, int cols, float spacing, Vector2 cellSize)
        {
            GameObject obj = new GameObject(name, typeof(GridLayoutGroup));
            obj.transform.SetParent(parent, false);
            dynamicObjects.Add(obj);
            GridLayoutGroup group = obj.GetComponent<GridLayoutGroup>();
            group.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            group.constraintCount = cols;
            group.spacing = new Vector2(spacing, spacing);
            group.cellSize = cellSize;
            group.childAlignment = TextAnchor.MiddleCenter;
            return group;
        }

        private static void Stretch(RectTransform rect, float left = 0, float right = 0, float top = 0, float bottom = 0)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }
    }
}
```

- [ ] **Step 4: Compile check**

Run:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.15f1\Editor\Unity.exe" -batchmode -projectPath "C:\Users\samet\My project" -quit -logFile "C:\Users\samet\My project\Logs\compile-check.log"
```

Expected: exit code 0. `Logs\compile-check.log` contains no C# compiler errors.

- [ ] **Step 5: Commit if git exists**

```powershell
git rev-parse --is-inside-work-tree
git add Assets/SquareFlow/Scripts/UI Assets/SquareFlow/Scripts/Effects Assets/SquareFlow/Scripts/Runtime
git commit -m "feat: add square flow native ui"
```

Expected in this project right now: first command reports this is not a git repository, so skip the add/commit commands.

---

## Task 6: Scene Builder And SampleScene Wiring

**Files:**
- Create: `Assets/SquareFlow/Editor/SquareFlowSceneBuilder.cs`
- Modify: `Assets/Scenes/SampleScene.unity`

- [ ] **Step 1: Add editor scene builder**

`Assets/SquareFlow/Editor/SquareFlowSceneBuilder.cs`:

```csharp
using SquareFlow.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace SquareFlow.Editor
{
    public static class SquareFlowSceneBuilder
    {
        [MenuItem("Square Flow/Rebuild Scene")]
        public static void RebuildScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            foreach (GameObject root in scene.GetRootGameObjects())
                Object.DestroyImmediate(root);

            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(15, 12, 41, 255);
            camera.orthographic = true;
            camera.orthographicSize = 5f;

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            GameObject rootObject = new GameObject("SquareFlowRoot", typeof(SquareFlowGameController));

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = rootObject;
        }
    }
}
```

- [ ] **Step 2: Run scene builder from Unity menu**

Use the MCP menu tool or the Unity Editor menu:

```text
Square Flow/Rebuild Scene
```

Expected: `SampleScene` root objects become `Main Camera`, `EventSystem`, and `SquareFlowRoot`.

- [ ] **Step 3: Save scene and project**

Use Unity menu:

```text
File/Save
File/Save Project
```

Expected: `Assets/Scenes/SampleScene.unity` is saved and contains the new root objects.

- [ ] **Step 4: Compile and scene-load check**

Run:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.15f1\Editor\Unity.exe" -batchmode -projectPath "C:\Users\samet\My project" -quit -logFile "C:\Users\samet\My project\Logs\scene-check.log"
```

Expected: exit code 0. The log contains no compiler errors and no scene load exceptions.

- [ ] **Step 5: Commit if git exists**

```powershell
git rev-parse --is-inside-work-tree
git add Assets/SquareFlow/Editor Assets/Scenes/SampleScene.unity Assets/Scenes/SampleScene.unity.meta
git commit -m "feat: wire square flow scene"
```

Expected in this project right now: first command reports this is not a git repository, so skip the add/commit commands.

---

## Task 7: Gameplay Polish And Verification

**Files:**
- Modify: `Assets/SquareFlow/Scripts/UI/SquareFlowGameController.cs`
- Modify: `Assets/SquareFlow/Scripts/Effects/SquareFlowAudio.cs`
- Modify: `Assets/SquareFlow/Scripts/Core/GameRules.cs`

- [ ] **Step 1: Replace the board view with an absolute board, orbit ring, and moving orbiters**

Replace `SquareFlowGameController.AddBoard` with this absolute-positioned version, and keep the existing `CellColor` and `CellLabel` helpers:

```csharp
private void AddBoard(Transform parent, SquareFlowTheme theme)
{
    Image boardImage = AddPanel("BoardArea", parent, new Color(theme.Panel.r, theme.Panel.g, theme.Panel.b, 0.55f));
    RectTransform board = boardImage.rectTransform;
    board.sizeDelta = new Vector2(layout.CanvasWidth, layout.CanvasHeight);

    Image ring = AddPanel("OrbitRing", board, new Color(theme.SubtleText.r, theme.SubtleText.g, theme.SubtleText.b, 0.16f));
    RectTransform ringRect = ring.rectTransform;
    ringRect.anchorMin = new Vector2(0, 1);
    ringRect.anchorMax = new Vector2(0, 1);
    ringRect.pivot = new Vector2(0, 1);
    ringRect.anchoredPosition = new Vector2(layout.OrbitX, -layout.OrbitY);
    ringRect.sizeDelta = new Vector2(layout.OrbitWidth, layout.OrbitHeight);

    for (int r = 0; r < state.Shape.Rows; r++)
    for (int c = 0; c < state.Shape.Cols; c++)
    {
        if (!state.Shape.IsActive(r, c)) continue;
        BoardCell cell = state.Grid[r, c];
        Image cellImage = AddPanel($"Cell_{r}_{c}", board, CellColor(cell, theme));
        RectTransform rect = cellImage.rectTransform;
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(layout.Cell, layout.Cell);
        rect.anchoredPosition = new Vector2(layout.CellCenterX(c), -layout.CellCenterY(r));
        if (cell.IsOccupied) AddText(cellImage.transform, CellLabel(cell), Mathf.RoundToInt(layout.Cell * 0.42f), FontStyle.Bold, cell.Type == BoardCellType.Bomb ? Color.black : Color.white);
    }

    foreach (ActiveOrbiter orbiter in state.ActiveOrbiters)
    {
        Vector2 pos = layout.PathPosition(Mathf.Min(orbiter.Distance, layout.Perimeter - 0.1f));
        Image orbiterImage = AddPanel("Orbiter_" + orbiter.Id, board, orbiter.Wild ? theme.Wild : theme.Score);
        RectTransform rect = orbiterImage.rectTransform;
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(38, 38);
        rect.anchoredPosition = new Vector2(pos.x, -pos.y);
        AddText(orbiterImage.transform, orbiter.Wild ? $"*{orbiter.Ammo}" : orbiter.Ammo.ToString(), 16, FontStyle.Bold, orbiter.Wild ? Color.black : Color.white);
    }
}
```

Expected: the board area shows the orbit rectangle, shaped cells, and active orbiters moving around the perimeter as `RefreshGame` runs after rule events.

- [ ] **Step 2: Add game-screen controls**

In `SquareFlowGameController.AddHud`, append a control row after the two text lines:

```csharp
HorizontalLayoutGroup controls = AddRow("GameControls", parent, 6);
AddButton(controls.transform, "Menu", theme, ShowMenu);
AddButton(controls.transform, "Restart", theme, StartGame);
AddButton(controls.transform, darkMode ? "Light" : "Dark", theme, () =>
{
    darkMode = !darkMode;
    save.DarkMode = darkMode;
    RefreshGame();
});
AddButton(controls.transform, muted ? "Sound" : "Mute", theme, () =>
{
    muted = !muted;
    save.Muted = muted;
    RefreshGame();
});
```

Expected: while playing, the user can return to the menu, restart the level, toggle theme, and toggle mute.

- [ ] **Step 3: Add ten-level selector to the menu**

In `SquareFlowGameController.ShowMenu`, after the current-level text and before the Play button, add:

```csharp
HorizontalLayoutGroup levelRow = AddRow("LevelSelector", stack.transform, 4);
for (int i = 1; i <= BoardShapeCatalog.Count; i++)
{
    int selectedLevel = i;
    Button levelButton = AddButton(levelRow.transform, selectedLevel.ToString(), theme, () =>
    {
        save.Level = selectedLevel;
        ShowMenu();
    });
    levelButton.GetComponent<RectTransform>().sizeDelta = new Vector2(52, 46);
}
```

Expected: the menu exposes levels 1 through 10 and selecting a number updates the current board before pressing Play.

- [ ] **Step 4: Add loss/win persistence and completed level saving**

In `SaveDataService`, add:

```csharp
public void MarkCompleted(int level)
{
    HashSet<int> levels = CompletedLevels();
    levels.Add(level);
    SaveCompletedLevels(levels);
}
```

In `SquareFlowGameController.ShowResult`, replace the win branch with:

```csharp
if (state.Result == GameResult.Won)
{
    save.MarkCompleted(state.Level);
    save.Level = state.Level + 1;
    Play(784);
}
else
{
    Play(180);
}
```

Expected: winning advances the current level and persists completed-level state.

- [ ] **Step 5: Add leaderboard persistence**

In `SaveDataService`, add this serializable struct and methods:

```csharp
[System.Serializable]
public struct ScoreEntry
{
    public int level;
    public int moves;
    public int score;
}

[System.Serializable]
private sealed class ScoreList
{
    public List<ScoreEntry> entries = new List<ScoreEntry>();
}

public List<ScoreEntry> Scores()
{
    string json = PlayerPrefs.GetString(ScoresKey, "{\"entries\":[]}");
    return JsonUtility.FromJson<ScoreList>(json).entries;
}

public void AddScore(int level, int moves, int score)
{
    ScoreList list = new ScoreList { entries = Scores() };
    list.entries.Add(new ScoreEntry { level = level, moves = moves, score = score });
    list.entries = list.entries.OrderByDescending(x => x.score).Take(10).ToList();
    PlayerPrefs.SetString(ScoresKey, JsonUtility.ToJson(list));
}
```

In `SquareFlowGameController.ShowResult`, inside the win branch before advancing level, add:

```csharp
save.AddScore(state.Level, state.Moves, state.Score);
```

In `ShowMenu`, after the instruction text, add:

```csharp
foreach (SaveDataService.ScoreEntry score in save.Scores())
{
    AddText(stack.transform, $"{score.score:n0}  Lv{score.level}  {score.moves}m", 18, FontStyle.Bold, theme.Score);
}
```

Expected: wins are listed on the menu sorted by score and capped at ten entries.

- [ ] **Step 6: Run full edit-mode tests**

Run:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.15f1\Editor\Unity.exe" -batchmode -projectPath "C:\Users\samet\My project" -runTests -testPlatform EditMode -testResults "C:\Users\samet\My project\TestResults.xml" -quit -logFile "C:\Users\samet\My project\Logs\editmode-tests.log"
```

Expected: exit code 0, all edit-mode tests pass, and `TestResults.xml` reports zero failures.

- [ ] **Step 7: Run Unity compile check**

Run:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.15f1\Editor\Unity.exe" -batchmode -projectPath "C:\Users\samet\My project" -quit -logFile "C:\Users\samet\My project\Logs\compile-check.log"
```

Expected: exit code 0 with no compiler errors.

- [ ] **Step 8: Run Play Mode smoke check in editor**

Use Unity MCP:

```text
Unity_ManageEditor Action=Play WaitForCompletion=true
Unity_ReadConsole Action=Get Types=Error Format=Detailed Count=20
Unity_ManageEditor Action=Stop WaitForCompletion=true
```

Expected: Play Mode starts, no console errors are returned, and stopping Play Mode succeeds.

- [ ] **Step 9: Manual interaction checklist**

In Play Mode:

- [ ] Menu appears with title, level, Play, theme toggle, mute, reset, and instructions.
- [ ] Menu level selector changes the current board between the ten source shapes.
- [ ] Play starts level 1 with a Diamond board.
- [ ] Game screen has Menu, Restart, theme, and mute controls.
- [ ] Clicking the front shooter in a column increments moves and creates an active orbiter.
- [ ] Active orbiters are visible around the board orbit ring.
- [ ] Orbiters hit matching blocks, reduce HP, clear blocks, and score points.
- [ ] Bomb blocks clear a 3x3 area.
- [ ] Waiting queue fills from orbiters with leftover ammo.
- [ ] Five waiting shooters triggers wait-loss.
- [ ] Clearing all blocks triggers win and advances level.
- [ ] Theme and mute toggles persist after leaving Play Mode.

- [ ] **Step 10: Commit if git exists**

```powershell
git rev-parse --is-inside-work-tree
git add Assets/SquareFlow Assets/Scenes/SampleScene.unity
git commit -m "feat: polish square flow gameplay"
```

Expected in this project right now: first command reports this is not a git repository, so skip the add/commit commands.

---

## Final Verification

- [ ] Run all edit-mode tests with Unity batchmode.
- [ ] Run Unity compile check with batchmode.
- [ ] Enter Play Mode through Unity MCP and confirm no console errors.
- [ ] Use `Unity_ManageScene Action=GetHierarchy Depth=1` and confirm `Main Camera`, `EventSystem`, and `SquareFlowRoot`.
- [ ] Confirm `Assets/index.html` remains available as the source reference and is not used at runtime.

Expected final state: the Unity project launches a native Square Flow remake from `SampleScene`, with tested core logic, generated UI, persistence, and no console errors.
