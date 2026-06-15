using MilSim.Autoloads;
using MilSim.Data;
using MilSim.Entities.Buildings.Components;
using MilSim.Entities.Units.Components;

namespace MilSim.Entities.Buildings;

public partial class BaseBuilding : StaticBody2D, IDamageable, ISelectable
{
    [Export] public BuildingData Data { get; set; }
    [Export] public int OwnerId { get; set; }

    public float MaxHealth => _health.MaxHealth;
    public float CurrentHealth => _health.CurrentHealth;
    public bool IsDead => _health.IsDead;
    public bool IsSelected => _selection.IsSelected;

    protected HealthComponent _health;
    protected SelectionComponent _selection;
    protected IncomeComponent _income;
    protected ProductionComponent _production;

    public override void _Ready()
    {
        _health    = GetNode<HealthComponent>("HealthComponent");
        _selection = GetNode<SelectionComponent>("SelectionComponent");

        // Optional components — not all buildings have these
        _income     = GetNodeOrNull<IncomeComponent>("IncomeComponent");
        _production = GetNodeOrNull<ProductionComponent>("ProductionComponent");

        if (Data != null)
            ApplyData(Data);

        _health.Died += OnDied;

        EventBus.RaiseBuildingPlaced(
            GetInstanceId().ToString().GetHashCode(),
            OwnerId,
            Data?.BuildingType ?? BuildingType.Headquarters
        );
    }

    public override void _ExitTree()
    {
        _health.Died -= OnDied;
    }

    public void TakeDamage(float amount, int attackerId) => _health.TakeDamage(amount, attackerId);

    public void Select()   => _selection.Select();
    public void Deselect() => _selection.Deselect();

    protected virtual void ApplyData(BuildingData data)
    {
        _health.MaxHealth = data.MaxHealth;
        if (_selection != null) _selection.OwnerId = OwnerId;
    }

    private void OnDied(int attackerId)
    {
        int id = GetInstanceId().ToString().GetHashCode();
        BuildingType type = Data?.BuildingType ?? BuildingType.Headquarters;

        EventBus.RaiseBuildingDestroyed(id, OwnerId, type);

        if (type == BuildingType.Headquarters)
            EventBus.RaiseHqDestroyed(OwnerId);

        QueueFree();
    }
}
