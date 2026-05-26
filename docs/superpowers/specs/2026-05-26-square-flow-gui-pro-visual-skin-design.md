# Square Flow GUI Pro Visual Skin Design

Date: 2026-05-26
Project: OrbitFlow

## Goal

Use `Assets/Layer Lab/GUI Pro-CasualGame/` to redesign the game's visible text and UI styling with a sweeter casual-game look, while preserving the existing Square Flow gameplay, flow, button functions, and gameplay-screen layout.

The approved direction is the stronger "full GUI Pro style" look, but applied as a visual skin over the current UI rather than as a functional redesign.

## Non-Negotiable Constraints

- Do not remove any existing button.
- Do not add new buttons or new gameplay controls.
- Do not change any button action, click handler, menu flow, gameplay rule, save behavior, scoring behavior, level selection behavior, theme toggle behavior, restart behavior, mute behavior, or result flow.
- Do not move gameplay-screen elements. Score, best, level, status, action buttons, orbiter strip, waiting queue, shooter columns, and result panel keep their existing layout positions and interaction roles.
- Do not let the gameplay UI shift, overlap, or cover the playfield differently from the current layout.
- Treat the imported `Assets/Layer Lab/GUI Pro-CasualGame/` package as an asset source. Do not edit the third-party asset files unless a narrow import/reference fix is required.

## Visual Direction

The UI should feel like a polished casual mobile game:

- Rounded, colorful, friendly panel and button surfaces.
- A sweeter display font using GUI Pro's Lilita One TextMesh Pro font assets where compatible.
- Stronger title treatment with outline/shadow depth.
- More playful labels, stat cards, and button text.
- Brighter casual-game contrast, but still readable over the current Square Flow world view.

The main menu can receive the strongest version of this style because it is not constrained by active gameplay readability. Gameplay screens should use the same visual language more carefully, with layout and clear playfield priority preserved.

## Chosen Approach

Use a locked-layout GUI Pro skin layer.

`SquareFlowGameController` currently creates the menu, HUD, buttons, panels, labels, and result panel dynamically at runtime. The safest implementation is to keep that runtime builder, preserve the current `RectTransform` layout values and existing object hierarchy names, and change only the visual styling applied by helper methods.

This means:

- Keep `AddText`, `AddButton`, `AddPanel`, `AddGlassPanel`, `AddHeaderStatCard`, and related runtime construction methods as the integration points.
- Load selected GUI Pro TMP font assets from `Assets/Layer Lab/GUI Pro-CasualGame/ResourcesData/Fonts/`.
- Load selected GUI Pro sprites from `ResourcesData/Sprites/Components/` for panels, labels, ribbons, popups, and buttons when they fit the current sizes.
- Use generated fallback sprites when a GUI Pro sprite would distort or create layout risk.
- Keep all existing object names needed by tests and scene inspection.
- Keep existing button creation and `onClick` listener wiring.

Direct prefab replacement is not chosen for gameplay UI because GUI Pro prefabs carry their own nested transforms and default sizing. That would increase the risk of layout drift and event wiring regressions.

## Menu Scope

The main menu receives the fuller C-style treatment:

- `Square Flow` title becomes a larger sweet casual title with GUI Pro font, outline, and shadow.
- Stats card, level selector, instructions card, Play button, Reset All button, and theme toggle get GUI Pro-inspired surfaces.
- Existing content stays the same: title, swatches, theme toggle, level/board/max-orbs stats, Reset All, level selector, instructions, and Play.
- No Shop, inventory, character, reward, or new menu button is added.

## Gameplay Scope

Gameplay visuals receive skin-only changes:

- Score and best cards keep their current top layout and values.
- Level badge, shape label, status bar, combo/moves text, and action buttons keep their current positions.
- Home, restart, palette, and mute actions remain the same.
- Orbiter strip, waiting queue, shooter column cards, shooter slots, shooter ammo labels, and result panel keep their existing placement and interaction behavior.
- Text switches to the chosen sweet TMP font where legible.
- Text gets tuned outline/shadow treatment for readability.
- Panel and button sprites/colors are updated to casual GUI Pro style without changing dimensions.

## Asset Strategy

Use GUI Pro assets conservatively:

- Preferred font: `LilitaOne-Regular SDF.asset` or the size-specific outline variants where they render correctly.
- Preferred label/ribbon sprites: assets under `ResourcesData/Sprites/Components/Label/`.
- Preferred popup/panel sprites: assets under `ResourcesData/Sprites/Components/Popup/`.
- Preferred button surfaces: assets under `ResourcesData/Sprites/Components/Button/` if available and compatible with current `Image` slicing, otherwise retain generated rounded sprites with GUI Pro-inspired colors.

If a sprite cannot be loaded at runtime because it is not under a Unity `Resources` folder, either:

- move/copy only the selected runtime assets into an owned `Assets/Resources/SquareFlow/GUIPro/` folder, preserving third-party originals, or
- reference them through serialized/editor-managed fields only if that remains compatible with scene rebuilds.

The implementation should prefer a small owned runtime asset subset over broad dependency on the whole GUI Pro prefab tree.

## Testing And Verification

Edit-mode tests should protect the constraints:

- Existing menu and gameplay buttons still exist.
- Action button count and names remain unchanged.
- Gameplay UI layout positions and sizes do not drift.
- Text components use the intended GUI Pro or fallback sweet font.
- Core labels still render: title, score, best, level, moves/combo, waiting queue, result title, result score, menu labels.
- Gameplay rules tests remain unchanged and passing.

Manual Unity verification should include:

- Open `Assets/Scenes/SampleScene.unity`.
- Enter Play Mode.
- Check menu readability and button behavior.
- Start a level.
- Confirm gameplay UI did not move or overlap.
- Fire from shooter columns and waiting queue.
- Use home, restart, palette/theme, and mute.
- Complete or fail a level and verify result panel buttons.

## Out Of Scope

- New gameplay mechanics.
- New buttons, shops, rewards, character systems, settings pages, or inventory UI.
- Repositioning gameplay HUD or controls.
- Rebuilding the gameplay UI from GUI Pro prefabs.
- Editing third-party GUI Pro source assets directly.
