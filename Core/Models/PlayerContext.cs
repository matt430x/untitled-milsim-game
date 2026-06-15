namespace MilSim.Core.Models;

public class PlayerContext
{
    public int PlayerId { get; init; }
    public int TeamId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public Color TeamColor { get; set; }

    public float Money { get; set; }
    public UnitCaps UnitCaps { get; } = new();

    public bool IsLocalPlayer { get; init; }
    public bool IsAI { get; init; }
    public bool IsDisconnected { get; set; }
    public bool IsEliminated { get; set; }

    public int HqCount { get; set; }
}
