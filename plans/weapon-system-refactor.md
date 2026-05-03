# Weapon System Refactor — Playstyle Choice

## Core Mechanic
After beating the first miniboss (wave 1), you pick 1 of 3 playstyles. This defines your run.
Gates then upgrade that playstyle further.

## Office Theme Naming

| Mechanic | Office Name | Description |
|----------|-------------|-------------|
| Arrow | **Paper Plane** | Paper planes launched from a rubber band |
| Arrow split | **Paper Chain** | Splits into multiple planes on hit |
| Sword | **Paper Shredder** | Shredder blades burst outward |
| Sword burst | **Shredder Pulse** | Multiple shredding pulses |
| Damaged enemies | **Shredded docs** | Office documents being destroyed |
| Enemies | **Memos / Reports / Bosses** | Office problems to deal with |

## Three Playstyles (choose at wave 1 miniboss)

### 1. Rapid Fire (Speed Build)
| Stat | Starts at | Gains per gate |
|------|-----------|---------------|
| Fire Rate | High (start at 3/s) | +0.5/s |
| Damage | Low (base 5) | +3 |
| Split | None (0) | (locked) |

**Playstyle**: Spray and pray. Constant paper plane stream, chip damage adds up fast.

### 2. Split Shot (Multi Build)
| Stat | Starts at | Gains per gate |
|------|-----------|---------------|
| Fire Rate | Medium (2/s) | +0.2/s |
| Damage | Reduced (8) | +2 |
| Split | Starts with 2 | +1 per gate |

**Playstyle**: Each shot splits into multiple planes. Screen coverage. Great vs grouped enemies.

### 3. Power Shot (Heavy Build)
| Stat | Starts at | Gains per gate |
|------|-----------|---------------|
| Fire Rate | Low (0.5/s) | +0.1/s |
| Damage | High (25) | +10 |
| Split | None (0) | (locked) |

**Playstyle**: Slow, heavy hits. One-shot small enemies. Satisfying big numbers.

## How the system works
- **Base weapon** is always Paper Plane (the projectile)
- **`UpgradeState`** tracks which playstyle was chosen
- **Gates** during waves upgrade the chosen playstyle's stats
- Gates that don't match your playstyle still give generic bonuses (damage, range)
- Playstyle choice happens once per run (after wave 1 miniboss)

---

## File Changes

### 1. `Code/Upgrades/UpgradeData.cs` — Update UpgradeType enum
Add:
- `SwordFrequency` (replaces current sword cooldown)
- `SwordRange` (burst radius)
- `SplitCount` (arrow split)

### 2. `Code/Weapons/Sword.cs` — NEW: Sword burst weapon
- Scans for nearby enemies
- Bursts at interval, damaging all enemies in radius
- Burst count = multiple pulses per activation

### 3. `Code/Player/ArrowPlayer.cs` — Add sword logic
- `OnUpdate()`: also handle sword bursting alongside arrow firing
- Remove old `Arrow` references from combat section, redirect to new weapon system

### 4. `Code/Projectiles/Arrow.cs` — Add split logic
- On hit or distance limit, if split count > 0, spawn N smaller arrows
- Split arrows inherit damage/speed but have shorter range

### 5. Upgrade Gates
- Damage Gate → upgrades both ArrowDamage and SwordDamage
- Fire Rate Gate → upgrades ArrowFrequency (arrows) or SwordFrequency (sword) based on which is lower
- Need to split into more specific gates later

---

## Gate Types (Revised)

| Gate | What it upgrades |
|------|-----------------|
| **DAMAGE** (red) | ArrowDamage + SwordDamage |
| **FIRE RATE** (blue) | ArrowFrequency or SwordFrequency (whichever is lower) |
| **SPLIT** (green, new) | Arrow split count |
| **BURST** (orange, new) | Sword burst count |
| **RANGE** (purple, new) | ArrowDistance + SwordRange |

---

## Pause During Selection
- When entering `UpgradeSelect` state → `Scene.TimeScale = 0`
- When returning to `Playing` state → `Scene.TimeScale = 1`
- This pauses all physics/updates while reward cards are shown
- Implement in `GameManager.cs` state transitions

## Implementation Order

| Step | What | Files |
|------|------|-------|
| 1 | Add playstyle choice to upgrade UI (after wave 1 miniboss) | `UpgradePanel.razor`, `GameManager.cs` |
| 2 | Add split logic to Arrow projectile | `Arrow.cs` |
| 3 | Update ArrowPlayer to use playstyle stats | `ArrowPlayer.cs` |
| 4 | Update upgrade bonuses in ArrowPlayer | `ArrowPlayer.cs` |
| 5 | Update gates in WaveManager | `WaveManager.cs` |
| 6 | Add Sword/Shredder later as a separate unlockable playstyle | Future |
