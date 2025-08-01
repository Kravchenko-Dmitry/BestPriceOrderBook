using OrderBookAlgorithm.DomainClasses;

namespace OrderBookAlgorithm;

public class OrderAlgorithm : IOrderAlgorithm
{
    public List<Order> GetOrdersWithBestPrice(Order customerOrder, List<OrderBookRecord> availableOrders)
    {
        ValidateInputs(customerOrder, availableOrders);

        switch (customerOrder.Type)
        {
            // BUY CASE: Customer wants to buy BTC -> limited by each customer Euro account balance on the exchanges as customer can not buy more BTC as he has Euros
            case OrderType.Buy:
                {
                    return ProcessBuyOrders(customerOrder, availableOrders);
                }

            // SELL CASE: Customer wants to sell BTC -> limited by customer account's BTC amount on each exchange as customer can non sell BTC which he does not have
            case OrderType.Sell:
                {
                    return ProcessSellOrders(customerOrder, availableOrders);
                }

            default:
                throw new InvalidOperationException($"Unsupported order type: {customerOrder.Type}");
        }
    }

    private static void ValidateInputs(Order customerOrder, List<OrderBookRecord> availableOrders)
    {
        if (customerOrder == null)
        {
            throw new ArgumentNullException(nameof(customerOrder));
        }
        if (availableOrders == null)
        {
            throw new ArgumentNullException(nameof(availableOrders));
        }
    }

    private static List<Order> ProcessBuyOrders(Order customerOrder, List<OrderBookRecord> availableOrders)
    {
        var sortedAsks = GetSortedAsks(availableOrders);
        var remainingFunds = availableOrders.ToDictionary(r => r.Id, r => r.AvailableFunds!.Euro);

        return ProcessOrders(sortedAsks,
            customerOrder.Amount,
            remainingFunds,
            (order, availableFunds) => availableFunds / order.Price, // maxAmount from budget
            (order, amount) => amount * order.Price // cost calculation
        );
    }

    private static List<Order> ProcessSellOrders(Order customerOrder, List<OrderBookRecord> availableOrders)
    {
        var sortedBids = GetSortedBids(availableOrders);
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

    private static List<Order> ProcessOrders(
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
            }
        }

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
