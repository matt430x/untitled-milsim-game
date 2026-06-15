using MilSim.Autoloads;
using MilSim.Data;
using MilSim.Entities.Units.Components;

namespace MilSim.Entities.Units;

public partial class BaseUnit : CharacterBody2D, IDamageable, ISelectable, IOrderReceiver
{
    [Export] public UnitData Data { get; set; }

    public int OwnerId => _selection.OwnerId;
    public float MaxHealth => _health.MaxHealth;
    public float CurrentHealth => _health.CurrentHealth;
    public bool IsDead => _health.IsDead;
    public bool IsSelected => _selection.IsSelected;

    protected HealthComponent _health;
    protected MovementComponent _movement;
    protected CombatComponent _combat;
    protected SelectionComponent _selection;

    private readonly Queue<IOrder> _orderQueue = new();
    private IOrder _currentOrder;

    public override void _Ready()
    {
        _health    = GetNode<HealthComponent>("HealthComponent");
        _movement  = GetNode<MovementComponent>("MovementComponent");
        _selection = GetNode<SelectionComponent>("SelectionComponent");

        // CombatComponent is optional — Builders have none
        _combat = GetNodeOrNull<CombatComponent>("CombatComponent");

        if (Data != null)
            ApplyData(Data);

        _health.Died += OnDied;
    }

    public override void _ExitTree()
    {
        _health.Died -= OnDied;
    }

    // --- IDamageable ---
    public void TakeDamage(float amount, int attackerId) => _health.TakeDamage(amount, attackerId);

    // --- ISelectable ---
    public void Select()   => _selection.Select();
    public void Deselect() => _selection.Deselect();

    // --- IOrderReceiver ---
    public void IssueOrder(IOrder order)
    {
        _orderQueue.Clear();
        _currentOrder = order;
        ExecuteOrder(order);
    }

    public void QueueOrder(IOrder order)
    {
        _orderQueue.Enqueue(order);
    }

    public void ClearOrders()
    {
        _orderQueue.Clear();
        _currentOrder = null;
        _movement?.Stop();
        _combat?.ClearTarget();
    }

    protected virtual void ExecuteOrder(IOrder order) { }

    protected virtual void ApplyData(UnitData data)
    {
        _health.MaxHealth = data.MaxHealth;
        if (_movement != null) _movement.MoveSpeed = data.MoveSpeed;
        if (_combat != null)   _combat.LoadFromData(data);
    }

    private void OnDied(int attackerId)
    {
        EventBus.RaiseUnitDied(GetInstanceId().ToString().GetHashCode(), OwnerId);
        QueueFree();
    }
}
