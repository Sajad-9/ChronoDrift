# ChronoDrift — Game Design Document

## Vision

A fast, satisfying, infinitely replayable arena roguelike where **physics and timing beat raw speed**. The player is never overwhelmed — they always have tools — but every tool has a cooldown that demands planning.

The core fantasy: *a fighter pilot who can slow time, summon black holes, and whose every missed shot can still ricochet back to hit something.*

---

## Core Design Pillars

### 1. Every Shot Can Become a Trick Shot
Missed bolts bounce off walls. Each bounce adds +50% damage and turns the bolt gold. A shot that ricochets four times and kills a shielded Orbiter feels like a highlight reel moment every time.

### 2. Abilities Feel Ultimate
Each ability has a distinct "character":
- **Railgun** — precision, fast, satisfying
- **Singularity** — crowd control gravity well; cinematic pull and boom
- **Phase-Dash** — escape, repositioning, with an EMP upgrade that makes it offensive
- **Black Hole Rift** — the ultimate. Slow. Dramatic. Screen-shaking. Worth saving for.
- **Chrono Focus** — the "bullet time" button. Slows everything but drains fast — earned by killing.

### 3. Build Identity Through Upgrades
The 11-upgrade pool ensures no two runs feel identical. The player's ship evolves from a simple railgun fighter into one of many archetypes:
- **Ricochet Sniper** — Ricochet Mastery × 3, Arc Lightning, Overclock Core
- **Black Hole Bomber** — Rift Core Overcharge × 3, Phase Shockwave
- **Orbital Tank** — Orbital Aegis, Hull Reinforcement, Chrono Siphon, Void Amplifier

### 4. The Arena is a Character
The 42×42-unit arena has 4 ricochet pillars placed at tactical mid-range positions. They're not just decoration — they define angles, create cover for enemies, and enable geometry plays. The floor is a glowing neon grid. The walls glow blue. The arena breathes.

---

## Difficulty Curve

| Wave | Enemies Spawned | New Threat |
|---|---|---|
| 1 | 7 Swarmers | — |
| 2 | 9 mix (Swarmer/Sniper) | Sniper telegraph |
| 3 | 11 mix + Orbiters | Shield deflection |
| 4 | 13 mix + Elites + MineLayer | DriftTrail dasher + mines |
| 5 | Hex-Titan Overlord + 5 fodder | Boss bullet-hell phase |
| 6–9 | Scaled mix | +16% HP per wave |
| 10 | Boss wave 2 — harder stats | Faster spiral, more nova bolts |

Dynamic scaling: enemy HP multiplies by `1 + (wave-1) × 0.16`.

---

## Achievement Design Philosophy

Achievements reward *skill*, not grind. They're:
- **Visible immediately** (sliding banner popup, 2.4s duration)
- **Short-session** — achievable in a single run
- **Layered** — first Triple Kill is easy, Killing Spree is a flex, Combo × 16 is mastery

They serve a dual purpose: teaching mechanics (Ricochet Master teaches wall-bounce) and celebrating skill expression.

---

## Audio Design

The entire soundscape is synthesized. Design rationale:
- **Railgun**: descending sweep → feels like energy being expelled
- **Ricochet**: ascending pitch per bounce → audio feedback of increasing power
- **Singularity**: rising sweep → tension building to detonation
- **Black Hole**: deep descending sweep (320→28 Hz) → gravitational pull sensation
- **Chrono ambient pitch-shift** → reinforces the subjective experience of time slowing

The ambient track uses A1–D2 bass progression (a minor feel) to establish unease. When Chrono Focus activates, the track drops to 0.62× pitch — the world "slows down" sonically.

---

## Roguelite Upgrade Design

### Why 11 options in a pool of 3?

The upgrade system uses Fisher-Yates shuffle. With 11 options, the chance of seeing the same combo twice in a 10-wave run is very low. Each run feels different from the last.

### Upgrade synergies

| Combo | Synergy |
|---|---|
| Ricochet + Arc Lightning | Bounced bolts chain to multiple enemies |
| Split Shot + Ricochet | 5 bolts ricocheting produces chaotic coverage |
| Orbital Aegis + Void Amplifier | Shields block while Singularity crowds enemies |
| Rift Core × 2 + Phase Shockwave | Ultimate recharges fast; dash clears the aftermath |
| Chrono Siphon + Overclock Core | Kill fast, slow time free — the speed-demon build |

---

## Technical Notes

### Procedural Mesh Factory
All 3D shapes are hand-computed vertex arrays:
- **Delta-wing ship** — 8-triangle craft with cockpit rise and wing sweep
- **Tetrahedron** — 4 faces, used for Swarmers/Elites
- **Diamond** — 8-face bipyramid for Snipers
- **Octahedron** — 8-face for Orbiters/MineLayer
- **Custom boss hull** — hexagonal body + rotating ring nodes

### Material System
`MaterialLibrary` caches Unity Standard shader instances keyed by name. All emissive colours are set via `_EmissionColor`. The arena floor uses a procedurally generated grid texture (256×256, C# pixel-by-pixel).

### No Physics Rigidbodies
All movement is manual `transform.position` manipulation. This avoids physics jitter and gives precise control over all movement feel. Collision uses `Physics.OverlapSphere` and `Physics.SphereCast`.

---

*Document version: 2.0 — updated after the v2 feature pass.*
