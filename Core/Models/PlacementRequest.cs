using MilSim.Data;

namespace MilSim.Core.Models;

/// <summary>
/// One request to enter placement mode, raised by the spawn menu and consumed by
/// PlacementController. Bundles everything placement needs so adding a new field
/// (cost, build time, etc.) doesn't churn the EventBus signature.
/// </summary>
public readonly struct PlacementRequest
{
    public PackedScene   Scene    { get; init; }
    public PlaceableKind Kind     { get; init; }
    public UnitData      Unit     { get; init; } // null unless Kind == Unit
    public BuildingData  Building { get; init; } // null unless Kind == Building
    public int           OwnerId  { get; init; }
}
