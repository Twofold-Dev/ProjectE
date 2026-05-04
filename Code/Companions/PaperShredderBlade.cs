/// <summary>
/// An orbiting shredder blade that circles the player.
/// All blades burst-fire simultaneously when ArrowPlayer triggers the burst.
/// </summary>
public sealed class PaperShredderBlade : Component
{
	#region Configuration

	[Property] public float OrbitRadius { get; set; } = 60f;
	[Property] public float OrbitSpeed { get; set; } = 120f;
	[Property] public float SeekSpeed { get; set; } = 500f;
	[Property] public float ReturnSpeed { get; set; } = 300f;
	[Property] public float HitRange { get; set; } = 30f;
	[Property] public float Damage { get; set; } = 10f;
	[Property] public Guid OwnerId { get; set; }
	[Property] public float OrbitAngle { get; set; } = 0f;
	[Property] public int BladeBounce { get; set; } = 0;
	[Property] public SoundEvent LaunchSound { get; set; }
	[Property] public float LaunchVolume { get; set; } = 1f;
	[Property] public SoundEvent HitSound { get; set; }
	[Property] public float HitVolume { get; set; } = 1f;

	#endregion

	#region State

	private enum BladeState { Orbiting, Seeking, Returning }
	private BladeState _state = BladeState.Orbiting;
	private float _currentAngle;
	private GameObject _target;
	private ArrowPlayer _owner;
	private int _bouncesLeft;
	private HashSet<Guid> _hitEnemies = new();

	/// <summary>
	/// True when blade is orbiting and ready for a burst launch.
	/// </summary>
	public bool IsIdle => _state == BladeState.Orbiting;

	#endregion

	protected override void OnStart()
	{
		_currentAngle = OrbitAngle;
		_bouncesLeft = BladeBounce;

		foreach ( var p in Scene.GetAllComponents<ArrowPlayer>() )
		{
			if ( p.Network.OwnerId == OwnerId )
			{
				_owner = p;
				break;
			}
		}
	}

	protected override void OnFixedUpdate()
	{
		if ( !Networking.IsHost ) return;
		if ( _owner == null || !_owner.IsValid() )
		{
			GameObject.Destroy();
			return;
		}

		switch ( _state )
		{
			case BladeState.Orbiting:
				UpdateOrbit();
				break;
			case BladeState.Seeking:
				UpdateSeek();
				break;
			case BladeState.Returning:
				UpdateReturn();
				break;
		}
	}

	/// <summary>
	/// Called by ArrowPlayer to launch this blade at a target (burst fire).
	/// </summary>
	public void BurstLaunch( GameObject target )
	{
		if ( _state != BladeState.Orbiting ) return;
		if ( !target.IsValid() ) return;
		_target = target;
		_state = BladeState.Seeking;
		_hitEnemies.Clear(); // fresh flight

		// Play launch sound
		if ( LaunchSound is not null )
		{
			var handle = Sound.Play( LaunchSound, WorldPosition );
			handle.Volume = LaunchVolume;
		}
	}

	private void UpdateOrbit()
	{
		_currentAngle += OrbitSpeed * Time.Delta;
		var pos = _owner.WorldPosition;
		pos.x += MathF.Cos( _currentAngle * MathF.PI / 180f ) * OrbitRadius;
		pos.y += MathF.Sin( _currentAngle * MathF.PI / 180f ) * OrbitRadius;
		WorldPosition = pos;

		var tangent = _owner.WorldPosition - WorldPosition;
		if ( tangent.Length > 0.01f )
			WorldRotation = Rotation.LookAt( tangent );
	}

	private void UpdateSeek()
	{
		if ( _target == null || !_target.IsValid() ||
			!_target.Components.TryGet<Enemy>( out var enemy ) || !enemy.IsAlive )
		{
			_state = BladeState.Returning;
			_target = null;
			return;
		}

		var dir = ( _target.WorldPosition - WorldPosition ).Normal;
		WorldPosition += dir * SeekSpeed * Time.Delta;
		// Smoothly rotate to X=90 while facing target (scissors tilt)
		var seekRot = Rotation.LookAt( dir ) * Rotation.From( 90, 0, 0 );
		WorldRotation = Rotation.Slerp( WorldRotation, seekRot, Time.Delta * 8f );

		if ( WorldPosition.Distance( _target.WorldPosition ) < HitRange )
		{
			enemy.TakeDamage( Damage, OwnerId );
			_hitEnemies.Add( enemy.GameObject.Id );

			// Play hit sound
			if ( HitSound is not null )
			{
				var handle = Sound.Play( HitSound, WorldPosition );
				handle.Volume = HitVolume;
			}

			// Blade bounce: seek nearest enemy instead of returning
			if ( _bouncesLeft > 0 )
			{
				_bouncesLeft--;
				Enemy nextTarget = null;
				float nearestDist = 800f;
				foreach ( var e in Scene.GetAllComponents<Enemy>() )
				{
					if ( !e.IsAlive || _hitEnemies.Contains( e.GameObject.Id ) ) continue;
					var d = WorldPosition.Distance( e.WorldPosition );
					if ( d < nearestDist ) { nearestDist = d; nextTarget = e; }
				}
				if ( nextTarget != null )
				{
					_target = nextTarget.GameObject;
					return; // continue seeking
				}
			}

			_state = BladeState.Returning;
			_target = null;
			_hitEnemies.Clear(); // reset for next flight
		}
	}

	private void UpdateReturn()
	{
		var dir = ( _owner.WorldPosition - WorldPosition ).Normal;
		WorldPosition += dir * ReturnSpeed * Time.Delta;
		// Smoothly rotate back to X=0 while facing player
		var returnRot = Rotation.LookAt( dir );
		WorldRotation = Rotation.Slerp( WorldRotation, returnRot, Time.Delta * 8f );

		if ( WorldPosition.Distance( _owner.WorldPosition ) < OrbitRadius * 0.8f )
		{
			_state = BladeState.Orbiting;
			_currentAngle = OrbitAngle; // reset to evenly-spaced position
		}
	}
}
