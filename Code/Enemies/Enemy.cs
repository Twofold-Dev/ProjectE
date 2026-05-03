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

	private Sandbox.UI.Label _hpLabel;

	protected override void OnStart()
	{
		CurrentHealth = BaseHealth;

		// Create world HP label above the enemy
		var hpWorld = Components.Create<WorldPanel>();
		if ( hpWorld.IsValid() )
		{
			hpWorld.PanelSize = new Vector2( 200, 80 );
			hpWorld.LookAtCamera = true;
			hpWorld.Transform.LocalPosition = new Vector3( 0, 0, 60 );

			var root = hpWorld.GetPanel();
			if ( root.IsValid() )
			{
				_hpLabel = root.AddChild<Sandbox.UI.Label>();
				_hpLabel.Text = $"{CurrentHealth:F0}";
				_hpLabel.Style.FontSize = 40;
				_hpLabel.Style.FontColor = Color.White;
				_hpLabel.Style.FontWeight = 900;
			}
		}
	}

	protected override void OnFixedUpdate()
	{
		if ( !IsAlive ) return;
		if ( !Networking.IsHost ) return;

		// Move leftward (-X) toward players
		WorldPosition += Vector3.Left * Speed * Time.Delta;

		// Check if enemy has reached Y=0 (player's position)
		if ( WorldPosition.y >= 0 )
		{
			DamageNearestPlayer();
		}
	}

	/// <summary>
	/// Apply damage to this enemy. Server-only.
	/// </summary>
	[Rpc.Broadcast]
	public void TakeDamage( float damage, Guid attackerId )
	{
		if ( !IsAlive ) return;

		CurrentHealth -= damage;

		// Update HP label
		if ( _hpLabel.IsValid() )
		{
			_hpLabel.Text = $"{CurrentHealth:F0}";
		}

		if ( CurrentHealth <= 0 )
		{
			CurrentHealth = 0;
			IsAlive = false;
			KilledBy = attackerId;

			OnDeath();
		}
	}

	private void DamageNearestPlayer()
	{
		if ( !IsAlive ) return;

		// Find the player closest to this enemy's position
		var players = Scene.GetAllComponents<ArrowPlayer>()
			.Where( p => !p.IsDead )
			.OrderBy( p => Math.Abs( p.WorldPosition.y - WorldPosition.y ) )
			.ToList();

		if ( players.Count == 0 ) return;

		var target = players.First();
		Log.Info( $"Enemy reached player {target.GameObject.Name}, dealing {Damage} damage" );
		target.TakeDamage( Damage, Guid.Empty );

		// Enemy is consumed on contact
		IsAlive = false;
		OnDeath();
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
