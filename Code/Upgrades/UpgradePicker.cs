using Sandbox;

/// <summary>
/// Attached to the Player Controller prefab (same GameObject as ArrowPlayer).
/// Detects nearby UpgradeGate components and handles pickup reliably.
/// Host applies directly; clients send [Rpc.Host].
/// Each player only picks up for themselves — no race conditions.
/// </summary>
public sealed class UpgradePicker : Component
{
	[Property] public float PickupRadius { get; set; } = 50f;

	protected override void OnFixedUpdate()
	{
		if ( !GameObject.IsValid() ) return;

		var gm = Scene.GetAllComponents<GameManager>().FirstOrDefault();
		if ( gm == null || gm.State != GameManager.GameState.Playing )
			return;

		var player = GetComponent<ArrowPlayer>();
		if ( !player.IsValid() || player.IsDead )
			return;

		var um = player.GetComponent<UpgradeManager>();
		if ( !um.IsValid() )
			return;

		// Scan for nearby unclaimed gates
		foreach ( var gate in Scene.GetAllComponents<UpgradeGate>() )
		{
			if ( !gate.IsValid() ) continue;
			if ( gate.UpgradeApplied ) continue;

			var dist = WorldPosition.Distance( gate.WorldPosition );
			if ( dist < PickupRadius )
			{
				ClaimUpgrade( gate, player, um );
				return; // one pickup per frame
			}
		}
	}

	private void ClaimUpgrade( UpgradeGate gate, ArrowPlayer player, UpgradeManager um )
	{
		if ( Networking.IsHost )
		{
			// Host: apply upgrade directly
			ApplyGateUpgrade( gate, um );
			gate.UpgradeApplied = true;
		}
		else
		{
			// Client: tell host to apply
			RequestPickup( gate.UpgradeType, gate.Amount );
			gate.UpgradeApplied = true;
		}

		// Fly label to player (runs locally for instant feedback)
		gate.FlyLabelToPlayer( player );
	}

	/// <summary>
	/// Client sends this to the host to apply the upgrade.
	/// </summary>
	[Rpc.Host]
	public void RequestPickup( UpgradeType type, float amount )
	{
		var caller = Rpc.Caller;
		if ( caller == null ) return;

		var player = Scene.GetAllComponents<ArrowPlayer>()
			.FirstOrDefault( p => p.Network.Owner == caller );
		if ( !player.IsValid() || player.IsDead ) return;

		var um = player.GetComponent<UpgradeManager>();
		if ( !um.IsValid() ) return;

		// Apply the upgrade using the gate's logic
		var state = um.CurrentUpgrades ?? new UpgradeState();
		ApplyUpgradeToState( type, amount, state, player );
		// Reassign to trigger [Sync] replication to clients
		um.CurrentUpgrades = state;
	}

	private void ApplyGateUpgrade( UpgradeGate gate, UpgradeManager um )
	{
		var state = um.CurrentUpgrades ?? new UpgradeState();
		ApplyUpgradeToState( gate.UpgradeType, gate.Amount, state, GetComponent<ArrowPlayer>() );
		// Reassign to trigger [Sync] replication to clients
		um.CurrentUpgrades = state;
	}

	private void ApplyUpgradeToState( UpgradeType type, float amount, UpgradeState state, ArrowPlayer player )
	{
		switch ( type )
		{
			case UpgradeType.ArrowDamage:
				if ( state.ArrowDamage < 10 ) state.ArrowDamage++;
				if ( state.SwordDamage < 10 ) state.SwordDamage++;
				break;

			case UpgradeType.ArrowFrequency:
				if ( state.ArrowFrequency < state.SwordFrequency && state.ArrowFrequency < 10 )
					state.ArrowFrequency++;
				else if ( state.SwordFrequency < 10 )
					state.SwordFrequency++;
				break;

			case UpgradeType.SplitCount:
				if ( state.SplitCount < 10 ) state.SplitCount++;
				break;

			case UpgradeType.SwordCount:
				if ( state.SwordCount < 8 ) state.SwordCount++;
				break;

			case UpgradeType.ArrowDistance:
				if ( state.ArrowDistance < 10 ) state.ArrowDistance++;
				if ( state.SwordRange < 10 ) state.SwordRange++;
				break;

			case UpgradeType.HealthBoost:
				if ( state.HealthBoost < 10 )
				{
					state.HealthBoost++;
					player.MaxHealth += 20f;
					player.Health = Math.Min( player.Health + 20f, player.MaxHealth );
				}
				break;
		}
	}
}
