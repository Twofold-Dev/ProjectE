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
	/// </summary>
	public List<UpgradeType> CurrentOptions { get; private set; } = new();

	/// <summary>
	/// The last upgrade this player selected (empty if none).
	/// </summary>
	public UpgradeType? LastSelected { get; private set; } = null;

	/// <summary>
	/// Whether this player has made their selection this round.
	/// </summary>
	public bool HasSelected { get; set; } = false;

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

		var pool = GetAvailableUpgrades();
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
	/// Get available upgrade types (not yet at max level).
	/// </summary>
	private List<UpgradeType> GetAvailableUpgrades()
	{
		var available = new List<UpgradeType>();
		var state = CurrentUpgrades ?? new UpgradeState();

		// Define max levels per upgrade type
		if ( state.ArrowFrequency < 10 ) available.Add( UpgradeType.ArrowFrequency );
		if ( state.ArrowDamage < 10 ) available.Add( UpgradeType.ArrowDamage );
		if ( state.ArrowSpeed < 10 ) available.Add( UpgradeType.ArrowSpeed );
		if ( state.ArrowDistance < 10 ) available.Add( UpgradeType.ArrowDistance );
		if ( state.SwordCount < 8 ) available.Add( UpgradeType.SwordCount );
		if ( state.SwordDamage < 10 ) available.Add( UpgradeType.SwordDamage );
		if ( state.PetCount < 6 ) available.Add( UpgradeType.PetCount );
		if ( state.PetFireRate < 8 ) available.Add( UpgradeType.PetFireRate );
		if ( state.MovementSpeed < 10 ) available.Add( UpgradeType.MovementSpeed );
		if ( state.HealthBoost < 10 ) available.Add( UpgradeType.HealthBoost );

		return available;
	}

	/// <summary>
	/// Called by the UI when a player clicks an upgrade.
	/// Sends RPC to server to apply and validate.
	/// </summary>
	[Rpc.Host]
	public void SelectUpgrade( UpgradeType type )
	{
		if ( HasSelected ) return;

		// Validate this is one of the offered options
		if ( !CurrentOptions.Contains( type ) ) return;

		CurrentUpgrades.ApplyUpgrade( type );
		LastSelected = type;
		HasSelected = true;

		Log.Info( $"{GameObject.Name} selected upgrade: {type}" );

		// Apply immediate effects
		ApplyUpgradeEffect( type );

		// Check if all players are ready
		CheckAllReady();
	}

	/// <summary>
	/// Apply the upgrade's immediate effects to the player.
	/// </summary>
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

	/// <summary>
	/// Called when this player confirms without selecting (all upgrades maxed).
	/// </summary>
	public void ConfirmSelection()
	{
		HasSelected = true;
		CheckAllReady();
	}

	private void CheckAllReady()
	{
		if ( !Networking.IsHost ) return;

		var allReady = Scene.GetAllComponents<UpgradeManager>()
			.All( u => u.HasSelected );

		if ( allReady )
		{
			_gm?.OnAllPlayersReady();
		}
	}
}
