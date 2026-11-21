# PROGRESS — Flag Bandits

## Active session: YES — Antigravity, Flash, 2026-11-21

## Current Status

- Phase: 0 (Setup), COMPLETE
- Done: design, context files, Unity CLI 1.0.1, Unity 6000.3.29f1 (6.3 LTS) URP project at repo root, `unity pipeline install`, Git LFS + `.gitignore`, `Assets/_Project` folder structure
- Next up: Phase 1 — Offline player (move, jump, camera, basic gun, health)
- Unity version installed: 6000.3.29f1 (6.3 LTS)
- Blockers: Android NDK install failed — fix before Phase 10

## Next session

- Task: Phase 1 — Offline player (move, jump, camera, basic gun, health)
- Tool: Antigravity → Flash (or Claude Code → Sonnet, Medium effort)

## Open Questions

- Character classes, or all players the same?
- Quick Play: bot-fill to even teams, or full 5v5?

## Decisions

- 2026-11-21: Game name = "Flag Bandits".
- 2026-11-21: Minimum 3 players total; uneven teams allowed. Max 5v5.
- 2026-11-21: Context system = AGENTS.md + docs/PROGRESS.md on GitHub.
- 2026-11-21: Unity CLI used for agent control of the editor.
- 2026-11-21: Unity 6000.3.29f1 (6.3 LTS) selected and installed.

## Log (newest first)

<!-- Format:
### YYYY-MM-DD | Phase X — task | Tool: ___ | Model: ___
- Done:
- Files changed:
- Bugs/Notes:
-->

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
