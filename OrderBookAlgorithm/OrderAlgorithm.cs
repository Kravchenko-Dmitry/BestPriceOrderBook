using OrderBookAlgorithm.DomainClasses;

namespace OrderBookAlgorithm;

public class OrderAlgorithm : IOrderAlgorithm
{
    public List<Order> GetOrdersWithBestPrice(Order customerOrder, List<OrderBookRecord> availableOrders)
    {
        var dummyOrders = new List<Order>
    {
        new Order
        {
            Id = Guid.NewGuid(),
            Time = DateTime.UtcNow,
            Type = OrderType.Buy,
            Kind = OrderKind.Limit,
            Amount = 0.01m,
            Price = 57226.46m
        },
        new Order
        {
            Id = Guid.NewGuid(),
            Time = DateTime.UtcNow.AddMinutes(-5),
            Type = OrderType.Sell,
            Kind = OrderKind.Market,
            Amount = 0.5m,
            Price = 57230.12m
        }
    };

        return dummyOrders;
    }
}
