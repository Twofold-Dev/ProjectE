using Sandbox;

/// <summary>
/// Day/Night cycle component.
/// Attach to your sun (DirectionalLight) GameObject.
/// Rotates the sun and adjusts light color over time.
/// </summary>
public sealed class DayNightCycle : Component
{
	[Property, Title( "Cycle Duration (seconds)" )]
	public float CycleDuration { get; set; } = 120f;

	[Property, Title( "Y-Angle Offset" ), Range( -180f, 180f, 1f )]
	public float YAngleOffset { get; set; } = -70f;

	[Property, Title( "Day Light Color" )]
	public Color DayColor { get; set; } = new Color( 1f, 0.95f, 0.8f );

	[Property, Title( "Night Light Color" )]
	public Color NightColor { get; set; } = new Color( 0.2f, 0.2f, 0.35f );

	[Property, Title( "Start Time (0-1)" ), Range( 0f, 1f, 0.01f )]
	public float StartTime { get; set; } = 0.25f;

	[Property, Title( "Paused" )]
	public bool Paused { get; set; } = false;

	private DirectionalLight _sun;
	private float _timeOfDay;
	private SpotLight[] _spotlights;

	protected override void OnStart()
	{
		_sun = GameObject.GetComponent<DirectionalLight>();
		if ( _sun == null )
		{
			Log.Warning( "DayNightCycle: No DirectionalLight found on this GameObject." );
		}

		// Cache all spotlights in the scene so we can toggle them during day
		_spotlights = Scene.GetAllComponents<SpotLight>().ToArray();
		if ( _spotlights.Length > 0 )
			Log.Info( $"DayNightCycle: Found {_spotlights.Length} spotlights to control" );

		_timeOfDay = StartTime * CycleDuration;
	}

	protected override void OnUpdate()
	{
		if ( Paused ) return;
		if ( _sun == null ) return;

		// Advance time
		_timeOfDay += Time.Delta;
		if ( _timeOfDay > CycleDuration )
			_timeOfDay -= CycleDuration;

		float t = _timeOfDay / CycleDuration; // 0..1

		// Sun angle: rotate around X axis with Y offset for angled shadows
		// t=0.25 = sunrise (horizon), t=0.5 = noon (overhead), t=0.75 = sunset (horizon)
		float angle = t * 360f - 90f;
		GameObject.WorldRotation = Rotation.From( angle, YAngleOffset, 0 );

		// Sun height: derive from angle so it's in sync with rotation
		// angle -90 (midnight)= -1, 0 (horizon)= 0, 90 (noon)= +1, 180 (horizon)= 0
		float sunHeight = MathF.Sin( angle * MathF.PI / 180f );
		float dayFactor = MathF.Max( 0, sunHeight ); // 0..1

		// Lerp light color between night and day
		_sun.LightColor = Color.Lerp( NightColor, DayColor, dayFactor );

		// Toggle shadows based on time
		_sun.Shadows = dayFactor > 0.1f;

		// Toggle spotlights: on during night, off during day
		// dayFactor=0 (night) → lights on, dayFactor>=0.3 (day) → lights off
		bool lightsOn = dayFactor < 0.3f;
		foreach ( var light in _spotlights )
		{
			if ( !light.IsValid() ) continue;
			light.Enabled = lightsOn;
		}
	}

	/// <summary>Get current time of day as a 0-1 value (0=midnight, 0.5=noon).</summary>
	public float GetTimeOfDay() => _timeOfDay / CycleDuration;
}
