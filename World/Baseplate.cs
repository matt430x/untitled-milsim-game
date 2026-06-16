namespace MilSim.World;

/// <summary>
/// Flat ground plane for the match. Tile coordinates map 1:1 to world XZ coordinates.
/// The visual mesh and ground collider are created at runtime so this node stays
/// lightweight in the scene file.
/// </summary>
public partial class Baseplate : Node3D
{
    [Export] public int   TilesWide  { get; set; } = 80;
    [Export] public int   TilesTall  { get; set; } = 80;
    [Export] public float TileSize   { get; set; } = 1f;
    [Export] public Color GroundColor { get; set; } = new Color(0.33f, 0.52f, 0.23f);

    public override void _Ready()
    {
        float w = TilesWide * TileSize;
        float h = TilesTall * TileSize;
        var center = new Vector3(w * 0.5f, 0f, h * 0.5f);

        // Visual ground mesh
        var visual = new MeshInstance3D();
        var plane  = new PlaneMesh { Size = new Vector2(w, h) };
        var mat    = new StandardMaterial3D { AlbedoColor = GroundColor };
        plane.Material = mat;
        visual.Mesh = plane;
        visual.Position = center;
        AddChild(visual);

        // Physics collider so raycasts hit the ground
        var body   = new StaticBody3D();
        var col    = new CollisionShape3D();
        var shape  = new BoxShape3D { Size = new Vector3(w, 0.02f, h) };
        col.Shape  = shape;
        body.Position = new Vector3(center.X, -0.01f, center.Z);
        body.AddChild(col);
        AddChild(body);
    }

    // --- Coordinate helpers ---

    public Vector3 TileToWorld(Vector2I tile) =>
        new(tile.X * TileSize, 0f, tile.Y * TileSize);

    public Vector2I WorldToTile(Vector3 world) =>
        new((int)(world.X / TileSize), (int)(world.Z / TileSize));

    public bool IsInBounds(Vector2I tile) =>
        tile.X >= 0 && tile.X < TilesWide &&
        tile.Y >= 0 && tile.Y < TilesTall;

    public bool IsWorldPositionInBounds(Vector3 world) =>
        IsInBounds(WorldToTile(world));

    public Vector3 MapCenter() =>
        new(TilesWide * TileSize * 0.5f, 0f, TilesTall * TileSize * 0.5f);
}
