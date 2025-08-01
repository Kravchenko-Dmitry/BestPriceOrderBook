using Microsoft.Extensions.Logging;
using OrderBookAlgorithm.DomainClasses;

namespace OrderBookAlgorithm;

public class OrderAlgorithm(ILogger<OrderAlgorithm> logger) : IOrderAlgorithm
{
    private readonly ILogger<OrderAlgorithm> _logger = logger;

    public List<Order> GetOrdersWithBestPrice(Order customerOrder, List<OrderBookRecord> availableOrders)
    {
        ValidateInputs(customerOrder, availableOrders);

        return customerOrder.Type switch
        {
            // BUY CASE: Customer wants to buy BTC -> limited by each customer Euro account balance on the exchanges as customer can not buy more BTC as he has Euros
            OrderType.Buy => ProcessBuyOrders(customerOrder, availableOrders),
            // SELL CASE: Customer wants to sell BTC -> limited by customer account's BTC amount on each exchange as customer can non sell BTC which he does not have
            OrderType.Sell => ProcessSellOrders(customerOrder, availableOrders),
            _ => throw new InvalidOperationException($"Unsupported order type: {customerOrder.Type}")
        };
    }

    private void ValidateInputs(Order customerOrder, List<OrderBookRecord> availableOrders)
    {
        if (customerOrder == null)
        {
            _logger.LogWarning("Invalid argument {ArgumentName}", nameof(customerOrder));
            throw new ArgumentNullException(nameof(customerOrder));
        }
        if (availableOrders == null)
        {
            _logger.LogWarning("Invalid argument {ArgumentName}", nameof(availableOrders));
            throw new ArgumentNullException(nameof(availableOrders));
        }
    }

    private List<Order> ProcessBuyOrders(Order customerOrder, List<OrderBookRecord> availableOrders)
    {
        var sortedAsks = GetSortedAsks(availableOrders);
        _logger.LogInformation("Found totally {OrderCount} ask order(s) in over {ExchangeCount} exchange(s) ", sortedAsks.Count(), availableOrders.Count);
        var remainingFunds = availableOrders.ToDictionary(r => r.Id, r => r.AvailableFunds!.Euro);

        return ProcessOrders(sortedAsks,
            customerOrder.Amount,
            remainingFunds,
            (order, availableFunds) => availableFunds / order.Price, // maxAmount from budget
            (order, amount) => amount * order.Price // cost calculation
        );
    }

    private List<Order> ProcessSellOrders(Order customerOrder, List<OrderBookRecord> availableOrders)
    {
        var sortedBids = GetSortedBids(availableOrders);
        _logger.LogInformation("Found totally {OrderCount} bid order(s) in over {ExchangeCount} exchange(s) ", sortedBids.Count(), availableOrders.Count);
        var remainingFunds = availableOrders.ToDictionary(r => r.Id, r => r.AvailableFunds!.Crypto);

        return ProcessOrders(sortedBids,
            customerOrder.Amount,
            remainingFunds,
            (order, availableFunds) => availableFunds, // maxAmount is the available BTC
            (order, amount) => amount // BTC deduction
        );
    }

    private static IEnumerable<(string ExchangeId, Order Order)> GetSortedAsks(List<OrderBookRecord> availableOrders)
    {
        return availableOrders
            .SelectMany(record => record.OrderBook.Asks
                .Select(ask => (ExchangeId: record.Id, Order: ask.Order)))
            .OrderBy(x => x.Order.Price);
    }

    private static IEnumerable<(string ExchangeId, Order Order)> GetSortedBids(List<OrderBookRecord> availableOrders)
    {
        return availableOrders
            .SelectMany(record => record.OrderBook.Bids
                .Select(bid => (ExchangeId: record.Id, Order: bid.Order)))
            .OrderByDescending(x => x.Order.Price);
    }

    private List<Order> ProcessOrders(
     IEnumerable<(string ExchangeId, Order Order)> sortedOrders,
     decimal remainingAmount,
     Dictionary<string, decimal> remainingFunds,
     Func<Order, decimal, decimal> calculateMaxAmount,
     Func<Order, decimal, decimal> calculateCost)
    {
        var result = new List<Order>();

        foreach (var (exchangeId, order) in sortedOrders)
        {
            if (remainingAmount <= 0)
            {
                break;
            }

            var availableFunds = remainingFunds[exchangeId];
            var maxAmountFromFunds = calculateMaxAmount(order, availableFunds);
            var amountToProcess = Math.Min(Math.Min(order.Amount, remainingAmount), maxAmountFromFunds);

            if (amountToProcess > 0)
            {
                result.Add(CreateOrderFromTemplate(order, amountToProcess));

                var cost = calculateCost(order, amountToProcess);
                remainingFunds[exchangeId] -= cost;
                remainingAmount -= amountToProcess;

                _logger.LogInformation("Order with Id {OrderId} from {ExchangeId} qualifies for original order", order.Id, exchangeId);
            }
        }

        _logger.LogInformation("Found {OrderCount} contra-side order(s) to qualify original order", result.Count);

        return result;
    }

    private static Order CreateOrderFromTemplate(Order order, decimal amount)
    {
        return new Order
        {
            Id = order.Id,
            Time = order.Time,
            Kind = order.Kind,
            Type = order.Type,
            Price = order.Price,
            Amount = amount
        };
    }
}
