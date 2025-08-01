using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OrderBookAlgorithm;
using OrderBookAlgorithm.DomainClasses;

Console.WriteLine("=== Best Price OrderBook Console ===");

using var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddScoped<IOrderBookRepository, OrderBookRepository>();
        services.AddScoped<IOrderAlgorithm, OrderAlgorithm>();
        services.AddScoped<OrderManager>();
    })
    .Build();

var orderManager = host.Services.GetRequiredService<OrderManager>();

var customerOrder = new Order
{
    Id = Guid.NewGuid(),
    Time = DateTime.UtcNow,
    Type = OrderType.Buy,
    Kind = OrderKind.Limit,
    Amount = 0.5m
};

// Get best price orders
var bestOrders = await orderManager.ProvideBestPriceOrdersAsync(customerOrder);

Console.WriteLine("=== Best Price Orders ===");

foreach (var order in bestOrders)
{
    Console.WriteLine($"{order.Type} {order.Amount} BTC @ {order.Price} EUR");
}
