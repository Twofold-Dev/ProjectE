using Sandbox;

public sealed class UpgradeGate : Component
{
	[Property] public UpgradeType UpgradeType { get; set; }

	protected override void OnFixedUpdate()
	{
		if ( !Networking.IsHost ) return;

		foreach ( var player in Scene.GetAllComponents<ArrowPlayer>() )
		{
			if ( player.IsDead ) continue;
			var dist = WorldPosition.Distance( player.WorldPosition );
			if ( dist < 50f )
			{
				var um = player.GetComponent<UpgradeManager>();
				if ( um.IsValid() )
				{
					ApplyUpgrade( player, um );
				}
				GameObject.Destroy();
				return;
			}
		}
	}

	private void ApplyUpgrade( ArrowPlayer player, UpgradeManager um )
	{
		var state = um.CurrentUpgrades ?? new UpgradeState();

		switch ( UpgradeType )
		{
			case UpgradeType.ArrowFrequency:
				if ( state.ArrowFrequency < 10 )
				{
					state.ArrowFrequency++;
					player.BaseFireRate += 0.3f;
				}
				break;

			case UpgradeType.ArrowDamage:
				if ( state.ArrowDamage < 10 )
				{
					state.ArrowDamage++;
					player.ArrowDamage += 5f;
				}
				break;
		}
	}
}
