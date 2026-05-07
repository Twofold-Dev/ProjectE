/// <summary>
/// Manages per-player upgrade state: offering 3 random choices between waves,
/// applying the selected upgrade, and notifying GameManager when ready.
/// Attached to each Player GameObject alongside ArrowPlayer.
/// </summary>
public sealed class UpgradeManager : Component
{
	[Sync] public UpgradeState CurrentUpgrades { get; set; } = new();

	/// <summary>
	/// The 3 upgrade options currently offered to this player.
	/// Each client generates their own locally via OfferUpgrades().
	/// </summary>
	public List<UpgradeType> CurrentOptions { get; private set; } = new();

	/// <summary>
	/// The last upgrade this player selected (empty if none).
	/// </summary>
	public UpgradeType? LastSelected { get; set; } = null;

	/// <summary>
	/// Whether this player has made their selection this round. Synced so host can check all ready.
	/// </summary>
	[Sync] public bool HasSelected { get; set; } = false;

	/// <summary>
	/// Whether this player has chosen their playstyle. Per-player, not global.
	/// </summary>
	[Sync] public bool PlaystyleSelected { get; set; } = false;

	private GameManager _gm;

	protected override void OnStart()
	{
		_gm = Scene.GetAllComponents<GameManager>().FirstOrDefault();
	}

	/// <summary>
	/// Generate 3 random upgrade options (no duplicates, no maxed-out upgrades).
	/// </summary>
	public void OfferUpgrades()
	{
		HasSelected = false;
		CurrentOptions.Clear();
		Log.Info( $"OfferUpgrades: {GameObject.Name} HasSelected reset" );

		var currentWave = _gm?.CurrentWave ?? 1;
		var pool = GetAvailableUpgrades( currentWave );
		if ( pool.Count == 0 )
		{
			// All upgrades maxed — mark as ready and broadcast
			HasSelected = true;
			BroadcastSelection();
			return;
		}

		var rng = Random.Shared;
		var count = Math.Min( 3, pool.Count );

		while ( CurrentOptions.Count < count )
		{
			var pick = pool[rng.Int( 0, pool.Count - 1 )];
			if ( !CurrentOptions.Contains( pick ) )
			{
				CurrentOptions.Add( pick );
			}
		}
	}

	/// <summary>Broadcast game resume to all clients. Always gets fresh GameManager reference.</summary>
	[Broadcast]
	public void ResumeGame()
	{
		var gm = Scene.GetAllComponents<GameManager>().FirstOrDefault();
		if ( !gm.IsValid() )
		{
			Log.Warning( "ResumeGame: No GameManager found" );
			return;
		}

		if ( !gm.PlaystylePhaseComplete )
			gm.PlaystylePhaseComplete = true;

		gm.State = GameManager.GameState.Playing;
		gm.Scene.TimeScale = 1;
		Log.Info( $"ResumeGame: IsHost={Networking.IsHost}" );
	}

	/// <summary>Broadcast upgrade phase state to all clients. Always gets fresh GameManager reference.</summary>
	[Broadcast]
	public void BroadcastUpgradePhase( int wave )
	{
		var gm = Scene.GetAllComponents<GameManager>().FirstOrDefault();
		if ( !gm.IsValid() ) return;
		gm.CurrentWave = wave;
		gm.State = GameManager.GameState.UpgradeSelect;
		gm.Scene.TimeScale = 0;

		// Reset HasSelected for ALL players to prevent stale BroadcastSelection
		// from previous waves corrupting this selection phase.
		foreach ( var um in Scene.GetAllComponents<UpgradeManager>() )
		{
			um.HasSelected = false;
		}
	}

	/// <summary>
	/// Get available upgrade types based on current wave (progression discovery) and max level caps.
	/// </summary>
	private List<UpgradeType> GetAvailableUpgrades( int currentWave )
	{
		var available = new List<UpgradeType>();
		var state = CurrentUpgrades ?? new UpgradeState();

		if ( state.ArrowFrequency < 10 ) available.Add( UpgradeType.ArrowFrequency );
		if ( state.ArrowDamage < 10 ) available.Add( UpgradeType.ArrowDamage );
		if ( state.ArrowSpeed < 10 ) available.Add( UpgradeType.ArrowSpeed );
		if ( state.ArrowDistance < 10 ) available.Add( UpgradeType.ArrowDistance );
		if ( state.HealthBoost < 10 ) available.Add( UpgradeType.HealthBoost );

		if ( currentWave >= 4 )
		{
			if ( state.SplitCount < 10 ) available.Add( UpgradeType.SplitCount );
			if ( state.CritChance < 5 ) available.Add( UpgradeType.CritChance );
			if ( state.PenPierce < 5 ) available.Add( UpgradeType.PenPierce );
		}

		if ( currentWave >= 6 )
		{
			if ( state.SwordCount < 8 ) available.Add( UpgradeType.SwordCount );
			if ( state.SwordCount > 0 )
			{
				if ( state.SwordDamage < 10 ) available.Add( UpgradeType.SwordDamage );
				if ( state.SwordFrequency < 10 ) available.Add( UpgradeType.SwordFrequency );
				if ( state.SwordRange < 10 ) available.Add( UpgradeType.SwordRange );
			}
		}

		if ( currentWave >= 8 )
		{
			if ( state.PetCount < 6 ) available.Add( UpgradeType.PetCount );
			if ( state.PetCount > 0 )
			{
				if ( state.PetFireRate < 8 ) available.Add( UpgradeType.PetFireRate );
			}
		}

		if ( currentWave >= 10 )
		{
			if ( state.BladeBounce < 5 ) available.Add( UpgradeType.BladeBounce );
		}

		return available;
	}

	[Rpc.Host]
	public void SelectUpgrade( UpgradeType type )
	{
		if ( HasSelected )
		{
			Log.Info( $"SelectUpgrade skipped: HasSelected already true for {GameObject.Name}, caller={Rpc.Caller?.DisplayName ?? "null"}" );
			return;
		}
		Log.Info( $"SelectUpgrade processing: um={GameObject.Name}, type={type}, caller={Rpc.Caller?.DisplayName ?? "null"}" );

		// Note: CurrentOptions is intentionally NOT synced (each client generates locally).
		// We trust the client's selection since the UI constrains choices.
		// Validation removed to avoid host/client option mismatch.

		// Apply upgrade — must reassign to trigger [Sync] replication
		var state = CurrentUpgrades ?? new UpgradeState();
		state.ApplyUpgrade( type );
		CurrentUpgrades = state;

		LastSelected = type;
		HasSelected = true;
		Log.Info( $"{GameObject.Name} selected upgrade: {type}" );
		ApplyUpgradeEffect( type );
		BroadcastSelection();
		CheckAllReady();
	}

	[Rpc.Host]
	public void SelectPlaystyle( Playstyle ps )
	{
		if ( HasSelected )
		{
			Log.Info( $"SelectPlaystyle skipped: HasSelected already true for {GameObject.Name}, caller={Rpc.Caller?.DisplayName ?? "null"}" );
			return;
		}
		Log.Info( $"SelectPlaystyle processing: um={GameObject.Name}, style={ps}, caller={Rpc.Caller?.DisplayName ?? "null"}" );

		// Must reassign to trigger [Sync] replication
		var state = CurrentUpgrades ?? new UpgradeState();
		state.ChosenPlaystyle = ps;
		state.PlaystyleLocked = true;
		CurrentUpgrades = state;
		HasSelected = true;
		PlaystyleSelected = true;

		Log.Info( $"{GameObject.Name} selected playstyle: {ps}" );
		BroadcastSelection();
		CheckAllReady();
	}

	private void ApplyUpgradeEffect( UpgradeType type )
	{
		var player = GameObject.GetComponent<ArrowPlayer>();
		if ( !player.IsValid() ) return;

		switch ( type )
		{
			case UpgradeType.HealthBoost:
				player.MaxHealth += 20f;
				player.Health = Math.Min( player.Health + 20f, player.MaxHealth );
				break;
		}
	}

	/// <summary>Broadcast HasSelected so all clients (including host) see the selection.</summary>
	[Broadcast]
	public void BroadcastSelection()
	{
		HasSelected = true;
	}

	public void CheckAllReady()
	{
		if ( !Networking.IsHost ) return;

		var allReady = Scene.GetAllComponents<UpgradeManager>()
			.All( u => u.HasSelected );

		if ( !allReady ) return;

		Log.Info( $"UpgradeManager: All players ready, resuming game" );

		// Directly resume instead of relying on GameManager.OnUpdate polling
		var gm = Scene.GetAllComponents<GameManager>().FirstOrDefault();
		if ( !gm.IsValid() ) return;

		// Mark playstyle phase complete the first time all players are ready
		// This ensures future waves show upgrade cards, not playstyle cards
		if ( !gm.PlaystylePhaseComplete )
			gm.PlaystylePhaseComplete = true;

		gm.State = GameManager.GameState.Playing;
		gm.Scene.TimeScale = 1;
		ResumeGame();
	}
}
