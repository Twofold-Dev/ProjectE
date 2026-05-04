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
	ArrowFrequency,  // FIRE RATE gate
	ArrowDamage,     // DAMAGE gate
	ArrowSpeed,
	ArrowDistance,   // RANGE gate
	SwordCount,      // BLADE+X gate
	SwordDamage,
	SwordFrequency,  // CD DOWN gate
	SwordRange,
	SplitCount,      // PEN+X gate
	PenBounce,       // card only
	PenPierce,       // card only
	BladeBounce,     // card only
	PetCount,
	PetFireRate,
	HealthBoost,
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
	public int SwordCount { get; set; } = 1;
	public int SwordDamage { get; set; } = 0;
	public int SwordFrequency { get; set; } = 0;
	public int SwordRange { get; set; } = 0;
	public int SplitCount { get; set; } = 0;
	public int PenBounce { get; set; } = 0;
	public int PenPierce { get; set; } = 0;
	public int BladeBounce { get; set; } = 0;
	public int PetCount { get; set; } = 0;
	public int PetFireRate { get; set; } = 0;
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
			case UpgradeType.SwordFrequency: SwordFrequency++; break;
			case UpgradeType.SwordRange: SwordRange++; break;
			case UpgradeType.SplitCount: SplitCount++; break;
			case UpgradeType.PenBounce: PenBounce++; break;
			case UpgradeType.PenPierce: PenPierce++; break;
			case UpgradeType.BladeBounce: BladeBounce++; break;
			case UpgradeType.PetCount: PetCount++; break;
			case UpgradeType.PetFireRate: PetFireRate++; break;
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
		UpgradeType.SwordFrequency => SwordFrequency,
		UpgradeType.SwordRange => SwordRange,
		UpgradeType.SplitCount => SplitCount,
		UpgradeType.PenBounce => PenBounce,
		UpgradeType.PenPierce => PenPierce,
		UpgradeType.BladeBounce => BladeBounce,
		UpgradeType.PetCount => PetCount,
		UpgradeType.PetFireRate => PetFireRate,
		UpgradeType.HealthBoost => HealthBoost,
		_ => 0,
	};
}
