using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OrderBookAlgorithm;
using OrderBookAlgorithm.DomainClasses;
using OrderBookConsole;

Console.WriteLine("=== Best Price OrderBook Console ===\n");

using var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddScoped<IOrderBookRepository, OrderBookRepository>();
        services.AddScoped<IOrderAlgorithm, OrderAlgorithm>();
        services.AddScoped<OrderManager>();
    })
    .Build();

var orderManager = host.Services.GetRequiredService<OrderManager>();

// Ask user
var selectedType = MenuHelper.GetOrderTypeFromUser(OrderType.Buy);
var amount = MenuHelper.GetOrderAmountFromUser(0.5m);

var customerOrder = new Order
{
    Id = Guid.NewGuid(),
    Time = DateTime.UtcNow,
    Type = selectedType,
    Kind = OrderKind.Limit,
    Amount = amount,
};

Console.WriteLine("\n=== Getting the Best Price OrderBooks ===\n");

// Get best price orders
var bestOrders = await orderManager.ProvideBestPriceOrdersAsync(customerOrder);

Console.WriteLine("\n=== Best Price Orders ===\n");

foreach (var order in bestOrders)
{
    Console.WriteLine($"{order.Type} {order.Amount} BTC @ {order.Price} EUR");
}

Console.WriteLine("\n=== End of Program ===\n");
