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
	public float MaxHealth { get; set; } = 50f;

	[Property, Category( "Combat" )]
	public float ArrowSpeed { get; set; } = 500f;

	[Property, Category( "Combat" )]
	public float ArrowDamage { get; set; } = 10f;

	[Property, Category( "Combat" )]
	public float ArrowDistance { get; set; } = 800f;

	[Property, Category( "Models" ), Title( "Pen Model" )]
	public Model PenModel { get; set; }

	[Property, Category( "Models" ), Title( "Pen Scale" )]
	public Vector3 PenScale { get; set; } = new Vector3( 2f, 0.25f, 0.25f );

	[Property, Category( "Models" ), Title( "Blade Model" )]
	public Model BladeModel { get; set; }

	[Property, Category( "Models" ), Title( "Blade Scale" )]
	public Vector3 BladeScale { get; set; } = new Vector3( 1.5f, 0.1f, 3f );

	[Property, Category( "Models" ), Title( "Dog Model" )]
	public Model DogModel { get; set; }

	[Property, Category( "Models" ), Title( "Dog Projectile Model" )]
	public Model DogProjectileModel { get; set; }

	[Property, Category( "Audio" ), Title( "Sound Mixer" )]
	public string SoundMixer { get; set; } = "Game";

	[Property, Category( "Audio" ), Title( "Pen Throw Sound" )]
	public SoundEvent PenFireSound { get; set; }

	[Property, Category( "Audio" ), Title( "Pen Throw Volume" ), Range( 0, 1, 0.05f )]
	public float PenFireVolume { get; set; } = 1f;

	[Property, Category( "Audio" ), Title( "Pen Hit Sound" )]
	public SoundEvent PenHitSound { get; set; }

	[Property, Category( "Audio" ), Title( "Pen Hit Volume" ), Range( 0, 1, 0.05f )]
	public float PenHitVolume { get; set; } = 1f;

	[Property, Category( "Audio" ), Title( "Scissor Launch Sound" )]
	public SoundEvent ScissorLaunchSound { get; set; }

	[Property, Category( "Audio" ), Title( "Scissor Launch Volume" ), Range( 0, 1, 0.05f )]
	public float ScissorLaunchVolume { get; set; } = 1f;

	[Property, Category( "Audio" ), Title( "Scissor Hit Sound" )]
	public SoundEvent ScissorHitSound { get; set; }

	[Property, Category( "Audio" ), Title( "Scissor Hit Volume" ), Range( 0, 1, 0.05f )]
	public float ScissorHitVolume { get; set; } = 1f;

	[Property, Category( "Audio" ), Title( "Dog Fire Sound" )]
	public SoundEvent DogFireSound { get; set; }

	[Property, Category( "Audio" ), Title( "Dog Fire Volume" ), Range( 0, 1, 0.05f )]
	public float DogFireVolume { get; set; } = 1f;

	#endregion

	#region State

	[Sync] public float Health { get; set; } = 100f;
	[Sync] public bool IsDead { get; set; } = false;

	private UpgradeManager _um;
	private TimeSince _timeSinceLastFire = 0;
	private PlayerController _pc;
	private List<PaperShredderBlade> _shredderBlades = new();
	private List<DeskBuddy> _deskBuddies = new();
	private TimeSince _timeSinceBurst = 0;
	private float _burstCooldown = 2f;

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
				Playstyle.SplitShot => 1.5f,
				Playstyle.PowerShot => 0.6f,
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
				Playstyle.RapidFire => 6f,
				Playstyle.SplitShot => 7f,
				Playstyle.PowerShot => 30f,
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

		// --- Sync shredder blades ---
		if ( Networking.IsHost )
		{
			SyncShredders();
			HandleShredderBurst();
			SyncBuddies();
		}

		// --- Auto-fire ---
		var effectiveFireRate = PlaystyleBaseFireRate + GetFireRateBonus();
		var interval = 1.0f / effectiveFireRate;

		if ( _timeSinceLastFire >= interval )
		{
			_timeSinceLastFire = 0;
			ThrowPen();
		}
	}

	#region Combat

	private void ThrowPen()
	{
		var count = GetSplitCount();
		var baseAngle = -90f;
		var spread = 6f;
		var startAngle = count > 0 ? -(count * spread * 0.5f) : 0f;

		for ( int i = 0; i <= count; i++ )
		{
			SpawnPen( baseAngle + startAngle + i * spread );
		}
	}

	private void SpawnPen( float yaw )
	{
		var penGo = new GameObject( true, $"Pen_{GameObject.Name}" );
		penGo.WorldPosition = WorldPosition + Rotation.FromYaw( yaw ).Forward * 30f;
		penGo.WorldRotation = Rotation.FromYaw( yaw );

		var pen = penGo.Components.Create<Pen>();
		pen.Speed = PlaystyleArrowSpeed + GetSpeedBonus();
		pen.Damage = PlaystyleArrowDamage + GetDamageBonus();
		pen.MaxDistance = PlaystyleArrowDistance + GetDistanceBonus();
		pen.OwnerId = Network.OwnerId;
		pen.SplitCount = 0;
		pen.BounceCount = GetPenBounce();
		pen.PierceCount = GetPenPierce();
		Log.Info( $"Pen spawned with PierceCount={pen.PierceCount} BounceCount={pen.BounceCount}" );

		var model = penGo.Components.Create<ModelRenderer>();
		model.Model = PenModel ?? Model.Cube;
		penGo.LocalScale = PenScale;
		model.Tint = GetPlayerColor();

		// Pass audio references to the pen
		var penComponent = penGo.Components.Get<Pen>();
		if ( penComponent.IsValid() )
		{
			penComponent.HitSound = PenHitSound;
			penComponent.HitVolume = PenHitVolume;
			penComponent.MixerTarget = SoundMixer;
		}

		// Play pen throw sound
		if ( PenFireSound is not null )
		{
			var handle = Sound.Play( PenFireSound, WorldPosition );
			handle.Volume = PenFireVolume;
		}

		penGo.NetworkSpawn( null );
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

		// Destroy shredder blades
		CleanupShredders();

		if ( _pc.IsValid() )
		{
			// Create ragdoll before disabling anything
			_pc.CreateRagdoll( $"Ragdoll_{GameObject.Name}" );

			// Hide ALL model renderers on this GameObject and its children
			foreach ( var renderer in GameObject.Components.GetAll<ModelRenderer>( FindMode.EverythingInSelfAndChildren ) )
			{
				if ( renderer.IsValid() )
					renderer.Enabled = false;
			}
			foreach ( var renderer in GameObject.Components.GetAll<SkinnedModelRenderer>( FindMode.EverythingInSelfAndChildren ) )
			{
				if ( renderer.IsValid() )
					renderer.Enabled = false;
			}

			// Freeze physics body completely
			var rb = GameObject.Components.Get<Rigidbody>();
			if ( rb.IsValid() )
			{
				rb.Velocity = Vector3.Zero;
				rb.AngularVelocity = Vector3.Zero;
				rb.MotionEnabled = false; // stop all physics simulation
			}

			// Disable the PlayerController (stops all processing)
			_pc.Enabled = false;
		}

		// Notify GameManager
		var gm = Scene.GetAllComponents<GameManager>().FirstOrDefault();
		gm?.OnPlayerDied( this );
	}

	#endregion

	#region Card Upgrades (Unique)

	public int GetPenBounce()
	{
		if ( _um?.CurrentUpgrades == null ) return 0;
		return _um.CurrentUpgrades.PenBounce;
	}

	public int GetPenPierce()
	{
		if ( _um?.CurrentUpgrades == null ) return 0;
		return _um.CurrentUpgrades.PenPierce;
	}

	public int GetBladeBounce()
	{
		if ( _um?.CurrentUpgrades == null ) return 0;
		return _um.CurrentUpgrades.BladeBounce;
	}

	#endregion

	#region Buddies

	public int GetBuddyCount()
	{
		if ( _um?.CurrentUpgrades == null ) return 0;
		return _um.CurrentUpgrades.PetCount;
	}

	public float GetBuddyFireRate()
	{
		if ( _um?.CurrentUpgrades == null ) return 1f;
		return 1f + _um.CurrentUpgrades.PetFireRate * 0.5f;
	}

	private void SyncBuddies()
	{
		var target = GetBuddyCount();
		while ( _deskBuddies.Count < target )
		{
			SpawnBuddy( _deskBuddies.Count, target );
		}
		while ( _deskBuddies.Count > target && _deskBuddies.Count > 0 )
		{
			var last = _deskBuddies[^1];
			if ( last.IsValid() )
				last.GameObject.Destroy();
			_deskBuddies.RemoveAt( _deskBuddies.Count - 1 );
		}
		for ( int i = 0; i < _deskBuddies.Count; i++ )
		{
			if ( _deskBuddies[i].IsValid() )
			{
				_deskBuddies[i].BuddyIndex = i;
				_deskBuddies[i].BuddyCount = _deskBuddies.Count;
				_deskBuddies[i].FireRate = GetBuddyFireRate();
			}
		}
	}

	private void SpawnBuddy( int index, int total )
	{
		var go = new GameObject( true, $"Buddy_{GameObject.Name}_{index}" );
		go.WorldPosition = WorldPosition;

		var buddy = go.Components.Create<DeskBuddy>();
		buddy.BuddyIndex = index;
		buddy.BuddyCount = total;
		buddy.OwnerId = Network.OwnerId;
		buddy.FireRate = GetBuddyFireRate();
		buddy.DogModel = DogModel;
		buddy.FireSound = DogFireSound;
		buddy.FireVolume = DogFireVolume;
		buddy.ProjectileModel = DogProjectileModel;

		var model = go.Components.Create<ModelRenderer>();
		model.Model = DogModel ?? Model.Cube;
		go.LocalScale = Vector3.One;
		model.Tint = new Color( 0.8f, 0.6f, 0.4f );

		go.NetworkSpawn( null );
		_deskBuddies.Add( buddy );
	}

	private void CleanupBuddies()
	{
		foreach ( var buddy in _deskBuddies )
		{
			if ( buddy.IsValid() )
				buddy.GameObject.Destroy();
		}
		_deskBuddies.Clear();
	}

	#endregion

	#region Shredders

	public int GetShredderCount()
	{
		if ( _um?.CurrentUpgrades == null ) return 0;
		return _um.CurrentUpgrades.SwordCount;
	}

	public float GetShredderDamage()
	{
		if ( _um?.CurrentUpgrades == null ) return 10f;
		return 10f + _um.CurrentUpgrades.SwordDamage * 8f;
	}

	public int GetShredderRange()
	{
		if ( _um?.CurrentUpgrades == null ) return 600;
		return 600 + _um.CurrentUpgrades.SwordRange * 100;
	}

	private void HandleShredderBurst()
	{
		// Only fire burst when all blades are idle (orbiting)
		bool allIdle = true;
		foreach ( var blade in _shredderBlades )
		{
			if ( !blade.IsValid() || !blade.IsIdle )
			{
				allIdle = false;
				break;
			}
		}
		if ( _shredderBlades.Count == 0 ) allIdle = false;

		// Wait for cooldown
		var cooldown = Math.Max( 0.3f, _burstCooldown - _um.CurrentUpgrades.SwordFrequency * 0.2f );
		if ( !allIdle || _timeSinceBurst < cooldown ) return;

		// Find nearest enemy
		Enemy nearest = null;
		float nearestDist = GetShredderRange(); // max burst range
		foreach ( var enemy in Scene.GetAllComponents<Enemy>() )
		{
			if ( !enemy.IsAlive ) continue;
			var dist = WorldPosition.Distance( enemy.WorldPosition );
			if ( dist < nearestDist )
			{
				nearestDist = dist;
				nearest = enemy;
			}
		}

		if ( nearest == null ) return; // no enemies to burst at

		// Burst fire — launch ALL blades at nearest enemy simultaneously
		foreach ( var blade in _shredderBlades )
		{
			if ( blade.IsValid() )
				blade.BurstLaunch( nearest.GameObject );
		}
		_timeSinceBurst = 0;
	}

	private void SyncShredders()
	{
		var target = GetShredderCount();
		// Spawn blades up to target
		while ( _shredderBlades.Count < target )
		{
			SpawnShredder( _shredderBlades.Count );
		}
		// Remove excess blades
		while ( _shredderBlades.Count > target && _shredderBlades.Count > 0 )
		{
			var last = _shredderBlades[^1];
			if ( last.IsValid() )
				last.GameObject.Destroy();
			_shredderBlades.RemoveAt( _shredderBlades.Count - 1 );
		}
		// Recalculate even spacing for all remaining blades
		var count = _shredderBlades.Count;
		for ( int i = 0; i < count; i++ )
		{
			if ( _shredderBlades[i].IsValid() )
				_shredderBlades[i].OrbitAngle = (360f / count) * i;
		}
	}

	private void SpawnShredder( int index )
	{
		var go = new GameObject( true, $"Shredder_{GameObject.Name}_{index}" );
		go.WorldPosition = WorldPosition;

		var blade = go.Components.Create<PaperShredderBlade>();
		blade.OrbitAngle = (360f / Math.Max( 1, GetShredderCount() )) * index;
		blade.Damage = GetShredderDamage();
		blade.BladeBounce = GetBladeBounce();
		blade.OwnerId = Network.OwnerId;
var model = go.Components.Create<ModelRenderer>();
model.Model = BladeModel ?? Model.Cube;
go.LocalScale = Vector3.One;

model.Tint = new Color( 0.55f, 0.55f, 0.65f );

// Pass audio references to the blade
blade.HitSound = ScissorHitSound;
blade.HitVolume = ScissorHitVolume;
blade.LaunchSound = ScissorLaunchSound;
blade.LaunchVolume = ScissorLaunchVolume;
blade.MixerTarget = SoundMixer;

go.NetworkSpawn( null );
_shredderBlades.Add( blade );
	}

	private void CleanupShredders()
	{
		foreach ( var blade in _shredderBlades )
		{
			if ( blade.IsValid() )
				blade.GameObject.Destroy();
		}
		_shredderBlades.Clear();
	}

	#endregion

	#region Upgrades

	public int GetSplitCount()
	{
		if ( _um?.CurrentUpgrades == null ) return 0;
		if ( !_um.CurrentUpgrades.PlaystyleLocked ) return 0;
		var splitBase = _um.CurrentUpgrades.ChosenPlaystyle == Playstyle.SplitShot ? 2 : 0;
		return splitBase + _um.CurrentUpgrades.SplitCount;
	}

	public float EffectiveFireRate => PlaystyleBaseFireRate + GetFireRateBonus();
	public float EffectiveDamage => PlaystyleArrowDamage + GetDamageBonus();
	public float EffectiveSpeed => PlaystyleArrowSpeed + GetSpeedBonus();
	public float EffectiveRange => PlaystyleArrowDistance + GetDistanceBonus();
	public int EffectiveSplitCount => GetSplitCount() + 1;

	public float GetFireRateBonus()
	{
		if ( _um?.CurrentUpgrades == null ) return 0;
		return _um.CurrentUpgrades.ArrowFrequency * 0.15f;
	}

	public float GetDamageBonus( float yawOffset = 0f )
	{
		if ( _um?.CurrentUpgrades == null ) return 0;
		var bonus = _um.CurrentUpgrades.ArrowDamage * 3f;
		// Split shot arrows deal reduced damage
		if ( GetSplitCount() > 0 && yawOffset != 0f )
			bonus *= 0.5f;
		return bonus;
	}

	/// <summary>
	/// Calculate current total effective DPS factoring in range.
	/// Used by WaveManager to set enemy HP proportional to player damage output.
	/// Pens that can't reach the enemy contribute reduced DPS.
	/// </summary>
	public float GetCurrentDPS( float spawnDistance = 800f )
	{
		var fireRate = EffectiveFireRate;
		var damage = EffectiveDamage;
		var pens = EffectiveSplitCount;
		var baseDPS = fireRate * damage * pens;

		// Factor in range: pen travel distance relative to enemy spawn distance
		float maxDist = PlaystyleArrowDistance + GetDistanceBonus();
		float rangeFactor = Math.Clamp( maxDist / spawnDistance, 0f, 1f );

		return baseDPS * rangeFactor;
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
