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
	public float SpawnY { get; set; } = -500f;

	[Property, Category( "Waves" )]
	public float SpawnXRange { get; set; } = 200f; // random X offset within lane (-200 to 200)

	[Property, Category( "Waves" )]
	public int SubBossInterval { get; set; } = 5;

	[Property, Category( "Gates" )]
	public float GateSpawnInterval { get; set; } = 8f;

	[Property, Category( "Spawning" )]
	public float SpawnDistance { get; set; } = 800f;

	[Property, Category( "Spawning" )]
	public float CleanupDistance { get; set; } = 300f;


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
	private TimeSince _timeSinceLastGateSpawn = 0;
	private GameManager _gm;

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
				break;

			case GameManager.GameState.GameOver:
				WaveActive = false;
				break;
		}
	}

	private float GetPlayerY()
	{
		foreach ( var p in Scene.GetAllComponents<ArrowPlayer>() )
			return p.WorldPosition.y;
		return 0;
	}

	private void HandlePlayingState()
	{
		CleanupBehindPlayer();

		if ( !WaveActive )
		{
			// Start a new wave
			StartNextWave();
		}
		else
		{
			// Spawn upgrade gates periodically during the wave
					if ( _timeSinceLastGateSpawn >= GateSpawnInterval )
					{
						_timeSinceLastGateSpawn = 0;
						SpawnUpgradeGate();
					}
		
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
		var spawnY = GetPlayerY() - SpawnDistance;
		var spawnPos = new Vector3( Random.Shared.Float( -SpawnXRange, SpawnXRange ), spawnY, 0 );

		var enemyGo = new GameObject( true, $"Enemy_{CurrentWave}_{_enemiesSpawnedThisWave}" );
		enemyGo.WorldPosition = spawnPos;
		enemyGo.WorldRotation = Rotation.FromYaw( 90f );

		// Calculate dynamic HP based on player DPS (factoring in range)
		float totalPlayerDPS = 0f;
		int playerCount = 0;
		foreach ( var p in Scene.GetAllComponents<ArrowPlayer>() )
		{
			totalPlayerDPS += p.GetCurrentDPS( SpawnDistance );
			playerCount++;
		}
		float avgDPS = playerCount > 0 ? totalPlayerDPS / playerCount : 30f;

		// Target TTK = 1.5s, hit buffer = 1.3x (can miss ~23% of shots)
		float baseDynamicHP = avgDPS * 1.5f * 1.3f;

		Enemy enemy;

		// Determine enemy type based on wave difficulty
		if ( CurrentWave % SubBossInterval == 0 && CurrentWave > 0 && _enemiesSpawnedThisWave == _targetEnemyCount - 1 )
		{
			enemy = CreateBossEnemy( enemyGo );
			enemy.BaseHealth = baseDynamicHP * 5f; // bosses take 5x longer
		}
		else if ( CurrentWave >= FinalBossWave && _enemiesSpawnedThisWave == _targetEnemyCount - 1 )
		{
			enemy = CreateBossEnemy( enemyGo );
			enemy.BaseHealth = baseDynamicHP * 8f;
			enemy.ScoreValue *= 5;
		}
		else
		{
			enemy = CreateRandomEnemy( enemyGo );
			// Type-specific HP multiplier: Fast=0.6x, Tank=3x, Basic=1x
			float typeMult = enemy is FastEnemy ? 0.6f : enemy is TankEnemy ? 3f : 1f;
			enemy.BaseHealth = baseDynamicHP * typeMult;
		}

		// Speed scales slightly with wave
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

		// World-space HP label at enemy position
		var hpGo = new GameObject( true, "HP_Label" );
		hpGo.Parent = enemyGo;
		hpGo.LocalPosition = Vector3.Zero;
		hpGo.LocalScale = new Vector3( 5, 5, 5 );
		var hpWorldPanel = hpGo.Components.Create<WorldPanel>();
		hpWorldPanel.PanelSize = new Vector2( 800, 200 );
		hpWorldPanel.LookAtCamera = true;
		hpWorldPanel.RenderOptions.AfterUI = true;
		var hpLabel = hpGo.Components.Create<Sandbox.UI.EnemyHpPanel>();
		hpLabel.Enemy = enemy;

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

	private void SpawnUpgradeGate()
	{
		// 3 gates spanning the lane
		float[] gatePositions = { 133f, 0f, -133f };
		// Gate types — HealthBoost appears twice for increased spawn chance
		UpgradeType[] gateTypes = {
			UpgradeType.ArrowDamage,
			UpgradeType.ArrowFrequency,
			UpgradeType.SplitCount,
			UpgradeType.SwordFrequency,
			UpgradeType.SwordCount,
			UpgradeType.ArrowDistance,
			UpgradeType.HealthBoost, // extra chance
			UpgradeType.HealthBoost, // extra chance
		};

		for ( int i = 0; i < gatePositions.Length; i++ )
		{
			var type = gateTypes[Random.Shared.Int( 0, gateTypes.Length - 1 )];
			var amount = GetRandomAmount( type );
			var displayName = GetGateDisplayName( type );
			var amountText = GetAmountText( type, amount );

			var gateGo = new GameObject( true, $"Gate_{type}_{i}" );
			var spawnY = GetPlayerY() - SpawnDistance;
			gateGo.WorldPosition = new Vector3( gatePositions[i], spawnY, 0 );
			gateGo.WorldRotation = Rotation.From( 0, 90, 0 );
			gateGo.LocalScale = new Vector3( 1.5f, 1.5f, 1.5f );

			var gate = gateGo.Components.Create<UpgradeGate>();
			gate.UpgradeType = type;
			gate.Amount = amount;
			gate.DisplayName = displayName;
			gate.AmountText = amountText;

			// Use GameManager's gate model if set
			var gateModel = _gm?.GateModel ?? Model.Plane;
			var model = gateGo.Components.Create<ModelRenderer>();
			model.Model = gateModel;
			if ( _gm?.GateModel is null )
				model.Tint = GetGateColor( type ); // only tint if using fallback plane

			var col = gateGo.Components.Create<BoxCollider>();
			col.Center = new Vector3( 1.795f, 31.593f, 35.45f );
			col.Scale = new Vector3( 17.755f, 63.07f, 65.20f );

			// Physics so players can push through
			var body = gateGo.Components.Create<Rigidbody>();
			body.MotionEnabled = true;
			body.Gravity = true;
			body.MassOverride = 50f;

			// Label — same sizing as enemy HP panel
			var labelGo = new GameObject( true, "GateLabel" );
			labelGo.Parent = gateGo;
			labelGo.LocalPosition = new Vector3( 0, 30, 0 );
			labelGo.LocalScale = new Vector3( 5f, 5f, 5f );
			var wp = labelGo.Components.Create<WorldPanel>();
			wp.PanelSize = new Vector2( 1200, 300 );
			wp.LookAtCamera = true;
			wp.RenderOptions.AfterUI = true;
			var label = labelGo.Components.Create<Sandbox.UI.GateLabel>();
			label.Text = $"{displayName}\n{amountText}";

			gateGo.NetworkSpawn( null );
		}
	}

	/// <summary>
	/// Spawn an upgrade pickup at a position (from enemy drop).
	/// </summary>
	private void SpawnUpgradePickup( Vector3 position )
	{
		// Drops can be any single upgrade type
		UpgradeType[] gateTypes = {
			UpgradeType.ArrowFrequency, UpgradeType.ArrowDamage,
			UpgradeType.ArrowSpeed, UpgradeType.ArrowDistance,
			UpgradeType.SwordCount, UpgradeType.SwordDamage,
			UpgradeType.SwordFrequency, UpgradeType.SwordRange,
			UpgradeType.SplitCount, UpgradeType.HealthBoost
		};
		var type = gateTypes[Random.Shared.Int( 0, gateTypes.Length - 1 )];
		var amount = GetRandomAmount( type );

		var go = new GameObject( true, $"Drop_{type}" );
		go.WorldPosition = position;
		go.WorldRotation = Rotation.From( 0, 0, -90 );
		go.LocalScale = Vector3.One * 0.5f;

		var gate = go.Components.Create<UpgradeGate>();
		gate.UpgradeType = type;
		gate.Amount = amount;
		gate.DisplayName = GetGateDisplayName( type );
		gate.AmountText = GetAmountText( type, amount );

		// Use GameManager's gate model if set
		var model = go.Components.Create<ModelRenderer>();
		model.Model = _gm?.GateModel ?? Model.Plane;
		if ( _gm?.GateModel is null )
			model.Tint = GetGateColor( type );

		var col = go.Components.Create<BoxCollider>();
		col.Center = Vector3.Zero;
		col.Scale = new Vector3( 20f, 20f, 40f );

		// Physics so players can push through
		var body = go.Components.Create<Rigidbody>();
		body.MotionEnabled = true;
		body.Gravity = true;
		body.MassOverride = 30f;

		var labelGo = new GameObject( true, "DropLabel" );
		labelGo.Parent = go;
		labelGo.LocalPosition = new Vector3( 0, 30, 0 );
		labelGo.LocalScale = new Vector3( 5f, 5f, 5f );
		var wp = labelGo.Components.Create<WorldPanel>();
		wp.PanelSize = new Vector2( 1200, 300 );
		wp.LookAtCamera = true;
		wp.RenderOptions.AfterUI = true;
		var label = labelGo.Components.Create<Sandbox.UI.GateLabel>();
		label.Text = $"{GetGateDisplayName( type )}\n{GetAmountText( type, amount )}";

		go.NetworkSpawn( null );
	}

	private float GetRandomAmount( UpgradeType type )
	{
		return type switch
		{
			UpgradeType.ArrowFrequency => Random.Shared.Float( 0.1f, 0.2f ),
			UpgradeType.ArrowDamage => Random.Shared.Int( 2, 5 ),
			UpgradeType.ArrowSpeed => Random.Shared.Int( 30, 80 ),
			UpgradeType.ArrowDistance => Random.Shared.Int( 50, 150 ),
			UpgradeType.SwordCount => 1,
			UpgradeType.SwordDamage => Random.Shared.Int( 3, 8 ),
			UpgradeType.SplitCount => 1,
			UpgradeType.SwordFrequency => 1,
			UpgradeType.SwordRange => 1,
			UpgradeType.HealthBoost => Random.Shared.Int( 10, 20 ),
			_ => 1,
		};
	}

	private string GetGateDisplayName( UpgradeType type ) => type switch
	{
		UpgradeType.ArrowFrequency => "FIRE RATE",
		UpgradeType.ArrowDamage => "DAMAGE",
		UpgradeType.ArrowDistance => "RANGE",
		UpgradeType.SwordCount => "BLADE",
		UpgradeType.SwordFrequency => "CD DOWN",
		UpgradeType.SplitCount => "PEN",
		UpgradeType.ArrowSpeed => "PROJ SPD",
		UpgradeType.SwordDamage => "SCISSOR DMG",
		UpgradeType.SwordRange => "SCISSOR RNG",
		UpgradeType.HealthBoost => "HEALTH",
		_ => type.ToString(),
	};

	private string GetAmountText( UpgradeType type, float amount )
	{
		return type switch
		{
			UpgradeType.ArrowFrequency => $"+{amount:F1}/s",
			UpgradeType.ArrowDamage => $"+{(int)amount}",
			UpgradeType.ArrowSpeed => $"+{(int)amount}",
			UpgradeType.ArrowDistance => $"+{(int)amount}",
			UpgradeType.SwordCount => $"+{(int)amount}",
			UpgradeType.SwordDamage => $"+{(int)amount}",
			UpgradeType.SwordFrequency => $"+{(int)amount}",
			UpgradeType.SwordRange => $"+{(int)amount}",
			UpgradeType.SplitCount => $"+{(int)amount}",
			UpgradeType.HealthBoost => $"+{(int)amount} HP",
			_ => $"+{(int)amount}",
		};
	}

	private Color GetGateColor( UpgradeType type ) => type switch
	{
		UpgradeType.ArrowFrequency => new Color( 0.3f, 0.7f, 1f, 0.4f ),   // blue
		UpgradeType.ArrowDamage => new Color( 1f, 0.3f, 0.3f, 0.4f ),      // red
		UpgradeType.ArrowSpeed => new Color( 0.3f, 1f, 0.3f, 0.4f ),       // green
		UpgradeType.ArrowDistance => new Color( 0.7f, 0.3f, 1f, 0.4f ),    // purple
		UpgradeType.SwordCount => new Color( 1f, 0.7f, 0.3f, 0.4f ),       // orange
		UpgradeType.SwordDamage => new Color( 1f, 0.4f, 0.4f, 0.4f ),      // red-pink
		UpgradeType.SplitCount => new Color( 0.3f, 1f, 0.3f, 0.4f ),       // green
		UpgradeType.SwordFrequency => new Color( 0.3f, 0.7f, 1f, 0.4f ),   // blue
		UpgradeType.SwordRange => new Color( 0.7f, 0.3f, 1f, 0.4f ),       // purple
		UpgradeType.HealthBoost => new Color( 0.3f, 1f, 0.3f, 0.4f ),      // green
		_ => new Color( 0.5f, 0.5f, 0.5f, 0.4f ),
	};

	private void OnEnemyKilled( Enemy enemy, Guid killerId )
	{
		EnemiesRemaining--;

		// Grant score
		if ( _gm.IsValid() )
		{
			_gm.AddScore( enemy.ScoreValue );
		}

		// 30% chance to drop an upgrade pickup
		if ( Random.Shared.Float( 0, 1 ) < 0.3f && enemy.IsValid() )
		{
			SpawnUpgradePickup( enemy.WorldPosition );
		}

		Log.Info( $"Enemy killed by {killerId}. {EnemiesRemaining} remaining." );
	}

	private void CleanupBehindPlayer()
	{
		if ( !Networking.IsHost ) return;
		var playerY = GetPlayerY();
		var cleanupY = playerY + CleanupDistance;

		foreach ( var enemy in Scene.GetAllComponents<Enemy>() )
		{
			if ( enemy.IsValid() && enemy.WorldPosition.y > cleanupY )
				enemy.GameObject.Destroy();
		}

		foreach ( var gate in Scene.GetAllComponents<UpgradeGate>() )
		{
			if ( gate.IsValid() && gate.WorldPosition.y > cleanupY )
				gate.GameObject.Destroy();
		}
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
