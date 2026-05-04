# Project Audit v2 — Arrow a Row Clone (Office Theme)

## ✅ Completed Features

### Core Gameplay Loop
- **PlayerController** — S&Box PlayerController + ArrowPlayer for lane movement
- **Pen Projectile** — Speed, Damage, MaxDistance, Split, Bounce, Pierce mechanics
- **Enemies** — Basic, Fast, Tank, Boss types
- **Wave Manager** — Dynamic spawning, wave tracking, DPS-based enemy HP
- **Game Manager** — State machine (Lobby, Playing, UpgradeSelect, GameOver)

### Walking-Forward System
- Auto-walk, tile recycling, camera follower, static enemies

### Weapon System (3 Playstyles)
- Rapid Fire (3/s, 6 dmg), Split Shot (1.5/s, 7×3), Power Shot (0.6/s, 30)
- Card selection after wave 1, pause on every wave clear

### Upgrade Gates
- 3 gates per spawn, 6 types (DAMAGE, FIRE RATE, PEN+X, CD DOWN, BLADE+X, RANGE)
- Random variable amounts (e.g. BLADE +1 or +2)
- Physics rigidbodies with gravity — pushable
- Label flies into player on pickup (FlyToPlayer)
- Auto-destroy 5s after pickup
- Enemy drops: 30% chance on death

### Scissors Companion
- Orbiting, burst-fire at nearest enemy
- Blade bounce (chains between enemies)
- Smooth X=90 rotation on seek, X=0 on return
- Custom model assignable

### Desk Buddy (Coffee Dog)
- Follows behind at Y=50, Z=50
- Sine-wave bobbing at 2Hz
- Shoots homing coffee projectiles at nearest enemy
- Starts with 1 dog (PetCount=1)
- PetFireRate = +0.5 shots/s per level
- Custom dog model + projectile model assignable

### Unique Card Upgrades
- Bouncing Pens (wall bounces), Piercing Pens (enemy pierce), Blade Bounce
- 13 total upgrade types

### Audio
- 4 SoundEvents + volume sliders (pen throw/hit, scissor launch/hit, dog fire)
- Mixer: Game channel, occlusion disabled

### UI
- HUD (health bar, wave, score, per-player stats)
- Upgrade panel (playstyle + cards)
- Enemy HP world panels
- Gate labels on world panels

### Models
- Pen, Scissors, Coffee Dog Buddy, Coffee Rocket, Gate model
- All assignable via inspector properties

### Death
- Ragdoll, body hide, physics freeze, forward stop

---

## ❌ Not Yet Implemented

| Priority | Feature | Why |
|----------|---------|-----|
| **🟡 Medium** | **Game Over screen** | Show score, play again. GameManager.GameOver state already wired. Just needs a Razor panel. |
| **🟡 Medium** | **Visual polish** | Damage numbers floating on hit, pen trails, enemy death effects |
| **🟢 Low** | **Boss mechanics** | Printer (paper wads), HR Manager (slow), CEO — currently just big HP |
| **🟢 Low** | **Lobby / Main Menu** | Host/join, ready-up, scene transitions |
| **🟢 Low** | **Meta-progression** | Permanent upgrades between runs |

---

## Files (19 Code Files, ~4,000 lines)

| Group | Files |
|-------|-------|
| **Core** | GameManager.cs, ArrowPlayer.cs, Pen.cs |
| **Enemies** | Enemy.cs |
| **Waves** | WaveManager.cs |
| **Upgrades** | UpgradeData.cs, UpgradeManager.cs, UpgradeGate.cs, UpgradePanel.razor |
| **Companions** | PaperShredderBlade.cs, DeskBuddy.cs, BuddyProjectile.cs |
| **Ground** | TileRecycler.cs, CameraFollower.cs |
| **UI** | GameHud.razor, EnemyHpPanel.razor, GateLabel.razor, HudManager.cs |
| **Assets** | pen, scissors, coffeedogbuddy, coffeerocket models + textures; 5 sound files |
