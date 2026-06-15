namespace MilSim.World;

/// <summary>
/// Visual ground plane and coordinate system for the match.
/// The tile grid is internal infrastructure only — invisible to the player.
/// Unit movement uses NavigationAgent2D (smooth). Buildings are placed in world space.
/// Tiles are only used for terrain type queries and build-radius checks.
/// </summary>
public partial class Baseplate : Node2D
{
    [Export] public int TilesWide  { get; set; } = 80;
    [Export] public int TilesTall  { get; set; } = 80;
    [Export] public int TileWidth  { get; set; } = 128;
    [Export] public int TileHeight { get; set; } = 64;
    [Export] public Color GroundColor  { get; set; } = new Color(0.33f, 0.52f, 0.23f);
    [Export] public Color BorderColor  { get; set; } = new Color(0f, 0f, 0f, 0.4f);

    public override void _Draw()
    {
        Vector2[] corners = GetMapCorners();
        DrawPolygon(corners, new Color[] { GroundColor });
        DrawPolyline(new Vector2[] { corners[0], corners[1], corners[2], corners[3], corners[0] }, BorderColor, 2f);
    }

    // --- Coordinate helpers (internal use only) ---

    public Vector2 TileToWorld(Vector2I tile)
    {
        float x = (tile.X - tile.Y) * TileWidth  * 0.5f;
        float y = (tile.X + tile.Y) * TileHeight * 0.5f;
        return new Vector2(x, y);
    }

    public Vector2I WorldToTile(Vector2 world)
    {
        float tileX = (world.X / (TileWidth  * 0.5f) + world.Y / (TileHeight * 0.5f)) * 0.5f;
        float tileY = (world.Y / (TileHeight * 0.5f) - world.X / (TileWidth  * 0.5f)) * 0.5f;
        return new Vector2I((int)MathF.Floor(tileX), (int)MathF.Floor(tileY));
    }

    public bool IsInBounds(Vector2I tile)
    {
        return tile.X >= 0 && tile.X < TilesWide &&
               tile.Y >= 0 && tile.Y < TilesTall;
    }

    public bool IsWorldPositionInBounds(Vector2 world)
    {
        return IsInBounds(WorldToTile(world));
    }

    public Vector2 MapCenter()
    {
        return TileToWorld(new Vector2I(TilesWide / 2, TilesTall / 2));
    }

    private Vector2[] GetMapCorners()
    {
        float hw = TileWidth  * 0.5f;
        float hh = TileHeight * 0.5f;

        return new Vector2[]
        {
            TileToWorld(new Vector2I(0,            0           )) + new Vector2(  0, -hh), // top
            TileToWorld(new Vector2I(TilesWide - 1, 0          )) + new Vector2( hw,   0), // right
            TileToWorld(new Vector2I(TilesWide - 1, TilesTall-1)) + new Vector2(  0,  hh), // bottom
            TileToWorld(new Vector2I(0,            TilesTall - 1)) + new Vector2(-hw,   0), // left
        };
    }
}
