using OrderBookAlgorithm.DomainClasses;

namespace OrderBookAlgorithm;

public class OrderAlgorithm : IOrderAlgorithm
{
    public List<Order> GetOrdersWithBestPrice(Order customerOrder, List<OrderBookRecord> availableOrders)
    {
        if (customerOrder == null)
        {
            throw new ArgumentNullException(nameof(customerOrder));
        }
        if (availableOrders == null)
        {
            throw new ArgumentNullException(nameof(availableOrders));
        }

        var result = new List<Order>();

        switch (customerOrder.Type)
        {
            // BUY CASE: Customer wants to buy BTC -> limited by each customer Euro account balance on the exchanges as customer can not buy more BTC as he has Euros
            case OrderType.Buy:
                {
                    var allAsks = availableOrders
                        .SelectMany(record => record.OrderBook.Asks
                            .Select(ask => new
                            {
                                ExchangeId = record.Id,
                                Order = ask.Order,
                            }))
                        .OrderBy(x => x.Order.Price) // cheapest first
                        .ToList();

                    decimal remainingAmount = customerOrder.Amount;

                    var remainingEuroPerExchange = availableOrders.ToDictionary(r => r.Id, r => r.AvailableFunds!.Euro);

                    foreach (var entry in allAsks)
                    {
                        if (remainingAmount <= 0)
                        {
                            break;
                        }

                        var euroLeft = remainingEuroPerExchange[entry.ExchangeId];
                        var maxBtcFromBudget = euroLeft / entry.Order.Price;

                        decimal amountToBuy = Math.Min(entry.Order.Amount, remainingAmount);
                        amountToBuy = Math.Min(amountToBuy, maxBtcFromBudget);

                        if (amountToBuy > 0)
                        {
                            result.Add(new Order
                            {
                                Id = entry.Order.Id,
                                Time = entry.Order.Time,
                                Kind = entry.Order.Kind,
                                Type = entry.Order.Type,
                                Price = entry.Order.Price,
                                Amount = amountToBuy
                            });

                            // Deduct EUR spent from this exchange’s remaining budget
                            remainingEuroPerExchange[entry.ExchangeId] -= amountToBuy * entry.Order.Price;
                            remainingAmount -= amountToBuy;
                        }
                    }

                    break;
                }

            // SELL CASE: Customer wants to sell BTC -> limited by customer account BTC on each exchange as customer can non sell BTC as he has
            case OrderType.Sell:
                {
                    var allBids = availableOrders
                        .SelectMany(record => record.OrderBook.Bids
                            .Select(bid => new
                            {
                                ExchangeId = record.Id,
                                Order = bid.Order
                            }))
                        .OrderByDescending(x => x.Order.Price) // highest first
                        .ToList();

                    decimal remainingAmount = customerOrder.Amount;

                    // Track remaining BTC balance per exchange (customer’s holdings)
                    var remainingBtcPerExchange = availableOrders.ToDictionary(r => r.Id, r => r.AvailableFunds!.Crypto);

                    foreach (var entry in allBids)
                    {
                        if (remainingAmount <= 0)
                        {
                            break;
                        }

                        var btcLeft = remainingBtcPerExchange[entry.ExchangeId];

                        // How much BTC can we sell from this order considering customer’s BTC on this exchange
                        decimal amountToSell = Math.Min(entry.Order.Amount, remainingAmount);
                        amountToSell = Math.Min(amountToSell, btcLeft);

                        if (amountToSell > 0)
                        {
                            result.Add(new Order
                            {
                                Id = entry.Order.Id,
                                Time = entry.Order.Time,
                                Kind = entry.Order.Kind,
                                Type = entry.Order.Type,
                                Price = entry.Order.Price,
                                Amount = amountToSell
                            });

                            // Deduct BTC sold from customer’s remaining BTC balance on this exchange
                            remainingBtcPerExchange[entry.ExchangeId] -= amountToSell;
                            remainingAmount -= amountToSell;
                        }
                    }

                    break;
                }

            default:
                throw new InvalidOperationException($"Unsupported order type: {customerOrder.Type}");
        }

        return result;
    }
}
