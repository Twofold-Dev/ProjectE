# Pen-Only Baseline Plan

## Objective
Strip the starting loadout to just the Pen weapon (no playstyles, no companions) so we can establish a clean baseline for balance. Progression (playstyles, upgrades) will be re-introduced later from this baseline.

---

## Changes

### 1. Remove playstyle selection from wave 1

**File:** [`Code/Upgrades/UpgradeManager.cs`](Code/Upgrades/UpgradeManager.cs)

Currently after wave 1, `GameManager.OnWaveCompleted()` detects wave 1 and calls `um.OfferUpgrades()` but with a special `!PlaystyleChosen` check. We skip the playstyle selection entirely — the first wave completion goes straight to normal upgrade gates like every other wave.

**Change:** In `GameManager.OnWaveCompleted()`, remove the wave-1 playstyle special case. Also set `PlaystyleChosen = true` initially so the playstyle lock is bypassed.

### 2. Remove playstyle stat modifiers from ArrowPlayer

**File:** [`Code/Player/ArrowPlayer.cs`](Code/Player/ArrowPlayer.cs)

The properties `PlaystyleBaseFireRate`, `PlaystyleArrowDamage`, `PlaystyleArrowSpeed`, `PlaystyleArrowDistance` all return different values based on the chosen playstyle. Since there's no playstyle, they should all return the baseline values:

- `PlaystyleBaseFireRate` → always `1.0` (so the pen fires at 1/s)
- `PlaystyleArrowDamage` → always `10` (so each pen does 10 damage)
- `PlaystyleArrowSpeed` → always the property value (500 default)
- `PlaystyleArrowDistance` → always the property value (800 default)

Alternatively, just remove the playstyle properties entirely and use the base properties directly in `OnUpdate()`:

**In `OnUpdate()`:** Change `var effectiveFireRate = PlaystyleBaseFireRate + GetFireRateBonus();` to `var effectiveFireRate = BaseFireRate + GetFireRateBonus();`

**In `SpawnPen()`:** Remove the playstyle damage/speed/distance overrides.

### 3. Verify companions don't spawn

**File:** [`Code/Player/ArrowPlayer.cs`](Code/Player/ArrowPlayer.cs)

The methods `GetBuddyCount()` and `GetShredderCount()` return values based on the upgrade state (which starts at 0). By default they return 0, so no companions should spawn. This should already work but verify:
- `SyncBuddies()` — only spawns if count > 0
- `SyncShredders()` — only spawns if count > 0

### 4. Resulting baseline

| Stat | Value |
|------|-------|
| Fire rate | 1.0 /s |
| Damage | 10 per pen |
| Pen speed | 500 |
| Pen distance | 800 |
| HP | 50 |
| Movement | 300 px/s |
| Companions | None |

vs current wave 1:
- Rapid Fire: 3/s, 6 dmg → 18 DPS
- Split Shot: 1.5/s, 7x3 → 31.5 DPS
- Power Shot: 0.6/s, 30 → 18 DPS

**New wave 1 DPS:** 1 × 10 = **10 DPS**

### 5. Test and rebalance

With the baseline established, test wave 1:
- 1 Basic enemy (30 HP) → 3 seconds to kill
- With 4 enemies spawning → ~12 seconds to clear
- Player has 50 HP, enemies do ~5-10 damage per hit

If this feels too slow or too hard, adjust `ArrowDamage` and `BaseFireRate` properties on the ArrowPlayer prefab directly (no code change needed — they're `[Property]` fields).

---

## Files to Modify

| # | File | Change |
|---|------|--------|
| 1 | [`Code/GameManager.cs`](Code/GameManager.cs) | Skip playstyle selection in `OnWaveCompleted()` |
| 2 | [`Code/Player/ArrowPlayer.cs`](Code/Player/ArrowPlayer.cs) | Use base properties instead of playstyle properties in fire rate/damage/speed/distance |
