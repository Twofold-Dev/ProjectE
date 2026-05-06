using System.Text.Json;

namespace Progression;

/// <summary>
/// Persistent currency data — Paperclips (earned from kills and achievements).
/// Saved via FileSystem.Data using System.Text.Json.
/// </summary>
public sealed class CurrencyData
{
	public int Paperclips { get; set; }

	private static readonly string FilePath = "progression/currency.json";

	public void Save()
	{
		FileSystem.Data.WriteAllText( FilePath, JsonSerializer.Serialize( this, ProgressionManager.JsonOptions ) );
	}

	public static CurrencyData Load()
	{
		if ( FileSystem.Data.FileExists( FilePath ) )
		{
			var json = FileSystem.Data.ReadAllText( FilePath );
			return JsonSerializer.Deserialize<CurrencyData>( json, ProgressionManager.JsonOptions ) ?? new CurrencyData();
		}
		return new CurrencyData();
	}
}
