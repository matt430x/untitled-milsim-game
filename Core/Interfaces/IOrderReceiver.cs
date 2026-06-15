namespace MilSim.Core.Interfaces;

public interface IOrderReceiver
{
    void IssueOrder(IOrder order);
    void QueueOrder(IOrder order);
    void ClearOrders();
}
