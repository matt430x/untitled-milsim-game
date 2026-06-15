using MilSim.Data;

namespace MilSim.Core.Interfaces;

public interface IProducer
{
    bool IsProducing { get; }
    void QueueProduction(UnitData unitData);
    void CancelProduction();
}
