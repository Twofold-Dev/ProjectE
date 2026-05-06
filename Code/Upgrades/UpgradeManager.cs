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
			// All upgrades maxed — just confirm ready
			ConfirmSelection();
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

	/// <summary>
	/// Broadcast to all clients to generate upgrade options locally.
	/// Called by host after setting UpgradeSelect state.
	/// </summary>
	[Broadcast]
	public void BroadcastOfferUpgrades()
	{
		OfferUpgrades();
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
		gm.State = GameManager.GameState.Playing;
		gm.Scene.TimeScale = 1;
		Log.Info( $"ResumeGame: IsHost={Networking.IsHost}" );
	}

	/// <summary>Broadcast upgrade phase state to all clients. Always gets fresh GameManager reference.</summary>
	[Broadcast]
	public void BroadcastUpgradePhase( int wave, bool playstyleChosen )
	{
		var gm = Scene.GetAllComponents<GameManager>().FirstOrDefault();
		if ( !gm.IsValid() ) return;
		gm.CurrentWave = wave;
		gm.State = GameManager.GameState.UpgradeSelect;
		gm.PlaystyleChosen = playstyleChosen;
		gm.Scene.TimeScale = 0;
	}

	[Broadcast]
	public void BroadcastPlaystyleChosen()
	{
		var gm = Scene.GetAllComponents<GameManager>().FirstOrDefault();
		if ( !gm.IsValid() ) return;
		gm.PlaystyleChosen = true;
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
		if ( HasSelected ) return;
		if ( !CurrentOptions.Contains( type ) ) return;

		CurrentUpgrades.ApplyUpgrade( type );
		LastSelected = type;
		HasSelected = true;
		Log.Info( $"{GameObject.Name} selected upgrade: {type}" );
		ApplyUpgradeEffect( type );
		ConfirmSelection();
		CheckAllReady();
	}

	[Rpc.Host]
	public void SelectPlaystyle( Playstyle ps )
	{
		if ( HasSelected ) return;

		CurrentUpgrades.ChosenPlaystyle = ps;
		CurrentUpgrades.PlaystyleLocked = true;
		HasSelected = true;

		_gm = Scene.GetAllComponents<GameManager>().FirstOrDefault();
		if ( _gm.IsValid() )
		{
			_gm.PlaystyleChosen = true;
			BroadcastPlaystyleChosen();
		}

		Log.Info( $"{GameObject.Name} selected playstyle: {ps}" );
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

	[Broadcast]
	public void ConfirmSelection()
	{
		HasSelected = true;
	}

	private void CheckAllReady()
	{
		if ( !Networking.IsHost ) return;

		var allReady = Scene.GetAllComponents<UpgradeManager>()
			.All( u => u.HasSelected );

		Log.Info( $"UpgradeManager: CheckAllReady allReady={allReady}, count={Scene.GetAllComponents<UpgradeManager>().Count()}" );
	}
}
