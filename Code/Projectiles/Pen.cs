/// <summary>
/// A pen projectile thrown by the player. Moves forward and damages enemies on collision.
/// Server-validated: damage is only applied host-side.
/// </summary>
public sealed class Pen : Component
{
	[Property] public float Speed { get; set; } = 500f;
	[Property] public float Damage { get; set; } = 10f;
	[Property] public float MaxDistance { get; set; } = 800f;
	[Property] public Guid OwnerId { get; set; }
	[Property] public int SplitCount { get; set; } = 0;
	[Property] public int BounceCount { get; set; } = 0;
	[Property] public int PierceCount { get; set; } = 0;
	[Property] public SoundEvent HitSound { get; set; }
	[Property] public float HitVolume { get; set; } = 1f;
	[Property] public string MixerTarget { get; set; } = "Game";

	private Vector3 _startPosition;
	private bool _hasHit = false;
	private int _bouncesLeft;
	private int _piercesLeft;
	private HashSet<Guid> _hitEnemies = new();

	protected override void OnStart()
	{
		_startPosition = WorldPosition;
		_bouncesLeft = BounceCount;
		_piercesLeft = PierceCount;
	}

	protected override void OnFixedUpdate()
	{
		if ( _hasHit ) return;

		// Move in the pen's facing direction
		WorldPosition += WorldRotation.Forward * Speed * Time.Delta;

		// Bounce off lane boundaries
		if ( _bouncesLeft > 0 )
		{
			var pos = WorldPosition;
			if ( pos.x < -200f || pos.x > 200f )
			{
				pos.x = pos.x.Clamp( -200f, 200f );
				WorldPosition = pos;
				var angles = WorldRotation.Yaw();
				WorldRotation = Rotation.FromYaw( -angles );
				_bouncesLeft--;
			}
		}

		// Check distance limit
		if ( WorldPosition.Distance( _startPosition ) >= MaxDistance )
		{
			DestroyPen();
			return;
		}

		// Check collision with enemies (server-side)
		if ( Networking.IsHost )
		{
			CheckEnemyCollision();
		}
	}

	private void CheckEnemyCollision()
	{
		foreach ( var enemy in Scene.GetAllComponents<Enemy>() )
		{
			if ( !enemy.IsValid() || !enemy.IsAlive ) continue;
			if ( _hitEnemies.Contains( enemy.GameObject.Id ) ) continue;

			// Use enemy scale for hit radius — bigger enemies are easier to hit
			var scale = enemy.WorldScale;
			var hitRadius = MathF.Max( scale.x, MathF.Max( scale.y, scale.z ) ) * 20f + 15f;
			if ( WorldPosition.Distance( enemy.WorldPosition ) > hitRadius )
				continue;

			enemy.TakeDamage( Damage, OwnerId );
			_hitEnemies.Add( enemy.GameObject.Id );

			// Play hit sound
			if ( HitSound is not null )
			{
				var handle = Sound.Play( HitSound, WorldPosition );
				handle.Volume = HitVolume;
			}

			if ( _piercesLeft > 0 )
			{
				_piercesLeft--;
				return; // exit loop, keep flying — will check again next frame
			}

			_hasHit = true;
			DestroyPen();
			return;
		}
	}

	private readonly List<Enemy> _hitBuffer = new();

	private void DestroyPen()
	{
		if ( SplitCount > 0 && Vector3.DistanceBetween( _startPosition, WorldPosition ) > 10f )
		{
			SpawnSplitPens();
		}
		GameObject.Destroy();
	}

	private void SpawnSplitPens()
	{
		var spread = 15f;
		var startAngle = -(SplitCount - 1) * spread * 0.5f;

		for ( int i = 0; i < SplitCount; i++ )
		{
			var splitGo = new GameObject( true, $"Split_{OwnerId}_{i}" );
			splitGo.WorldPosition = WorldPosition;
			splitGo.WorldRotation = WorldRotation * Rotation.FromYaw( startAngle + i * spread );

			var split = splitGo.Components.Create<Pen>();
			split.Damage = Damage * 0.5f;
			split.Speed = Speed * 0.8f;
			split.MaxDistance = MaxDistance * 0.5f;
			split.OwnerId = OwnerId;
			split.SplitCount = 0; // no further splitting

			var model = splitGo.Components.Create<ModelRenderer>();
			model.Model = Model.Cube;
			splitGo.LocalScale = new Vector3( 1f, 0.15f, 0.15f );
			splitGo.NetworkSpawn( null );
		}
	}
}
