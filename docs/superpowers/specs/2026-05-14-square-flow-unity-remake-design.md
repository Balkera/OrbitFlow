# Square Flow Unity Remake Design

Date: 2026-05-14
Project: C:\Users\samet\My project
Source reference: Assets\index.html

## Goal

Remake the provided HTML game, Square Flow, as a native Unity game in the existing Unity 6000.3 project. The remake should preserve the original rules, level structure, scoring, and visual flow while using Unity C# systems, Unity UI, and 2D presentation rather than embedding the HTML file.

## Source Game Summary

Square Flow is a 2D puzzle/action game. The player fires colored shooter balls from three columns. Fired shooters orbit around a shaped block board and fire inward whenever they pass a row or column line that contains a valid target. Normal shooters hit only matching colors. Wild shooters hit any normal color. Bomb blocks can be hit by any shooter and clear a 3x3 area.

The player wins by clearing every block. The player loses when the waiting queue reaches five shooters or when no active, queued, or column shooters remain. Up to five orbiters can be active at once. Shooters that finish an orbit with leftover ammo move into the waiting queue.

## Recommended Approach

Build a native Unity 2D/UI remake in the existing SampleScene.

This approach gives a faithful port of the HTML mechanics while keeping the code maintainable. The game logic will be isolated from Unity presentation where practical, with C# model classes covered by edit-mode tests. Runtime scripts will bind that logic to Unity UI, animation, effects, audio, and persistence.

## Scope

The first complete Unity version includes:

- Menu screen with title, current level, board name, max orbiters, level selector, reset, play, light/dark toggle, mute, and leaderboard.
- Game screen with HUD, board, orbit ring, active orbiter slots, waiting queue, three shooter columns, result panel, restart, and menu controls.
- All ten source board shapes: Diamond, Dino, Heart, Pizza, Smiley, Fish, Skull, Tree, Hourglass, Crown.
- Four normal block colors, HP blocks, bomb blocks, wild shooters, hidden non-front shooters, active orbiter limit, and waiting queue limit.
- Matching score, move, combo, level-unlock, save, reset, and leaderboard behavior.
- Native effects approximating the HTML version: orb glows, shots, hit flash, damage shake, bomb flash, particle pops, score popups, and simple generated audio cues.

Out of scope for the first pass:

- Online leaderboard or network sharing.
- Exact browser CSS rendering parity.
- Reusing React, JavaScript, or a web view.
- New levels or mechanics beyond the HTML source.

## Scene Architecture

The existing SampleScene will become the Square Flow scene. The default cube will be removed. The scene will contain:

- Main Camera configured for 2D orthographic presentation.
- EventSystem for UI input.
- Canvas using Screen Space Overlay or Screen Space Camera, depending on what best fits responsive board scaling.
- SquareFlowRoot object containing game coordinator scripts.
- BoardArea container for generated cell views, orbit ring, orbiters, shots, and effects.
- UI containers for menu, HUD, queues, shooter columns, result panel, and leaderboard.

Most visual elements can be generated at runtime from prefabs or simple UI/Image objects. This avoids brittle manual scene wiring and keeps the board responsive across different aspect ratios.

## Code Architecture

Suggested folders:

- Assets/SquareFlow/Scripts/Core
- Assets/SquareFlow/Scripts/Runtime
- Assets/SquareFlow/Scripts/UI
- Assets/SquareFlow/Scripts/Effects
- Assets/SquareFlow/Prefabs
- Assets/SquareFlow/Tests/EditMode

Core logic classes:

- SquareFlowConstants: shared constants such as speed, max active orbiters, wait queue size, colors, and scoring values.
- BoardShape and BoardShapeCatalog: shape data and lookup by level.
- BoardCell: color, HP, and special type.
- Shooter: id, color, ammo, wild flag, and hidden flag.
- BoardGenerator: creates the grid for a level and places bomb blocks.
- ShooterGenerator: creates three shooter columns from the grid HP/color requirements plus extra shooters.
- BoardLayout: computes cell positions, orbit rectangle, path distance, perimeter, and fire points.
- TargetingSystem: finds the first valid target for a shooter from a fire point.
- GameState: owns current level, grid, shooter columns, waiting queue, active orbiters, moves, score, combo, and result.
- GameRules: applies fire, orbit advance, hits, bombs, queueing, win/loss checks, scoring, and combo updates.
- SaveDataService: wraps PlayerPrefs for level, completed levels, leaderboard, dark mode, and mute.

Runtime scripts:

- SquareFlowGameController: orchestrates menu, start, restart, frame update, and UI refresh.
- OrbiterRuntime: stores runtime distance/ammo state and visual references for active orbiters.
- BoardView: generates and refreshes block cells.
- ShooterColumnView and QueueView: render clickable shooters and locked/hidden states.
- HudView and MenuView: bind score, moves, combo, level, result, leaderboard, and toggles.
- EffectsController: plays shot trails, flashes, shake, particles, score popups, and audio cues.

## Data Flow

Starting a level:

1. Load current level and settings from PlayerPrefs.
2. Resolve the level's board shape from BoardShapeCatalog.
3. Generate the grid with BoardGenerator.
4. Generate shooter columns with ShooterGenerator.
5. Compute BoardLayout for the current canvas/viewport.
6. Bind GameState to BoardView, HUD, queues, and shooter columns.

Firing a shooter:

1. Player clicks the front shooter in a column or a shooter in the waiting queue.
2. If fewer than five orbiters are active, GameRules removes the shooter from its source and creates an active orbiter at distance zero.
3. Moves increment and active slot UI updates.
4. Orbiter movement starts or continues.

Advancing orbiters:

1. Each frame, active orbiter distance increases by the source speed value.
2. For each crossed fire point, TargetingSystem checks for a valid target.
3. A valid hit reduces target HP, clears a block, or detonates a bomb.
4. Ammo decreases per hit.
5. When ammo reaches zero or the orbiter completes the perimeter, the orbiter is removed.
6. If it completed the perimeter with leftover ammo, it enters the waiting queue if space remains.
7. Win/loss conditions are checked after hits and orbiter removals.

## Visual Design

The Unity remake should keep the compact arcade-puzzle style of the HTML game. Blocks are rounded squares with bright red, blue, yellow, and green fills. Bomb blocks use an animated rainbow or bright special styling. Wild shooters use pale silver/gray styling with a star mark. The background supports dark and light themes.

The board is centered with an orbit rectangle around it. UI should remain dense and playable on desktop and phone-like aspect ratios. The first implementation should prioritize clarity and responsiveness over exact pixel parity.

## Audio

The HTML game generates short oscillator and noise effects. Unity will use simple generated AudioClips or small procedural clips for fire, wild fire, blocked click, hit, destroy, bomb, win, lose, and level-up. A mute toggle persists through PlayerPrefs.

## Persistence

PlayerPrefs keys will store:

- Current unlocked/current level.
- Completed level set.
- Leaderboard entries, sorted by score and capped at ten.
- Dark mode flag.
- Muted flag.

The reset action clears level progress, completed levels, and leaderboard data, matching the source game behavior.

## Testing Strategy

Use Unity edit-mode tests for core logic before wiring scene behavior.

Tests should cover:

- Shape catalog returns the expected shape count, dimensions, and active cell masks.
- Board generation fills only active cells and creates at least one bomb block.
- HP values stay within the level-scaled maximum.
- Shooter generation accounts for normal block HP by color and creates three columns.
- Targeting returns the first visible valid target from top, right, bottom, and left.
- Wild shooters target normal blocks regardless of color.
- Bomb targeting is valid and bomb detonation clears the center plus valid neighbors.
- Firing respects the max-active-orbiter limit.
- Completed orbiters with leftover ammo enter the waiting queue.
- Wait queue at five triggers the wait-loss condition.
- Empty board triggers win.
- No active, queued, or column shooters triggers loss.

Scene/runtime verification should include:

- Unity compile check with no console errors.
- Edit-mode tests passing.
- Enter Play Mode and verify the menu appears.
- Start a game, fire shooters, observe orbit movement and hits.
- Verify restart, menu, dark/light, mute, and reset controls.

## Risks And Mitigations

Risk: The HTML version mixes logic, layout, animation, and UI state in one React file.
Mitigation: Port behavior into small C# logic classes first, then bind views.

Risk: Random board generation can make visual testing inconsistent.
Mitigation: Core generators should accept an injectable random source or seed in tests.

Risk: Exact UI parity can consume time without improving playability.
Mitigation: Preserve mechanics and visual identity first; tune spacing and animation after the game is playable.

Risk: Unity scene YAML is hard to hand-edit safely.
Mitigation: Prefer C# runtime generation and minimal scene setup. Use the Unity editor/MCP for scene save and verification where possible.

## Acceptance Criteria

- The Unity project opens without compile errors.
- SampleScene launches a native Square Flow remake, not the HTML file.
- The player can complete or lose a level using the same core rules as the HTML source.
- All ten source shapes are playable through level selection/progression.
- Save data, reset, mute, theme toggle, score, moves, combo, and leaderboard are functional.
- Edit-mode tests cover the core rules listed above.
