namespace MilSim.Core.Orders;

public class MoveOrder : IOrder
{
    public OrderType Type => OrderType.Move;
    public Vector2 Destination { get; }

    public MoveOrder(Vector2 destination) => Destination = destination;
}
