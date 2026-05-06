# Multiplayer Implementation Comparison

## Overview

Comparison of how your project (Project E — Arrow a Row) handles multiplayer vs. three reference S&box games: **clover_meadows**, **sbox-hc1**, and **pizza_clicker**.

---

## 1. NetworkHelper — Built-in vs. Custom

| Aspect | Your Project | clover_meadows | sbox-hc1 | pizza_clicker |
|--------|-------------|----------------|----------|---------------|
| NetworkHelper | Built-in `Sandbox.NetworkHelper` | Custom `NetworkHelper.cs` with `INetworkListener` | Custom `GameNetworkManager` with `INetworkListener` | No NetworkHelper — UI-based |
| StartServer | `true` on both scenes | Custom `OnLoad()` checks `!Networking.IsActive` | Checks `!Networking.IsActive` in `OnStart()` | `OnStart()` calls `Networking.CreateLobby()` |
| Player spawning | Automatic via `OnActive()` (built-in) | **Commented out** in NetworkHelper, done by GameManager instead | Manual via `GetOrCreateClient()` + `NetworkSpawn()` | No player spawning (UI game) |

**Key insight**: Both clover_meadows and sbox-hc1 use **custom** NetworkHelper/GameNetworkManager components, NOT the built-in `Sandbox.NetworkHelper`. They manually control player spawning rather than relying on the built-in auto-spawn behavior.

---

## 2. Player Spawning After Scene Transitions

| Aspect | Your Project (Before Fix) | clover_meadows | sbox-hc1 |
|--------|-------------------------|----------------|----------|
| Who spawns players? | Built-in NetworkHelper's `OnActive()` | `GameManager.SpawnPlayer()` via queue | `GameNetworkManager.OnActive()` → `GetOrCreateClient()` |
| Handles already-connected clients after scene load? | **NO** — built-in `OnActive()` only fires on initial connect | **YES** — `SpawnPlayers()` runs every frame from `OnFixedUpdate()`, checking the spawn queue | **YES** — single scene, no transitions |
| Spawn queue pattern | None | `List<Connection> _spawnQueue` + `[Rpc.Owner] RequestSpawn()` | `GetOrCreateClient()` with recycling logic |

**Key insight**: The built-in `Sandbox.NetworkHelper.OnActive()` only fires when a **client initially connects**, not when a new scene loads. Both reference projects handle this by either:
- Using a **single scene** (pizza_clicker, sbox-hc1 — no scene transitions needed)
- Having a **manual spawn queue** that re-processes connections after scene load (clover_meadows)

---

## 3. Scene Transitions

| Aspect | Your Project (Before Fix) | clover_meadows | sbox-hc1 |
|--------|-------------------------|----------------|----------|
| Scene load API | `Scene.LoadFromFile()` | `Game.ActiveScene.LoadFromFile()` | N/A (single scene) |
| RPC wrapping | `[Rpc.Host]` on scene load methods | Static method `LoadRealm()`, no RPC | N/A |
| ISceneStartup | Added (was missing) | Implements `ISceneStartup` | Doesn't use ISceneStartup |
| Scene init callbacks | Added `OnHostPreInitialize`, `OnHostInitialize`, `OnClientInitialize` | Same 3 methods, reset player state | Uses `IGameEventHandler` pattern instead |

**Key insight**: `Game.ActiveScene.LoadFromFile()` is the platform-level scene manager that properly propagates scene transitions to all connected clients. `Scene.LoadFromFile()` is the component-local reference that may not replicate.

---

## 4. Ready State / Lobby Sync

| Aspect | Your Project (Before Fix) | clover_meadows | sbox-hc1 |
|--------|-------------------------|----------------|----------|
| Ready state storage | `HashSet<Guid>` (NOT synced) | No lobby/ready system | No lobby/ready system |
| Ready sync mechanism | Added `[Sync] string ReadyStateIds` | N/A | Uses `NetDictionary<Team, string>` for synced state |
| Client-side read | Added `IsPlayerReady()` checks synced string | N/A | Uses `[Sync]` properties |

**Key insight**: Any data that needs to be visible on ALL clients must be `[Sync]`. Unsynchronized collections like `HashSet<Guid>` are host-only. clover_meadows avoids this by not having a ready system. sbox-hc1 uses `NetDictionary<Team, string>` for synced game state.

---

## 5. Architecture Pattern Comparison

```
┌─────────────────────────────────────────────────────────────┐
│                    Your Project (Original)                    │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  lobby.scene                   arrow_game.scene               │
│  ┌──────────────────┐          ┌──────────────────┐          │
│  │ Sandbox.Network   │  load    │ Sandbox.Network   │          │
│  │ Helper            │ ───────► │ Helper            │          │
│  │ StartServer=true  │          │ StartServer=true  │          │
│  │ PlayerPrefab:     │          │ PlayerPrefab:     │          │
│  │  PlayerController │          │  PlayerController │          │
│  │  (no ArrowPlayer) │          │  + ArrowPlayer    │          │
│  └──────────────────┘          └──────────────────┘          │
│         │                              │                     │
│         │ OnActive() fires            │ OnActive() DOESN'T  │
│         │ on initial connect           │ fire for already-   │
│         │ spawns PlayerController      │ connected clients   │
│         │ for lobby                    │ → NO ArrowPlayer    │
│         ▼                              ▼  spawned!           │
│  PlayerController                  (nothing)                 │
│  (lobby walk)                                               │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                    clover_meadows Reference                    │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  mainmenu.scene                  clover.scene                 │
│  ┌──────────────────┐   load    ┌──────────────────┐         │
│  │ Custom            │ ───────► │ Custom            │         │
│  │ NetworkHelper     │          │ NetworkHelper     │         │
│  │ (spawning         │          │ (spawning         │         │
│  │  commented out)   │          │  commented out)   │         │
│  └──────────────────┘          └──────────────────┘         │
│         │                              │                     │
│         │ GameManager                  │ GameManager         │
│         │ .OnConnected()               │ .SpawnPlayers()     │
│         │ adds to spawn queue          │ runs every frame    │
│         │                              │ processes queue     │
│         ▼                              ▼                     │
│  SpawnQueue: [conn1, conn2]     PlayerCharacter x2 spawned   │
│                                 (for ALL connections)        │
└─────────────────────────────────────────────────────────────┘
```

---

## 6. Voxel Party Comparison

Voxel Party uses the **same built-in `Sandbox.NetworkHelper`** as your project.

| Aspect | Voxel Party | Your Project |
|--------|-------------|-------------|
| NetworkHelper | Built-in `Sandbox.NetworkHelper` | Built-in `Sandbox.NetworkHelper` |
| Scene transitions | **None** — each scene is a self-contained game (Speed Build, Telephone, etc.) | **Has transitions** — lobby → arrow_game |
| Scene flow | Menu scene (no NetworkHelper) → Game scene (has NetworkHelper) | Lobby scene (NetworkHelper) → Game scene (NetworkHelper) |
| Player spawning | Built-in `OnActive()` only — works because no scene transitions | Built-in `OnActive()` + GameManager `SpawnPlayersForAllConnections()` |

**Key takeaway**: Voxel Party avoids the scene transition problem by not having multi-scene flows. Each game mode scene is standalone, so the built-in NetworkHelper's `OnActive()` is sufficient. Your project needs the extra `SpawnPlayersForAllConnections()` because clients are already connected before the game scene loads.

## 7. Summary of Differences (6 Key Points)

1. **Scene load API** — `Scene.LoadFromFile()` vs `Game.ActiveScene.LoadFromFile()`
2. **NetworkHelper** — Built-in `Sandbox.NetworkHelper` vs custom implementation with manual spawning
3. **OnActive timing** — Built-in fires only on initial connect; custom can reprocess on scene load
4. **Spawn queue pattern** — clover_meadows queues connections and spawns from `OnFixedUpdate()`
5. **ISceneStartup** — clover_meadows uses it to reset state between scene transitions
6. **Sync for client-visible data** — `[Sync]` property for ready state instead of unsynced HashSet
