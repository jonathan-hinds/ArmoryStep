# OneStep foundation

Unity `6000.4.10f1` foundation for a portrait-first, mobile-browser, tile roguelike. This pass intentionally implements no combat, world generation, turns, movement resolution, inventory, progression, or duel rules.

## Start here

1. Open `Assets/_Project/Scenes/Bootstrap.unity`.
2. Complete the Unity Dashboard steps in `Docs/UnityDashboard.md`.
3. Enter Play Mode. The bootstrap initializes Unity Gaming Services, signs in anonymously, and loads `FoundationTest`.
4. Use the validation scene to inspect the fixed portrait frame, safe area, Input System diagnostics, touch d-pad, and Relay host/join lifecycle.

Run `Tools > OneStep > Build Foundation` after intentionally changing the generated scene layout or base project settings. The command is idempotent and recreates both foundation scenes and placeholder assets.

## Documentation

- `Docs/Architecture.md` — folders, assemblies, ownership, and extension points
- `Docs/UnityDashboard.md` — required project linking, environments, Authentication, Relay, and testing
- `Docs/WebBuild.md` — development/production builds, local serving, itch.io packaging, and browser risks
- `Docs/OneBitResearch.md` — researched presentation/control constraints and what this setup does (and does not) implement

The source-controlled project state lives in `Assets`, `Packages`, and `ProjectSettings`. `Library`, `Temp`, `Logs`, `UserSettings`, and `Builds` are generated and ignored.
