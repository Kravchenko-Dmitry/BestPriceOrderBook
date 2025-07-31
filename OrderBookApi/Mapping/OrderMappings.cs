using OrderBookAlgorithm.DataClasses;
using OrderBookApi.DTO;
using OrderKind = OrderBookAlgorithm.DataClasses.OrderKind;
using OrderType = OrderBookAlgorithm.DataClasses.OrderType;

namespace OrderBookApi.Mapping;

public static class OrderMappings
{
    public static Order ToDomainOrder(this PlaceOrderRequest request)
    {
        return new Order
        {
            Id = Guid.NewGuid(),
            Time = DateTime.UtcNow,
            Type = (OrderType)request.Type,
            Kind = (OrderKind)request.Kind,
            Amount = request.Amount,
            Price = request.Price
        };
    }
}
