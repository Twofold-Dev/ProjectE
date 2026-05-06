using System.Text.Json;

namespace Progression;

/// <summary>
/// Permanent upgrade levels purchased with Paperclips.
/// Each upgrade has a max level; levels persist across all runs.
/// </summary>
public sealed class ShopData
{
	// Upgrade levels (0 = not purchased)
	public int SharperPens { get; set; }
	public int FasterFiring { get; set; }
	public int ArmorPlating { get; set; }
	public int OfficeCoffee { get; set; }
	public int BulkOrder { get; set; }

	// Max levels
	public const int MaxSharperPens = 5;
	public const int MaxFasterFiring = 5;
	public const int MaxArmorPlating = 5;
	public const int MaxOfficeCoffee = 3;
	public const int MaxBulkOrder = 3;

	// Cost per level (index = level)
	public static readonly int[] CostSharperPens = { 0, 50, 100, 200, 400, 800 };
	public static readonly int[] CostFasterFiring = { 0, 50, 100, 200, 400, 800 };
	public static readonly int[] CostArmorPlating = { 0, 75, 150, 300, 600, 1200 };
	public static readonly int[] CostOfficeCoffee = { 0, 100, 300, 600 };
	public static readonly int[] CostBulkOrder = { 0, 200, 500, 1000 };

	public int GetNextCost( string upgrade )
	{
		var level = upgrade switch
		{
			nameof(SharperPens) => SharperPens,
			nameof(FasterFiring) => FasterFiring,
			nameof(ArmorPlating) => ArmorPlating,
			nameof(OfficeCoffee) => OfficeCoffee,
			nameof(BulkOrder) => BulkOrder,
			_ => 0
		};

		var costs = upgrade switch
		{
			nameof(SharperPens) => CostSharperPens,
			nameof(FasterFiring) => CostFasterFiring,
			nameof(ArmorPlating) => CostArmorPlating,
			nameof(OfficeCoffee) => CostOfficeCoffee,
			nameof(BulkOrder) => CostBulkOrder,
			_ => Array.Empty<int>()
		};

		if ( level < 0 || level >= costs.Length - 1 ) return -1;
		return costs[level + 1];
	}

	public int GetMaxLevel( string upgrade ) => upgrade switch
	{
		nameof(SharperPens) => MaxSharperPens,
		nameof(FasterFiring) => MaxFasterFiring,
		nameof(ArmorPlating) => MaxArmorPlating,
		nameof(OfficeCoffee) => MaxOfficeCoffee,
		nameof(BulkOrder) => MaxBulkOrder,
		_ => 0
	};

	public int GetLevel( string upgrade ) => upgrade switch
	{
		nameof(SharperPens) => SharperPens,
		nameof(FasterFiring) => FasterFiring,
		nameof(ArmorPlating) => ArmorPlating,
		nameof(OfficeCoffee) => OfficeCoffee,
		nameof(BulkOrder) => BulkOrder,
		_ => 0
	};

	public void SetLevel( string upgrade, int level )
	{
		switch ( upgrade )
		{
			case nameof(SharperPens): SharperPens = level; break;
			case nameof(FasterFiring): FasterFiring = level; break;
			case nameof(ArmorPlating): ArmorPlating = level; break;
			case nameof(OfficeCoffee): OfficeCoffee = level; break;
			case nameof(BulkOrder): BulkOrder = level; break;
		}
	}

	private static readonly string FilePath = "progression/upgrades.json";

	public void Save()
	{
		FileSystem.Data.WriteAllText( FilePath, JsonSerializer.Serialize( this, ProgressionManager.JsonOptions ) );
	}

	public static ShopData Load()
	{
		if ( FileSystem.Data.FileExists( FilePath ) )
		{
			var json = FileSystem.Data.ReadAllText( FilePath );
			return JsonSerializer.Deserialize<ShopData>( json, ProgressionManager.JsonOptions ) ?? new ShopData();
		}
		return new ShopData();
	}
}
