# Meta-Progression Plan

## Core Philosophy
Start each run with basic pens only. Upgrade gates and cards provide in-run power. Meta-progression (permanent upgrades, unlocks, achievements) happens between runs and persists across game sessions.

---

## 1. Currency System

| Term | Description |
|------|-------------|
| **Paperclips** | Earned during runs. 1 Paperclip per enemy kill. Bonus from bosses/waves |
| **Staples** | Premium currency from achievements and wave milestones |

**Earning rates:**
- Basic enemy kill: 1 Paperclip
- Elite enemy: 3 Paperclips
- Boss kill: 10 Paperclips + 1 Staple
- Wave completion bonus: 5 Paperclips per wave
- First-time achievements: Staple rewards

---

## 2. Upgrade Shop (Permanent Upgrades)

Purchased with Paperclips. Persist across all runs.

| Upgrade | Max Level | Costs | Effect per Level |
|---------|-----------|-------|------------------|
| Sharper Pens | 5 | 50/100/200/400/800 | +2 base damage |
| Faster Firing | 5 | 50/100/200/400/800 | +0.1 base fire rate |
| Sturdier Pens | 5 | 50/100/200/400/800 | +1 pen bounce |
| Armor Plating | 5 | 75/150/300/600/1200 | +20 starting HP |
| Office Coffee | 3 | 100/300/600 | +5% move speed |
| Bulk Order | 3 | 200/500/1000 | +1 starting pen count |

---

## 3. Unlockable Playstyles

Unlocked permanently with Staples. Selectable before starting a run.

| Playstyle | Cost | Effect |
|-----------|------|--------|
| Rapid Fire | Free (starter) | 3 fire rate, 6 dmg |
| Split Shot | 5 Staples | 1.5 fire rate, 7x3 dmg |
| Power Shot | 10 Staples | 0.6 fire rate, 30 dmg |

---

## 4. Achievement System

| Achievement | Requirement | Reward |
|-------------|-------------|--------|
| First Steps | Complete wave 1 | 1 Staple |
| Wave Rider | Reach wave 10 | 3 Staples |
| Wave Master | Reach wave 25 | 10 Staples |
| Pen Pincher | Kill 100 enemies total | 2 Staples |
| Office Massacre | Kill 1000 enemies total | 10 Staples |
| Unscathed | Kill a boss without taking damage | 3 Staples |
| Marathon | Survive 30 min in one run | 5 Staples |
| Collector | Unlock all playstyles | 15 Staples |
| Maxed Out | Fully upgrade all shop items | 25 Staples |
| Papercut | Deal 1000 damage in one run | 2 Staples |

---

## 5. Stat Tracking

Persistent stats shown in lobby.

| Stat | Tracked |
|------|---------|
| Total enemies killed | Cumulative |
| Total damage dealt | Cumulative |
| Highest wave reached | Best run |
| Total runs played | Counter |
| Total time played | Cumulative seconds |
| Total Paperclips earned | Cumulative |

---

## 6. What Stays / Changes In-Game

| Keep | Remove from starting loadout |
|-----|------------------------------|
| Pens shooting | Scissors (PaperShredderBlade) |
| Enemy waves | Coffee dog (DeskBuddy) |
| Lane movement (A/D) | |
| Upgrade gates + cards | |
| Score/HUD display | |
| Game Over → back to lobby | |

---

## 7. Data Persistence

All data via `FileSystem.Data` (survives game restarts):

```
/progression/
  currency.json
  upgrades.json
  unlocks.json
  achievements.json
  stats.json
```

---

## Implementation Order

1. Create `Code/Progression/` folder with data classes (CurrencyData, ShopData, UnlockData, AchievementData, StatsData)
2. Add persistent save/load manager
3. Wire Paperclip earning on enemy kill
4. Create Shop UI panel
5. Create Achievement UI panel
6. Wire permanent upgrades into ArrowPlayer stats at run start
7. Remove scissors/dog from starting loadout
8. Add stat tracking
