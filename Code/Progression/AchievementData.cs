using System.Text.Json;

namespace Progression;

/// <summary>
/// Achievement definitions and tracking.
/// Each achievement can be earned once and gives a Paperclip reward.
/// </summary>
public sealed class AchievementData
{
	public bool FirstSteps { get; set; }      // complete wave 1
	public bool WaveRider { get; set; }        // reach wave 10
	public bool WaveMaster { get; set; }       // reach wave 25
	public bool PenPincher { get; set; }       // kill 100 enemies total
	public bool OfficeMassacre { get; set; }   // kill 1000 enemies total
	public bool Unscathed { get; set; }        // kill boss without taking damage
	public bool Marathon { get; set; }         // survive 30 min in one run
	public bool Collector { get; set; }        // unlock all playstyles
	public bool MaxedOut { get; set; }         // fully upgrade all shop items
	public bool Papercut { get; set; }          // deal 1000 damage in one run

	public struct AchievementInfo
	{
		public string Id;
		public string Name;
		public string Description;
		public int PaperclipReward;
	}

	public static readonly AchievementInfo[] All = new[]
	{
		new AchievementInfo { Id = nameof(FirstSteps), Name = "First Steps", Description = "Complete wave 1", PaperclipReward = 1 },
		new AchievementInfo { Id = nameof(WaveRider), Name = "Wave Rider", Description = "Reach wave 10", PaperclipReward = 3 },
		new AchievementInfo { Id = nameof(WaveMaster), Name = "Wave Master", Description = "Reach wave 25", PaperclipReward = 10 },
		new AchievementInfo { Id = nameof(PenPincher), Name = "Pen Pincher", Description = "Kill 100 enemies total", PaperclipReward = 2 },
		new AchievementInfo { Id = nameof(OfficeMassacre), Name = "Office Massacre", Description = "Kill 1000 enemies total", PaperclipReward = 10 },
		new AchievementInfo { Id = nameof(Unscathed), Name = "Unscathed", Description = "Kill a boss without taking damage", PaperclipReward = 3 },
		new AchievementInfo { Id = nameof(Marathon), Name = "Marathon", Description = "Survive 30 minutes in one run", PaperclipReward = 5 },
		new AchievementInfo { Id = nameof(Collector), Name = "Collector", Description = "Unlock all playstyles", PaperclipReward = 15 },
		new AchievementInfo { Id = nameof(MaxedOut), Name = "Maxed Out", Description = "Fully upgrade all shop items", PaperclipReward = 25 },
		new AchievementInfo { Id = nameof(Papercut), Name = "Papercut", Description = "Deal 1000 damage in one run", PaperclipReward = 2 },
	};

	public bool IsUnlocked( string id ) => id switch
	{
		nameof(FirstSteps) => FirstSteps,
		nameof(WaveRider) => WaveRider,
		nameof(WaveMaster) => WaveMaster,
		nameof(PenPincher) => PenPincher,
		nameof(OfficeMassacre) => OfficeMassacre,
		nameof(Unscathed) => Unscathed,
		nameof(Marathon) => Marathon,
		nameof(Collector) => Collector,
		nameof(MaxedOut) => MaxedOut,
		nameof(Papercut) => Papercut,
		_ => false
	};

	public void Unlock( string id )
	{
		switch ( id )
		{
			case nameof(FirstSteps): FirstSteps = true; break;
			case nameof(WaveRider): WaveRider = true; break;
			case nameof(WaveMaster): WaveMaster = true; break;
			case nameof(PenPincher): PenPincher = true; break;
			case nameof(OfficeMassacre): OfficeMassacre = true; break;
			case nameof(Unscathed): Unscathed = true; break;
			case nameof(Marathon): Marathon = true; break;
			case nameof(Collector): Collector = true; break;
			case nameof(MaxedOut): MaxedOut = true; break;
			case nameof(Papercut): Papercut = true; break;
		}
	}

	private static readonly string FilePath = "progression/achievements.json";

	public void Save()
	{
		FileSystem.Data.WriteAllText( FilePath, JsonSerializer.Serialize( this, ProgressionManager.JsonOptions ) );
	}

	public static AchievementData Load()
	{
		if ( FileSystem.Data.FileExists( FilePath ) )
		{
			var json = FileSystem.Data.ReadAllText( FilePath );
			return JsonSerializer.Deserialize<AchievementData>( json, ProgressionManager.JsonOptions ) ?? new AchievementData();
		}
		return new AchievementData();
	}
}
