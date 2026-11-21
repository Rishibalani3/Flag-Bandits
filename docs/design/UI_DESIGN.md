# UI Design — Flag Bandits (spec v1, by Claude)

## Principles
1. **Thumb-first:** designed for a 5–6" phone in landscape, then
   scaled up for desktop.
2. **2 taps to a match:** Main Menu → Quick Play → playing.
3. **Readable in chaos:** big, bold, high-contrast; the HUD only shows
   what matters right now.
4. **Cartoony and bouncy:** rounded shapes, thick outlines, playful
   motion.

## Theme
### Colors (UITheme.asset)
| Token | Hex | Use |
|---|---|---|
| bg | #1B1F3B | Menu backgrounds |
| panel | #2A2F5A | Cards, panels, popups |
| panelLight | #3A4075 | Hover/pressed, input fields |
| accent | #FFC93C | Main buttons (PLAY), highlights |
| text | #FFFFFF | Primary text |
| textDim | #B8C0E0 | Secondary text, hints |
| success | #3DDC84 | Ready, capture, positive |
| danger | #FF4D5E | Errors, low health, leave |
| teamBlue | #2D8CFF | Blue team |
| teamOrange | #FF8A1F | Orange team |

**Teams are Blue vs Orange (not red)**, because that pair stays
distinguishable for colorblind players. Teams also get a shape:
Blue = ◆ diamond, Orange = ▲ triangle, used on flags, badges, and
markers.

### Fonts (free, OFL license — record in docs/ASSETS.md)
- Headings and buttons: **Lilita One** (bold, rounded, cartoony)
- Body and numbers: **Nunito Bold / ExtraBold**
- Both as TextMeshPro font assets with a dark outline (0.2) for
  in-game text.

### Sizes (reference 1920×1080)
| Element | Size |
|---|---|
| Min touch target | 120×120 px |
| Main button (PLAY) | 520×160 px |
| Standard button | 360×120 px |
| Heading text | 72 px |
| Body text | 40 px |
| Smallest text allowed | 32 px |
| Corner radius | 28 px |
| Screen edge padding | 48 px (inside the safe area) |

- Landscape only. All screens respect the safe area (notches).
- Canvas Scaler: Scale With Screen Size, 1920×1080, match 0.5.

## Screen Flow
```
Splash/Loading → Main Menu
Main Menu → Quick Play → Matchmaking → Match
Main Menu → Private Room → (Create → Lobby) or (Join code → Lobby) → Match
Main Menu → Settings
Match → Results → (Play Again → Matchmaking) or (Main Menu)
Any screen → Error/Reconnect popup
```

## Screens

### 1. Splash / Loading
- Logo centered, loading bar below, a random tip at the bottom
  ("Flag carriers are slow — escort them!").

### 2. Main Menu
```
┌──────────────────────────────────────────────┐
│ [Player name ✎]                  [⚙ Settings]│
│                                              │
│   (3D character idle, left half)   FLAG      │
│                                   BANDITS    │
│                              [  ▶ PLAY   ]   │ ← accent, biggest
│                              [ PRIVATE ROOM ]│
│                                              │
│ [Shop🔒] [Pass🔒] [Profile🔒]   v0.1         │ ← post-MVP, locked
└──────────────────────────────────────────────┘
```
- PLAY = Quick Play. The character does a fun idle animation.
- Locked buttons show "Coming soon" when tapped.

### 3. Matchmaking
- Big spinner (a spinning flag), "Finding players… 0:12"
- Player slots fill in as people join; empty slots say "Bot" when
  the match starts.
- [CANCEL] button at the bottom.

### 4. Private Room
**Choice:** two big cards, [CREATE ROOM] and [JOIN ROOM].
**Join:** 5 large letter boxes (auto-uppercase, paste supported),
then [JOIN].
**Lobby:**
```
┌──────────────────────────────────────────────┐
│ Room code: KQXZP [📋 Copy] [Share]   [Leave] │
│ ┌── BLUE ◆ ──┐            ┌── ORANGE ▲ ──┐   │
│ │ Rishi 👑 ✓ │  [⇄ Swap]  │ Aman      ✓  │   │
│ │ Priya   ✓  │            │ (empty)      │   │
│ └────────────┘            └──────────────┘   │
│ Host settings: Map [Ruined Town ▾]           │
│  Time [− 6 min +]  Captures [− 3 +]          │
│  Team size [− 3 +]  Bots [on/off]            │
│                [ READY ]  [ START ] (host)   │
└──────────────────────────────────────────────┘
```
- Only the host can edit settings; others see them read-only.
- START is enabled when there are at least 3 players and everyone
  is ready. Uneven teams are allowed.

### 5. Settings
Tabs: **Controls** (sensitivity, invert Y, button layout editor
later), **Audio** (master, music, SFX), **Graphics** (Low/Med/High,
FPS 30/60), **Account** (name).

### 6. In-Game HUD
```
┌──────────────────────────────────────────────┐
│ kill feed      ◆ 2  [ 4:32 ]  1 ▲     [⏸]   │
│ (top-left)     ◆ status   ▲ status           │
│                                              │
│                      +  ← crosshair          │
│  ◄ off-screen flag arrows on screen edges ►  │
│                                              │
│  (joystick)      ████ 100 HP     [🔄] [💣]   │
│                  Rifle 24/90     [⬆]  [🔫]   │
└──────────────────────────────────────────────┘
```
- **Top center:** score + timer. Under each score, a flag status
  icon: At base 🏳 / Taken (carrier name) / Dropped (return
  countdown ring).
- **Off-screen arrows:** always point to both flags (team colored).
- **Bottom center:** health bar (turns danger red below 30%) + weapon
  and ammo.
- **Context button:** appears above Fire only when relevant: "Pick
  up", "Climb", "Lock door", "Use mortar".

### 7. Mobile Controls
| Control | Position | Behaviour |
|---|---|---|
| Move | Left half, floating joystick | Appears where the thumb lands |
| Aim/look | Right half, drag | Drag anywhere not on a button |
| Fire | Bottom right, 180px | Hold to fire; light aim assist |
| Jump | Left of Fire | Tap |
| Reload | Above Fire | Tap (auto-reloads when empty) |
| Grenade | Above Jump | Tap = throw; hold = show arc |
| Context | Above Reload | Only when available |
- All buttons are semi-transparent (60%) and brighten on press.
- Desktop: WASD, mouse, Space, R, G, E; key hints are shown only on
  desktop.

### 8. Flag Carrier State
- Screen border glows in the enemy flag's color.
- Top banner: "YOU HAVE THE FLAG — GET HOME!"
- Big arrow to your base; Fire is limited to the basic gun (other
  weapon icons greyed out).

### 9. Feedback
- Hit marker: white X; red X + "ding" on a kill
- Damage direction: red arc on the screen edge
- Capture: full-width banner "◆ BLUE CAPTURED!" + confetti + sound
- Flag taken or dropped: short banner + sound
- Low health: red vignette pulse

### 10. Mortar View
- Camera switches to a top-down map view.
- Drag to aim; the landing circle shows the splash radius and turns
  red inside the 15m minimum range.
- Reload ring (3s) around the Fire button; [EXIT] top-right.
- While in the mortar the player is exposed: show a warning icon if
  an enemy is aiming at you.

### 11. Death / Respawn
- Killed-by card: "Killed by Aman (Shotgun)", 1.5s.
- **Respawn choice** (5s timer, auto-picks base):
  `[🏠 Base]` or a living teammate's card `[Priya — near mid]`.

### 12. Scoreboard (hold Tab / tap score)
- Two team columns: name, kills, deaths, captures, ping.

### 13. Results
- Huge "VICTORY" (success) / "DEFEAT" (danger) / "DRAW".
- MVP player card, team stats, coins earned.
- [2× COINS — watch ad] (post-MVP, opt-in), [PLAY AGAIN], [MENU].

### 14. Popups
- Error / Reconnect / Confirm leave: centered panel, title, message,
  max 2 buttons. Dim the background to 60% black.
- Toasts (small, top center, 2s) for minor info ("Code copied").

## Components (prefabs in Prefabs/UI/)
PrimaryButton, SecondaryButton, IconButton, Panel, Popup, Toast,
TeamBadge, PlayerCard, Stepper (− value +), Toggle, Dropdown,
CodeInput (5 boxes), FlagStatusIcon, KillFeedItem, OffscreenArrow,
Banner.

## Motion
- Button press: scale to 0.92 in 0.08s, bounce back.
- Panels: pop in (scale 0.8 → 1 with overshoot, 0.2s).
- Banners: slide in from the top, hold 2s, slide out.
- Use one small tween script of our own (no paid plugins).

## Rules
- All text strings go in one strings file/ScriptableObject
  (for localization later).
- No information conveyed by color alone (always add a shape or
  icon).
- The HUD must never cover the crosshair or the center of the
  screen.

## Build Order (Phase 8b)
1. UITheme + fonts + core components
2. HUD (score, timer, flag status, health, ammo)
3. Mobile controls
4. Main Menu → Matchmaking → Results
5. Private Room (create/join/lobby)
6. Respawn, mortar view, scoreboard, popups, settings