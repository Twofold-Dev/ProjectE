/// <summary>
/// Server-only wave spawner. Handles enemy spawning, difficulty scaling, and wave progression.
/// Attach this to the GameManager GameObject or a dedicated WaveManager GameObject.
/// </summary>
public sealed class WaveManager : Component
{
	#region Configuration

	[Property, Category( "Waves" )]
	public float TimeBetweenWaves { get; set; } = 3f;

	[Property, Category( "Waves" )]
	public int BaseEnemyCount { get; set; } = 3;

	[Property, Category( "Waves" )]
	public int MaxEnemyCount { get; set; } = 15;

	[Property, Category( "Waves" )]
	public float SpawnInterval { get; set; } = 0.8f;

	[Property, Category( "Waves" )]
	public float SpawnY { get; set; } = -500f; // spawn at far end of lane

	[Property, Category( "Waves" )]
	public float SpawnXRange { get; set; } = 200f; // random X offset within lane (-200 to 200)

	[Property, Category( "Waves" )]
	public int SubBossInterval { get; set; } = 5; // sub-boss every N waves

	[Property, Category( "Waves" )]
	public int FinalBossWave { get; set; } = 20;

	[Property, Category( "Difficulty" )]
	public float HealthScale { get; set; } = 1.08f; // per-wave HP multiplier

	[Property, Category( "Difficulty" )]
	public float SpeedScale { get; set; } = 1.03f; // per-wave speed multiplier

	#endregion

	#region Models & Materials

	[Property, Category( "Visuals" ), Title( "Default Model" ), Description( "Fallback model when no type-specific model is assigned" )]
	public Model DefaultModel { get; set; }

	[Property, Category( "Visuals" ), Title( "Basic Enemy Model" )]
	public Model BasicModel { get; set; }

	[Property, Category( "Visuals" ), Title( "Fast Enemy Model" )]
	public Model FastModel { get; set; }

	[Property, Category( "Visuals" ), Title( "Tank Enemy Model" )]
	public Model TankModel { get; set; }

	[Property, Category( "Visuals" ), Title( "Boss Enemy Model" )]
	public Model BossModel { get; set; }

	[Property, Category( "Visuals" ), Title( "Basic Enemy Material" )]
	public Material BasicMaterial { get; set; }

	[Property, Category( "Visuals" ), Title( "Fast Enemy Material" )]
	public Material FastMaterial { get; set; }

	[Property, Category( "Visuals" ), Title( "Tank Enemy Material" )]
	public Material TankMaterial { get; set; }

	[Property, Category( "Visuals" ), Title( "Boss Enemy Material" )]
	public Material BossMaterial { get; set; }

	#endregion

	#region State

	[Sync] public int CurrentWave { get; set; } = 0;
	[Sync] public int EnemiesRemaining { get; set; } = 0;
	[Sync] public bool WaveActive { get; set; } = false;

	private int _enemiesSpawnedThisWave = 0;
	private int _targetEnemyCount = 0;
	private TimeSince _timeSinceLastSpawn = 0;
	private TimeSince _timeSinceWaveEnd = 0;
	private GameManager _gm;
	private GameManager.GameState _lastState = GameManager.GameState.Lobby;

	#endregion

	protected override void OnStart()
	{
		_gm = Scene.GetAllComponents<GameManager>().FirstOrDefault();
	}

	protected override void OnFixedUpdate()
	{
		if ( !Networking.IsHost ) return; // server only

		if ( _gm == null || !_gm.IsValid() )
		{
			_gm = Scene.GetAllComponents<GameManager>().FirstOrDefault();
			return;
		}

		switch ( _gm.State )
		{
			case GameManager.GameState.Playing:
				HandlePlayingState();
				break;

			case GameManager.GameState.UpgradeSelect:
				HandleUpgradeState();
				break;

			case GameManager.GameState.GameOver:
				WaveActive = false;
				break;
		}
	}

	private void HandlePlayingState()
	{
		if ( !WaveActive )
		{
			// Start a new wave
			StartNextWave();
		}
		else
		{
			// Spawn enemies at interval
			if ( _enemiesSpawnedThisWave < _targetEnemyCount )
			{
				if ( _timeSinceLastSpawn >= SpawnInterval )
				{
					_timeSinceLastSpawn = 0;
					SpawnEnemy();
					_enemiesSpawnedThisWave++;
				}
			}

			// Check if all enemies are dead
			if ( _enemiesSpawnedThisWave >= _targetEnemyCount && EnemiesRemaining <= 0 )
			{
				WaveComplete();
			}
		}
	}

	private void HandleUpgradeState()
	{
		// Wave was completed, waiting for players to pick upgrades
		// GameManager handles transition back to Playing
	}

	private void StartNextWave()
	{
		CurrentWave++;
		_enemiesSpawnedThisWave = 0;
		_timeSinceLastSpawn = 0;

		// Calculate enemy count for this wave
		int playerCount = Math.Max( 1, Scene.GetAllComponents<ArrowPlayer>().Count() );
		_targetEnemyCount = Math.Min( BaseEnemyCount + CurrentWave / 2, MaxEnemyCount ) * playerCount;

		EnemiesRemaining = _targetEnemyCount;
		WaveActive = true;

		Log.Info( $"=== Wave {CurrentWave} started ({_targetEnemyCount} enemies) ===" );
		OnWaveStarted( CurrentWave, _targetEnemyCount );
	}

	private void SpawnEnemy()
	{
		var spawnPos = new Vector3( Random.Shared.Float( -SpawnXRange, SpawnXRange ), SpawnY, 0 );

		var enemyGo = new GameObject( true, $"Enemy_{CurrentWave}_{_enemiesSpawnedThisWave}" );

		// Set position BEFORE any components are created, so the initial
		// network snapshot from NetworkSpawn captures the correct position
		enemyGo.WorldPosition = spawnPos;

		Enemy enemy;

		// Determine enemy type based on wave difficulty
		if ( CurrentWave % SubBossInterval == 0 && CurrentWave > 0 && _enemiesSpawnedThisWave == _targetEnemyCount - 1 )
		{
			// Sub-boss: spawns as the last enemy of the wave
			enemy = CreateBossEnemy( enemyGo );
		}
		else if ( CurrentWave >= FinalBossWave && _enemiesSpawnedThisWave == _targetEnemyCount - 1 )
		{
			// Final boss
			enemy = CreateBossEnemy( enemyGo );
			enemy.BaseHealth *= 3f;
			enemy.ScoreValue *= 5;
		}
		else
		{
			// Random regular enemy type
			enemy = CreateRandomEnemy( enemyGo );
		}

		// Apply difficulty scaling
		enemy.BaseHealth *= MathF.Pow( HealthScale, CurrentWave - 1 );
		enemy.Speed *= MathF.Pow( SpeedScale, CurrentWave - 1 );

		// Visual: use assigned model or cube placeholder
		var model = enemyGo.Components.Create<ModelRenderer>();
		model.Model = GetEnemyModel( enemy ) ?? Model.Cube;
		enemyGo.LocalScale = GetEnemyScale( enemy );

		// Material: apply type-specific material if assigned
		var material = GetEnemyMaterial( enemy );
		if ( material is not null )
		{
			model.Tint = Color.White; // ensure tint doesn't interfere with material
		}

		// Collider + Rigidbody: needed so arrows can detect hits via physics
		var collider = enemyGo.Components.Create<BoxCollider>();
		collider.IsTrigger = true;
		var body = enemyGo.Components.Create<Rigidbody>();
		body.MotionEnabled = false;
		body.CollisionEventsEnabled = false;

		// NetworkSpawn captures the current state (including position) for replication
		enemyGo.NetworkSpawn( null );

		// Track when enemy dies to decrement EnemiesRemaining
		enemy.OnEnemyDied += OnEnemyKilled;
	}

	private Enemy CreateRandomEnemy( GameObject go )
	{
		// Weighted random: more basic, fewer tanks
		var roll = Random.Shared.Float( 0, 1 );
		Enemy enemy;

		if ( roll < 0.5f )
		{
			enemy = go.Components.Create<BasicEnemy>();
			enemy.BaseHealth = 30f;
			enemy.Speed = 80f;
			enemy.ScoreValue = 100;
		}
		else if ( roll < 0.8f )
		{
			enemy = go.Components.Create<FastEnemy>();
			enemy.BaseHealth = 15f;
			enemy.Speed = 180f;
			enemy.ScoreValue = 150;
		}
		else
		{
			enemy = go.Components.Create<TankEnemy>();
			enemy.BaseHealth = 120f;
			enemy.Speed = 40f;
			enemy.ScoreValue = 300;
		}

		return enemy;
	}

	private BossEnemy CreateBossEnemy( GameObject go )
	{
		var boss = go.Components.Create<BossEnemy>();
		boss.BaseHealth = 500f * MathF.Pow( 1.1f, CurrentWave );
		boss.Speed = 30f;
		boss.ScoreValue = 1000 + CurrentWave * 200;

		// Scale with player count
		int playerCount = Math.Max( 1, Scene.GetAllComponents<ArrowPlayer>().Count() );
		boss.BaseHealth *= 0.7f + 0.3f * playerCount;

		return boss;
	}

	private Model GetEnemyModel( Enemy enemy )
	{
		if ( enemy is FastEnemy && FastModel is not null ) return FastModel;
		if ( enemy is TankEnemy && TankModel is not null ) return TankModel;
		if ( enemy is BossEnemy && BossModel is not null ) return BossModel;
		if ( enemy is BasicEnemy && BasicModel is not null ) return BasicModel;
		return DefaultModel; // null = fallback to Model.Cube in SpawnEnemy
	}

	private Material GetEnemyMaterial( Enemy enemy )
	{
		if ( enemy is FastEnemy && FastMaterial is not null ) return FastMaterial;
		if ( enemy is TankEnemy && TankMaterial is not null ) return TankMaterial;
		if ( enemy is BossEnemy && BossMaterial is not null ) return BossMaterial;
		if ( enemy is BasicEnemy && BasicMaterial is not null ) return BasicMaterial;
		return null;
	}

	private Vector3 GetEnemyScale( Enemy enemy )
	{
		if ( enemy is TankEnemy ) return new Vector3( 2f, 2f, 2f );
		if ( enemy is FastEnemy ) return new Vector3( 0.7f, 0.7f, 0.7f );
		if ( enemy is BossEnemy ) return new Vector3( 3f, 3f, 3f );
		return Vector3.One; // BasicEnemy
	}

	private void OnEnemyKilled( Enemy enemy, Guid killerId )
	{
		EnemiesRemaining--;

		// Grant score
		if ( _gm.IsValid() )
		{
			_gm.AddScore( enemy.ScoreValue );
		}

		Log.Info( $"Enemy killed by {killerId}. {EnemiesRemaining} remaining." );
	}

	private void WaveComplete()
	{
		WaveActive = false;
		Log.Info( $"=== Wave {CurrentWave} complete! ===" );
		OnWaveCompleted( CurrentWave );

		if ( _gm.IsValid() )
		{
			_gm.OnWaveCompleted( CurrentWave );
		}
	}

	#region RPCs

	[Rpc.Broadcast]
	public void OnWaveStarted( int wave, int enemyCount )
	{
		Log.Info( $"Wave {wave} incoming — {enemyCount} enemies!" );
	}

	[Rpc.Broadcast]
	public void OnWaveCompleted( int wave )
	{
		Log.Info( $"Wave {wave} cleared!" );
	}

	#endregion
}
