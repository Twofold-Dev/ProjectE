public enum Playstyle
{
	RapidFire,  // High fire rate, low damage
	SplitShot,  // Medium rate, reduced damage, splits into multiple
	PowerShot,  // Low fire rate, high damage
}

/// <summary>
/// Types of upgrades available.
/// </summary>
public enum UpgradeType
{
	ArrowFrequency,  // Coffee Rush — faster shooting
	ArrowDamage,     // Stapler Power — more damage per shot
	ArrowSpeed,      // Aerodynamics — faster projectile
	ArrowDistance,   // Paper Stream — longer range
	SwordCount,      // Extra Shredders — more orbiting shredders
	SwordDamage,     // Sharp Blades — stronger shredders
	PetCount,        // Desk Buddies — more companions
	PetFireRate,     // Busy Buddies — faster companion attacks
	MovementSpeed,   // Office Chair — faster movement
	HealthBoost,     // Sick Days — bonus health
}

/// <summary>
/// Per-player upgrade levels, synced via [Sync].
/// Each property tracks the current level (0 = not acquired yet).
/// </summary>
public sealed class UpgradeState
{
	public Playstyle ChosenPlaystyle { get; set; } = Playstyle.RapidFire;
	public bool PlaystyleLocked { get; set; } = false;

	public int ArrowFrequency { get; set; } = 0;
	public int ArrowDamage { get; set; } = 0;
	public int ArrowSpeed { get; set; } = 0;
	public int ArrowDistance { get; set; } = 0;
	public int SwordCount { get; set; } = 0;
	public int SwordDamage { get; set; } = 0;
	public int PetCount { get; set; } = 0;
	public int PetFireRate { get; set; } = 0;
	public int MovementSpeed { get; set; } = 0;
	public int HealthBoost { get; set; } = 0;

	/// <summary>
	/// Apply a single upgrade level increase.
	/// </summary>
	public void ApplyUpgrade( UpgradeType type )
	{
		switch ( type )
		{
			case UpgradeType.ArrowFrequency: ArrowFrequency++; break;
			case UpgradeType.ArrowDamage: ArrowDamage++; break;
			case UpgradeType.ArrowSpeed: ArrowSpeed++; break;
			case UpgradeType.ArrowDistance: ArrowDistance++; break;
			case UpgradeType.SwordCount: SwordCount++; break;
			case UpgradeType.SwordDamage: SwordDamage++; break;
			case UpgradeType.PetCount: PetCount++; break;
			case UpgradeType.PetFireRate: PetFireRate++; break;
			case UpgradeType.MovementSpeed: MovementSpeed++; break;
			case UpgradeType.HealthBoost: HealthBoost++; break;
		}
	}

	/// <summary>
	/// Get the current level for a given upgrade type.
	/// </summary>
	public int GetLevel( UpgradeType type ) => type switch
	{
		UpgradeType.ArrowFrequency => ArrowFrequency,
		UpgradeType.ArrowDamage => ArrowDamage,
		UpgradeType.ArrowSpeed => ArrowSpeed,
		UpgradeType.ArrowDistance => ArrowDistance,
		UpgradeType.SwordCount => SwordCount,
		UpgradeType.SwordDamage => SwordDamage,
		UpgradeType.PetCount => PetCount,
		UpgradeType.PetFireRate => PetFireRate,
		UpgradeType.MovementSpeed => MovementSpeed,
		UpgradeType.HealthBoost => HealthBoost,
		_ => 0,
	};
}
