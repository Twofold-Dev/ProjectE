/// <summary>
/// Follows the player along the Y axis.
/// Position: camera keeps editor-set X/Z, only Y follows player.
/// Rotation: uses these properties if set, otherwise keeps editor rotation.
/// </summary>
public sealed class CameraFollower : Component
{
	[Property, Range( 0, 1 )] public float Smoothness { get; set; } = 0.95f;

	[Property, Category( "Position" )] public float FollowDistance { get; set; } = 0f;
	[Property, Category( "Position" )] public float FollowHeight { get; set; } = 0f;

	[Property, Category( "Rotation" )] public bool OverrideRotation { get; set; } = false;
	[Property, Category( "Rotation" )] public float Pitch { get; set; } = 30f;
	[Property, Category( "Rotation" )] public float Yaw { get; set; } = 0f;
	[Property, Category( "Rotation" )] public float Roll { get; set; } = 0f;

	protected override void OnUpdate()
	{
		// Don't follow during lobby/menu state
		var gm = Scene.GetAllComponents<GameManager>().FirstOrDefault();
		if ( gm == null || gm.State != GameManager.GameState.Playing )
			return;

		var player = GetPlayer();
		if ( player == null ) return;

		// Follow Y position
		var targetY = player.WorldPosition.y + FollowDistance;
		var smooth = 1f - MathF.Pow( Smoothness, Time.Delta * 60f );
		var targetZ = player.WorldPosition.z + FollowHeight;
		WorldPosition = WorldPosition.WithY( WorldPosition.y.LerpTo( targetY, smooth ) )
									 .WithZ( WorldPosition.z.LerpTo( targetZ, smooth ) );

		// Apply rotation if override is on
		if ( OverrideRotation )
		{
			WorldRotation = Rotation.From( Pitch, Yaw, Roll );
		}
	}

	private ArrowPlayer GetPlayer()
	{
		foreach ( var p in Scene.GetAllComponents<ArrowPlayer>() )
			return p;
		return null;
	}
}
