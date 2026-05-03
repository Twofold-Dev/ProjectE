# Office Theme — Brainstorm

## Core Concept
You're an office worker trapped after hours. Office supplies have come to life. Survive the nightly office cleanup wave.

---

## Player Identity

| Concept | Description |
|---------|-------------|
| **Temp Worker** | Hired for "night shift organization" — it's a warzone |
| **Intern** | Lowest on the totem pole, has to fight through |
| **Facilities Night Guard** | Like FNAF but with office supplies |

## Projectiles (What you shoot)

| Theme | Description | In-Game Mechanic |
|-------|-------------|------------------|
| **Paper Plane** | Folded A4 paper | Standard projectile, fast, basic |
| **Stapler Shot** | Launch a stapler | Heavy damage, slower |
| **Rubber Band** | Snap a rubber band | Bounces off walls (ricochet upgrade) |
| **Pencil** | Throw a sharpened pencil | Pierces through enemies |
| **Paper Shredder** | Shredder blade arc | AoE cone attack, short range |
| **Sticky Note** | Throw a sticky note | Slows enemies on hit (debuffer) |
| **Coffee Splash** | Throw hot coffee | AoE splash damage |
| **Hole Puncher** | Puncher disc | Returns like a boomerang |

## Enemies

| Theme | Type | Behavior |
|-------|------|----------|
| **Angry Memo** | Basic | Walks toward you, standard HP |
| **TPS Report** | Fast | Moves quickly, low HP, "needs cover sheet" |
| **Filing Cabinet** | Tank | Slow, high HP, blocks other enemies |
| **Coffee Machine** | Spawner | Sits in back, spawns smaller enemies ("espresso shots") |
| **Printer** | Boss | Jams and shoots paper wads, summons memo minions |
| **HR Manager** | Boss | Slows player, reduces fire rate ("write-up") |
| **CEO** | Final Boss | All previous boss abilities, massive HP |
| **Paper Shredder** | Hazard | Stationary, destroys projectiles that enter it |
| **Water Cooler** | Healer enemy | Heals nearby enemies |
| **Keyboard** | Sniper | Periodically fires a single keycap (high damage) |

## Upgrades / Power-ups

| Current Name | Office Theme Name | Mechanic |
|-------------|-------------------|----------|
| Arrow Frequency | **Coffee Rush** | More caffeinated = shoot faster |
| Arrow Damage | **Stapler Power** | Use the heavy-duty stapler |
| Arrow Speed | **Aerodynamics** | Better paper plane folding technique |
| Arrow Range | **Paper Stream** | Longer paper trail |
| Movement Speed | **Office Chair** | Upgraded wheels on your chair |
| Max Health | **Sick Days** | More sick leave banked |
| Sword Count | **Paper Shredder** | Extra shredding blades orbit you |
| Pet Count | **Desk Buddies** | More office friends following you |

## Companions

| Current | Office Theme | Behavior |
|---------|-------------|----------|
| FlyingSword | **Paper Shredder** | Orbits player, damages enemies on contact |
| DragonPet | **Desk Buddy** | Follows player, shoots paperclips at enemies |
| *(new)* | **Sticky Note Blob** | Absorbs one enemy attack then pops |
| *(new)* | **Coffee Cup** | Gives temporary fire rate boost when picked up |

## Unique Office Mechanics

### 1. Overtime Meter
Instead of a standard health bar, you have an **Overtime Meter**. Taking damage adds overtime hours. When overtime hits 100%, you're fired (game over). Certain upgrades reduce overtime accumulation.

### 2. Cubicle Walls
Enemies can be lured into cubicle walls. The walls block enemies for a few seconds before breaking. Players can use this for positioning.

### 3. The 5 o'clock Rush
Every 10 waves, a **"5 o'clock Rush"** trigger appears — all enemies move 2x speed for 15 seconds, then a bonus rewards wave.

### 4. Office Supply Pickups
Periodic pickups fall from the ceiling:
- **Donut** — Full heal
- **Salary Bonus** — Extra score multiplier
- **IT Support** — Free upgrade (choose any)
- **Coffee Refill** — 5 seconds of infinite ammo

### 5. Performance Review (Boss Mechanic)
Bosses do a "Performance Review" — players must stop shooting and "look busy" (stop moving) or take damage during this phase.

## Visual / Audio Ideas

| Element | Idea |
|---------|------|
| Background | Office cubicle farm, water cooler, break room |
| Floor | Carpet tiles / linoleum |
| Player model | Office worker with tie, glasses |
| Soundtrack | Chill lo-fi → intensifies with waves |
| SFX | Stapler *CHUNK*, paper crinkle, keyboard clacks |
| Death effect | "You're Fired!" stamp on screen |
| Wave announcement | "Meeting in 5 minutes" / "New assignment" / "Performance review" |

---

## Priority Implementation Order

1. Rename upgrade display names to office theme (quick win)
2. Add models/materials in editor (visual pass)
3. Implement unique mechanics one at a time
4. Boss battles (Printer → HR Manager → CEO)
