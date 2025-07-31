namespace OrderBookApi.Dto;

public record PlaceOrderRequest(
    OrderType Type,
    OrderKind Kind,
    decimal Amount,
    decimal Price
);
