# Lobby Scene — Plan

## Goal
A simple lobby scene where players join, see each other, ready up, and start the game.

## Files

| File | Action | Purpose |
|------|--------|---------|
| `Assets/scenes/lobby.scene` | **NEW** | Simple office room scene with lighting, spawn points |
| `Code/Lobby/LobbyManager.cs` | **NEW** | Server-authoritative lobby state: player tracking, ready state, scene transition |
| `Code/UI/LobbyPanel.razor` | **NEW** | Host/join info, ready button, player list |

## Scene: lobby.scene
- Simple office room environment
- 2 player spawn points
- Camera positioned to see the room
- Lighting setup
- `LobbyManager` component on a root GameObject

## LobbyManager Component

```csharp
public sealed class LobbyManager : Component
{
    private struct LobbyPlayer
    {
        public Guid ConnectionId;
        public string Name;
        public bool IsReady;
    }
    
    private List<LobbyPlayer> _players = new();
    private bool _gameStarted = false;
    
    OnStart():
        if host: set up lobby, log "Waiting for players"
    
    [Rpc.Host]
    PlayerReady():
        mark player as ready
        check if all ready → StartGame()
    
    StartGame():
        if already started → return
        _gameStarted = true
        load arrow_game.scene via Scene.LoadFromFile()
    
    GetLobbyState() → returns player list + ready status for UI
```

## LobbyPanel.razor UI

| Element | Description |
|---------|-------------|
| Title | "Project E - Lobby" |
| Player list | Shows each player + ready status |
| Ready button | Toggle ready state |
| Start hint | "Waiting for players..." or "Starting..." |
| Host info | Shows connection info for P2P |

## Scene Transition
- `LobbyManager.StartGame()` calls `Scene.LoadFromFile("assets/scenes/arrow_game.scene.scene")`
- `GameOverPanel.OnPlayAgain()` calls `Scene.LoadFromFile("assets/scenes/lobby.scene")` or `Game.Restart()`

## Networking
- Uses existing S&Box P2P networking
- LobbyManager tracks players via `Networking.Ip` or connection GUIDs
- Ready state uses `[Rpc.Host]` for server authority
- Scene loaded by host, replicated to all clients

## Implementation Order
1. Create `lobby.scene` with lighting + spawn points
2. Create `LobbyManager.cs` 
3. Create `LobbyPanel.razor`
4. Wire scene transitions (lobby → game → lobby)
5. Fix "Play Again" to load lobby instead of restart
