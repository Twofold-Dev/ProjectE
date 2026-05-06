using System.Text.Json;

namespace Progression;

/// <summary>
/// Persistent cumulative stat tracking across all runs.
/// </summary>
public sealed class StatsData
{
	public int TotalEnemiesKilled { get; set; }
	public int TotalDamageDealt { get; set; }
	public int HighestWaveReached { get; set; }
	public int TotalRunsPlayed { get; set; }
	public float TotalTimePlayedSeconds { get; set; }
	public int TotalPaperclipsEarned { get; set; }

	private static readonly string FilePath = "progression/stats.json";

	public void Save()
	{
		FileSystem.Data.WriteAllText( FilePath, JsonSerializer.Serialize( this, ProgressionManager.JsonOptions ) );
	}

	public static StatsData Load()
	{
		if ( FileSystem.Data.FileExists( FilePath ) )
		{
			var json = FileSystem.Data.ReadAllText( FilePath );
			return JsonSerializer.Deserialize<StatsData>( json, ProgressionManager.JsonOptions ) ?? new StatsData();
		}
		return new StatsData();
	}
}
