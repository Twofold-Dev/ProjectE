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

	[Property, Category( "Movement" )]
	public float ForwardSpeed { get; set; } = 200f;

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


	private float PlaystyleBaseFireRate
	{
		get
		{
			if ( _um?.CurrentUpgrades == null || !_um.CurrentUpgrades.PlaystyleLocked )
				return 1f;
			return _um.CurrentUpgrades.ChosenPlaystyle switch
			{
				Playstyle.RapidFire => 3f,
				Playstyle.SplitShot => 2f,
				Playstyle.PowerShot => 0.5f,
				_ => 1f,
			};
		}
	}

	private float PlaystyleArrowDamage
	{
		get
		{
			if ( _um?.CurrentUpgrades == null || !_um.CurrentUpgrades.PlaystyleLocked )
				return 10f;
			return _um.CurrentUpgrades.ChosenPlaystyle switch
			{
				Playstyle.RapidFire => 5f,
				Playstyle.SplitShot => 8f,
				Playstyle.PowerShot => 25f,
				_ => 10f,
			};
		}
	}

	private float PlaystyleArrowSpeed
	{
		get
		{
			if ( _um?.CurrentUpgrades == null || !_um.CurrentUpgrades.PlaystyleLocked )
				return 500f;
			return _um.CurrentUpgrades.ChosenPlaystyle switch
			{
				Playstyle.RapidFire => 500f,
				Playstyle.SplitShot => 400f,
				Playstyle.PowerShot => 700f,
				_ => 500f,
			};
		}
	}

	private float PlaystyleArrowDistance
	{
		get
		{
			if ( _um?.CurrentUpgrades == null || !_um.CurrentUpgrades.PlaystyleLocked )
				return 800f;
			return _um.CurrentUpgrades.ChosenPlaystyle switch
			{
				Playstyle.RapidFire => 600f,
				Playstyle.SplitShot => 500f,
				Playstyle.PowerShot => 1000f,
				_ => 800f,
			};
		}
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

		// A/D lane movement + auto-walk forward
		_pc.WishVelocity = new Vector3( moveInput.y * MoveSpeed, -ForwardSpeed, 0 );

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

		// --- Lane clamping (X only, Y is free for forward walk) ---
		var pos = WorldPosition;
		pos.x = pos.x.Clamp( LaneMinX, LaneMaxX );
		WorldPosition = pos;

		// --- Auto-fire ---
		var effectiveFireRate = PlaystyleBaseFireRate + GetFireRateBonus();
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
		var count = GetSplitCount();
		var baseAngle = -90f;
		var spread = 6f;
		var startAngle = count > 0 ? -(count * spread * 0.5f) : 0f;

		for ( int i = 0; i <= count; i++ )
		{
			SpawnSingleArrow( baseAngle + startAngle + i * spread );
		}
	}

	private void SpawnSingleArrow( float yaw )
	{
		var arrowGo = new GameObject( true, $"Arrow_{GameObject.Name}" );
		arrowGo.WorldPosition = WorldPosition + Rotation.FromYaw( yaw ).Forward * 30f;
		arrowGo.WorldRotation = Rotation.FromYaw( yaw );

		var arrow = arrowGo.Components.Create<Arrow>();
		arrow.Speed = PlaystyleArrowSpeed + GetSpeedBonus();
		arrow.Damage = PlaystyleArrowDamage + GetDamageBonus();
		arrow.MaxDistance = PlaystyleArrowDistance + GetDistanceBonus();
		arrow.OwnerId = Network.OwnerId;
		arrow.SplitCount = 0;

		var model = arrowGo.Components.Create<ModelRenderer>();
		model.Model = Model.Cube;
		arrowGo.LocalScale = new Vector3( 2f, 0.25f, 0.25f );
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

	public int GetSplitCount()
	{
		if ( _um?.CurrentUpgrades == null ) return 0;
		if ( !_um.CurrentUpgrades.PlaystyleLocked ) return 0;
		return _um.CurrentUpgrades.ChosenPlaystyle == Playstyle.SplitShot ? 2 + _um.CurrentUpgrades.ArrowDistance : 0;
	}

	public float EffectiveFireRate => PlaystyleBaseFireRate + GetFireRateBonus();
	public float EffectiveDamage => PlaystyleArrowDamage + GetDamageBonus();
	public float EffectiveSpeed => PlaystyleArrowSpeed + GetSpeedBonus();
	public float EffectiveRange => PlaystyleArrowDistance + GetDistanceBonus();
	public int EffectiveSplitCount => GetSplitCount() + 1;

	public float GetFireRateBonus()
	{
		if ( _um?.CurrentUpgrades == null ) return 0;
		return _um.CurrentUpgrades.ArrowFrequency * 0.3f;
	}

	public float GetDamageBonus( float yawOffset = 0f )
	{
		if ( _um?.CurrentUpgrades == null ) return 0;
		var bonus = _um.CurrentUpgrades.ArrowDamage * 5f;
		// Split shot arrows deal reduced damage
		if ( GetSplitCount() > 0 && yawOffset != 0f )
			bonus *= 0.5f;
		return bonus;
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
		var players = Scene.GetAllComponents<ArrowPlayer>().ToList();
		var index = players.IndexOf( this );
		return index == 1 ? Color.Red : Color.Blue;
	}

	#endregion
}
