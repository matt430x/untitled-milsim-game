namespace MilSim.Core.Orders;

public class MoveOrder : IOrder
{
    public OrderType Type => OrderType.Move;
    public Vector3 Destination { get; }
    public MoveOrder(Vector3 destination) => Destination = destination;
}
