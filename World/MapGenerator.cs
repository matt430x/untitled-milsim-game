using MilSim.Autoloads;

namespace MilSim.World;

public static class MapGenerator
{
    public static void Generate(MapPreset preset, MapSize size, int seed, int playerCount)
    {
        var (w, h) = SizeToTiles(size);

        // Stage 1: elevation + edge falloff
        var elev      = new float[w, h];
        var elevNoise = BuildElevNoise(preset, seed);
        for (int x = 0; x < w; x++)
        for (int z = 0; z < h; z++)
        {
            float raw  = elevNoise.GetNoise2D(x, z);
            float nx   = (x / (float)(w - 1)) * 2f - 1f;
            float nz   = (z / (float)(h - 1)) * 2f - 1f;
            float dist = Mathf.Min(Mathf.Sqrt(nx * nx + nz * nz) / Mathf.Sqrt(2f), 1f);
            elev[x, z] = raw - preset.EdgeFalloff * dist;
        }

        // Stage 2: moisture
        var moist      = new float[w, h];
        var moistNoise = BuildMoistureNoise(preset, seed ^ unchecked((int)0x9e3779b9));
        for (int x = 0; x < w; x++)
        for (int z = 0; z < h; z++)
            moist[x, z] = (moistNoise.GetNoise2D(x, z) + 1f) * 0.5f;

        // Stage 3: classify
        float waterLine  = ComputeThreshold(elev, w, h, preset.LandRatio);
        float maxElev    = float.MinValue;
        for (int x = 0; x < w; x++)
        for (int z = 0; z < h; z++)
            if (elev[x, z] > maxElev) maxElev = elev[x, z];

        float elevRange = maxElev - waterLine;
        var   tiles     = new TerrainType[w, h];

        for (int x = 0; x < w; x++)
        for (int z = 0; z < h; z++)
        {
            float e = elev[x, z];
            if (e < waterLine) { tiles[x, z] = TerrainType.Water; continue; }

            float t = elevRange > 0.001f ? (e - waterLine) / elevRange : 0f;

            if      (t < preset.BeachHeight)    tiles[x, z] = TerrainType.Beach;
            else if (t > preset.SnowHeight)     tiles[x, z] = TerrainType.Snow;
            else if (t > preset.MountainHeight) tiles[x, z] = TerrainType.Mountain;
            else tiles[x, z] = moist[x, z] > 0.5f ? TerrainType.Forest : TerrainType.Plains;
        }

        // Stage 4: rivers
        if (preset.HasRivers && preset.RiverCount > 0)
            CarveRivers(tiles, elev, w, h, preset.RiverCount, seed);

        // Stage 4.5: morphological smoothing — erode isolated mountain blobs
        SmoothTerrain(tiles, w, h);

        // Stage 5: discrete height levels
        var heights = new float[w, h];
        for (int x = 0; x < w; x++)
        for (int z = 0; z < h; z++)
        {
            heights[x, z] = tiles[x, z] switch
            {
                TerrainType.Water    => -0.5f,
                TerrainType.Beach    => 0.0f,
                TerrainType.Mountain => 3.5f,
                TerrainType.Snow     => 4.5f,
                _                    => 1.0f,
            };
        }

        // Stage 6: crystals
        var rng      = new Random(seed);
        var crystals = PlaceCrystals(tiles, w, h, size, rng);

        // Stage 7: spawn points
        var spawns = PlaceSpawnPoints(tiles, w, h, playerCount);

        MapData.Initialize(tiles, heights, crystals, spawns);
        EventBus.RaiseMapGenerated();
    }

    // -------------------------------------------------------------------------
    // Noise builders
    // -------------------------------------------------------------------------

    private static FastNoiseLite BuildElevNoise(MapPreset preset, int seed) => new()
    {
        Seed           = seed,
        NoiseType      = FastNoiseLite.NoiseTypeEnum.Simplex,
        Frequency      = preset.NoiseScale,
        FractalType    = FastNoiseLite.FractalTypeEnum.Fbm,
        FractalOctaves = preset.Octaves,
        FractalGain    = preset.Persistence,
    };

    private static FastNoiseLite BuildMoistureNoise(MapPreset preset, int seed) => new()
    {
        Seed      = seed,
        NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex,
        Frequency = preset.MoistureScale,
    };

    // -------------------------------------------------------------------------
    // Histogram threshold — guarantees exactly LandRatio fraction of tiles are land
    // -------------------------------------------------------------------------

    private static float ComputeThreshold(float[,] values, int w, int h, float landRatio)
    {
        int     total = w * h;
        float[] flat  = new float[total];
        int     i     = 0;
        for (int x = 0; x < w; x++)
        for (int z = 0; z < h; z++)
            flat[i++] = values[x, z];

        Array.Sort(flat);

        int waterCount = Mathf.Clamp((int)((1f - landRatio) * total), 0, total - 1);
        return flat[waterCount];
    }

    // -------------------------------------------------------------------------
    // River carving — trace downhill from mountain peaks to water
    // -------------------------------------------------------------------------

    private static void CarveRivers(TerrainType[,] tiles, float[,] elev, int w, int h, int riverCount, int seed)
    {
        // Collect mountain/snow tiles sorted by elevation descending
        var seeds = new List<(int x, int z, float e)>();
        for (int x = 0; x < w; x++)
        for (int z = 0; z < h; z++)
            if (tiles[x, z] == TerrainType.Mountain || tiles[x, z] == TerrainType.Snow)
                seeds.Add((x, z, elev[x, z]));

        seeds.Sort((a, b) => b.e.CompareTo(a.e));

        int minDist      = Mathf.Max(w, h) / (riverCount * 2 + 1);
        var chosenSeeds  = new List<(int x, int z)>();
        int riversCarved = 0;

        foreach (var s in seeds)
        {
            if (riversCarved >= riverCount) break;

            bool tooClose = false;
            foreach (var c in chosenSeeds)
            {
                if (Math.Abs(c.x - s.x) + Math.Abs(c.z - s.z) < minDist)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose) continue;

            chosenSeeds.Add((s.x, s.z));
            riversCarved++;

            // Trace downhill
            int cx = s.x, cz = s.z;
            int[] dx = { -1, 1, 0, 0 };
            int[] dz = {  0, 0,-1, 1 };

            for (int step = 0; step < w + h; step++)
            {
                if (tiles[cx, cz] == TerrainType.Water) break;

                if (tiles[cx, cz] != TerrainType.Mountain && tiles[cx, cz] != TerrainType.Snow)
                    tiles[cx, cz] = TerrainType.River;

                int   bestX = cx, bestZ = cz;
                float bestE = elev[cx, cz];
                for (int d = 0; d < 4; d++)
                {
                    int nx2 = cx + dx[d], nz2 = cz + dz[d];
                    if (nx2 < 0 || nx2 >= w || nz2 < 0 || nz2 >= h) continue;
                    if (elev[nx2, nz2] < bestE)
                    {
                        bestE = elev[nx2, nz2];
                        bestX = nx2; bestZ = nz2;
                    }
                }
                if (bestX == cx && bestZ == cz) break; // local minimum
                cx = bestX; cz = bestZ;
            }
        }
    }

    // -------------------------------------------------------------------------
    // Crystal placement — greedy with minimum spacing on flat land
    // -------------------------------------------------------------------------

    private static Vector2I[] PlaceCrystals(TerrainType[,] tiles, int w, int h, MapSize size, Random rng)
    {
        int   count    = size switch { MapSize.Small => 8, MapSize.Medium => 12, MapSize.Large => 18, MapSize.Massive => 28, _ => 12 };
        float minDist  = size switch { MapSize.Small => 10f, MapSize.Medium => 12f, MapSize.Large => 15f, MapSize.Massive => 20f, _ => 12f };

        var candidates = new List<Vector2I>();
        for (int x = 0; x < w; x++)
        for (int z = 0; z < h; z++)
            if (tiles[x, z] == TerrainType.Plains || tiles[x, z] == TerrainType.Forest)
                candidates.Add(new Vector2I(x, z));

        // Fisher-Yates shuffle
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
        }

        var result = new List<Vector2I>();
        foreach (var c in candidates)
        {
            if (result.Count >= count) break;

            bool tooClose = false;
            foreach (var r in result)
            {
                float dx = r.X - c.X, dz = r.Y - c.Y;
                if (Mathf.Sqrt(dx * dx + dz * dz) < minDist) { tooClose = true; break; }
            }
            if (!tooClose) result.Add(c);
        }

        return result.ToArray();
    }

    // -------------------------------------------------------------------------
    // Spawn placement — radially distributed on buildable land
    // -------------------------------------------------------------------------

    private static Vector2I[] PlaceSpawnPoints(TerrainType[,] tiles, int w, int h, int playerCount)
    {
        float cx     = w / 2f;
        float cz     = h / 2f;
        float radius = Mathf.Min(w, h) * 0.35f;
        var   spawns = new Vector2I[playerCount];

        for (int i = 0; i < playerCount; i++)
        {
            float angle = (float)(2.0 * Math.PI * i / playerCount);
            int   tx    = (int)(cx + radius * Mathf.Cos(angle));
            int   tz    = (int)(cz + radius * Mathf.Sin(angle));
            spawns[i]   = FindNearestBuildable(tiles, w, h, tx, tz);
        }

        return spawns;
    }

    private static Vector2I FindNearestBuildable(TerrainType[,] tiles, int w, int h, int targetX, int targetZ)
    {
        var queue   = new Queue<Vector2I>();
        var visited = new HashSet<Vector2I>();
        var start   = new Vector2I(Mathf.Clamp(targetX, 0, w - 1), Mathf.Clamp(targetZ, 0, h - 1));

        queue.Enqueue(start);
        visited.Add(start);

        int[] dx = { -1, 1, 0, 0 };
        int[] dz = {  0, 0,-1, 1 };

        while (queue.Count > 0)
        {
            var pos = queue.Dequeue();
            if (tiles[pos.X, pos.Y] == TerrainType.Plains || tiles[pos.X, pos.Y] == TerrainType.Forest)
                return pos;

            for (int d = 0; d < 4; d++)
            {
                var next = new Vector2I(pos.X + dx[d], pos.Y + dz[d]);
                if (next.X < 0 || next.X >= w || next.Y < 0 || next.Y >= h) continue;
                if (!visited.Add(next)) continue;
                queue.Enqueue(next);
            }
        }

        return start; // fallback: no buildable tile found (degenerate map)
    }

    // -------------------------------------------------------------------------
    // Morphological smoothing — erode isolated mountain/snow tiles
    // -------------------------------------------------------------------------

    private static void SmoothTerrain(TerrainType[,] tiles, int w, int h)
    {
        int[] dx = { -1, 1, 0, 0 };
        int[] dz = {  0, 0,-1, 1 };

        for (int pass = 0; pass < 3; pass++)
        {
            var next = (TerrainType[,])tiles.Clone();
            for (int x = 0; x < w; x++)
            for (int z = 0; z < h; z++)
            {
                if (tiles[x, z] != TerrainType.Mountain && tiles[x, z] != TerrainType.Snow) continue;

                int hi = 0;
                for (int d = 0; d < 4; d++)
                {
                    int nx = x + dx[d], nz = z + dz[d];
                    if (nx < 0 || nx >= w || nz < 0 || nz >= h) continue;
                    if (tiles[nx, nz] == TerrainType.Mountain || tiles[nx, nz] == TerrainType.Snow) hi++;
                }

                if (hi < 2) next[x, z] = TerrainType.Plains;
            }
            for (int x = 0; x < w; x++)
            for (int z = 0; z < h; z++)
                tiles[x, z] = next[x, z];
        }
    }

    // -------------------------------------------------------------------------

    private static (int w, int h) SizeToTiles(MapSize size) => size switch
    {
        MapSize.Small   => (64,  64),
        MapSize.Medium  => (96,  96),
        MapSize.Large   => (128, 128),
        MapSize.Massive => (192, 192),
        _               => (96,  96),
    };
}
