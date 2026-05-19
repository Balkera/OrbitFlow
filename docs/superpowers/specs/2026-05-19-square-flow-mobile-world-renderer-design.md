# Square Flow Mobile World Renderer Design

Date: 2026-05-19
Project: C:\Users\samet\My project

## Goal

Convert Square Flow toward a phone-shaped, touch-first Unity game while preparing for a later installable Android build. The immediate goal is not Android packaging. The immediate goal is a robust mobile gameplay presentation: the board, orbiters, shots, and effects should render as world-space 2D objects, while Unity Canvas remains responsible for HUD, menus, touch buttons, and text-heavy UI.

## Chosen Approach

Use the most robust architecture: move active gameplay visuals out of Unity UI Canvas and into a world-space 2D renderer.

The existing `SquareFlow.Core` model and rules remain the source of truth. Runtime views observe `GameState`, call `GameRules`, and update Unity objects. Gameplay objects should not be destroyed and recreated on routine state changes. They should be reused, pooled, or updated in place.

Canvas remains appropriate for:

- Main menu
- Pause and result panels
- HUD text
- Level selector
- Mute, restart, and navigation buttons
- Touch controls that are fundamentally UI

World-space 2D rendering becomes responsible for:

- Board cells
- Tile depth/highlights
- Orbit ring
- Active orbiters
- Shot trails
- Hit, destroy, and bomb effects
- Any gameplay element that moves or updates frequently

## Architecture

The current large `SquareFlowGameController` should be split into smaller runtime views. The controller continues to coordinate game state, but visual responsibilities move into focused components.

Core components:

- `SquareFlowGameController`: owns screen flow, level start/restart, rule calls, save data, and high-level event dispatch.
- `BoardWorldView`: creates and updates world-space board cell sprites from `BoardCell[,]` and `BoardLayout`.
- `OrbitRingWorldView`: renders the path around the board with reusable sprite segments or line rendering.
- `OrbiterWorldView`: creates, pools, and moves active orbiter sprites based on `ActiveOrbiter.Distance`.
- `ShooterDockView`: presents shooter columns and waiting queue as touch-first controls. This can stay Canvas for accessible buttons or become a hybrid world-space visual plus Canvas/input hit target if needed.
- `SquareFlowHudView`: score, moves, combo, level, pause, restart, and mute.
- `SquareFlowMenuView`: title, play, level selector, theme, mute, reset, leaderboard.
- `EffectsController`: pools shot trails, hit flashes, block pops, bomb flashes, and score feedback.
- `MobileCameraController`: positions an orthographic camera for portrait play, safe-area spacing, and board scaling.

## Data Flow

Starting a level:

1. Controller loads settings and selected level.
2. Core generators create the board grid and shooter columns.
3. `BoardLayout` computes logical board positions.
4. `MobileCameraController` maps the logical board into the portrait viewport.
5. World views create or reuse the required visual objects.
6. HUD and shooter controls bind to the same `GameState`.

During play:

1. Touch input selects a shooter from the dock or waiting queue.
2. Controller calls `GameRules.FireFromColumn` or `GameRules.FireFromWaiting`.
3. Rules update `GameState`.
4. Views update only the changed visuals.
5. `OrbiterWorldView` moves active orbiters each frame from the current state.
6. `EffectsController` plays pooled effects for emitted `GameEvent`s.

## Mobile Layout

The game should be designed around portrait orientation with a 1080 by 1920 reference. The layout must account for safe areas and one-handed use.

Recommended layout:

- HUD near the top safe area.
- Board centered in the main visual area.
- Waiting queue close to the board but not competing with shooter controls.
- Shooter columns near the bottom as large touch targets.
- Pause, restart, and mute reachable but secondary.

Touch targets should be sized for phone use. Gameplay should not depend on mouse hover, tiny buttons, or desktop-only layout assumptions.

## Performance Requirements

The mobile renderer should avoid the current pattern of clearing and rebuilding the gameplay view for normal interactions.

Required behavior:

- Board cell objects are created once per level and updated in place.
- Active orbiter objects are pooled.
- Shot and impact effects are pooled.
- HUD text updates do not trigger board rebuilds.
- Firing a shooter updates shooter controls, orbiters, and changed cells only.
- Camera and world scaling do not require recreating the board.

Canvas performance rules:

- Keep Canvas for static or text-heavy UI.
- Separate frequently changing HUD elements from large static menu/panel canvases if needed.
- Avoid putting the moving board under Canvas.
- Avoid rebuilding large Canvas hierarchies during gameplay.

## Android Readiness

The immediate work is phone-shaped and touch-first, but the architecture should prepare for Android packaging later.

Later Android work should include:

- Portrait orientation lock.
- Android player settings.
- URP mobile quality profile.
- Texture and audio compression settings.
- Device Simulator checks.
- Real-device profiling.
- Frame time and allocation checks on representative Android hardware.

The world-space renderer makes that later work safer because moving gameplay visuals will use Unity's 2D rendering path instead of large dynamic Canvas hierarchies.

## Testing Strategy

Core edit-mode tests should continue to verify rules, board generation, targeting, scoring, win/loss, and persistence boundaries.

Additional runtime verification should cover:

- Portrait camera frames the board and controls on phone-like aspect ratios.
- Touch targets are large enough and reachable.
- Starting, firing, orbiting, hit effects, restart, pause/menu, mute, and result flow work.
- Firing and orbiter movement do not recreate the entire gameplay hierarchy.
- Pooled objects are reused after repeated shots and effects.
- No avoidable per-frame allocations in the main gameplay loop.

## Acceptance Criteria

- The game screen is portrait-first and touch-first in the Unity editor.
- Board cells, orbit ring, active orbiters, and shot/effect visuals render in world space.
- Canvas is limited to HUD, menus, text, and touch UI.
- Existing core rules remain intact.
- Normal firing and orbiter updates reuse visual objects instead of rebuilding the whole board.
- The structure is ready for a later Android packaging phase without another rendering rewrite.

## Out Of Scope

- Shipping an Android APK or AAB in this phase.
- Online services, ads, billing, analytics, or store integration.
- Major new game mechanics.
- Exact visual parity with the earlier browser/Canvas implementation.
