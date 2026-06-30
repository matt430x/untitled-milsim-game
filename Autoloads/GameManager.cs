using MilSim.Autoloads;

namespace MilSim.Autoloads;

public partial class GameManager : Node
{
    public static GameManager Instance { get; private set; }

    public GameState CurrentState { get; private set; } = GameState.MainMenu;
    public GameSettings CurrentSettings { get; private set; } = new();

    public override void _Ready()
    {
        Instance = this;
        EventBus.OnHqDestroyed += OnHqDestroyed;
    }

    public override void _ExitTree()
    {
        EventBus.OnHqDestroyed -= OnHqDestroyed;
    }

    private void OnHqDestroyed(int playerId)
    {
        PlayerManager.Instance?.EliminatePlayer(playerId);
    }

    public void StartMatch(GameSettings settings)
    {
        CurrentSettings = settings;
        TransitionTo(GameState.Loading);
    }

    public void OnMatchLoaded()
    {
        TransitionTo(GameState.InGame);
        EventBus.RaiseMatchStarted();
    }

    public void EndMatch(int winnerTeamId)
    {
        TransitionTo(GameState.PostGame);
        EventBus.RaiseMatchEnded(winnerTeamId);
    }

    private void TransitionTo(GameState newState)
    {
        CurrentState = newState;
        EventBus.RaiseGameStateChanged(newState);
    }
}
