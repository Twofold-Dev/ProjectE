/// <summary>
/// Server-authoritative game state machine.
/// Single-scene approach (Sausage Survivor pattern): menu area + game area in one scene.
/// Camera teleports between them — no Scene.LoadFromFile() transitions.
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

	[Sync] public int ReadyCount { get; set; } = 0;
	[Sync] public string ReadyStateIds { get; set; } = "";

	private HashSet<Guid> _readyPlayerIds = new();

	[Property, Category( "Gates" ), Title( "Gate Model" )]
	public Model GateModel { get; set; }

	/// <summary>
	/// The menu/UI camera GameObject (with CameraComponent).
	/// Enabled when State = Lobby, disabled during gameplay.
	/// </summary>
	[Property, Title( "Menu Camera" )]
	public GameObject MenuCamera { get; set; }

	/// <summary>
	/// The game camera GameObject (with CameraComponent).
	/// Enabled during gameplay, disabled when State = Lobby.
	/// Should have CameraFollower component attached.
	/// </summary>
	[Property, Title( "Game Camera" )]
	public GameObject GameCamera { get; set; }

	public Progression.ProgressionManager Progression { get; private set; }
	public int RunPaperclipsEarned { get; set; }
	public int RunEnemiesKilled { get; set; }
	public int RunDamageDealt { get; set; }
	private TimeSince _runTimer;
	private TimeSince _lastProgressionSave;

	public struct LeaderboardEntry
	{
		public ulong SteamId;
		public string Name;
		public int HighestWave;
		public int EnemiesKilled;
		public int Paperclips;
		public float PlayTime;
	}
	public List<LeaderboardEntry> LeaderboardEntries { get; private set; } = new();
	private TimeSince _lastLeaderboardSync;

	protected override void OnStart()
	{
		Log.Info( $"GameManager.OnStart: IsHost={Networking.IsHost}" );
		Progression = new Progression.ProgressionManager();
		RunPaperclipsEarned = 0;
		RunEnemiesKilled = 0;
		RunDamageDealt = 0;
		_lastProgressionSave = 0;

		// Start in Lobby state — show menu camera, hide game camera
		State = GameState.Lobby;
		ShowMenuCamera();
	}

	/// <summary>
	/// Called from MainMenuPanel when START GAME is clicked.
	/// Broadcast to all clients — switches everyone to game camera.
	/// UI button is only shown for host; the Broadcast ensures clients also transition.
	/// </summary>
	[Broadcast]
	public void StartGame()
	{
		Log.Info( "StartGame: switching to game camera" );
		State = GameState.Playing;
		_runTimer = 0;
		ShowGameCamera();
	}

	/// <summary>
	/// Toggle ready state for the calling player.
	/// Runs on host, then broadcasts the updated state to all clients
	/// so their UI refreshes immediately.
	/// </summary>
	[Rpc.Host]
	public void TogglePlayerReady()
	{
		var caller = Rpc.Caller;
		if ( caller == null ) return;

		if ( _readyPlayerIds.Contains( caller.Id ) )
			_readyPlayerIds.Remove( caller.Id );
		else
			_readyPlayerIds.Add( caller.Id );

		ReadyCount = _readyPlayerIds.Count;
		ReadyStateIds = string.Join( ",", _readyPlayerIds );
		Log.Info( $"Player {caller.DisplayName} toggled ready ({ReadyCount} ready)" );

		// Broadcast dummy RPC to all clients so their MainMenuPanel gets an Update call
		RefreshClientUI();
	}

	/// <summary>
	/// Broadcast-only RPC that triggers OnUpdate on all clients.
	/// The MainMenuPanel polls [Sync] ReadyStateIds in OnUpdate and refreshes UI.
	/// </summary>
	[Broadcast]
	public void RefreshClientUI()
	{
		// Nothing to do here — this RPC forces the client to tick,
		// which triggers OnUpdate on PanelComponents, which checks ReadyStateIds
	}

	/// <summary>
	/// Called from GameOverPanel "Back to Lobby" button.
	/// Switches back to menu camera, resets game state.
	/// </summary>
	public void ReturnToLobby()
	{
		Log.Info( "ReturnToLobby: switching to menu camera" );
		State = GameState.Lobby;
		ShowMenuCamera();

		// Reset game state
		CurrentWave = 0;
		TotalScore = 0;
		RunPaperclipsEarned = 0;
		RunEnemiesKilled = 0;
		RunDamageDealt = 0;
	}

	private void ShowGameCamera()
	{
		if ( GameCamera != null )
		{
			GameCamera.Enabled = true; // enable whole GameObject (CameraComponent + CameraFollower)

			// Ensure it's set as main camera
			var gameCam = GameCamera.GetComponent<CameraComponent>();
			if ( gameCam.IsValid() )
				gameCam.IsMainCamera = true;
		}
		else
		{
			Log.Warning( "GameCamera is not set in the scene!" );
		}

		if ( MenuCamera != null )
		{
			MenuCamera.Enabled = false; // disable whole GameObject
		}
	}

	private void ShowMenuCamera()
	{
		if ( MenuCamera != null )
		{
			MenuCamera.Enabled = true; // enable whole GameObject

			var menuCam = MenuCamera.GetComponent<CameraComponent>();
			if ( menuCam.IsValid() )
				menuCam.IsMainCamera = true;
		}
		else
		{
			Log.Warning( "MenuCamera is not set in the scene!" );
		}

		if ( GameCamera != null )
		{
			GameCamera.Enabled = false; // disable whole GameObject
		}
	}

	public bool IsPlayerReady( Guid connectionId )
	{
		if ( Networking.IsHost )
			return _readyPlayerIds.Contains( connectionId );

		if ( string.IsNullOrEmpty( ReadyStateIds ) ) return false;
		return ReadyStateIds.Split( ',', StringSplitOptions.RemoveEmptyEntries )
			.Contains( connectionId.ToString() );
	}

	protected override void OnUpdate()
	{
		if ( !Networking.IsHost ) return;

		// NOTE: No auto-start check here — we're using a single scene.
		// StartGame() is called explicitly from MainMenuPanel when PLAY is clicked.

		// Per-frame check: if all UpgradeManagers have selected, resume via networked UpgradeManager
		if ( State == GameState.UpgradeSelect )
		{
			var allSelected = Scene.GetAllComponents<UpgradeManager>().All( u => u.HasSelected );
			if ( allSelected && Scene.GetAllComponents<UpgradeManager>().Any() )
			{
				State = GameState.Playing;
				Scene.TimeScale = 1;
				// Broadcast to all clients via reliably-networked UpgradeManager
				var firstUm = Scene.GetAllComponents<UpgradeManager>().FirstOrDefault();
				if ( firstUm.IsValid() )
					firstUm.ResumeGame();
				Log.Info( "GameManager: All upgraded, resuming" );
			}
		}

		if ( _lastProgressionSave > 60f && Progression != null )
		{
			_lastProgressionSave = 0;
			Progression.SaveAll();
		}
		if ( _lastLeaderboardSync > 3f && Progression != null )
		{
			_lastLeaderboardSync = 0;
			BroadcastPlayerStats(
				Progression.Stats.HighestWaveReached,
				Progression.Stats.TotalEnemiesKilled,
				Progression.Currency.Paperclips,
				Progression.Stats.TotalTimePlayedSeconds
			);
		}
	}

	[Broadcast]
	public void BroadcastPlayerStats( int highestWave, int enemiesKilled, int paperclips, float playTime )
	{
		var steamId = Rpc.Caller.SteamId;
		var name = Rpc.Caller.DisplayName;
		var existing = LeaderboardEntries.FirstOrDefault( e => e.SteamId == steamId );
		if ( existing.SteamId != 0 ) LeaderboardEntries.Remove( existing );
		LeaderboardEntries.Add( new LeaderboardEntry
		{
			SteamId = steamId, Name = name,
			HighestWave = highestWave, EnemiesKilled = enemiesKilled,
			Paperclips = paperclips, PlayTime = playTime
		} );
	}

	public void OnPlayerDied( ArrowPlayer player )
	{
		if ( !Networking.IsHost ) return;
		var alivePlayers = Scene.GetAllComponents<ArrowPlayer>().Where( p => !p.IsDead ).ToList();
		if ( alivePlayers.Count == 0 )
		{
			State = GameState.GameOver;
			if ( Progression != null )
				Progression.AwardRun( RunPaperclipsEarned, RunEnemiesKilled, RunDamageDealt, CurrentWave, _runTimer );
		}
	}

	public void OnWaveCompleted( int wave )
	{
		if ( !Networking.IsHost ) return;
		CurrentWave = wave;
		State = GameState.UpgradeSelect;
		Scene.TimeScale = 0;
		// Offer upgrades to all players on host
		foreach ( var um in Scene.GetAllComponents<UpgradeManager>() )
			um.OfferUpgrades();
		// Broadcast state to all clients (each client's UpgradePanel calls OfferUpgrades locally)
		var firstUm = Scene.GetAllComponents<UpgradeManager>().FirstOrDefault();
		if ( firstUm.IsValid() )
			firstUm.BroadcastUpgradePhase( wave, PlaystyleChosen );
	}

	public void OnAllPlayersReady()
	{
		if ( !Networking.IsHost ) return;
		State = GameState.Playing;
		Scene.TimeScale = 1;
	}

	public void AddScore( int points )
	{
		if ( !Networking.IsHost ) return;
		TotalScore += points;
	}
}
