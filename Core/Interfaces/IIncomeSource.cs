namespace MilSim.Core.Interfaces;

public interface IIncomeSource
{
    int OwnerId { get; }
    float IncomePerTick { get; }
    bool IsActive { get; }
}
