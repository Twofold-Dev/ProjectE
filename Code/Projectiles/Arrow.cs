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

	private Vector3 _startPosition;
	private bool _hasHit = false;

	protected override void OnStart()
	{
		_startPosition = WorldPosition;
	}

	protected override void OnFixedUpdate()
	{
		if ( _hasHit ) return;

		// Move rightward
		WorldPosition += Vector3.Right * Speed * Time.Delta;

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
		// TODO: Replace with pooled return later
		GameObject.Destroy();
	}
}
