# OneBit Adventure foundation research

Research was conducted on 2026-08-03 to inform project setup without implementing gameplay.

## Verified characteristics

- The official site describes a one-handed, minimal, turn-based experience.
- The current Google Play listing explicitly offers swipe or d-pad exploration and says enemies move only when the player moves.
- The developer's Newgrounds page documents `WASD` for move/attack, Space for waiting, click/touch drag for move/attack, and click/touch without drag for waiting.
- TouchArcade describes portrait presentation matched to a vertically scrolling overworld rather than a conventional maze camera.
- 148Apps describes long portrait hallways, cardinal d-pad movement, per-step enemy turns, and bump interaction.
- Official update notes show that swipe and d-pad are selectable modes, modern versions preserve a 0.2-second wait delay in swipe mode, d-pad inputs can be held/combined, and movement input is intended to respond rapidly.

Sources:

- https://www.onebitadventure.com/
- https://play.google.com/store/apps/details?id=com.GalacticSlice.OneBitAdventure
- https://www.newgrounds.com/portal/view/895349
- https://toucharcade.com/2019/11/22/toucharcade-game-of-the-week-onebit-adventure/
- https://www.148apps.com/onebit-adventure/onebit-adventure-review/
- https://www.onebitadventure.com/v1-2-72-tutorial-update-part-1/
- https://www.onebitadventure.com/fork-in-the-road-update-release-notes/
- https://www.onebitadventure.com/v1-3-56-57-release-notes/

## Foundation consequences

- Portrait is the authoritative view. The starting reference is 144×256 pixels at 16 pixels per unit: a clean 9×16 logical tile frame. This is a tunable project choice derived from the portrait/tile presentation, not a claim that OneBit's private internal render resolution is known.
- Wider displays are letterboxed instead of expanding horizontal visibility.
- Input is represented as actions (`Move`, `Wait`, `PointerPosition`, `PrimaryAction`, confirm/cancel), with keyboard, controller, mouse, touch, and an Input System on-screen d-pad.
- The test scene visualizes raw actions only. It does not interpret drag thresholds, resolve a tile direction, repeat movement, wait a turn, bump an entity, move the marker, scroll the camera, or run enemies.
- Future movement must remain cardinal and command/turn based. It should add configurable swipe-vs-d-pad interpretation and wait timing in the Overworld assembly, after direct gameplay capture and feel testing.
- Modern OneBit also has optional tap-to-attack and attack-button features. Those are combat-facing features and intentionally excluded from this setup.

## Required validation before gameplay

Before movement implementation, record direct reference sessions on at least one phone and the browser build. Measure visible tile columns/rows, player screen anchor, camera advance/backtrack limits, swipe dead zone, drag-repeat cadence, d-pad repeat cadence, tap/wait timing, UI capture regions, and behavior at diagonal drags. Store those values in data assets rather than hardcoding them.
