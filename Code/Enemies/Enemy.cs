/// <summary>
/// Base class for all enemy types in Arrow a Row.
/// Server-spawned, replicated to all clients.
/// </summary>
public class Enemy : Component
{
	/// <summary>
	/// Fired when this enemy dies. Passes (enemy, killerPlayerId).
	/// Used by WaveManager to track remaining enemies.
	/// </summary>
	public Action<Enemy, Guid> OnEnemyDied;

	[Property] public float BaseHealth { get; set; } = 30f;
	[Property] public float Speed { get; set; } = 100f;
	[Property] public float Damage { get; set; } = 10f;
	[Property] public int ScoreValue { get; set; } = 100;

	[Sync] public float CurrentHealth { get; set; }
	[Sync] public bool IsAlive { get; set; } = true;

	/// <summary>
	/// The player who dealt the killing blow.
	/// </summary>
	public Guid KilledBy { get; private set; }

	protected override void OnStart()
	{
		CurrentHealth = BaseHealth;
	}

	protected override void OnFixedUpdate()
	{
		if ( !IsAlive ) return;
		if ( !Networking.IsHost ) return;

		// Check if player has walked past this enemy
		foreach ( var player in Scene.GetAllComponents<ArrowPlayer>() )
		{
			if ( player.IsDead ) continue;
			// Player is behind this enemy (player Y < enemy Y since walking -Y)
			if ( player.WorldPosition.y < WorldPosition.y )
			{
				var dist = Math.Abs( player.WorldPosition.y - WorldPosition.y );
				if ( dist < 50f )
				{
					// Deal damage equal to enemy's remaining HP
					player.TakeDamage( CurrentHealth, Guid.Empty );
					IsAlive = false;
					OnDeath();
					return;
				}
			}
		}
	}

	/// <summary>
	/// Apply damage to this enemy. Server-authoritative — clients send [Rpc.Host] requests.
	/// The host validates damage values, applies them, and [Sync] replicates to all clients.
	/// </summary>
	[Rpc.Host]
	public void TakeDamage( float damage, Guid attackerId )
	{
		if ( !IsAlive ) return;
		if ( !Networking.IsHost ) return;

		// Clamp damage to prevent abuse
		damage = Math.Clamp( damage, 0, 1000f );

		CurrentHealth -= damage;

		if ( CurrentHealth <= 0 )
		{
			CurrentHealth = 0;
			IsAlive = false;
			KilledBy = attackerId;

			OnDeath();
		}
	}

	protected virtual void OnDeath()
	{
		// Notify WaveManager
		OnEnemyDied?.Invoke( this, KilledBy );

		// Override in subclasses for death effects
		GameObject.Destroy();
	}
}

/// <summary>
/// Basic enemy — walks straight, low HP.
/// </summary>
public sealed class BasicEnemy : Enemy
{
	protected override void OnStart()
	{
		base.OnStart();
		BaseHealth = 30f;
		Speed = 80f;
		ScoreValue = 100;
	}
}

/// <summary>
/// Fast enemy — moves quickly, low HP.
/// </summary>
public sealed class FastEnemy : Enemy
{
	protected override void OnStart()
	{
		base.OnStart();
		BaseHealth = 15f;
		Speed = 180f;
		ScoreValue = 150;
	}
}

/// <summary>
/// Tank enemy — slow, high HP.
/// </summary>
public sealed class TankEnemy : Enemy
{
	protected override void OnStart()
	{
		base.OnStart();
		BaseHealth = 120f;
		Speed = 40f;
		ScoreValue = 300;
	}
}

/// <summary>
/// Boss enemy — large HP pool, special attacks (future), oversized placeholder.
/// </summary>
public sealed class BossEnemy : Enemy
{
	protected override void OnStart()
	{
		base.OnStart();
		BaseHealth = 500f;
		Speed = 30f;
		ScoreValue = 1000;
	}
}
