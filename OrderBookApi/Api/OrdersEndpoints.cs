using OrderBookAlgorithm;
using OrderBookApi.Dto;
using OrderBookApi.Mapping;

namespace OrderBookApi.Api;

public static class OrdersEndpoints
{
    public static void MapOrdersEndpoints(this WebApplication app)
    {
        app.MapPost("/orders/bestprice", async (PlaceOrderRequest request, OrderManager orderManager) =>
        {
            if (!Enum.IsDefined(typeof(OrderType), request.Type))
            {
                return Results.BadRequest($"Invalid order type '{request.Type}'. Allowed values: {string.Join(", ", Enum.GetNames(typeof(OrderType)))}");
            }
            if (!Enum.IsDefined(typeof(OrderKind), request.Kind))
            {
                return Results.BadRequest($"Invalid order kind '{request.Kind}'. Allowed values: {string.Join(", ", Enum.GetNames(typeof(OrderKind)))}");
            }

            if (request.Amount <= 0)
            {
                return Results.BadRequest("Order Amount must be greater than 0");
            }
            if (request.Price <= 0)
            {
                return Results.BadRequest("Order Price must be greater than 0");
            }

            var customOrder = request.ConvertToDomainOrder();

            var bestOrders = await orderManager.ProvideBestPriceOrdersAsync(customOrder);
            return Results.Ok(bestOrders);
        })
        .WithName("GetBestPriceOrders")
        .WithOpenApi();
    }
}
