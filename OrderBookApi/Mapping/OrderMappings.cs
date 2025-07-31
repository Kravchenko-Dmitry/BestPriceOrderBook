using OrderBookAlgorithm.DomainClasses;
using OrderBookApi.Dto;
using OrderKind = OrderBookAlgorithm.DomainClasses.OrderKind;
using OrderType = OrderBookAlgorithm.DomainClasses.OrderType;

namespace OrderBookApi.Mapping;

public static class OrderMappings
{
    public static Order ConvertToDomainOrder(this PlaceOrderRequest request)
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
