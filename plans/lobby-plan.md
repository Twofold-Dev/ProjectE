# Lobby / Main Menu Scene — Plan

## Design Decision (Post-Research)
After testing reference games, none implement multiplayer in the main menu scene.
Multiplayer is only active once inside the game scene.
Therefore: **the lobby/main menu scene is a purely single-player title screen.**
No player lists, connection info, or ready states are shown on the main menu.

## Goal
A simple main menu scene where players can start a game or browse meta-progression (shop, achievements, leaderboard).

## Files

| File | Action | Purpose |
|------|--------|---------|
| `Assets/scenes/lobby.scene` | **EXISTING** | Simple office room scene with lighting, spawn points, and title screen UI |
| `Code/GameManager.cs` | **USES** | `StartGame()` (non-RPC) loads the game scene; single-player entry point |
| `Code/UI/MainMenuPanel.razor` | **EXISTING** | Single-player title screen: PLAY button, shop, achievements, leaderboard, quit |
| `Code/UI/LobbyHudManager.cs` | **EXISTING** | Creates the MainMenuPanel and chat in the lobby scene |

## Scene: lobby.scene
- Simple office room environment (title screen backdrop)
- 2 player spawn points (unused in menu, available for game scene)
- Camera positioned to see the room
- Lighting setup
- `GameManager` component (handles state; stays in Lobby state until PLAY is clicked)
- `NetworkHelper` with `StartServer: true` (standard S&Box infrastructure)
- `LobbyHudManager` (creates MainMenuPanel UI)

## MainMenuPanel.razor UI

| Element | Description |
|---------|-------------|
| Title | "PROJECT E" |
| Subtitle | "Infinite Office Runner" |
| PLAY button | Calls `GameManager.StartGame()` → loads arrow_game.scene |
| Paperclips currency | Shows current paperclip count |
| SHOP button | Opens meta-progression shop overlay |
| ACHV button | Opens achievements overlay |
| LEAD button | Toggles leaderboard display |
| RESET button | Triple-click to reset all progression |
| QUIT button | Calls `Game.Close()` |
| Roadmap panel | Side panel showing planned features |

## Scene Transition
- `MainMenuPanel` PLAY → `GameManager.StartGame()` → `Scene.LoadFromFile("scenes/arrow_game.scene.scene")`
- `GameOverPanel.OnPlayAgain()` → `Scene.LoadFromFile("scenes/lobby.scene")`

## Key Principle
- **Main menu is single-player only.** Multiplayer connections/UI only exist inside the game scene.
- `GameManager.StartGame()` is a plain method (no `[Rpc.Host]`) since it's called from the local title screen.
- Once inside the game scene, `GameManager.HostStartGame()` / `TogglePlayerReady()` handle multiplayer.
