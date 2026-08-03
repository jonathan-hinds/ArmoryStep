# ArmoryStep

Unity project for ArmoryStep. The project currently uses Unity `6000.4.10f1`.

## Safe source-control workflow

The complete, reproducible project state is stored in these committed paths:

- `Assets/` (including every `.meta` file)
- `Packages/`
- `ProjectSettings/`

Unity's `Library/`, `Temp/`, `Logs/`, `UserSettings/`, IDE files, and builds are deliberately ignored because they are machine-local or generated. Unity recreates them after a checkout. Binary art, audio, video, fonts, and native plugins are stored through Git LFS.

For a reliable checkpoint:

```powershell
git status
git add -A
git commit -m "Describe the working checkpoint"
git push
```

A commit protects against bad local edits; the push protects against loss of the computer. Uncommitted work is not recoverable from Git.

Before opening the project on another machine, install Git LFS and clone normally. If binary assets appear as tiny text pointer files, run:

```powershell
git lfs install
git lfs pull
```

To return all tracked files to a known working checkpoint, first commit or stash anything worth keeping, then use Git's revert/restore tools. Never copy another project's `Library/` folder or commit generated caches as a workaround.
