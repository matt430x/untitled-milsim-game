namespace MilSim.Data;

[GlobalClass]
public partial class BuildingData : Resource
{
    [Export] public string BuildingName { get; set; } = string.Empty;
    [Export] public BuildingType BuildingType { get; set; }
    [Export] public float MaxHealth { get; set; }
    [Export] public int Cost { get; set; }
    [Export] public float BuildTime { get; set; }
    [Export] public float BuildRadius { get; set; }
    [Export] public bool RequiresCommandCenter { get; set; } = true;
    [Export] public bool RequiresCrystal { get; set; }
    // When true, this building claims the crystal zone it sits in, blocking enemies
    // from building there. Set false for buildings that don't establish territory.
    [Export] public bool ClaimsCrystalZone { get; set; } = true;
    [Export] public float IncomePerTick { get; set; }
    [Export] public float Damage { get; set; }
    [Export] public float AttackRange { get; set; }
    [Export] public float AttackCooldown { get; set; }
    [Export] public PackedScene Scene { get; set; }
    [Export] public Godot.Collections.Array<UnitData> ProducibleUnits { get; set; } = new();
}
