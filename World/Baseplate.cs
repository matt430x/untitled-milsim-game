using MilSim.Autoloads;

namespace MilSim.World;

public partial class Baseplate : Node3D
{
    [Export] public float TileSize { get; set; } = 1f;

    private static readonly Color ColorBeach    = new(0.83f, 0.71f, 0.51f);
    private static readonly Color ColorPlains   = new(0.33f, 0.52f, 0.23f);
    private static readonly Color ColorForest   = new(0.18f, 0.36f, 0.13f);
    private static readonly Color ColorMountain = new(0.48f, 0.46f, 0.42f);
    private static readonly Color ColorSnow     = new(0.92f, 0.93f, 0.95f);
    private static readonly Color ColorRiver    = new(0.29f, 0.50f, 0.78f);
    private static readonly Color ColorCliff    = new(0.30f, 0.27f, 0.23f);

    public override void _Ready()    => EventBus.OnMapGenerated += Build;
    public override void _ExitTree() => EventBus.OnMapGenerated -= Build;

    private void Build()
    {
        var terrainMesh = BuildTerrainMesh();
        AddChild(new MeshInstance3D { Mesh = terrainMesh });

        var body = new StaticBody3D();
        body.AddChild(new CollisionShape3D { Shape = terrainMesh.CreateTrimeshShape() });
        AddChild(body);

        int   W    = MapData.Width,  H    = MapData.Height;
        float mapW = W * TileSize,   mapD = H * TileSize;
        AddChild(new MeshInstance3D
        {
            Mesh = new PlaneMesh
            {
                Size     = new Vector2(mapW, mapD),
                Material = new StandardMaterial3D
                {
                    AlbedoColor  = new Color(0.15f, 0.30f, 0.65f, 0.82f),
                    Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                    ShadingMode  = BaseMaterial3D.ShadingModeEnum.Unshaded,
                },
            },
            Position = new Vector3(mapW * 0.5f, 0f, mapD * 0.5f),
        });
    }

    private ArrayMesh BuildTerrainMesh()
    {
        int W = MapData.Width, H = MapData.Height;

        var posList        = new List<Vector3>();
        var colorList      = new List<Color>();
        var idxList        = new List<int>();
        var flatVertKey    = new Dictionary<(int, int, int), int>();
        var flatColorAccum = new Dictionary<int, (float r, float g, float b, int n)>();

        // Returns index of a shared flat-top vertex; accumulates color for later averaging.
        int AddFlatVert(int vx, int vz, float y, Color c)
        {
            int hk  = Mathf.RoundToInt(y * 10f);
            var key = (vx, vz, hk);
            if (flatVertKey.TryGetValue(key, out int found))
            {
                var ac = flatColorAccum[found];
                flatColorAccum[found] = (ac.r + c.R, ac.g + c.G, ac.b + c.B, ac.n + 1);
                return found;
            }
            int idx = posList.Count;
            flatVertKey[key]    = idx;
            flatColorAccum[idx] = (c.R, c.G, c.B, 1);
            posList.Add(new Vector3(vx * TileSize, y, vz * TileSize));
            colorList.Add(c); // placeholder; replaced after accumulation pass
            return idx;
        }

        // --- Flat tops: Plains, Forest, River, Mountain, Snow ---
        for (int tx = 0; tx < W; tx++)
        for (int tz = 0; tz < H; tz++)
        {
            var t = MapData.GetTile(tx, tz);
            if (t == TerrainType.Water || t == TerrainType.Beach) continue;

            float y = MapData.HeightMap[tx, tz];
            var   c = TileColor(t);

            int i00 = AddFlatVert(tx,     tz,     y, c);
            int i10 = AddFlatVert(tx + 1, tz,     y, c);
            int i01 = AddFlatVert(tx,     tz + 1, y, c);
            int i11 = AddFlatVert(tx + 1, tz + 1, y, c);

            idxList.Add(i00); idxList.Add(i10); idxList.Add(i11);
            idxList.Add(i00); idxList.Add(i11); idxList.Add(i01);
        }

        // Replace placeholder colors with averaged values
        foreach (var kvp in flatColorAccum)
        {
            var ac = kvp.Value;
            colorList[kvp.Key] = new Color(ac.r / ac.n, ac.g / ac.n, ac.b / ac.n);
        }

        // --- Beach slopes: vertex heights averaged from surrounding tile heights ---
        for (int tx = 0; tx < W; tx++)
        for (int tz = 0; tz < H; tz++)
        {
            if (MapData.GetTile(tx, tz) != TerrainType.Beach) continue;

            int b = posList.Count;
            posList.Add(new Vector3( tx      * TileSize, CornerH(tx,     tz,     W, H),  tz      * TileSize));
            posList.Add(new Vector3((tx + 1) * TileSize, CornerH(tx + 1, tz,     W, H),  tz      * TileSize));
            posList.Add(new Vector3( tx      * TileSize, CornerH(tx,     tz + 1, W, H), (tz + 1) * TileSize));
            posList.Add(new Vector3((tx + 1) * TileSize, CornerH(tx + 1, tz + 1, W, H), (tz + 1) * TileSize));
            colorList.Add(ColorBeach); colorList.Add(ColorBeach);
            colorList.Add(ColorBeach); colorList.Add(ColorBeach);

            idxList.Add(b); idxList.Add(b + 1); idxList.Add(b + 3);
            idxList.Add(b); idxList.Add(b + 3); idxList.Add(b + 2);
        }

        // --- Cliff walls: vertical quads between different-height flat-top tiles ---
        for (int tx = 0; tx < W; tx++)
        for (int tz = 0; tz < H; tz++)
        {
            var tA = MapData.GetTile(tx, tz);
            if (tA == TerrainType.Water || tA == TerrainType.Beach) continue;

            float yA = MapData.HeightMap[tx, tz];

            for (int d = 0; d < 2; d++) // d=0: east neighbor; d=1: south neighbor
            {
                int nx = d == 0 ? tx + 1 : tx;
                int nz = d == 0 ? tz     : tz + 1;
                if (nx >= W || nz >= H) continue;

                var   tB = MapData.GetTile(nx, nz);
                float yB;

                if      (tB == TerrainType.Beach) continue;
                else if (tB == TerrainType.Water) yB = 0f;
                else                              yB = MapData.HeightMap[nx, nz];

                if (Mathf.Abs(yA - yB) < 0.01f) continue;

                float yHi = Mathf.Max(yA, yB);
                float yLo = Mathf.Min(yA, yB);
                int   bw  = posList.Count;

                if (d == 0) // wall along east edge of tx: x = (tx+1)*TileSize
                {
                    posList.Add(new Vector3((tx + 1) * TileSize, yHi,  tz      * TileSize));
                    posList.Add(new Vector3((tx + 1) * TileSize, yHi, (tz + 1) * TileSize));
                    posList.Add(new Vector3((tx + 1) * TileSize, yLo,  tz      * TileSize));
                    posList.Add(new Vector3((tx + 1) * TileSize, yLo, (tz + 1) * TileSize));
                }
                else // wall along south edge of tz: z = (tz+1)*TileSize
                {
                    posList.Add(new Vector3( tx      * TileSize, yHi, (tz + 1) * TileSize));
                    posList.Add(new Vector3((tx + 1) * TileSize, yHi, (tz + 1) * TileSize));
                    posList.Add(new Vector3( tx      * TileSize, yLo, (tz + 1) * TileSize));
                    posList.Add(new Vector3((tx + 1) * TileSize, yLo, (tz + 1) * TileSize));
                }

                colorList.Add(ColorCliff); colorList.Add(ColorCliff);
                colorList.Add(ColorCliff); colorList.Add(ColorCliff);

                idxList.Add(bw); idxList.Add(bw + 1); idxList.Add(bw + 3);
                idxList.Add(bw); idxList.Add(bw + 3); idxList.Add(bw + 2);
            }
        }

        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = posList.ToArray();
        arrays[(int)Mesh.ArrayType.Color]  = colorList.ToArray();
        arrays[(int)Mesh.ArrayType.Index]  = idxList.ToArray();

        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        mesh.SurfaceSetMaterial(0, new StandardMaterial3D
        {
            VertexColorUseAsAlbedo = true,
            ShadingMode            = BaseMaterial3D.ShadingModeEnum.Unshaded,
            CullMode               = BaseMaterial3D.CullModeEnum.Disabled,
        });

        return mesh;
    }

    // Average height of the up-to-4 tiles surrounding vertex (vx, vz).
    private static float CornerH(int vx, int vz, int W, int H)
    {
        float sum = 0f; int n = 0;
        for (int dx = -1; dx <= 0; dx++)
        for (int dz = -1; dz <= 0; dz++)
        {
            int tx = vx + dx, tz = vz + dz;
            if (tx < 0 || tx >= W || tz < 0 || tz >= H) continue;
            sum += MapData.HeightMap[tx, tz];
            n++;
        }
        return n > 0 ? sum / n : -0.5f;
    }

    private static Color TileColor(TerrainType t) => t switch
    {
        TerrainType.Beach    => ColorBeach,
        TerrainType.Plains   => ColorPlains,
        TerrainType.Forest   => ColorForest,
        TerrainType.Mountain => ColorMountain,
        TerrainType.Snow     => ColorSnow,
        TerrainType.River    => ColorRiver,
        _                    => ColorPlains,
    };

    // --- Coordinate helpers ---

    public Vector3  TileToWorld(Vector2I tile) => new(tile.X * TileSize, MapData.GetWorldHeight(tile.X, tile.Y), tile.Y * TileSize);
    public Vector2I WorldToTile(Vector3 world)  => new((int)(world.X / TileSize), (int)(world.Z / TileSize));

    public bool IsInBounds(Vector2I tile) =>
        tile.X >= 0 && tile.X < MapData.Width &&
        tile.Y >= 0 && tile.Y < MapData.Height;

    public bool IsWorldPositionInBounds(Vector3 world) => IsInBounds(WorldToTile(world));

    public Vector3 MapCenter() => new(MapData.Width * TileSize * 0.5f, 0f, MapData.Height * TileSize * 0.5f);
}
