namespace OrderBookApi.DTO;

public record PlaceOrderRequest(
    OrderType Type,
    OrderKind Kind,
    decimal Amount,
    decimal Price
);
