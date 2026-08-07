# OneStep playable foundation

Unity `6000.4.10f1` mobile/WebGL vertical slice modeled on OneBit Adventure's character-to-adventure flow.

## Play

1. Open `Assets/_Project/Scenes/Adventure.unity` and enter Play Mode.
2. Select one of five cards. An empty card creates the placeholder Wayfarer; an existing card starts or resumes its run.
3. Touch/click an unused gameplay area and drag from that exact point. The temporary joystick is cardinal, repeats at turn-safe cadence, and disappears on release. A tap waits one turn.
4. WASD/arrows, gamepad d-pad/stick, and Space-to-wait are silent desktop alternatives.

The nine-column world always fits the portrait frame. Travel upward, bump enemies to attack, gain permanent levels, and reach a bonfire every 100 progress steps. Rest does not create a checkpoint. **Save and Go Home** stores the active run; death clears it but preserves the character.

Run `Tools > OneStep > Build Foundation` to recreate scenes/settings, and `Tools > OneStep > Web Build > Development` for the responsive WebGL build.

See `Docs/OneBitResearch.md`, `Docs/Architecture.md`, and `Docs/WebBuild.md`.
