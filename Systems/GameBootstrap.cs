using MilSim.Autoloads;

namespace MilSim.Systems;

/// <summary>
/// Runs once when Game.tscn loads. Registers player contexts and transitions
/// GameManager to InGame so EconomySystem ticks and money operations work.
/// </summary>
public partial class GameBootstrap : Node
{
    public override void _Ready()
    {
        float startMoney = GameManager.Instance?.CurrentSettings?.StartingMoney ?? 500f;

        PlayerManager.Instance.RegisterPlayer(new PlayerContext
        {
            PlayerId     = 1,
            TeamId       = 1,
            DisplayName  = "Player",
            IsLocalPlayer = true,
            Money        = startMoney,
        });

        PlayerManager.Instance.RegisterPlayer(new PlayerContext
        {
            PlayerId    = 2,
            TeamId      = 2,
            DisplayName = "Opponent",
            IsAI        = true,
            Money       = startMoney,
        });

        GameManager.Instance?.OnMatchLoaded();
    }
}
