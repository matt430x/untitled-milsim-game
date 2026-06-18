using MilSim.Autoloads;
using MilSim.Data;
using MilSim.Entities.Buildings.Components;
using MilSim.Entities.Buildings.Visual;
using MilSim.Entities.Units.Components;

namespace MilSim.Entities.Buildings;

public partial class BaseBuilding : StaticBody3D, IDamageable, ISelectable
{
    [Export] public BuildingData Data    { get; set; }
    [Export] public int          OwnerId { get; set; }

    public float MaxHealth     => _health.MaxHealth;
    public float CurrentHealth => _health.CurrentHealth;
    public bool  IsDead        => _health.IsDead;
    public bool  IsSelected    => _selection.IsSelected;

    protected HealthComponent       _health;
    protected SelectionComponent    _selection;
    protected IncomeComponent       _income;
    protected ProductionComponent   _production;
    protected TurretCombatComponent _turret;

    public override void _Ready()
    {
        _health     = GetNode<HealthComponent>("HealthComponent");
        _selection  = GetNode<SelectionComponent>("SelectionComponent");
        _income     = GetNodeOrNull<IncomeComponent>("IncomeComponent");
        _production = GetNodeOrNull<ProductionComponent>("ProductionComponent");
        _turret     = GetNodeOrNull<TurretCombatComponent>("TurretCombatComponent");

        if (Data != null) ApplyData(Data);

        _health.Died += OnDied;

        bool hostile = PlayerManager.Instance.AreHostile(OwnerId, PlayerManager.Instance.LocalPlayerId);
        var box = GetNodeOrNull<BuildingBoxPlaceholder>("BuildingBoxPlaceholder");
        box?.SetHostile(hostile);
        box?.SetLabel(Data != null ? Data.BuildingName : Name);

        SelectionRegistry.Register(this);

        EventBus.RaiseBuildingPlaced(
            GetInstanceId().ToString().GetHashCode(),
            OwnerId,
            Data?.BuildingType ?? BuildingType.Headquarters
        );
    }

    public override void _ExitTree()
    {
        SelectionRegistry.Unregister(this);
        _health.Died -= OnDied;
    }

    public void TakeDamage(float amount, int attackerId) => _health.TakeDamage(amount, attackerId);
    public void Select()   => _selection.Select();
    public void Deselect() => _selection.Deselect();

    public bool CanProduce => _production != null && Data?.ProducibleUnits?.Count > 0;

    public bool QueueUnit(UnitData unitData) => _production?.QueueProduction(unitData) ?? false;

    protected virtual void ApplyData(BuildingData data)
    {
        _health.MaxHealth = data.MaxHealth;
        _health.InitHealth();

        if (_selection != null) _selection.OwnerId = OwnerId;

        if (_income != null)
        {
            _income.OwnerId       = OwnerId;
            _income.IncomePerTick = data.IncomePerTick;
            _income.SetActive(true);
        }

        if (_production != null) _production.OwnerId = OwnerId;
        if (_turret     != null) _turret.LoadFromData(data);
    }

    private void OnDied(int attackerId)
    {
        int          id   = GetInstanceId().ToString().GetHashCode();
        BuildingType type = Data?.BuildingType ?? BuildingType.Headquarters;

        EventBus.RaiseBuildingDestroyed(id, OwnerId, type);
        if (type == BuildingType.Headquarters)
            EventBus.RaiseHqDestroyed(OwnerId);

        QueueFree();
    }
}
