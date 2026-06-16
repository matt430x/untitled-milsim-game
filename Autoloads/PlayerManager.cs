using MilSim.Autoloads;

namespace MilSim.Autoloads;

public partial class PlayerManager : Node
{
    public static PlayerManager Instance { get; private set; }

    public int LocalPlayerId => NetworkManager.Instance?.LocalPlayerId ?? 1;

    private readonly Dictionary<int, PlayerContext> _players = new();

    public override void _Ready()
    {
        Instance = this;
        EventBus.OnPlayerDisconnected += HandlePlayerDisconnected;
    }

    public override void _ExitTree()
    {
        EventBus.OnPlayerDisconnected -= HandlePlayerDisconnected;
    }

    public void RegisterPlayer(PlayerContext context)
    {
        _players[context.PlayerId] = context;
        EventBus.RaisePlayerJoined(context.PlayerId);
    }

    public PlayerContext GetPlayer(int playerId)
    {
        return _players.TryGetValue(playerId, out var ctx) ? ctx : null;
    }

    public IEnumerable<PlayerContext> GetAllPlayers() => _players.Values;

    public IEnumerable<PlayerContext> GetActivePlayers()
    {
        foreach (var p in _players.Values)
            if (!p.IsEliminated && !p.IsDisconnected)
                yield return p;
    }

    /// Owners on different teams are hostile. Owners with no registered PlayerContext
    /// (e.g. prototype scenes with no player bootstrap yet) default to hostile unless
    /// their OwnerId matches exactly.
    public bool AreHostile(int ownerIdA, int ownerIdB)
    {
        if (ownerIdA == ownerIdB) return false;

        var a = GetPlayer(ownerIdA);
        var b = GetPlayer(ownerIdB);
        if (a == null || b == null) return true;

        return a.TeamId != b.TeamId;
    }

    public bool TrySpendMoney(int playerId, float amount)
    {
        var player = GetPlayer(playerId);
        if (player == null || player.Money < amount)
            return false;

        player.Money -= amount;
        EventBus.RaiseMoneyChanged(playerId, player.Money);
        return true;
    }

    public void AddMoney(int playerId, float amount)
    {
        var player = GetPlayer(playerId);
        if (player == null) return;

        player.Money += amount;
        EventBus.RaiseMoneyChanged(playerId, player.Money);
    }

    public void EliminatePlayer(int playerId)
    {
        var player = GetPlayer(playerId);
        if (player == null) return;

        player.IsEliminated = true;
        EventBus.RaisePlayerEliminated(playerId);
        CheckWinCondition();
    }

    private void HandlePlayerDisconnected(int playerId)
    {
        var player = GetPlayer(playerId);
        if (player != null)
            player.IsDisconnected = true;
    }

    private void CheckWinCondition()
    {
        var remaining = new List<int>();
        foreach (var p in _players.Values)
            if (!p.IsEliminated)
                remaining.Add(p.TeamId);

        var distinctTeams = new HashSet<int>(remaining);
        if (distinctTeams.Count == 1)
            GameManager.Instance.EndMatch(remaining[0]);
    }
}
