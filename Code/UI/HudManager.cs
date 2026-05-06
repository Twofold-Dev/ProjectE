/// <summary>
/// Creates the HUD UI. PanelComponents are added as components to a GameObject
/// alongside a ScreenPanel component.
/// Attach this to any persistent GameObject (e.g. GameManager).
/// </summary>
public sealed class HudManager : Component
{
	private GameObject _uiRoot;

	protected override void OnStart()
	{
		Log.Info( "HudManager: Creating UI root" );

		// Create a GameObject for the UI (not saved, not networked)
		_uiRoot = new GameObject( true, "HUD" )
		{
			Flags = GameObjectFlags.NotSaved | GameObjectFlags.NotNetworked
		};

		// Add ScreenPanel (required for Razor UI to render)
		var screenPanel = _uiRoot.AddComponent<ScreenPanel>();

		// Add game PanelComponent panels
		_uiRoot.AddComponent<Sandbox.UI.MainMenuPanel>(); // shows only when State=Lobby
		_uiRoot.AddComponent<Sandbox.UI.GameHud>();
		_uiRoot.AddComponent<Sandbox.UI.UpgradePanel>();
		_uiRoot.AddComponent<Sandbox.UI.GameOverPanel>();
		_uiRoot.AddComponent<Sandbox.UI.Chatbox>();

		Log.Info( "HudManager: UI created" );
	}

	protected override void OnDestroy()
	{
		if ( _uiRoot.IsValid() )
		{
			_uiRoot.Destroy();
		}
	}
}
