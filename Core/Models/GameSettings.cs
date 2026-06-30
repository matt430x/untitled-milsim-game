namespace MilSim.Core.Models;

public class GameSettings
{
    public GameMode   GameMode   { get; set; } = GameMode.FFA;
    public MapSize    MapSize    { get; set; } = MapSize.Medium;
    public MapPreset  MapPreset  { get; set; } = MapPreset.Continents;
    public int PlayerCount { get; set; } = 2;
    public float StartingMoney { get; set; } = 100f;
    public MatchSpeed MatchSpeed { get; set; } = MatchSpeed.Normal;
    public bool HasTimeLimit { get; set; }
    public float TimeLimitSeconds { get; set; }
    public bool AllowAircraft { get; set; } = true;
    public bool AllowNaval { get; set; } = true;
    public bool IsTeamGame { get; set; }
    public bool IsPrivateLobby { get; set; }
}
