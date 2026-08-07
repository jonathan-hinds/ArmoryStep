# Overworld module

The rules layer is `AdventureSession` + `AdventureWorld`; it has no UI or device dependencies. `CharacterRepository` persists five character slots and explicit active-run snapshots. `OneBitGameRoot`, `AdventureWorldView`, `FloatingJoystickInput`, `DiscreteInputDriver`, and `VerticalCameraFollower` are replaceable Unity adapters around those rules.

Tune foundational content in `AdventureConfiguration.asset`. Add future enemies through definitions and behavior policies rather than branching presentation code.
