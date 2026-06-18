using MilSim.Data;

namespace MilSim.Core.Interfaces;

public interface IProducer
{
    bool IsProducing { get; }
    bool QueueProduction(UnitData unitData);
    void CancelProduction();
}
