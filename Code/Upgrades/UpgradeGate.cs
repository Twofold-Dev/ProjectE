using Sandbox;

public sealed class UpgradeGate : Component
{
	/// <summary>
	/// What type of upgrade this gate gives.
	/// </summary>
	[Property] public UpgradeType UpgradeType { get; set; }

	/// <summary>
	/// How much bonus this specific gate gives.
	/// Set by the spawner (WaveManager) so each gate can differ.
	/// </summary>
	[Property] public float Amount { get; set; } = 1f;

	/// <summary>
	/// Display name shown on the gate label and pickup text.
	/// Set by the spawner (WaveManager).
	/// </summary>
	[Property] public string DisplayName { get; set; } = "";

	/// <summary>
	/// Bonus amount text shown on pickup confirmation.
	/// </summary>
	[Property] public string AmountText { get; set; } = "";

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
					SpawnPickupText();
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
			// DAMAGE gate: upgrades both ArrowDamage + SwordDamage
			case UpgradeType.ArrowDamage:
				if ( state.ArrowDamage < 10 ) state.ArrowDamage++;
				if ( state.SwordDamage < 10 ) state.SwordDamage++;
				break;

			// FIRE RATE gate: upgrades whichever is lower (ArrowFrequency or SwordFrequency)
			case UpgradeType.ArrowFrequency:
				if ( state.ArrowFrequency < state.SwordFrequency && state.ArrowFrequency < 10 )
					state.ArrowFrequency++;
				else if ( state.SwordFrequency < 10 )
					state.SwordFrequency++;
				break;

			// SPLIT gate: pen split count
			case UpgradeType.SplitCount:
				if ( state.SplitCount < 10 ) state.SplitCount++;
				break;

			// BURST gate: shredder blade count
			case UpgradeType.SwordCount:
				if ( state.SwordCount < 8 ) state.SwordCount++;
				break;

			// RANGE gate: upgrades both ArrowDistance + SwordRange
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

	/// <summary>
	/// Spawn a floating confirmation text that floats upward and fades.
	/// </summary>
	private void SpawnPickupText()
	{
		if ( string.IsNullOrEmpty( DisplayName ) ) return;

		var go = new GameObject( true, "PickupText" );
		go.WorldPosition = WorldPosition + Vector3.Up * 20f;

		// Floating label — same styling as enemy HP
		var labelGo = new GameObject( true, "Label" );
		labelGo.Parent = go;
		labelGo.LocalPosition = Vector3.Zero;
		labelGo.LocalScale = new Vector3( 5f, 5f, 5f );
		var wp = labelGo.Components.Create<WorldPanel>();
		wp.PanelSize = new Vector2( 1200, 300 );
		wp.LookAtCamera = true;
		wp.RenderOptions.AfterUI = true;
		var label = labelGo.Components.Create<Sandbox.UI.GateLabel>();
		label.Text = $"{DisplayName}\n{AmountText}";

		// Auto-destroy after 1.5s
		var destroyer = go.Components.Create<FloatAndDestroy>();
		destroyer.Lifetime = 1.5f;
		destroyer.FloatSpeed = 30f;

		go.NetworkSpawn( null );
	}
}

/// <summary>
/// Helper component that makes a GameObject float upward then destroy itself.
/// </summary>
public sealed class FloatAndDestroy : Component
{
	[Property] public float Lifetime { get; set; } = 1.5f;
	[Property] public float FloatSpeed { get; set; } = 30f;

	private TimeSince _timeSinceStart = 0;

	protected override void OnFixedUpdate()
	{
		if ( !Networking.IsHost ) return;

		WorldPosition += Vector3.Up * FloatSpeed * Time.Delta;

		if ( _timeSinceStart >= Lifetime )
		{
			GameObject.Destroy();
		}
	}
}
