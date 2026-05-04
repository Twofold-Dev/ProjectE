/// <summary>
/// Projectile fired by Desk Buddy companions.
/// Homing projectile that flies toward the target enemy and damages on contact.
/// </summary>
public sealed class BuddyProjectile : Component
{
	[Property] public float Speed { get; set; } = 300f;
	[Property] public float Damage { get; set; } = 5f;
	[Property] public Guid OwnerId { get; set; }
	[Property] public GameObject Target { get; set; }

	protected override void OnFixedUpdate()
	{
		if ( !Networking.IsHost ) return;

		if ( !Target.IsValid() )
		{
			GameObject.Destroy();
			return;
		}

		// Check if target enemy is still alive
		if ( !Target.Components.TryGet<Enemy>( out var enemy ) || !enemy.IsAlive )
		{
			GameObject.Destroy();
			return;
		}

		// Fly toward target
		var dir = ( Target.WorldPosition - WorldPosition ).Normal;
		WorldPosition += dir * Speed * Time.Delta;
		WorldRotation = Rotation.LookAt( dir );

		// Hit check
		if ( WorldPosition.Distance( Target.WorldPosition ) < 30f )
		{
			enemy.TakeDamage( Damage, OwnerId );
			GameObject.Destroy();
		}
	}
}
