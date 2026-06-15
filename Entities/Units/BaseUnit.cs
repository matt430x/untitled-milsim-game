using MilSim.Autoloads;
using MilSim.Core.Orders;
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
        _movement.ArrivedAtDestination += OnArrivedAtDestination;

        SelectionRegistry.Register(this);
    }

    public override void _ExitTree()
    {
        SelectionRegistry.Unregister(this);
        _health.Died -= OnDied;
        _movement.ArrivedAtDestination -= OnArrivedAtDestination;
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
        // If idle, start immediately rather than silently doing nothing
        if (_currentOrder == null && _orderQueue.Count == 0)
            IssueOrder(order);
        else
            _orderQueue.Enqueue(order);
    }

    public void ClearOrders()
    {
        _orderQueue.Clear();
        _currentOrder = null;
        _movement?.Stop();
        _combat?.ClearTarget();
    }

    protected virtual void ExecuteOrder(IOrder order)
    {
        switch (order)
        {
            case MoveOrder move:
                _movement?.MoveTo(move.Destination);
                break;
        }
    }

    private void OnArrivedAtDestination()
    {
        _currentOrder = null;
        if (_orderQueue.TryDequeue(out IOrder next))
        {
            _currentOrder = next;
            ExecuteOrder(next);
        }
    }

    /// Returns all pending destinations in order: current move target first, then queued waypoints.
    public List<Vector2> GetRoute()
    {
        var pts = new List<Vector2>();
        if (_currentOrder is MoveOrder mo)
            pts.Add(mo.Destination);
        foreach (var order in _orderQueue)
            if (order is MoveOrder wp)
                pts.Add(wp.Destination);
        return pts;
    }

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
