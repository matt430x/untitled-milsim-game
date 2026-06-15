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
    [Export] public PackedScene Scene { get; set; }
}
