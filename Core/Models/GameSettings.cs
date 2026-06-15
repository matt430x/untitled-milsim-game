namespace MilSim.Core.Models;

public class GameSettings
{
    public int PlayerCount { get; set; }
    public float StartingMoney { get; set; } = 500f;
    public MatchSpeed MatchSpeed { get; set; } = MatchSpeed.Normal;
    public bool HasTimeLimit { get; set; }
    public float TimeLimitSeconds { get; set; }
    public bool AllowAircraft { get; set; } = true;
    public bool AllowNaval { get; set; } = true;
    public bool IsTeamGame { get; set; }
    public bool IsPrivateLobby { get; set; }
}
