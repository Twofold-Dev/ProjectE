/// <summary>
/// A projectile fired by the player. Moves rightward and damages enemies on collision.
/// Server-validated: damage is only applied host-side.
/// </summary>
public sealed class Arrow : Component
{
	[Property] public float Speed { get; set; } = 500f;
	[Property] public float Damage { get; set; } = 10f;
	[Property] public float MaxDistance { get; set; } = 800f;
	[Property] public Guid OwnerId { get; set; }
	[Property] public int SplitCount { get; set; } = 0;

	private Vector3 _startPosition;
	private bool _hasHit = false;

	protected override void OnStart()
	{
		_startPosition = WorldPosition;
	}

	protected override void OnFixedUpdate()
	{
		if ( _hasHit ) return;

		// Move in the arrow's facing direction
		WorldPosition += WorldRotation.Forward * Speed * Time.Delta;

		// Check distance limit
		if ( WorldPosition.Distance( _startPosition ) >= MaxDistance )
		{
			DestroyArrow();
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
		_hitBuffer.Clear();
		_hitBuffer.AddRange( Scene.GetAllComponents<Enemy>() );

		foreach ( var enemy in _hitBuffer )
		{
			if ( !enemy.IsValid() ) continue;
			if ( !enemy.IsAlive ) continue;

			// Use enemy scale for hit radius — bigger enemies are easier to hit
			var scale = enemy.WorldScale;
			var hitRadius = MathF.Max( scale.x, MathF.Max( scale.y, scale.z ) ) * 20f + 15f;
			if ( WorldPosition.Distance( enemy.WorldPosition ) > hitRadius )
				continue;

			_hasHit = true;
			enemy.TakeDamage( Damage, OwnerId );
			DestroyArrow();
			return;
		}
	}

	private readonly List<Enemy> _hitBuffer = new();

	private void DestroyArrow()
	{
		if ( SplitCount > 0 && Vector3.DistanceBetween( _startPosition, WorldPosition ) > 10f )
		{
			SpawnSplitArrows();
		}
		GameObject.Destroy();
	}

	private void SpawnSplitArrows()
	{
		var spread = 15f;
		var startAngle = -(SplitCount - 1) * spread * 0.5f;

		for ( int i = 0; i < SplitCount; i++ )
		{
			var splitGo = new GameObject( true, $"Split_{OwnerId}_{i}" );
			splitGo.WorldPosition = WorldPosition;
			splitGo.WorldRotation = WorldRotation * Rotation.FromYaw( startAngle + i * spread );

			var split = splitGo.Components.Create<Arrow>();
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
