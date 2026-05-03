/// <summary>
/// Attached alongside the built-in PlayerController on the same GameObject.
/// Handles lane-constrained movement, auto-fire, health, and upgrade state.
/// The PlayerController must have UseInputControls=false (ArrowPlayer sets WishVelocity).
/// </summary>
public sealed class ArrowPlayer : Component
{
	#region Configuration

	[Property, Category( "Movement" )]
	public float MoveSpeed { get; set; } = 300f;

	[Property, Category( "Movement" )]
	public float LaneMinX { get; set; } = -200f;

	[Property, Category( "Movement" )]
	public float LaneMaxX { get; set; } = 200f;

	[Property, Category( "Combat" )]
	public float BaseFireRate { get; set; } = 1.0f; // arrows per second

	[Property, Category( "Combat" )]
	public float MaxHealth { get; set; } = 100f;

	[Property, Category( "Combat" )]
	public float ArrowSpeed { get; set; } = 500f;

	[Property, Category( "Combat" )]
	public float ArrowDamage { get; set; } = 10f;

	[Property, Category( "Combat" )]
	public float ArrowDistance { get; set; } = 800f;

	#endregion

	#region State

	[Sync] public float Health { get; set; } = 100f;
	[Sync] public bool IsDead { get; set; } = false;

	private UpgradeManager _um;
	private TimeSince _timeSinceLastFire = 0;
	private PlayerController _pc;

	#endregion

	protected override void OnAwake()
	{
		_pc = GameObject.GetComponent<PlayerController>();
		_um = GameObject.GetComponent<UpgradeManager>();

		if ( !_pc.IsValid() )
		{
			Log.Warning( "ArrowPlayer requires a PlayerController component on the same GameObject." );
			return;
		}

		// --- Auto-configure PlayerController for Arrow a Row ---
		// You don't need to set these in the editor — ArrowPlayer does it automatically.

		// Input: ArrowPlayer handles movement via W/S keys
		_pc.UseInputControls = false;

		// Movement: no jumping, no ducking, snappy response
		_pc.JumpSpeed = 0;
		_pc.DuckedHeight = _pc.BodyHeight;
		_pc.RunSpeed = MoveSpeed;
		_pc.WalkSpeed = MoveSpeed;
		_pc.RunByDefault = true;
		_pc.AccelerationTime = 0.05f;
		_pc.DeaccelerationTime = 0.15f;

		// Interaction: no pressing/use needed in an auto-shooter
		_pc.EnablePressing = false;

		// Camera: disable PlayerController camera controls — you have a static scene camera
		_pc.UseCameraControls = false;

		Health = MaxHealth;
	}

	protected override void OnFixedUpdate()
	{
		if ( Scene.IsEditor ) return; // don't run gameplay logic in edit mode
		if ( IsProxy ) return;
		if ( IsDead ) return;
		if ( !_pc.IsValid() ) return;

		// Read W/S for lane movement: W = right, S = left
		// Input.AnalogMove returns (x=left/right, y=forward/back, z=0)
		// We use .y: W(forward=+1) → right(+X), S(backward=-1) → left(-X)
		var moveInput = Input.AnalogMove;

		// Remap W/S to X-axis lane movement
		_pc.WishVelocity = new Vector3( moveInput.y, 0, 0 ) * MoveSpeed;

		// Handle jump prevention (JumpSpeed=0 on PlayerController already handles this,
		// but double-check)
		if ( Input.Pressed( "Jump" ) )
		{
			// No jumping in Arrow a Row — silently ignore
		}
	}

	protected override void OnUpdate()
	{
		if ( IsProxy ) return; // only the owning client fires arrows
		if ( IsDead ) return;

		// --- Lane clamping ---
		// After physics runs, clamp world position to lane bounds
		var pos = WorldPosition;
		pos.x = pos.x.Clamp( LaneMinX, LaneMaxX );
		WorldPosition = pos;

		// --- Auto-fire ---
		var effectiveFireRate = BaseFireRate + GetFireRateBonus();
		var interval = 1.0f / effectiveFireRate;

		if ( _timeSinceLastFire >= interval )
		{
			_timeSinceLastFire = 0;
			FireArrow();
		}
	}

	#region Combat

	private void FireArrow()
	{
		// Spawn arrow projectile
		var arrowGo = new GameObject( true, $"Arrow_{GameObject.Name}" );
		arrowGo.WorldPosition = WorldPosition + Vector3.Right * 30f;
		arrowGo.WorldRotation = Rotation.FromYaw( 90f );

		var arrow = arrowGo.Components.Create<Arrow>();
		arrow.Speed = ArrowSpeed + GetSpeedBonus();
		arrow.Damage = ArrowDamage + GetDamageBonus();
		arrow.MaxDistance = ArrowDistance + GetDistanceBonus();
		arrow.OwnerId = Network.OwnerId;

		// Visual placeholder: stretched cube
		var model = arrowGo.Components.Create<ModelRenderer>();
		model.Model = Model.Cube; // default engine cube — replace later
		arrowGo.LocalScale = new Vector3( 2f, 0.25f, 0.25f ); // arrow shape
		model.Tint = GetPlayerColor();
		arrowGo.NetworkSpawn( null );
	}

	public void TakeDamage( float damage, Guid attackerId )
	{
		if ( IsDead ) return;

		Health -= damage;
		Log.Info( $"{GameObject.Name} took {damage} damage, health is now {Health}" );

		if ( Health <= 0 )
		{
			Health = 0;
			Die();
		}
	}

	private void Die()
	{
		IsDead = true;
		Log.Info( $"{GameObject.Name} has died." );

		// Notify GameManager
		var gm = Scene.GetAllComponents<GameManager>().FirstOrDefault();
		gm?.OnPlayerDied( this );

		// Disable the PlayerController visually / physically
		if ( _pc.IsValid() )
		{
			_pc.Enabled = false;
		}

		// Could add ragdoll / death effects here later
	}

	#endregion

	#region Upgrades

	public float GetFireRateBonus()
	{
		if ( _um?.CurrentUpgrades == null ) return 0;
		return _um.CurrentUpgrades.ArrowFrequency * 0.3f;
	}

	public float GetDamageBonus()
	{
		if ( _um?.CurrentUpgrades == null ) return 0;
		return _um.CurrentUpgrades.ArrowDamage * 5f;
	}

	public float GetSpeedBonus()
	{
		if ( _um?.CurrentUpgrades == null ) return 0;
		return _um.CurrentUpgrades.ArrowSpeed * 100f;
	}

	public float GetDistanceBonus()
	{
		if ( _um?.CurrentUpgrades == null ) return 0;
		return _um.CurrentUpgrades.ArrowDistance * 200f;
	}

	public float GetMoveSpeedBonus()
	{
		if ( _um?.CurrentUpgrades == null ) return 0;
		return _um.CurrentUpgrades.MovementSpeed * 50f;
	}

	#endregion

	#region Visual

	private Color GetPlayerColor()
	{
		// Simple: owner 0 = blue, owner 1 = red
		var index = Network.OwnerId == Scene.GetAllComponents<GameManager>().FirstOrDefault()?.ConnectedPlayers?.FirstOrDefault()
			? 0 : 1;
		return index == 1 ? Color.Red : Color.Blue;
	}

	#endregion
}
