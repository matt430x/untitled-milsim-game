namespace MilSim.Data;

[GlobalClass]
public partial class ResearchData : Resource
{
    [Export] public string ResearchId { get; set; } = string.Empty;
    [Export] public string ResearchName { get; set; } = string.Empty;
    [Export] public string Description { get; set; } = string.Empty;
    [Export] public ResearchCategory Category { get; set; }
    [Export] public int Cost { get; set; }
    [Export] public float ResearchTime { get; set; }
    [Export] public bool IsStackable { get; set; }
    [Export] public int MaxStacks { get; set; } = 1;
    [Export] public Texture2D Icon { get; set; }
}
