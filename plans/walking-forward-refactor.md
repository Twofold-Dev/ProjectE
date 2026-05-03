# Walking Forward + Tile Recycling — Architecture Plan

## Current vs New System

| System | Current (Enemy Approach) | New (Walking Forward) |
|--------|-------------------------|----------------------|
| Player | Stationary at Y=0, Y clamped | Auto-walks forward (constant Y velocity), no clamp |
| Enemies | Spawn at Y=-500, move toward Y=0 | Spawn at `playerY - SpawnDistance`, are STATIC |
| Gates | Same as enemies | Same — spawned ahead, static |
| Arrow direction | Already fires forward ✓ | No change needed |
| Ground | Manual editor placement | Tile recycling system, scrolls with camera |
| Cleanup | Enemies self-destroy at X < -220 | Objects behind camera (Y > playerY + cleanupMargin) destroyed |

---

## File Changes

### 1. `Code/Player/ArrowPlayer.cs` — Walking Forward

**Changes:**
- Remove Y clamping
- Add constant forward walk velocity to WishVelocity
- Keep A/D lane movement on X axis
- Add `[Property] float ForwardSpeed { get; set; } = 200f;` (configurable walk speed)

**New WishVelocity:**
```csharp
_pc.WishVelocity = new Vector3(
    moveInput.y * MoveSpeed,  // A/D lane movement
    -ForwardSpeed,            // constant forward walk
    0
);
```

### 2. `Code/Enemies/Enemy.cs` — Static Enemies

**Changes:**
- **Remove movement** from `OnFixedUpdate()` — enemies no longer move
- **Remove `DamageNearestPlayer()`** — enemies don't reach the player anymore (player reaches them)
- **Remove the Y=0 damage trigger** — obsolete
- Keep collision with arrows (already works)
- Keep HP, sync, death logic

**New OnFixedUpdate:**
```csharp
protected override void OnFixedUpdate()
{
    if ( !IsAlive ) return;
    if ( !Networking.IsHost ) return;
    // Enemies are static targets. Player walks into them.
    // Arrows collide via Arrow.cs distance check.
}
```

### 3. `Code/Upgrades/UpgradeGate.cs` — Static Gates

**Changes:**
- **Remove movement** — gates are static like enemies
- **Remove `if (WorldPosition.x < -250f)` cleanup** — new cleanup system handles it

**New OnFixedUpdate:**
```csharp
protected override void OnFixedUpdate()
{
    if ( !Networking.IsHost ) return;
    // Static gate. Player walks through it.
    CheckPlayerOverlap();
}
```

### 4. `Code/Waves/WaveManager.cs` — Spawn Ahead of Player

**Changes:**
- Add `[Property] float SpawnDistance { get; set; } = 800f;` — distance ahead to spawn
- Enemy spawn Y = `playerY - SpawnDistance` (ahead of player)
- Gate spawn Y = `playerY - SpawnDistance`
- Add cleanup logic: destroy enemies/gates behind the player (`Y > playerY + 200`)
- Remove `HandleUpgradeState` spawn logic (gates spawn during gameplay)

**Spawn logic:**
```csharp
private void SpawnEnemy()
{
    var playerY = GetPlayerY();
    var spawnY = playerY - SpawnDistance;
    var spawnPos = new Vector3(
        Random.Shared.Float( -SpawnXRange, SpawnXRange ),
        spawnY,
        0
    );
    // ... create enemy at spawnPos
}
```

### 5. `Code/Ground/TileRecycler.cs` — NEW: Tile Recycling System

**Concept:**
- Create N ground tiles (e.g., 10 tiles of 200 units each = 2000 units visible)
- Each tile is a flat ModelRenderer with a tiling texture
- Tiles are placed in a line
- When a tile moves behind the camera's view (Y > cameraY + tileSize), teleport it to the front

**Pseudo:**
```csharp
public sealed class TileRecycler : Component
{
    [Property] public int TileCount { get; set; } = 10;
    [Property] public float TileSize { get; set; } = 200f;
    
    private List<GameObject> _tiles = new();

    protected override void OnStart()
    {
        // Create tiles at Y = 0, -200, -400, ..., -(TileCount * TileSize)
        for (int i = 0; i < TileCount; i++)
        {
            var tile = new GameObject(true, $"GroundTile_{i}");
            tile.WorldPosition = new Vector3(0, -i * TileSize, 0);
            tile.LocalScale = new Vector3(500, 1, TileSize);
            var model = tile.Components.Create<ModelRenderer>();
            model.Model = Model.Plane;
            model.Tint = Color.Gray;
            _tiles.Add(tile);
        }
    }

    protected override void OnUpdate()
    {
        var playerY = GetPlayerY();
        var frontEdge = playerY - (TileCount * TileSize * 0.5f);
        
        foreach (var tile in _tiles)
        {
            // If tile is behind the player, move it to the front
            if (tile.WorldPosition.y > playerY + 200f)
            {
                var newY = frontEdge - TileSize;
                tile.WorldPosition = tile.WorldPosition.WithY(newY);
            }
        }
    }
}
```

### 6. `Code/Projectiles/Arrow.cs` — No Change Needed
- Arrow already moves with `Vector3.Right` and hits enemies via distance check
- Works the same whether enemies are moving or static

### 7. `Code/GameManager.cs` — HUD Scene Setup
- TileRecycler should auto-run on start
- Could create it in code or have the user add it in the editor

---

## Coordinate Reference (Y axis = forward)

```
Player (auto-walks negative Y direction)
  ↓ ↓ ↓ ↓ ↓ ↓ ↓
  Y = playerY
  Y = playerY - 200    ← cleanup zone (destroy behind player)
  ─────────────────
  Y = playerY - 800    ← spawn zone (static enemies/gates appear here)
  Y → -∞               ← never reached, things keep spawning ahead
```

---

## Implementation Order

| Step | Task | Dependency |
|------|------|-----------|
| 1 | Remove enemy/gate movement, make them static | None |
| 2 | Add auto-walk to ArrowPlayer (remove Y clamp) | Step 1 |
| 3 | Update WaveManager spawn to be ahead of player | Step 2 |
| 4 | Add cleanup (destroy behind player) | Step 3 |
| 5 | Create TileRecycler ground system | Step 2 |
| 6 | Test full loop | All steps |

---

## Risks & Considerations

| Risk | Mitigation |
|------|-----------|
| Player walks past enemies before hitting them | SpawnDistance needs tuning. If player speed > enemy spawn rate, increase spawn density |
| Gates pass through player without registering | Increase overlap detection radius or spawn gates closer together |
| Ground tiles have visible seams | Use tiling textures or add surrounding fog |
| Performance with many spawned objects | Cleanup system must be aggressive; tile count is fixed |
| Co-op sync issues | Player positions are synced via PlayerController — spawn positions calculated from host player Y |
