# Square Flow Prism Orbit Design

## Goal

Polish the Unity remake with the approved Prism Arcade direction and replace the current square shooter path with a rounded orbit path.

## Approved Direction

The game should keep its dark arcade identity while becoming more finished: crisp high-contrast colors, glassy dark panels, gold scoring accents, brighter shooter/tile colors, circular shooter tokens, and a visible glowing orbit ring around the board.

## Orbit Behavior

Active shooters should travel around a rounded orbit ring, not along the current square perimeter. The gameplay model still uses ordered fire points around the board so top, right, bottom, and left target checks remain deterministic, but the visual position of a shooter is projected onto an elliptical orbit around the board bounds.

The orbit starts near the upper-left arc and moves clockwise. Fire point distances keep the original rectangular side order for gameplay timing, then `PathPosition` projects those distances onto the ellipse so shooters visually follow the rounded ring.

## UI Scope

The Prism Arcade polish is limited to the runtime-generated uGUI scene:

- Update the theme palette for a dark prism arcade look.
- Draw the orbit ring as a rounded dotted/glow ring instead of four straight segments.
- Render active shooters as circular tokens with a soft glow and ammo text.
- Give panels, buttons, cells, and shooter controls rounded sprites with subtle outline/shadow treatment.
- Preserve existing menu/game flows, save data, level selection, leaderboard, light/dark toggle, mute, restart, and gameplay rules.

## Testing

Add layout tests that fail against the square path and pass once `BoardLayout.PathPosition` returns points on the rounded orbit. Existing gameplay tests should continue to pass because the ordered fire point contract remains intact.

## Constraints

Do not introduce third-party packages or asset dependencies. Keep the changes inside `Assets/SquareFlow` plus this spec/plan documentation. Unity batchmode tests may still be blocked while the editor has the project open, so fresh `dotnet build` checks and Unity Play Mode/console checks are the fallback verification.
