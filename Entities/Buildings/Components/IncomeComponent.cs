using MilSim.Autoloads;

namespace MilSim.Entities.Buildings.Components;

public partial class IncomeComponent : Node, IIncomeSource
{
    [Export] public float IncomePerTick { get; set; }
    [Export] public int OwnerId { get; set; }

    public bool IsActive { get; private set; }

    public override void _ExitTree()
    {
        if (IsActive)
            EventBus.RaiseIncomeSourceRemoved(this);
    }

    public void SetActive(bool active)
    {
        if (IsActive == active) return;
        IsActive = active;

        if (active)
            EventBus.RaiseIncomeSourceAdded(this);
        else
            EventBus.RaiseIncomeSourceRemoved(this);
    }
}
