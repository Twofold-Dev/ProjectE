# Upgrade Selection Flow Plan

## Design
- Each player sees BOTH panels (left = other player, right = self)
- Each player can ONLY click their OWN cards (right panel)
- Other player's cards are visible but NOT clickable
- Both must select individually before game proceeds
- After selecting, panel shows "Chosen" for that player
- "Waiting for other player..." shown until both are done

## Implementation

### UpgradePanel.razor
- Cards in the LOCAL player's panel (`um.Network.IsOwner = true`) → clickable
- Cards in the OTHER player's panel (`um.Network.IsOwner = false`) → NOT clickable, dimmed
- Show "Waiting for opponent..." text when one player has selected but the other hasn't

### CheckAllReady fix
- `CheckAllReady()` runs on HOST after EACH RPC selection
- Host has DIRECT access to all UMs' `HasSelected` states (authoritative)
- No `[Sync]` dependency — host reads the values directly
- If both `HasSelected` are true → `_gm.OnAllPlayersReady()`

### The REAL bug
The issue was likely that `HasSelected` was being set via `[Sync]` on proxy objects, and the host's local copy wasn't reflecting the change immediately. Fix: set `HasSelected` directly on the host's authoritative UM references.

## Approach
1. **Restore `Sandbox.NetworkHelper` in both scenes** (Voxel Party pattern)
2. **Remove `Networking.CreateLobby()` from GameManager** (NetworkHelper handles it)
3. **GameManager keeps**: state machine, scene transitions (`Game.ActiveScene.LoadFromFile`), ready sync, and spawn queue for game scene
4. **Simple queue pattern** from clover_meadows: `OnFixedUpdate()` processes `_spawnQueue`
5. **One file to rewrite**: `GameManager.cs`
6. **Scene edits**: restore `Sandbox.NetworkHelper` component in both scenes

## Goal
Replace the messy accumulated patches in `GameManager.cs` with a clean implementation based on clover_meadows' pattern. **Only one file to rewrite.**

## Key Pattern from clover_meadows

clover_meadows uses:
1. `OnFixedUpdate()` — processes spawn queue every frame (not OnStart, not flags)
2. `SpawnPlayer(Connection)` — single method, checks dedup internally
3. `OnActive()` — adds new connections to spawn queue (for INetworkListener)
4. No built-in NetworkHelper — GameManager handles everything

## Proposed Clean GameManager.cs Structure

```
GameManager
├── [Sync] fields: State, ReadyCount, ReadyStateIds
├── [Property] fields: GameScenePath, LobbyScenePath, PlayerPrefab, GateModel
├── Private fields: _readyPlayerIds (HashSet), _spawnQueue (List<Connection>)
├── Progression fields (unchanged)
│
├── OnStart()
│   ├── Init progression
│   ├── if (!Networking.IsActive) → Networking.CreateLobby()
│   └── if (isHost && WaveManager exists) → queue all connections for spawn
│
├── OnFixedUpdate()
│   └── if (isHost) → ProcessSpawnQueue()
│
├── ProcessSpawnQueue()
│   └── For each in _spawnQueue.ToList() → SpawnPlayer() + remove
│
├── SpawnPlayer(Connection)
│   └── if PlayerPrefab.valid && no ArrowPlayer for owner → Clone + NetworkSpawn
│
├── OnActive(Connection) [INetworkListener]
│   └── if (State == Playing) → _spawnQueue.Add(channel)
│
├── HostStartGame() [host-only]
│   └── Game.ActiveScene.LoadFromFile(GameScenePath)
│
├── TogglePlayerReady() [Rpc.Host] + IsPlayerReady()
│   └── Sync via ReadyStateIds (keep current working logic)
│
├── OnUpdate() [host-only]
│   ├── Auto-save progression
│   └── Broadcast leaderboard stats
│
├── ISceneStartup: OnHostPreInitialize, OnHostInitialize, OnClientInitialize
│
└── Game state: OnPlayerDied, OnWaveCompleted, OnAllPlayersReady, AddScore
```

## Changes from Current Messy State

| Current (Messy) | Clean (Proposed) |
|-----------------|------------------|
| `_pendingSpawn` flag + OnUpdate check | `_spawnQueue` + `OnFixedUpdate()` loop |
| `SpawnPlayersForAllConnections()` — one-shot | `ProcessSpawnQueue()` — runs every frame |
| Spawn logic mixed with OnStart game detection | Queue + Process pattern |
| `OnActive()` — separate implementation | Same, but adds to queue instead of spawning directly |
| `WaveManager` detection in OnStart | Same — kept as-is |

## Scenes (no changes needed)

Both scenes already have:
- Built-in NetworkHelper **removed** (done by user)
- GameManager with `PlayerPrefab` assigned in **arrow_game.scene** (confirmed)
- Lobby scene doesn't need PlayerController spawning — UI works via `Connection.All`

## Files to Change

| File | Action |
|------|--------|
| `Code/GameManager.cs` | Full clean rewrite (but keep progression, leaderboard, upgrade logic) |

## Remaining Files (NOT Changed)

- `Code/UI/MainMenuPanel.razor` — works as-is
- `Code/UI/GameHud.razor` — works as-is
- `Assets/scenes/lobby.scene` — already has NetworkHelper removed
- `Assets/scenes/arrow_game.scene.scene` — already has NetworkHelper removed, PlayerPrefab set
