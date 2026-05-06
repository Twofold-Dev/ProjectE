/// <summary>
/// A floating coffee dog buddy that follows the player, bobs up and down,
/// and shoots projectiles at the nearest enemy.
/// </summary>
public sealed class DeskBuddy : Component
{
	#region Configuration

	[Property] public float FollowOffsetY { get; set; } = 30f;
	[Property] public float FollowOffsetZ { get; set; } = 50f;
	[Property] public float BobSpeed { get; set; } = 2f;
	[Property] public float BobHeight { get; set; } = 5f;
	[Property] public float FireRate { get; set; } = 1f;
	[Property] public float ProjectileSpeed { get; set; } = 400f;
	[Property] public float ProjectileDamage { get; set; } = 5f;
	[Property] public float ProjectileRange { get; set; } = 600f;
	[Property] public Guid OwnerId { get; set; }
	[Property] public int BuddyIndex { get; set; } = 0;
	[Property] public int BuddyCount { get; set; } = 1;
	[Property] public Model DogModel { get; set; }
	[Property] public Model ProjectileModel { get; set; }
	[Property] public SoundEvent FireSound { get; set; }
	[Property] public float FireVolume { get; set; } = 1f;

	#endregion

	#region State

	private ArrowPlayer _owner;
	private TimeSince _timeSinceFire = 0;

	#endregion

	protected override void OnStart()
	{
		// Find owner
		foreach ( var p in Scene.GetAllComponents<ArrowPlayer>() )
		{
			if ( p.Network.OwnerId == OwnerId )
			{
				_owner = p;
				break;
			}
		}

		// Visual
		var model = Components.Create<ModelRenderer>();
		model.Model = DogModel ?? Model.Cube;
		GameObject.LocalScale = Vector3.One;
		model.Tint = new Color( 0.8f, 0.6f, 0.4f ); // warm brown tint for coffee dog
	}

	protected override void OnFixedUpdate()
	{
		if ( !Networking.IsHost ) return;
		if ( _owner == null || !_owner.IsValid() )
		{
			GameObject.Destroy();
			return;
		}

		// Calculate horizontal spacing based on count and index
		float xOffset = 0f;
		if ( BuddyCount > 1 )
		{
			float spacing = 60f;
			xOffset = -(BuddyCount - 1) * spacing * 0.5f + BuddyIndex * spacing;
		}

		// Bob up and down
		float zBob = MathF.Sin( Time.Now * BobSpeed + BuddyIndex * 1.5f ) * BobHeight;

		// Follow player position
		var targetPos = _owner.WorldPosition;
		targetPos.x += xOffset;
		targetPos.y += FollowOffsetY;
		targetPos.z += FollowOffsetZ + zBob;
		WorldPosition = targetPos;

		// Find nearest enemy
		Enemy nearest = null;
		float nearestDist = 800f;
		foreach ( var e in Scene.GetAllComponents<Enemy>() )
		{
			if ( !e.IsAlive ) continue;
			var d = WorldPosition.Distance( e.WorldPosition );
			if ( d < nearestDist )
			{
				nearestDist = d;
				nearest = e;
			}
		}

		// Face nearest enemy
		if ( nearest != null )
		{
			var dir = ( nearest.WorldPosition - WorldPosition ).Normal;
			WorldRotation = Rotation.LookAt( dir ) * Rotation.From( 0, 90, 0 );

			// Auto-fire
			if ( _timeSinceFire >= 1f / FireRate )
			{
				Fire( nearest );
				_timeSinceFire = 0;
			}
		}
	}

	private void Fire( Enemy target )
	{
		if ( !target.IsValid() ) return;

		var projGo = new GameObject( true, $"BuddyProj_{OwnerId}" );
		projGo.WorldPosition = WorldPosition;
		projGo.WorldRotation = Rotation.LookAt( (target.WorldPosition - WorldPosition).Normal );

		var proj = projGo.Components.Create<BuddyProjectile>();
		proj.Speed = ProjectileSpeed;
		proj.Damage = ProjectileDamage;
		proj.OwnerId = OwnerId;
		proj.Target = target.GameObject;

		// Small visual for the projectile
		var model = projGo.Components.Create<ModelRenderer>();
		model.Model = ProjectileModel ?? Model.Cube;
		projGo.LocalScale = ProjectileModel is null ? new Vector3( 0.3f, 0.3f, 0.3f ) : Vector3.One;
		model.Tint = new Color( 0.6f, 0.4f, 0.2f ); // coffee colored

		// Play fire sound
		if ( FireSound is not null )
		{
			var handle = Sound.Play( FireSound, WorldPosition );
			handle.Volume = FireVolume;
		}

		projGo.NetworkSpawn( null );
	}
}
