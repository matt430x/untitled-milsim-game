using MilSim.Entities.Crystals;

namespace MilSim.Systems;

/// <summary>
/// Global registry of all crystal nodes in the world. Crystals project the
/// build zone for RequiresCrystal buildings; PlacementController queries this.
/// </summary>
public static class CrystalRegistry
{
    private static readonly List<CrystalNode> _crystals = new();

    public static void Register(CrystalNode crystal)   => _crystals.Add(crystal);
    public static void Unregister(CrystalNode crystal) => _crystals.Remove(crystal);

    public static IReadOnlyList<CrystalNode> All => _crystals;
}
