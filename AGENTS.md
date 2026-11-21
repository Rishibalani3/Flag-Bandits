# AGENTS.md — Flag Bandits (shared by Claude Code + Antigravity + Cursor)

## Developer

- Rishi: C#/Java beginner–intermediate, new to Unity and multiplayer.
- Wants concise answers, questions only when blocked, no wasted tokens.
- AI writes code, plans, and debugs. Rishi works in the Unity editor,
  imports assets, tests, creates accounts, and deploys.
- Subscriptions: Claude Max 5x + Google AI Pro (Antigravity) +
  Cursor [plan: ___]. All are limited, so be efficient.

## Environment

- OS: Windows. Use PowerShell commands (not bash) unless the tool's
  terminal is bash.
- Unity CLI: installed (`unity` command). Use it for editor installs,
  console checks, Play Mode tests, and builds.
- Unity version: 6.7 LTS if available, otherwise 6.3 LTS.
  The exact installed version is recorded in PROGRESS.md. Never
  upgrade Unity mid-project without asking Rishi.
- Build modules: WebGL + Android (OpenJDK, SDK, NDK).

---

## SYNC SYSTEM (multiple tools, models, and computers)

- Tools in use: Claude Code, Antigravity, Cursor. All read this file.
- Source of truth = GitHub repo.
- `docs/PROGRESS.md` = current status + history. ALWAYS read it first.
- `AGENTS.md` = rules + design. Change it only when a design decision
  changes, and log the change in PROGRESS.md "Decisions".
- `CLAUDE.md` only contains `@AGENTS.md`. Never add content to it.
- Only ONE session at a time. If PROGRESS.md shows
  "Active session: YES", warn Rishi before continuing.

---

## SESSION PROTOCOL (all tools must follow)

### When Rishi writes "START"

1. Ask Rishi: "Pull latest from GitHub first, then say 'pulled'."
   Wait for it. Don't run git commands.
2. Read `docs/PROGRESS.md`.
3. Check the "Next session" block. If your tool/model doesn't match
   the recommendation, say:
   "⚠️ Recommended: [Tool] → [Model], [Effort]. Switch or continue?"
   and wait.
4. Set "Active session: YES — [tool], [model], [date]" in PROGRESS.md.
5. In 2–3 lines, say the current phase, the task, and what's done.
   Then begin.

### During the session

- One sub-task at a time: code → Rishi tests → next.
- Give clear Unity editor steps.
- Use the Unity CLI to check console errors before asking Rishi to
  copy-paste them.
- If a task gets harder than expected (netcode, desync, AI), recommend
  switching to the STRONG tier. If it's trivial, recommend FAST or TINY.

### When Rishi writes "WRAP UP" (or the task/phase is finished)

1. Update `docs/PROGRESS.md`:
   - Current Status
   - New log entry at the TOP (format is inside the file)
   - "Next session" block (task + tool + model + effort)
   - Set "Active session: NO"
2. Don't run git commands. Rishi commits and pushes himself.
   Give him a one-line commit message, e.g. "Phase 1: player movement".
3. End with EXACTLY this format:

   ✅ Session saved. Commit + push now.
   📝 Commit message: <message>
   ➡️ Next: Phase X — <task>
   🔧 Switch to: <Tool> → <Model>, <Effort> effort
   Write **START** when ready.

---

## MODEL TIERS

| Tier   | Claude Code           | Antigravity             | Cursor                        | Use for                                               |
| ------ | --------------------- | ----------------------- | ----------------------------- | ----------------------------------------------------- |
| STRONG | Opus, High effort     | Pro-tier, Planning mode | Claude Opus (thinking)        | Netcode, CTF logic, bot AI, desync bugs, architecture |
| FAST   | Sonnet, Medium effort | Flash                   | Claude Sonnet or Gemini Flash | Scripts, UI, editor steps                             |
| TINY   | Haiku, Low effort     | Flash                   | Smallest/auto model           | Renames, small edits                                  |

Default tool choice (saves quota):

- STRONG → Claude Code
- FAST/TINY → Antigravity Flash first, then Cursor, then Claude Sonnet
- Always use Agent mode in Cursor (rules don't apply to Tab completion
  or Inline Edit)

## PHASE PLAN (MVP)

| #   | Phase           | Work                                                                                                                                                    | Tier            |
| --- | --------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------- |
| 0   | Setup           | Unity CLI ✅, Unity login ✅, Unity 6.3 LTS + modules ✅ (NDK failed), project, Git + .gitignore + LFS, `unity pipeline install`, Unity plugin, folders | FAST            |
| 1   | Offline player  | Move, jump, camera (Cinemachine), basic gun, health                                                                                                     | FAST            |
| 2   | Networking core | Photon Fusion, 2 players synced                                                                                                                         | STRONG          |
| 3   | CTF logic       | Flags, captures, score, respawn, timer                                                                                                                  | STRONG          |
| 4a  | Map spec        | Review/refine docs/design/MAP_RUINED_TOWN.md (v1 drafted ✅)                                                                                            | STRONG (Claude) |
| 4b  | Map blockout    | Greybox Ruined Town from spec (ProBuilder), NavMesh + links                                                                                             | FAST            |
| 5   | Weapons         | Pickups, pistol, rifle, shotgun, grenades, melee                                                                                                        | FAST            |
| 6   | Center mortar   | Clock tower control + mortar (per map spec)                                                                                                             | FAST            |
| 7   | Rooms + bots    | Quick Play, private codes (FAST), bot AI (STRONG)                                                                                                       | STRONG          |
| 8a  | UI spec         | Write docs/design/UI_DESIGN.md                                                                                                                          | STRONG (Claude) |
| 8b  | UI build        | Build menus, HUD, scoreboard from spec + art pack                                                                                                       | FAST            |
| 9   | Playtest + fix  | Netcode bugs (STRONG), others (FAST)                                                                                                                    | Mixed           |
| 10  | Mobile          | Fix Android NDK, touch controls, Android build, performance                                                                                             | FAST            |
| 11+ | Post-MVP        | Maps 2–3, modes, monetization, accounts                                                                                                                 | Per feature     |

## UI RULES (all tools must follow)
- Follow docs/design/UI_DESIGN.md exactly. Don't redesign.
- UI system: uGUI (Canvas) + TextMeshPro. Never use UI Toolkit or
  IMGUI for game UI.
- Reuse prefabs in Assets/_Project/Prefabs/UI/ before making new ones.
- Colors, fonts, and sizes come from Assets/_Project/Settings/UITheme.asset.
  Never hardcode them.
- Min touch target 120×120 px at 1920×1080 reference.

---

## DESIGN OWNERSHIP

- Claude (Opus) is the DESIGN LEAD: UI design, map layouts, game feel
  values, architecture. Claude writes specs in docs/design/\*.md.
- Antigravity and Cursor IMPLEMENT specs exactly. They do not
  redesign. If a spec is unclear or wrong, add it to PROGRESS.md
  "Open Questions" and ask Rishi instead of improvising.
- Specs change only in a Claude session, or with Rishi's approval.
- Before building any UI or map, read the matching file in
  docs/design/.

## NETCODE RULES

- Photon Fusion 2, Shared Mode.
- Each client owns its own player (movement, shooting input).
- Master Client owns: flags, captures, score, timer, mortar, bots,
  pickups.
- Damage: shooter reports hit → victim's owner validates (range, line
  of sight, fire rate) before applying.
- Never trust a client for flag pickup/capture; Master Client decides.
- Post-MVP option: Server Mode (self-hosted dedicated) for anti-cheat.

## TUNING RULES

- All gameplay numbers (speed, damage, fire rate, timers) live in
  ScriptableObjects under Assets/\_Project/Settings/, never hardcoded,
  so balance changes need no code.

## PERFORMANCE BUDGET (low-end Android target)

- 60 FPS target, 30 FPS minimum
- Under ~150 draw calls per frame; use texture atlases and static
  batching
- Low-poly only; no real-time shadows on mobile

## ASSET RULES

- Asset Store packs go in Assets/ThirdParty/. Never edit them;
  copy what you need into \_Project/ first.
- Note each pack's name and license in docs/ASSETS.md.

## GAME DESIGN

**Flag Bandits** (working title): a 3D multiplayer CTF shooter. Cartoony
low-poly characters, Mini Militia–style gameplay, small dense maps,
and a central super-weapon.

- Platforms: WebGL → Android → iOS (iOS builds need a Mac, later)
- Stack: Unity (C#), Photon Fusion 2 (Shared Mode), Asset Store
  low-poly art

### CTF Rules

- 2 teams, 2 bases, 1 flag each. Bring the enemy flag to your base
  for 1 capture.
- Flag carrier is slowed and can only use the basic gun.
- A dropped flag stays ~10s, then returns to base.
- Respawn near a living teammate OR at your base flag.

### Teams & Matches

- Minimum 3 players TOTAL; uneven teams allowed (2v1). Max 5v5.
- Quick Play: bots fill empty slots. First to 3 captures or 6 minutes.
- Private room: 5-letter code. Host sets time, captures, team size,
  and map.

### Weapons

1. Basic gun at spawn
2. Better weapons as map pickups
3. One center super-weapon per map

### Maps

1. **Ruined Town (MVP)**, on foot: pistol, rifle, shotgun, grenades,
   melee. Center: mortar on a clock tower. Ladders, rooftops,
   lockable doors.
2. **Desert Rally** (later), vehicles: mounted MG, rockets, mines,
   oil slick. 2-seat vehicles. Must exit vehicle for the mortar.
   Vehicle destroyed = flag drops.
3. **Sky Islands** (later), jetpacks: SMG, shotgun, grenade launcher,
   sniper. Anti-air cannon. Carrier gets half fuel. Falling = death.

### Later

- Teleport pads, mystery boxes, rare sniper/RPG
- Crown and Bomb modes
- Monetization (no pay-to-win): skins, battle pass, opt-in rewarded ads
- Release: MVP → closed beta (20–50) → soft launch → full launch
- Production: low-end Android testing, server-side anti-cheat,
  store forms, Photon cost scaling

## CODING RULES

- Clean, modular C#; one responsibility per script.
- Folders: Assets/\_Project/{Scripts,Prefabs,Scenes,Materials}
- Follow Photon Fusion 2 Shared Mode patterns (State Authority, Master
  Client for game state).
- Small, testable steps; never dump huge systems at once.
- Use the Unity CLI to check console errors, run Play Mode tests, and
  build. Use screenshots only when verifying visuals.
- Never commit secrets (Photon App ID goes in a gitignored config).


 Unity CLI: if `unity` isn't found, use the full path
  `$env:LOCALAPPDATA\Unity\bin\unity.exe`. If that fails too, ask
  Rishi to run `where.exe unity`.