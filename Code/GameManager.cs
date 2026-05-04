/// <summary>
/// Server-authoritative game state machine.
/// Orchestrates wave spawning, upgrade pauses, and game over.
/// Only runs logic on the host/server.
/// </summary>
public sealed class GameManager : Component
{
	public enum GameState
	{
		Lobby,
		Playing,
		UpgradeSelect,
		GameOver
	}

	[Sync] public GameState State { get; set; } = GameState.Lobby;
	[Sync] public int CurrentWave { get; set; } = 0;
	[Sync] public int TotalScore { get; set; } = 0;
	[Sync] public bool PlaystyleChosen { get; set; } = false;

	[Property, Category( "Gates" ), Title( "Gate Model" )]
	public Model GateModel { get; set; }

	protected override void OnStart()
	{
		if ( Networking.IsHost )
		{
			Log.Info( "GameManager started (host)" );
			State = GameState.Playing;
		}
	}

	protected override void OnUpdate()
	{
		if ( !Networking.IsHost ) return;

		switch ( State )
		{
			case GameState.Playing:
				// WaveManager handles spawning; GameManager just tracks state
				break;

			case GameState.UpgradeSelect:
				// Waiting for all players to select upgrades
				break;

			case GameState.GameOver:
				// Show game over UI
				break;
		}
	}

	/// <summary>
	/// Called by ArrowPlayer when a player dies.
	/// </summary>
	public void OnPlayerDied( ArrowPlayer player )
	{
		if ( !Networking.IsHost ) return;

		Log.Info( $"Player {player.GameObject.Name} died." );

		// Check if all players are dead
		var alivePlayers = Scene.GetAllComponents<ArrowPlayer>()
			.Where( p => !p.IsDead )
			.ToList();

		if ( alivePlayers.Count == 0 )
		{
			State = GameState.GameOver;
			Log.Info( "All players dead. Game Over." );
		}
	}

	/// <summary>
	/// Called by WaveManager when a wave is completed.
	/// </summary>
	public void OnWaveCompleted( int wave )
	{
		if ( !Networking.IsHost ) return;

		CurrentWave = wave;
		State = GameState.UpgradeSelect;
		Scene.TimeScale = 0; // Pause game for upgrade selection on EVERY wave

		if ( wave == 1 && !PlaystyleChosen )
		{
			Log.Info( "Wave 1 complete — choose your playstyle" );
			return; // Don't offer upgrades yet — playstyle selection comes first
		}

		Log.Info( $"Wave {wave} completed. Upgrade select phase." );

		// Offer upgrades to each player
		foreach ( var um in Scene.GetAllComponents<UpgradeManager>() )
		{
			um.OfferUpgrades();
		}
	}

	public void OnAllPlayersReady()
	{
		if ( !Networking.IsHost ) return;

		State = GameState.Playing;
		Scene.TimeScale = 1;
		Log.Info( "All players ready. Resuming play." );
	}

	public void AddScore( int points )
	{
		if ( !Networking.IsHost ) return;
		TotalScore += points;
	}
}
