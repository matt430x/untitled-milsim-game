using MilSim.Autoloads;
using MilSim.Data;

namespace MilSim.Entities.Buildings.Components;

public partial class ProductionComponent : Node, IProducer
{
    [Signal] public delegate void ProductionCompleteEventHandler(PackedScene unitScene);
    [Signal] public delegate void ProductionProgressChangedEventHandler(float progress);

    [Export] public int OwnerId { get; set; }

    public bool IsProducing { get; private set; }

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

    public void QueueProduction(UnitData unitData)
    {
        _queue.Enqueue(unitData);
        if (!IsProducing)
            StartNext();
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
        EmitSignal(SignalName.ProductionComplete, _current.Scene);
        _current = null;
        _progressTimer = 0f;
        IsProducing = false;

        if (_queue.Count > 0)
            StartNext();
    }
}
