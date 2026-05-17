# Square Flow Reference Gameplay Screen Design

## Goal

Restyle only the active gameplay screen so it closely matches the provided dark arcade reference while preserving all existing Square Flow gameplay rules, save data, level progression, menu behavior, audio toggles, and result handling.

## Approved Direction

The gameplay screen should read as a compact dark arcade board:

- deep slate background with restrained panel borders
- compact top HUD with small uppercase level text and large yellow score
- small square utility buttons in the top-right area for menu/pause, restart, and sound
- centered rounded-square board with dark inactive cells, bright colored blocks, and clear tile depth
- thin golden orbit ring behind the board
- glowing active shooter dots on the ring
- vertical right-side waiting queue with colored circular shooters
- bottom shooter dock with compact rounded slots and circular shooter controls

The main menu, level selector, leaderboard, light/dark toggle flow, and result panel are out of scope except where existing gameplay navigation buttons still need to call those flows.

## Gameplay Layout

`SquareFlowGameController.RefreshGameView` should be the primary implementation surface. The gameplay view should replace the current full-width top HUD plus stacked queue/columns layout with the reference-inspired arrangement:

- a top status panel anchored near the top, wide enough for level/score and three utility buttons
- a central board panel area with transparent board backing so the ring and board feel layered
- a right queue panel showing waiting shooters vertically as circles
- a bottom dock showing one playable front shooter from each column as a circular control inside a small rounded slot

The board remains centered and playable on the existing 1080 x 1920 canvas reference resolution. Layout values should remain deterministic and runtime-generated, matching the current uGUI architecture.

## Visual Treatment

The gameplay theme should use dark slate surfaces, muted borders, bright red/blue/green/yellow tile colors, and a gold score/orbit accent. Inactive board cells should be near-black rounded squares. Occupied cells should use rounded square sprites with subtle outline and depth highlights.

Wild tiles should be visually distinct with a diagonal rainbow band or similarly clear multi-color treatment implemented through layered runtime UI elements, without adding external assets. Bomb cells should remain readable and keep their current special behavior.

Active orbiters should be circular tokens or glow points on the orbit ring, with ammo text where it remains legible. The golden ring should sit behind the board and use a thin continuous or dotted treatment close to the reference.

## Interaction

Existing controls stay intact:

- clicking a front shooter column fires that column
- clicking a waiting shooter fires from the waiting queue
- restart restarts the current level
- menu returns to the existing menu
- sound toggles mute state

The visual redesign must not change targeting, board generation, score, combo, queue, shooter, or win/loss logic.

## Testing

Existing edit mode tests should continue to pass. Add or update focused tests only if gameplay-visible helper logic is extracted from UI code. A `dotnet build` check is acceptable for C# compilation, and Unity edit mode tests should be run when the editor is available.

Manual verification should confirm:

- gameplay screen resembles the provided reference at the current canvas scale
- board, orbit ring, right queue, bottom dock, and top HUD do not overlap
- shooter buttons still fire correctly
- menu, restart, and mute controls still work
- result panel still appears after win/loss without breaking the new gameplay layout

## Constraints

Keep the change scoped to `Assets/SquareFlow` runtime-generated uGUI code and this design documentation. Do not add new packages, imported image assets, or unrelated menu redesign work.
