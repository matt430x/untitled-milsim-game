using MilSim.Data;

namespace MilSim.Autoloads;

/// <summary>
/// Central signal hub. All cross-system communication goes through here.
/// Subscribe: EventBus.OnUnitDied += MyHandler;
/// Unsubscribe in _ExitTree: EventBus.OnUnitDied -= MyHandler;
/// Raise: EventBus.RaiseUnitDied(unitId, playerId);
/// Only the owning system should raise its own events.
/// </summary>
public static class EventBus
{
    // --- Game State ---
    public static event Action<GameState> OnGameStateChanged;
    public static event Action OnMatchStarted;
    public static event Action<int> OnMatchEnded; // winning teamId

    // --- Economy ---
    public static event Action<int, float> OnMoneyChanged; // playerId, newAmount
    public static event Action<IIncomeSource> OnIncomeSourceAdded;
    public static event Action<IIncomeSource> OnIncomeSourceRemoved;

    // --- Units ---
    public static event Action<int, int> OnUnitSpawned; // unitId, playerId
    public static event Action<int, int> OnUnitDied;    // unitId, playerId

    // --- Buildings ---
    public static event Action<int, int, BuildingType> OnBuildingPlaced;     // buildingId, playerId, type
    public static event Action<int, int, BuildingType> OnBuildingDestroyed;  // buildingId, playerId, type
    public static event Action<int, int> OnBuildingConstructionComplete;     // buildingId, playerId

    // --- HQ & Elimination ---
    public static event Action<int> OnHqDestroyed;       // playerId
    public static event Action<int> OnPlayerEliminated;  // playerId

    // --- Research ---
    public static event Action<int, string> OnResearchStarted;   // playerId, researchId
    public static event Action<int, string> OnResearchCompleted; // playerId, researchId

    // --- Combat ---
    public static event Action<Vector3, Vector3> OnUnitFired; // from, to (world positions)

    // --- Selection ---
    public static event Action<List<int>> OnSelectionChanged; // list of selected entity ids

    // --- Players ---
    public static event Action<int> OnPlayerJoined;
    public static event Action<int> OnPlayerDisconnected;

    // --- Raise methods ---
    public static void RaiseGameStateChanged(GameState state)                          => OnGameStateChanged?.Invoke(state);
    public static void RaiseMatchStarted()                                             => OnMatchStarted?.Invoke();
    public static void RaiseMatchEnded(int winnerTeamId)                               => OnMatchEnded?.Invoke(winnerTeamId);

    public static void RaiseMoneyChanged(int playerId, float newAmount)                => OnMoneyChanged?.Invoke(playerId, newAmount);
    public static void RaiseIncomeSourceAdded(IIncomeSource source)                    => OnIncomeSourceAdded?.Invoke(source);
    public static void RaiseIncomeSourceRemoved(IIncomeSource source)                  => OnIncomeSourceRemoved?.Invoke(source);

    public static void RaiseUnitSpawned(int unitId, int playerId)                      => OnUnitSpawned?.Invoke(unitId, playerId);
    public static void RaiseUnitDied(int unitId, int playerId)                         => OnUnitDied?.Invoke(unitId, playerId);

    public static void RaiseBuildingPlaced(int id, int playerId, BuildingType type)    => OnBuildingPlaced?.Invoke(id, playerId, type);
    public static void RaiseBuildingDestroyed(int id, int playerId, BuildingType type) => OnBuildingDestroyed?.Invoke(id, playerId, type);
    public static void RaiseBuildingConstructionComplete(int id, int playerId)         => OnBuildingConstructionComplete?.Invoke(id, playerId);

    public static void RaiseHqDestroyed(int playerId)                                  => OnHqDestroyed?.Invoke(playerId);
    public static void RaisePlayerEliminated(int playerId)                             => OnPlayerEliminated?.Invoke(playerId);

    public static void RaiseResearchStarted(int playerId, string researchId)           => OnResearchStarted?.Invoke(playerId, researchId);
    public static void RaiseResearchCompleted(int playerId, string researchId)         => OnResearchCompleted?.Invoke(playerId, researchId);

    public static void RaiseUnitFired(Vector3 from, Vector3 to)                        => OnUnitFired?.Invoke(from, to);

    public static void RaiseSelectionChanged(List<int> selectedIds)                    => OnSelectionChanged?.Invoke(selectedIds);

    public static void RaisePlayerJoined(int playerId)                                 => OnPlayerJoined?.Invoke(playerId);
    public static void RaisePlayerDisconnected(int playerId)                           => OnPlayerDisconnected?.Invoke(playerId);
}
