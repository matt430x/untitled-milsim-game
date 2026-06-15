using MilSim.Autoloads;

namespace MilSim.Systems;

/// <summary>
/// Drives all passive income. On each tick, sums every registered income source
/// per player and credits their account via PlayerManager.
/// Income sources (Powerplants, Oil Rigs) self-register through IncomeComponent.
/// </summary>
public partial class EconomySystem : Node
{
    [Export] public float TickIntervalSeconds { get; set; } = 1f;

    private float _tickTimer;
    private readonly Dictionary<int, List<IIncomeSource>> _sources = new();

    public override void _Ready()
    {
        EventBus.OnIncomeSourceAdded   += RegisterSource;
        EventBus.OnIncomeSourceRemoved += UnregisterSource;
        EventBus.OnGameStateChanged    += OnGameStateChanged;
    }

    public override void _ExitTree()
    {
        EventBus.OnIncomeSourceAdded   -= RegisterSource;
        EventBus.OnIncomeSourceRemoved -= UnregisterSource;
        EventBus.OnGameStateChanged    -= OnGameStateChanged;
    }

    public override void _Process(double delta)
    {
        if (GameManager.Instance?.CurrentState != GameState.InGame) return;

        _tickTimer += (float)delta;
        if (_tickTimer < TickIntervalSeconds) return;

        _tickTimer = 0f;
        ProcessIncomeTick();
    }

    private void RegisterSource(IIncomeSource source)
    {
        if (!_sources.ContainsKey(source.OwnerId))
            _sources[source.OwnerId] = new List<IIncomeSource>();

        _sources[source.OwnerId].Add(source);
    }

    private void UnregisterSource(IIncomeSource source)
    {
        if (_sources.TryGetValue(source.OwnerId, out var list))
            list.Remove(source);
    }

    private void ProcessIncomeTick()
    {
        foreach (var (playerId, sources) in _sources)
        {
            float total = 0f;
            foreach (var source in sources)
                if (source.IsActive)
                    total += source.IncomePerTick;

            if (total > 0f)
                PlayerManager.Instance.AddMoney(playerId, total);
        }
    }

    private void OnGameStateChanged(GameState state)
    {
        if (state != GameState.InGame)
            _sources.Clear();
    }
}
