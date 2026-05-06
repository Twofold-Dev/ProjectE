# Shared Network Gates — Fix Plan

## The Problem
`[Broadcast] HostStartGame()` makes each client load the game scene **independently**. This breaks the S&box networking context — `NetworkSpawn`ed objects appear as frozen proxies on clients because the physics contexts are disconnected.

## The Fix: Proper S&box Scene Transition

### Step 1 — Remove `[Broadcast]` from `HostStartGame()`
Change from:
```csharp
[Rpc.Broadcast]
public void HostStartGame()
{
    Scene.LoadFromFile(GameScenePath);  // Each client loads independently
}
```
To:
```csharp
[Rpc.Host]
public void HostStartGame()
{
    Scene.LoadFromFile(GameScenePath);  // Host loads, S&box replicates to clients
}
```

This is how S&box is designed to work — the host loads a scene, and the engine automatically replicates it to all connected clients, preserving the networking context.

### Step 2 — Game scene has NO NetworkHelper
The game scene should NOT have `Sandbox.NetworkHelper`. Networking is already active from the lobby scene's NetworkHelper. Having a second NetworkHelper in the game scene would conflict.

**You**: Open `arrow_game.scene` in editor → remove `Sandbox.NetworkHelper` if present.

### Step 3 — GameManager spawns players in game scene
`OnStart()` detects `WaveManager` → `SpawnPlayersForAllConnections()` → spawns ArrowPlayers for all connections. (Already implemented.)

### Step 4 — Gates use `NetworkSpawn` (already done)
Gates are spawned with `NetworkSpawn(true, null)` + `Prop` component + `Rigidbody`. Since the networking context is now preserved (no `[Broadcast]`), the gates' Rigidbodies respond to ALL clients' physics.

## How Physics Works (Shared Networked Ball)
```
Host physics engine: processes ALL player interactions with gates
    ↓
Replicates gate position/velocity to ALL clients
    ↓
Client sees gate move in sync with host's physics
    ↓
Client can ALSO push the gate → interaction sent to host
    ↓
Host processes push → updates gate state → replicates to all
```

## Flow
1. Lobby scene → NetworkHelper creates lobby
2. Client connects to lobby
3. Host presses START → `[Rpc.Host] HostStartGame()` → `Scene.LoadFromFile()`
4. S&box replicates game scene to client (preserving networking context)
5. GameManager spawns ArrowPlayers for all connections
6. WaveManager spawns gates with `NetworkSpawn` → shared across all clients
7. ANY player can push gates → host processes physics → all see it
