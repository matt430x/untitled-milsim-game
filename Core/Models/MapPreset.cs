namespace MilSim.Core.Models;

public sealed class MapPreset
{
    public string Name           { get; init; }
    public float  LandRatio      { get; init; }  // target fraction of land tiles (0..1)
    public float  NoiseScale     { get; init; }  // noise frequency — lower = bigger landmasses
    public int    Octaves        { get; init; }  // fractal detail layers
    public float  Persistence    { get; init; }  // amplitude falloff per octave
    public float  EdgeFalloff    { get; init; }  // push map edges toward water (0=none, 1=strong)
    public float  MoistureScale  { get; init; }  // moisture noise frequency — controls forest/plains spread
    public float  BeachHeight    { get; init; }  // normalized land-elevation threshold below which = beach
    public float  MountainHeight { get; init; }  // normalized land-elevation threshold above which = mountain
    public float  SnowHeight     { get; init; }  // normalized land-elevation threshold above which = snow
    public bool   HasRivers      { get; init; }
    public int    RiverCount     { get; init; }

    public static readonly MapPreset Pangaea = new()
    {
        Name = "Pangaea",          LandRatio = 0.75f, NoiseScale = 0.018f,
        Octaves = 2, Persistence = 0.50f, EdgeFalloff = 0.00f,
        MoistureScale = 0.030f, BeachHeight = 0.06f, MountainHeight = 0.72f, SnowHeight = 0.88f,
        HasRivers = true, RiverCount = 4,
    };
    public static readonly MapPreset Continents = new()
    {
        Name = "Continents",       LandRatio = 0.50f, NoiseScale = 0.025f,
        Octaves = 3, Persistence = 0.50f, EdgeFalloff = 0.35f,
        MoistureScale = 0.035f, BeachHeight = 0.07f, MountainHeight = 0.70f, SnowHeight = 0.87f,
        HasRivers = true, RiverCount = 4,
    };
    public static readonly MapPreset SmallContinents = new()
    {
        Name = "Small Continents", LandRatio = 0.50f, NoiseScale = 0.040f,
        Octaves = 3, Persistence = 0.50f, EdgeFalloff = 0.30f,
        MoistureScale = 0.040f, BeachHeight = 0.08f, MountainHeight = 0.70f, SnowHeight = 0.87f,
        HasRivers = true, RiverCount = 3,
    };
    public static readonly MapPreset Archipelago = new()
    {
        Name = "Archipelago",      LandRatio = 0.28f, NoiseScale = 0.055f,
        Octaves = 3, Persistence = 0.50f, EdgeFalloff = 0.45f,
        MoistureScale = 0.050f, BeachHeight = 0.12f, MountainHeight = 0.65f, SnowHeight = 0.85f,
        HasRivers = false, RiverCount = 0,
    };
    public static readonly MapPreset Lakes = new()
    {
        Name = "Lakes",            LandRatio = 0.72f, NoiseScale = 0.045f,
        Octaves = 3, Persistence = 0.55f, EdgeFalloff = 0.00f,
        MoistureScale = 0.035f, BeachHeight = 0.04f, MountainHeight = 0.75f, SnowHeight = 0.90f,
        HasRivers = true, RiverCount = 6,
    };
    public static readonly MapPreset SevenSeas = new()
    {
        Name = "Seven Seas",       LandRatio = 0.25f, NoiseScale = 0.045f,
        Octaves = 2, Persistence = 0.50f, EdgeFalloff = 0.40f,
        MoistureScale = 0.045f, BeachHeight = 0.14f, MountainHeight = 0.65f, SnowHeight = 0.85f,
        HasRivers = false, RiverCount = 0,
    };
    public static readonly MapPreset Highlands = new()
    {
        Name = "Highlands",        LandRatio = 0.68f, NoiseScale = 0.030f,
        Octaves = 4, Persistence = 0.60f, EdgeFalloff = 0.10f,
        MoistureScale = 0.030f, BeachHeight = 0.05f, MountainHeight = 0.60f, SnowHeight = 0.82f,
        HasRivers = true, RiverCount = 5,
    };
    public static readonly MapPreset GrandPlains = new()
    {
        Name = "Grand Plains",     LandRatio = 0.82f, NoiseScale = 0.015f,
        Octaves = 1, Persistence = 0.50f, EdgeFalloff = 0.00f,
        MoistureScale = 0.025f, BeachHeight = 0.03f, MountainHeight = 0.90f, SnowHeight = 0.97f,
        HasRivers = false, RiverCount = 0,
    };
    public static readonly MapPreset Fractal = new()
    {
        Name = "Fractal",          LandRatio = 0.45f, NoiseScale = 0.040f,
        Octaves = 6, Persistence = 0.55f, EdgeFalloff = 0.20f,
        MoistureScale = 0.055f, BeachHeight = 0.06f, MountainHeight = 0.65f, SnowHeight = 0.84f,
        HasRivers = true, RiverCount = 5,
    };

    public static readonly MapPreset[] All =
    {
        Pangaea, Continents, SmallContinents, Archipelago,
        Lakes, SevenSeas, Highlands, GrandPlains, Fractal,
    };
}
