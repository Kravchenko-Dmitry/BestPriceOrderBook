namespace OrderBookApi.Dto;

public record PlaceOrderRequest(
    OrderType Type,
    decimal Amount
);
