namespace MilSim.World;

public static class MapData
{
    public static TerrainType[,] Tiles            { get; private set; }
    public static float[,]       HeightMap        { get; private set; }  // world-space Y per tile
    public static int            Width            { get; private set; }
    public static int            Height           { get; private set; }
    public static Vector2I[]     CrystalPositions { get; private set; }
    public static Vector2I[]     SpawnPoints      { get; private set; }
    public static bool           IsReady          => Tiles != null;
    public static float          WorldWidth       => Width;
    public static float          WorldHeight      => Height;

    internal static void Initialize(TerrainType[,] tiles, float[,] heightMap, Vector2I[] crystalPositions, Vector2I[] spawnPoints)
    {
        Tiles            = tiles;
        HeightMap        = heightMap;
        Width            = tiles.GetLength(0);
        Height           = tiles.GetLength(1);
        CrystalPositions = crystalPositions;
        SpawnPoints      = spawnPoints;
    }

    public static TerrainType GetTile(int x, int z)  => Tiles[x, z];
    public static bool        IsLand(int x, int z)   => Tiles[x, z] != TerrainType.Water;
    public static bool        IsWater(int x, int z)  => Tiles[x, z] == TerrainType.Water;
    public static bool        InBounds(int x, int z) => x >= 0 && x < Width && z >= 0 && z < Height;

    // Nearest-tile height lookup — used for Y-snapping units and spawn positioning.
    public static float GetWorldHeight(float worldX, float worldZ)
    {
        int tx = Mathf.Clamp((int)worldX, 0, Width  - 1);
        int tz = Mathf.Clamp((int)worldZ, 0, Height - 1);
        return HeightMap[tx, tz];
    }
}
