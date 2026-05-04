/// <summary>
/// Server-authoritative lobby manager.
/// Tracks connected players and ready states, then loads the game scene.
/// </summary>
public sealed class LobbyManager : Component
{
	[Property] public int RequiredPlayers { get; set; } = 1;
	[Property] public string GameScenePath { get; set; } = "assets/scenes/arrow_game.scene";

	[Sync] public bool GameStarted { get; set; } = false;
	[Sync] public int ReadyCount { get; set; } = 0;

	protected override void OnStart()
	{
		if ( Networking.IsHost )
		{
			Log.Info( "Lobby: Waiting for players..." );
		}
	}

	protected override void OnUpdate()
	{
		if ( !Networking.IsHost ) return;
		if ( GameStarted ) return;

		if ( ReadyCount >= RequiredPlayers )
		{
			StartGame();
		}
	}

	[Rpc.Host]
	public void ToggleReady()
	{
		ReadyCount = ReadyCount > 0 ? 0 : 1;
		Log.Info( $"Ready: {ReadyCount}/{RequiredPlayers}" );
	}

	[Rpc.Broadcast]
	public void RefreshUI()
	{
		// UI reads ReadyCount directly
	}

	private void StartGame()
	{
		if ( GameStarted ) return;
		GameStarted = true;
		Log.Info( "Starting game!" );

		if ( !string.IsNullOrEmpty( GameScenePath ) )
			Scene.LoadFromFile( GameScenePath );
	}
}
