# PROGRESS — Flag Bandits

## Active session: NO

## Current Status

- Phase: 1 (Offline player) COMPLETE; Phase 2 (Networking core) READY
- Done: Player movement, jump, sprint, ThirdPersonCamera (mouse look orbit, cursor lock), basic gun (raycast, visible tracers, muzzle flash, damage), Health & damage system, TargetDummy prefab, crosshair HUD & hit markers, fall reset at Y < -10, perimeter walls in OfflineTest.
- Next up: Phase 2 — Networking core (Photon Fusion setup, 2 players synced)
- Unity version installed: 6000.3.29f1 (6.3 LTS)
- Blockers: Android NDK install failed — fix before Phase 10

## Next session

- Task: Phase 2 — Photon Fusion networking core (2 players synced)
- Tool: Claude Code → Opus, High effort

## Open Questions

- Character classes, or all players the same?
- Quick Play: bot-fill to even teams, or full 5v5?

## Decisions

- 2026-11-21: Game name = "Flag Bandits".
- 2026-11-21: Minimum 3 players total; uneven teams allowed. Max 5v5.
- 2026-11-21: Context system = AGENTS.md + docs/PROGRESS.md on GitHub.
- 2026-11-21: Unity CLI used for agent control of the editor.
- 2026-11-21: Unity 6000.3.29f1 (6.3 LTS) selected and installed.
- 2026-11-21: Rule: No raw .unity/.prefab text edits; use Unity CLI / Editor commands for scene and prefab manipulations.

## Log (newest first)

<!-- Format:
### YYYY-MM-DD | Phase X — task | Tool: ___ | Model: ___
- Done:
- Files changed:
- Bugs/Notes:
-->

### 2026-11-21 | Phase 1 — Basic gun, health system, third-person camera & combat test | Tool: Antigravity | Model: Flash
- Done: implemented third-person orbit camera with mouse look and cursor lock; raycast gun shooting through crosshair with high-visibility tracers and muzzle flash; IDamageable health system with TargetDummy prefab (flash & respawn); fall reset at Y < -10; low perimeter walls in OfflineTest; crosshair HUD with hit markers; verified clean Play Mode via CLI screenshot.
- Files changed: Assets/_Project/Scripts/Combat/*, Assets/_Project/Scripts/Player/*, Assets/_Project/Scripts/UI/*, Assets/_Project/Settings/*, Assets/_Project/Prefabs/*, Assets/_Project/Scenes/OfflineTest.unity, docs/PROGRESS.md
- Bugs/Notes: fixed CrosshairUI canvas reparenting bug; removed legacy Cinemachine remnants; zero console errors.

### 2026-11-21 | Phase 1 — Player movement, jump & Cinemachine camera | Tool: Antigravity | Model: Flash
- Done: implemented PlayerSettings ScriptableObject and PlayerMovement component; created OfflineTest scene with ground, jumping platforms, and ramp via Unity CLI; added Cinemachine follow camera tracking Player; created Player prefab; verified clean console (0 errors).
- Files changed: Assets/_Project/Scripts/Player/PlayerSettings.cs, Assets/_Project/Scripts/Player/PlayerMovement.cs, Assets/_Project/Settings/PlayerSettings.asset, Assets/_Project/Prefabs/Player.prefab, Assets/_Project/Scenes/OfflineTest.unity, docs/PROGRESS.md
- Bugs/Notes: Unity CLI pipeline connected to live Editor on port 7800.

### 2026-11-21 | Phase 0 — Unity project setup & repo hygiene | Tool: Antigravity | Model: Flash
- Done: verified CLAUDE.md, .gitignore, and .gitattributes (Git LFS); created Unity 6000.3.29f1 URP project at repo root; ran `unity pipeline install`; registered project in Unity Hub; created `Assets/_Project/{Scripts,Prefabs,Scenes,Materials}` folders.
- Files changed: .gitattributes, .gitignore, .vsconfig, Assets/*, Packages/*, ProjectSettings/*, docs/PROGRESS.md
- Bugs/Notes: Android NDK failed to install, noted as blocker for Phase 10.

### 2026-11-21 | Phase 0 — Unity CLI install | Tool: Claude chat | Model: Opus

- Done: installed Unity CLI 1.0.1 via PowerShell
- Notes: Command Prompt can't run the installer; use PowerShell

### 2026-11-21 | Planning | Tool: Claude chat | Model: Opus

- Done: game design, phase plan, sync system
- Files changed: AGENTS.md, CLAUDE.md, docs/PROGRESS.md
