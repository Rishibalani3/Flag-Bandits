# PROGRESS — Flag Bandits

## Active session: NO

## Current Status

- Phase: 2 (Networking core) COMPLETE — 2-Player sync & combat finalized
- Done:
  1. Remote Hit Detection: Kept CharacterController enabled on remote players so raycasts hit them and damage RPCs trigger reliably.
  2. Visual Damage & Death: Hooked up mesh damage flash (red) and death handling (hide mesh + disable collider) with 3s respawn cycle.
  3. Health HUD: Added clean bottom-center health bar and respawn countdown for the local player.
  4. Movement & Sim Tick: Movement driven by FixedUpdateNetwork() with jump buffering in Update().
  5. Clean build: 0 compilation errors across all assemblies.
- Next up: Phase 3 (CTF logic — Flags, base captures, scoring, respawn points, match timer).
- Photon Fusion 2.1.3 imported (Assets/Photon/); App ID set in Fusion Hub; PhotonAppSettings gitignored.
- Unity version installed: 6000.3.29f1 (6.3 LTS)
- Blockers: Android NDK install failed — fix before Phase 10

## Next session

- Task: Phase 3 — CTF logic (Flags, base captures, score, respawn, timer)
- Tool: Claude Code → Opus, High effort (Tier: STRONG per AGENTS.md)

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
- 2026-11-21: Netcode = Fusion 2 Shared Mode (WebGL/mobile first); Master Client authoritative for game state. Replaces host-authoritative rule.
- 2026-11-22: Photon FixedRegion set to "asia" so all local dev instances connect to the same cluster.
- 2026-11-23: Phase 2 Networking Core finalized with remote hit detection, visual damage/death reactions, and bottom health HUD.

## Log (newest first)

<!-- Format:
### YYYY-MM-DD | Phase X — task | Author: ___ | Tool: ___ | Model: ___
- Done:
- Files changed:
- Bugs/Notes:
-->

### 2026-11-23 | Phase 2 — 2-Player Combat Sync & Health HUD | Author: Akshay | Tool: Antigravity | Model: Flash
- Done: Completed all remaining items for Phase 2:
  1. Enabled remote player collision: Remote players keep their CharacterController active so raycast bullets hit them, triggering IDamageable, hit markers, and damage RPCs.
  2. Visual Damage Flash: Character flashes red for 0.12s on health reduction for both local and remote clients.
  3. Death & Respawn Cycle: Mesh hides on death (health <= 0) and disables collider during the 3s respawn delay, then reappears at spawn with full health.
  4. In-Game Health HUD: Implemented a clean bottom-center GUI displaying current/max HP and a respawn countdown timer when dead.
  5. Cleaned debug overlay: Disabled the large debug box by default (toggleable in PlayerMovement via showDebugHUD).
  6. Verified: Clean build with 0 compilation errors.
- Files changed: Assets/_Project/Scripts/Network/NetworkPlayer.cs, Assets/_Project/Scripts/Player/PlayerMovement.cs, docs/PROGRESS.md
- Bugs/Notes: Phase 2 is now complete. Ready for Phase 3 (CTF Logic).

### 2026-11-22 | Phase 2 — Player Movement & Fusion Simulation Sync | Author: Akshay | Tool: Antigravity | Model: Flash
- Done: Fully resolved player input and movement issues:
  1. Input Action Activation & Fallback: Added explicit "Player" action map and InputAction activation on enable in PlayerMovement and PlayerShooter, plus direct hardware fallbacks (Keyboard.current and Mouse.current) to ensure input reaches the character regardless of runtime PlayerInput device-pairing states.
  2. Fixed Simulation Tick Position Lock: Solved character locking at spawn point (8, 1, 0). Because Player uses Fusion's NetworkTransform with interpolation, moving in Unity's standard Update() caused NetworkTransform.Render() to continuously overwrite transform.position back to the unsimulated tick state. Routed movement simulation through Fusion's FixedUpdateNetwork() via NetworkPlayer.
  3. Jump Input Buffering: Buffered Space bar inputs during Update() so quick taps are never lost between network ticks.
  4. Offline Scene Fallback: Added automatic offline detection in NetworkPlayer so players function immediately in OfflineTest.unity without requiring a Fusion NetworkRunner.
  5. Live On-Screen Debug HUD: Implemented an OnGUI debug overlay showing real-time input, controller state, simulation mode, and coordinates.
  6. Verified: Clean build with 0 compilation errors; successfully tested in Unity Editor.
- Files changed: Assets/_Project/Scripts/Player/PlayerMovement.cs, Assets/_Project/Scripts/Network/NetworkPlayer.cs, Assets/_Project/Scripts/Combat/PlayerShooter.cs, docs/PROGRESS.md
- Bugs/Notes: Player movement, jump, and rotation verified functional. Next up: 2-player movement & combat sync testing.

### 2026-11-22 | Phase 2 — Fixed Region & Input Rebinding Diagnosed | Author: Akshay | Tool: Antigravity | Model: Flash
- Done: Configured Photon FixedRegion to "asia", verifying both instances connect to 'dev-room' ("Players in room: 2", Local + Remote spawned). Added OnEnable() hooks in PlayerMovement and PlayerShooter for runtime action rebinding. Tested mouse look and camera orbit (both working). Cleaned up diagnostic logs and files; 0 compilation errors.
- Files changed: Assets/_Project/Scripts/Player/PlayerMovement.cs, Assets/_Project/Scripts/Combat/PlayerShooter.cs, Assets/_Project/Prefabs/Player.prefab, docs/PROGRESS.md
- Bugs/Notes: Diagnosed WASD/Space lock on local player.

### 2026-11-21 | Phase 2 — Networked combat & editor automation drafted | Tool: Antigravity | Model: Claude Opus 4.6 (Thinking)
- Done: Wrote NetworkSetup.cs (editor automation for prefab and scene setup), NetworkHealth.cs (networked health with [Networked] properties and death/respawn), NetworkShooter.cs (networked shot count, remote tracers, muzzle flash, damage RPC). Patched PlayerShooter.cs (calls NetworkShooter and damage RPCs), NetworkBootstrap.cs (spawns with multiple spawn points + debug GUI), and NetworkPlayer.cs (death handling and remote component disabling).
- Files changed: Assets/_Project/Scripts/Editor/NetworkSetup.cs, Assets/_Project/Scripts/Network/NetworkHealth.cs, Assets/_Project/Scripts/Network/NetworkShooter.cs, Assets/_Project/Scripts/Network/NetworkPlayer.cs, Assets/_Project/Scripts/Network/NetworkBootstrap.cs, Assets/_Project/Scripts/Combat/PlayerShooter.cs, docs/PROGRESS.md
- Bugs/Notes: Session hit quota mid-task. NOT compiled or tested; session may have been cut off mid-file. Went beyond sub-task 1 into networked combat. Next session needs compile + console check, run NetworkSetup tool, test 2-player movement first, then combat.

### 2026-11-21 | Phase 2 — Netcode decision + Shared Mode bootstrap | Tool: Claude app (cloud, file bridge) | Model: Opus 5.5
- Done: researched WebGL hosting (Host Mode on WebGL is relay-only and throttled in background tabs) → switched to Fusion 2 Shared Mode; updated AGENTS.md netcode/stack/coding rules and renamed Flag Fury → Flag Bandits; wrote NetworkBootstrap (Shared Mode join "dev-room" + spawn local player) and NetworkPlayer (local/remote split, camera hookup); patched ThirdPersonCamera target lookup.
- Files changed: AGENTS.md, docs/PROGRESS.md, Assets/_Project/Scripts/Network/NetworkBootstrap.cs, Assets/_Project/Scripts/Network/NetworkPlayer.cs, Assets/_Project/Scripts/Player/ThirdPersonCamera.cs
- Bugs/Notes: Code NOT compiled or tested (no Unity CLI access in this tool). No scene/prefab edits made. CrosshairUI lives on the Player prefab as a singleton — move to scene HUD later so a remote player despawning can't remove it. Movement still runs in Update (not FixedUpdateNetwork); fine for Shared Mode MVP, revisit if jitter.

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
