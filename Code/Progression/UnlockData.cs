using System.Text.Json;

namespace Progression;

/// <summary>
/// Tracks which playstyles the player has permanently unlocked.
/// </summary>
public sealed class UnlockData
{
	public bool RapidFireUnlocked { get; set; } = true; // starter
	public bool SplitShotUnlocked { get; set; }
	public bool PowerShotUnlocked { get; set; }

	public const int SplitShotCost = 500;   // Paperclips
	public const int PowerShotCost = 1000;  // Paperclips

	private static readonly string FilePath = "progression/unlocks.json";

	public void Save()
	{
		FileSystem.Data.WriteAllText( FilePath, JsonSerializer.Serialize( this, ProgressionManager.JsonOptions ) );
	}

	public static UnlockData Load()
	{
		if ( FileSystem.Data.FileExists( FilePath ) )
		{
			var json = FileSystem.Data.ReadAllText( FilePath );
			return JsonSerializer.Deserialize<UnlockData>( json, ProgressionManager.JsonOptions ) ?? new UnlockData();
		}
		return new UnlockData();
	}
}
