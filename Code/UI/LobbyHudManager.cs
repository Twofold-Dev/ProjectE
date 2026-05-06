/// <summary>
/// Creates the Lobby/Main Menu UI.
/// Only creates the MainMenuPanel — no game HUD elements.
/// Following Voxel-Party's pattern of separate HUD per scene.
/// </summary>
public sealed class LobbyHudManager : Component
{
	private GameObject _uiRoot;

	protected override void OnStart()
	{
		Log.Info( "LobbyHudManager: Creating lobby UI" );

		_uiRoot = new GameObject( true, "LobbyHUD" )
		{
			Flags = GameObjectFlags.NotSaved | GameObjectFlags.NotNetworked
		};

		var screenPanel = _uiRoot.AddComponent<ScreenPanel>();

		// Main menu panel + chat (works in both lobby and game)
		_uiRoot.AddComponent<Sandbox.UI.MainMenuPanel>();
		_uiRoot.AddComponent<Sandbox.UI.Chatbox>();

		Log.Info( "LobbyHudManager: Lobby UI created" );
	}

	protected override void OnDestroy()
	{
		if ( _uiRoot.IsValid() )
		{
			_uiRoot.Destroy();
		}
	}
}
