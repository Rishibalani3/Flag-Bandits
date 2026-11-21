# Map 1 — Ruined Town (spec v1, by Claude)

## Goals
- Small and dense: ~80m x 50m playable area. Fits 3v3 to 5v5.
- Mirrored layout (west vs east) so both teams are equal.
- 3 routes between bases, each with a different risk.

## Layout (top view)
        NORTH: ROOFTOP LANE (fast, exposed, planks between roofs)
 [BLUE BASE] — MAIN STREET — [TOWN SQUARE + CLOCK TOWER] — MAIN STREET — [RED BASE]
        SOUTH: MARKET ALLEYS (slow, tight, covered, lockable doors)

## Bases (each end)
- Ruined courtyard with flag on a raised platform
- 2 entrances: front (street) and side (alley)
- Respawn point behind the flag

## Routes
| Route | Speed | Cover | Best for |
|---|---|---|---|
| Rooftops | Fastest | Low (open) | Rifle users, quick flag runs |
| Main street | Medium | Medium (cars, rubble) | Team pushes |
| Alleys | Slowest | High (walls, doors) | Shotguns, sneaky flag carries |
- Ladders connect street and rooftops: 2 per side, plus 2 at the square
- Lockable doors: 4 in the alleys (2 per side), lock for 5s,
  10s cooldown

## Center: Clock Tower Mortar
- Mortar on the tower top, reached by 2 ladders (one each side).
- Tower top is exposed: visible from all rooftops.
- Mortar: 3s reload, minimum range 15m (it can't hit the square
  itself), wide splash.
- Counterplay: snipe the operator from the rooftops, or hide in the
  covered alleys.

## Weapon pickups (mirrored per side)
- Shotgun: in the alleys, near mid
- Rifle: on the rooftops, near mid
- Grenades (x2): main street, near the square
- Melee: available to everyone by default
- Respawn timer for pickups: 20s

## Flag-carrier routes (carrier is slowed)
- Rooftop run: fastest, but the mortar and rifles can spot you
- Alley run: safest, but slow and blockable by doors
- This tension is the core of the map.

## Bot navigation
- NavMesh on all walkable surfaces
- NavMesh Links for ladders and rooftop planks