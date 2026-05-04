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

	private bool _upgradeApplied = false;
	private TimeSince _timeSinceApplied = 0;

	protected override void OnFixedUpdate()
	{
		if ( !Networking.IsHost ) return;

		try
		{

		if ( _upgradeApplied )
		{
			if ( _timeSinceApplied >= 5f )
				GameObject.Destroy();
			return;
		}

		if ( !GameObject.IsValid() ) return;

		foreach ( var player in Scene.GetAllComponents<ArrowPlayer>() )
		{
			if ( player.IsDead ) continue;
			var dist = WorldPosition.Distance( player.WorldPosition );
			if ( dist < 60f )
			{
				var um = player.GetComponent<UpgradeManager>();
				if ( um.IsValid() )
				{
					ApplyUpgrade( player, um );
					// Fly the gate label into the player as visual feedback
					FlyLabelToPlayer( player );
					_upgradeApplied = true;
					_timeSinceApplied = 0;
				}
				return; // don't destroy — let physics push it around
			}
		}
		}
		catch ( System.Exception e )
		{
			Log.Error( $"UpgradeGate: {e.Message}" );
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
	/// Detach the gate's WorldPanel label and fly it into the player's body.
	/// </summary>
	private void FlyLabelToPlayer( ArrowPlayer player )
	{
		if ( !player.IsValid() ) return;
		if ( !GameObject.IsValid() ) return;

		// Copy children to array to avoid modifying collection during enumeration
		var children = GameObject.Children.ToArray();
		foreach ( var child in children )
		{
			if ( child.Name == "GateLabel" || child.Name == "DropLabel" || child.Name == "Label" )
			{
				child.SetParent( null );
				var flyer = child.Components.Create<FlyToPlayer>();
				flyer.Target = player.GameObject;
				flyer.Speed = 400f;
			}
		}
	}
}

/// <summary>
/// Flies the label toward the player, shrinking and fading as it reaches them.
/// </summary>
public sealed class FlyToPlayer : Component
{
	[Property] public GameObject Target { get; set; }
	[Property] public float Speed { get; set; } = 400f;
	[Property] public float Lifetime { get; set; } = 0.5f;

	private TimeSince _timeSinceStart = 0;
	private Sandbox.UI.GateLabel _label;

	protected override void OnStart()
	{
		_label = Components.Get<Sandbox.UI.GateLabel>();
		if ( !_label.IsValid() )
		{
			foreach ( var child in GameObject.Children )
			{
				_label = child.Components.Get<Sandbox.UI.GateLabel>();
				if ( _label.IsValid() ) break;
			}
		}
	}

	protected override void OnFixedUpdate()
	{
		if ( !Networking.IsHost ) return;
		if ( !Target.IsValid() ) { GameObject.Destroy(); return; }

		var targetPos = Target.WorldPosition;
		var dir = ( targetPos - WorldPosition ).Normal;
		WorldPosition += dir * Speed * Time.Delta;

		float progress = Math.Clamp( _timeSinceStart / Lifetime, 0f, 1f );
		if ( _label.IsValid() )
			_label.Panel.Style.Opacity = 1f - progress;

		GameObject.LocalScale = Vector3.One * (1f - progress * 0.8f);

		if ( progress >= 1f || WorldPosition.Distance( targetPos ) < 20f )
			GameObject.Destroy();
	}
}
