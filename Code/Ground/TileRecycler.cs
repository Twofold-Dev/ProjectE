/// <summary>
/// Takes tile/wall GameObjects from the inspector and recycles copies
/// to create infinite scrolling ground and walls.
/// </summary>
public sealed class TileRecycler : Component
{
	[Property, Category( "Ground" )]
	public GameObject TilePrefab { get; set; }

	[Property, Category( "Ground" )]
	public int TileCount { get; set; } = 10;

	[Property, Category( "Ground" )]
	public float TileSize { get; set; } = 200f;

	[Property, Category( "Walls" )]
	public GameObject LeftWallPrefab { get; set; }

	[Property, Category( "Walls" )]
	public GameObject RightWallPrefab { get; set; }

	[Property, Category( "Walls" )]
	public float WallXOffset { get; set; } = 200f;

	[Property, Category( "Walls" )]
	public int WallCount { get; set; } = 10;

	[Property, Category( "Walls" )]
	public float WallSize { get; set; } = 200f;

	private List<GameObject> _tiles = new();
	private List<GameObject> _leftWalls = new();
	private List<GameObject> _rightWalls = new();

	protected override void OnStart()
	{
		CloneAndRecycle( TilePrefab, _tiles, TileCount, TileSize, 0 );
		CloneAndRecycle( LeftWallPrefab, _leftWalls, WallCount, WallSize, -WallXOffset );
		CloneAndRecycle( RightWallPrefab, _rightWalls, WallCount, WallSize, WallXOffset );
	}

	private void CloneAndRecycle( GameObject prefab, List<GameObject> list, int count, float size, float xPos )
	{
		if ( !prefab.IsValid() ) return;
		prefab.Enabled = false;

		for ( int i = 0; i < count; i++ )
		{
			var clone = prefab.Clone( new CloneConfig
			{
				Name = $"{prefab.Name}_Recycle_{i}",
				Transform = new Transform( new Vector3( xPos, -size * i, 0 ) ),
				StartEnabled = true
			} );
			list.Add( clone );
		}
	}

	protected override void OnUpdate()
	{
		var playerY = GetPlayerY();
		if ( playerY == 0 ) return;

		RecycleList( _tiles, TileSize, playerY, 0 );
		RecycleList( _leftWalls, WallSize, playerY, -WallXOffset );
		RecycleList( _rightWalls, WallSize, playerY, WallXOffset );
	}

	private void RecycleList( List<GameObject> list, float size, float playerY, float xPos )
	{
		if ( list.Count == 0 ) return;
		var buffer = size * 3f;

		// Sort so we process from highest Y (back) to lowest Y (front)
		list.Sort( ( a, b ) => b.WorldPosition.y.CompareTo( a.WorldPosition.y ) );

		// Find the lowest Y to know where to place recycled tiles
		var lowest = list.Last().WorldPosition.y;

		foreach ( var obj in list )
		{
			if ( obj.WorldPosition.y > playerY + buffer )
			{
				lowest -= size;
				obj.WorldPosition = new Vector3( xPos, lowest, 0 );
			}
		}
	}

	private float GetPlayerY()
	{
		foreach ( var p in Scene.GetAllComponents<ArrowPlayer>() )
			return p.WorldPosition.y;
		return 0;
	}
}
