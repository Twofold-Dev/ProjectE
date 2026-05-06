/// <summary>
/// Animates the lobby dog: gently floats up/down, patrols between X=100 and X=-90,
/// and rotates Y between -90 and -75 in sync with movement.
/// Attach this to the coffee dog buddy GameObject in the lobby scene.
/// </summary>
public sealed class LobbyDogAnimator : Component
{
	[Property] public float PatrolSpeed { get; set; } = 0.3f;
	[Property] public float BobSpeed { get; set; } = 1.5f;
	[Property] public float BobHeight { get; set; } = 3f;
	[Property] public float StartX { get; set; } = 100f;
	[Property] public float EndX { get; set; } = -90f;
	[Property] public float StartRotY { get; set; } = -90f;
	[Property] public float EndRotY { get; set; } = -75f;

	private Vector3 _basePosition;
	private bool _initialized;

	protected override void OnStart()
	{
		_basePosition = WorldPosition;
		_initialized = true;

		// Ensure we have a ModelRenderer for the dog
		if ( Components.Get<ModelRenderer>() is null )
		{
			var renderer = Components.Create<ModelRenderer>();
			renderer.Model = Model.Cube;
		}
	}

	protected override void OnUpdate()
	{
		if ( !_initialized ) return;

		// Patrol: sin wave drives t from 0→1→0 cyclically
		float t = (MathF.Sin( Time.Now * PatrolSpeed ) + 1f) * 0.5f; // 0..1 loop

		// Lerp X between StartX and EndX
		float patrolX = MathX.Lerp( StartX, EndX, t );

		// Gentle vertical float
		float zBob = MathF.Sin( Time.Now * BobSpeed ) * BobHeight;

		// Set position
		WorldPosition = new Vector3( patrolX, _basePosition.y, _basePosition.z + zBob );

		// Lerp rotation Y between StartRotY and EndRotY
		// When at StartX (t=0) → StartRotY, at EndX (t=1) → EndRotY
		float rotY = MathX.Lerp( StartRotY, EndRotY, t );
		WorldRotation = Rotation.From( 0, rotY, 0 );
	}
}
