using System.Text.Json;
using System.Text.Json.Serialization;

namespace Progression;

/// <summary>
/// Central manager for all progression data.
/// Handles loading/saving of currency, shop, unlocks, achievements, and stats.
/// Follows clover_meadows persistence pattern with FileSystem.Data + System.Text.Json.
/// </summary>
public sealed class ProgressionManager
{
	public static readonly JsonSerializerOptions JsonOptions = new()
	{
		WriteIndented = true,
		IncludeFields = true,
		Converters = { new JsonStringEnumConverter() },
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
	};

	public CurrencyData Currency { get; private set; }
	public ShopData Shop { get; private set; }
	public UnlockData Unlocks { get; private set; }
	public AchievementData Achievements { get; private set; }
	public StatsData Stats { get; private set; }

	public ProgressionManager()
	{
		LoadAll();
	}

	public void LoadAll()
	{
		FileSystem.Data.CreateDirectory( "progression" );
		Currency = CurrencyData.Load();
		Shop = ShopData.Load();
		Unlocks = UnlockData.Load();
		Achievements = AchievementData.Load();
		Stats = StatsData.Load();

		// Push any already-unlocked achievements to S&Box service (pizza_clicker pattern)
		// This ensures retroactive credit for achievements earned before service was wired up
		foreach ( var a in AchievementData.All )
		{
			if ( Achievements.IsUnlocked( a.Id ) )
			{
				Sandbox.Services.Achievements.Unlock( a.Id.ToLowerInvariant() );
			}
		}
	}

	public void SaveAll()
	{
		Currency.Save();
		Shop.Save();
		Unlocks.Save();
		Achievements.Save();
		Stats.Save();
		PushStatsToService();
	}

	/// <summary>Push key metrics to S&Box global stat tracking service (pizza_clicker pattern).</summary>
	public void PushStatsToService()
	{
		Sandbox.Services.Stats.SetValue( "highest_wave", Stats.HighestWaveReached );
		Sandbox.Services.Stats.SetValue( "total_enemies_killed", Stats.TotalEnemiesKilled );
		Sandbox.Services.Stats.SetValue( "total_damage_dealt", Stats.TotalDamageDealt );
		Sandbox.Services.Stats.SetValue( "total_runs", Stats.TotalRunsPlayed );
		Sandbox.Services.Stats.SetValue( "total_time_seconds", (long)Stats.TotalTimePlayedSeconds );
		Sandbox.Services.Stats.SetValue( "total_paperclips", Stats.TotalPaperclipsEarned );
	}

	/// <summary>Add Paperclips at end of run, update stats.</summary>
	public void AwardRun( int paperclips, int enemiesKilled, int damageDealt, int highestWave, float runTimeSeconds )
	{
		Currency.Paperclips += paperclips;
		Stats.TotalEnemiesKilled += enemiesKilled;
		Stats.TotalDamageDealt += damageDealt;
		Stats.TotalRunsPlayed++;
		Stats.TotalTimePlayedSeconds += runTimeSeconds;
		Stats.TotalPaperclipsEarned += paperclips;
		if ( highestWave > Stats.HighestWaveReached )
			Stats.HighestWaveReached = highestWave;

		CheckAchievements();
		PushStatsToService();
		SaveAll();
	}

	/// <summary>Check and award any newly-unlocked achievements.</summary>
	private void CheckAchievements()
	{
		TryUnlock( nameof(AchievementData.FirstSteps), () => Stats.HighestWaveReached >= 1 );
		TryUnlock( nameof(AchievementData.WaveRider), () => Stats.HighestWaveReached >= 10 );
		TryUnlock( nameof(AchievementData.WaveMaster), () => Stats.HighestWaveReached >= 25 );
		TryUnlock( nameof(AchievementData.PenPincher), () => Stats.TotalEnemiesKilled >= 100 );
		TryUnlock( nameof(AchievementData.OfficeMassacre), () => Stats.TotalEnemiesKilled >= 1000 );
		TryUnlock( nameof(AchievementData.Papercut), () => Stats.TotalDamageDealt >= 1000 );

		TryUnlock( nameof(AchievementData.Collector), () => Unlocks.SplitShotUnlocked && Unlocks.PowerShotUnlocked );
		TryUnlock( nameof(AchievementData.MaxedOut), () =>
			Shop.SharperPens >= ShopData.MaxSharperPens &&
			Shop.FasterFiring >= ShopData.MaxFasterFiring &&
			Shop.ArmorPlating >= ShopData.MaxArmorPlating &&
			Shop.OfficeCoffee >= ShopData.MaxOfficeCoffee &&
			Shop.BulkOrder >= ShopData.MaxBulkOrder );
	}

	private void TryUnlock( string id, Func<bool> condition )
	{
		if ( !Achievements.IsUnlocked( id ) && condition() )
		{
			Achievements.Unlock( id );

			// Find reward — Paperclips only (single currency)
			foreach ( var a in AchievementData.All )
			{
				if ( a.Id == id )
				{
					Currency.Paperclips += a.PaperclipReward;
					Log.Info( $"Achievement unlocked: {a.Name} (+{a.PaperclipReward} Paperclips)" );
					break;
				}
			}

			// Register with S&Box global achievement service (pizza_clicker pattern)
			Sandbox.Services.Achievements.Unlock( id.ToLowerInvariant() );
		}
	}

	/// <summary>Try to purchase an upgrade level. Returns true if purchased.</summary>
	public bool PurchaseUpgrade( string upgradeId )
	{
		var cost = Shop.GetNextCost( upgradeId );
		if ( cost < 0 || Currency.Paperclips < cost ) return false;

		Currency.Paperclips -= cost;
		Shop.SetLevel( upgradeId, Shop.GetLevel( upgradeId ) + 1 );
		CheckAchievements();
		SaveAll();
		return true;
	}

	/// <summary>Try to unlock a playstyle with Paperclips. Returns true if unlocked.</summary>
	public bool UnlockPlaystyle( string playstyleId, int cost )
	{
		if ( Currency.Paperclips < cost ) return false;

		switch ( playstyleId )
		{
			case nameof(UnlockData.SplitShotUnlocked) when !Unlocks.SplitShotUnlocked:
				Unlocks.SplitShotUnlocked = true;
				break;
			case nameof(UnlockData.PowerShotUnlocked) when !Unlocks.PowerShotUnlocked:
				Unlocks.PowerShotUnlocked = true;
				break;
			default:
				return false;
		}

		Currency.Paperclips -= cost;
		CheckAchievements();
		SaveAll();
		return true;
	}

	/// <summary>Delete all progression files and reload defaults.</summary>
	public void ResetAll()
	{
		var files = new[] {
			"progression/currency.json",
			"progression/upgrades.json",
			"progression/unlocks.json",
			"progression/achievements.json",
			"progression/stats.json"
		};
		foreach ( var f in files )
		{
			if ( FileSystem.Data.FileExists( f ) )
				FileSystem.Data.DeleteFile( f );
		}
		LoadAll();
		Log.Info( "Progression reset to defaults." );
	}
}
