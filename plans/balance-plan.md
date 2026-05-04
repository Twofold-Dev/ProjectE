# Balance Plan — Mathematical Progression

## Current Baseline Stats

### Playstyle DPS at Wave 1 (no upgrades)

| Playstyle | Fire Rate | Dmg/Pen | Pens/Throw | DPS | 1 Basic Enemy (30 HP) |
|-----------|-----------|---------|------------|-----|----------------------|
| **Rapid Fire** | 3/s | 5 | 1 | 15 DPS | 2.0s kill |
| **Split Shot** | 2/s | 8 | 3 | 48 DPS | 1.25s kill (one throw) |
| **Power Shot** | 0.5/s | 25 | 1 | 12.5 DPS | 2.4s kill (2 hits) |

### Enemy Scaling (current)
- Health: `baseHP * 1.08^(wave-1)` — doubles every ~9 waves
- Count: `min(3 + wave/2, 15)` — linear up to 15
- Speed: `baseSpeed * 1.03^(wave-1)`

### Gate Upgrade per pickup (current)
| Gate | Bonus per pickup | After 10 pickups |
|------|-----------------|------------------|
| Fire Rate | +0.2-0.5/s | ~+3.5/s |
| Damage | +3-8 | ~+55 |

---

## Problem Analysis

### Problem 1: Gate scaling is exponential, not linear
After 10 wave completions (~20 gate pickups), a Rapid Fire player has:
- Fire Rate: 3 + (20 * 0.35 avg) = **10/s**
- Damage: 5 + (20 * 5.5 avg) = **115**
- DPS: 10 * 115 = **1,150 DPS**

Wave 10 enemy HP: 30 * 1.08^9 = **60 HP**
Kill time: 60 / 1150 = **0.05 seconds** — instant kill, trivial.

### Problem 2: Split Shot dominates early
Split Shot starts at 48 DPS (3x Rapid Fire, 4x Power Shot). With 3 pens per throw, it clears waves instantly.

### Problem 3: No TTK (Time To Kill) target curve
Without a target TTK, balance is guesswork. Enemies should take roughly **1-3 seconds to kill** at any wave.

---

## Proposed Balance Model

### Core Design Principle
All playstyles should have approximately **equivalent DPS** at the start, with the trade-off being playstyle feel, not raw power.

### Balanced Starting Stats

| Playstyle | Fire Rate | Dmg/Pen | Pens/Throw | DPS | Profile |
|-----------|-----------|---------|------------|-----|---------|
| **Rapid Fire** | 3/s | 6 | 1 | **18 DPS** | Consistent chip |
| **Split Shot** | 1.5/s | 7 | 3 (21 total) | **31.5 DPS** | Bursty, spread |
| **Power Shot** | 0.6/s | 30 | 1 | **18 DPS** | Heavy hits |

Split Shot is higher DPS because its damage is spread across 3 pens — often only 1-2 hit a single target.

### Proposed Gate Values (per pickup)
Gates should give diminishing returns, not linear bonuses:

| Gate | Current | Proposed | After 10 |
|------|---------|----------|----------|
| Fire Rate | +0.2-0.5/s | **+0.15/s** | +1.5/s |
| Damage | +3-8 | **+2-4** | +30 |
| Split Count | 1-2 | **1** | +10 pens (capped) |

**Result after 10 gates (Rapid Fire):**
- Fire Rate: 3 + 1.5 = 4.5/s
- Damage: 6 + 30 = 36
- DPS: 4.5 * 36 = **162 DPS**

### Enemy HP Scaling
Target: 1 basic enemy takes ~2 seconds at wave N

| Wave | Enemy HP | Enemy Count | Total HP |
|------|----------|-------------|----------|
| 1 | 30 | 3 | 90 |
| 5 | 55 | 5 | 275 |
| 10 | 100 | 8 | 800 |
| 15 | 190 | 10 | 1,900 |
| 20 (boss) | 350 | 15 | 5,250 |

**Formula:** `baseHP * 1.12^(wave-1)` (slightly steeper than current 1.08)

### Time-to-Kill Projection

| Wave | Enemy HP | Rapid DPS | Kill Time | Split DPS | Kill Time | Power DPS | Kill Time |
|------|----------|-----------|-----------|-----------|-----------|-----------|-----------|
| 1 | 30 | 18 | 1.7s | 31.5 | 0.95s | 18 | 1.7s |
| 5 | 55 | 55 | 1.0s | 63 | 0.9s | 55 | 1.0s |
| 10 | 100 | 162 | 0.6s | 120 | 0.8s | 162 | 0.6s |
| 15 | 190 | 280 | 0.7s | 200 | 0.95s | 280 | 0.7s |
| 20 | 350 | 400 | 0.9s | 280 | 1.25s | 400 | 0.9s |

All playstyles maintain roughly **0.6-1.7 second kills** across all waves.

### Player Health vs Enemy Damage
Current: Player starts at 100 HP. Enemies deal 10 damage on contact.

**Proposed:** Increase enemy contact damage per wave:

| Wave | Enemy Contact Dmg | Player HP | Hits to Die |
|------|-------------------|-----------|-------------|
| 1 | 10 | 100 | 10 |
| 5 | 18 | 100 | 5-6 |
| 10 | 35 | 120* | 3-4 |
| 15 | 65 | 140* | 2 |
| 20 | 120 | 180* | 1-2 |

*Player HP increases via Health Boost upgrades (gates and cards).

---

## Files to Modify

| File | Change |
|------|--------|
| [`ArrowPlayer.cs`](Code/Player/ArrowPlayer.cs) | Update Playstyle\* values: Rapid Fire (3/s, 6 dmg), Split Shot (1.5/s, 7 dmg, 2 split), Power Shot (0.6/s, 30 dmg) |
| [`ArrowPlayer.cs`](Code/Player/ArrowPlayer.cs:336) | Reduce `GetFireRateBonus()` from `* 0.3` to `* 0.15` |
| [`ArrowPlayer.cs`](Code/Player/ArrowPlayer.cs:344) | Reduce `GetDamageBonus()` from `* 5` to `* 3` |
| [`WaveManager.cs`](Code/Waves/WaveManager.cs:44) | Increase `HealthScale` from `1.08` to `1.12` |
| [`WaveManager.cs`](Code/Waves/WaveManager.cs:433-445) | Update `GetRandomAmount()` for Fire Rate (0.15) and Damage (2-4) |
| [`UpgradePanel.razor`](Code/UI/UpgradePanel.razor:245) | Update playstyle descriptions to match new values |

---

## Summary

| Metric | Current | Proposed |
|--------|---------|----------|
| Gate Fire Rate bonus | +0.3/s | +0.15/s |
| Gate Damage bonus | +5 | +3 |
| Health scale | 1.08 | 1.12 |
| Rapid Fire DPS | 15 | 18 |
| Split Shot DPS | 48 | 31.5 |
| Power Shot DPS | 12.5 | 18 |
| Kill time at wave 1 | 1.2-2.4s | 0.95-1.7s |
| Kill time at wave 20 | 0.05s | 0.6-1.25s |
