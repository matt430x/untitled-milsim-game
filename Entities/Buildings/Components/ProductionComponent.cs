using MilSim.Autoloads;
using MilSim.Data;
using MilSim.Entities.Units;
using MilSim.Entities.Units.Components;

namespace MilSim.Entities.Buildings.Components;

public partial class ProductionComponent : Node, IProducer
{
    [Signal] public delegate void ProductionCompleteEventHandler(PackedScene unitScene);
    [Signal] public delegate void ProductionProgressChangedEventHandler(float progress);

    [Export] public int OwnerId { get; set; }

    public bool IsProducing  { get; private set; }
    public int  QueueCount   => _queue.Count + (IsProducing ? 1 : 0);
    public float Progress    => _current != null && _current.TrainingTime > 0f
                                ? Math.Min(_progressTimer / _current.TrainingTime, 1f) : 0f;

    private readonly Queue<UnitData> _queue = new();
    private float _progressTimer;
    private UnitData _current;

    public override void _Process(double delta)
    {
        if (!IsProducing || _current == null) return;

        _progressTimer += (float)delta;
        float progress = _progressTimer / _current.TrainingTime;
        EmitSignal(SignalName.ProductionProgressChanged, Math.Min(progress, 1f));

        if (_progressTimer >= _current.TrainingTime)
            CompleteProduction();
    }

    public bool QueueProduction(UnitData unitData)
    {
        if (!PlayerManager.Instance.TrySpendMoney(OwnerId, unitData.Cost)) return false;
        _queue.Enqueue(unitData);
        if (!IsProducing) StartNext();
        return true;
    }

    public void CancelProduction()
    {
        if (_current != null)
            PlayerManager.Instance.AddMoney(OwnerId, _current.Cost);

        _current = null;
        _progressTimer = 0f;
        IsProducing = false;

        if (_queue.Count > 0)
            StartNext();
    }

    private void StartNext()
    {
        if (_queue.Count == 0) return;
        _current = _queue.Dequeue();
        _progressTimer = 0f;
        IsProducing = true;
    }

    private void CompleteProduction()
    {
        var completed = _current;
        EmitSignal(SignalName.ProductionComplete, completed.Scene);
        _current = null;
        _progressTimer = 0f;
        IsProducing = false;

        SpawnUnit(completed);

        if (_queue.Count > 0) StartNext();
    }

    private void SpawnUnit(UnitData data)
    {
        if (data?.Scene == null) return;
        var building = GetParent<Node3D>();
        var unit = data.Scene.Instantiate<BaseUnit>();
        unit.Data = data;
        unit.GetNode<SelectionComponent>("SelectionComponent").OwnerId = OwnerId;
        GetTree().Root.GetNode<Node3D>("Game/World/Units").AddChild(unit);
        unit.GlobalPosition = building.GlobalPosition + new Vector3(2f, 0f, 0f);
    }
}
