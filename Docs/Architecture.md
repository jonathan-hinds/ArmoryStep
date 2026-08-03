# Architecture

## Runtime ownership

| Assembly | Responsibility |
| --- | --- |
| `OneStep.Core` | Small engine-facing contracts, configuration data, scene loading, save/player-data boundaries |
| `OneStep.Platform` | Browser viewport notifications and safe-area fitting |
| `OneStep.Input` | Input System action reading and diagnostics; no movement or turn decisions |
| `OneStep.Presentation` | Fixed portrait camera, UI diagnostics, and grid visualization |
| `OneStep.Services` | Unity Gaming Services initialization and anonymous player identity |
| `OneStep.Networking` | Duel invitation/session lifecycle, Relay WSS selection, disconnect and host-change events |
| `OneStep.Bootstrap` | Composition root and the connection-test screen |
| `OneStep.Gameplay.Overworld` | Empty future feature boundary for map, movement, turns, interaction, and content |
| `OneStep.Gameplay.Duel` | Empty future feature boundary for duel rules, synchronization, and migration snapshots |

Editor and tests are isolated in `OneStep.Editor`, `OneStep.Tests.EditMode`, and `OneStep.Tests.PlayMode`. Gameplay assemblies are not auto-referenced, preventing unfinished features from leaking into the foundation.

## Folder layout

`Assets/_Project` owns all project-specific content:

- `Runtime` — feature-based C# modules
- `Editor` — reproducible scene/project setup and Web build commands
- `Settings` — ScriptableObject configuration and Input System actions
- `Scenes` — `Bootstrap` and `FoundationTest`
- `Art/Generated` — setup-only grid and marker placeholders
- `Art`, `Audio`, `Prefabs`, `Data` — production content destinations
- `Tests/EditMode`, `Tests/PlayMode` — isolated test assemblies

Web-only assets live in Unity-standard locations: `Assets/Plugins/WebGL` and `Assets/WebGLTemplates/OneStepResponsive`.

## Scenes

`Bootstrap` is build index 0. Its persistent composition root owns UGS identity, Multiplayer Services, Netcode for GameObjects, Unity Transport, and browser resize notifications. It loads `FoundationTest` after initialization.

`FoundationTest` contains a static 9-by-16 reference grid, non-moving player marker, pixel-perfect camera, 2D global light, safe-area visualization, layered UI, raw input diagnostics, and the multiplayer connection test. The marker does not move and no turn is advanced.

## Future extension rules

- Map data, collision, movement validation, turn scheduling, and camera-follow rules start in `OneStep.Gameplay.Overworld`, consuming abstract Input actions rather than devices.
- Duel rules and replicated game state start in `OneStep.Gameplay.Duel`, consuming `IDuelSessionService`; networking must not own game rules.
- Implement `IPlayerDataStore` in a persistence-specific module when the save format is known. Do not serialize gameplay MonoBehaviours directly.
- Add ScriptableObject content definitions under `Data` and authoring tools under `Editor`; keep runtime systems content-agnostic.
- Host migration currently exposes host-change and migration strategy boundaries. Actual world snapshot transfer must be designed with duel state, not guessed during setup.
