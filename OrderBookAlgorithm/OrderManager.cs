using OrderBookAlgorithm.DomainClasses;

namespace OrderBookAlgorithm;

public class OrderManager
{
    private readonly IOrderBookRepository _orderBookRepository;
    private readonly IOrderAlgorithm _exchangeAlgorithm;

    public OrderManager(IOrderBookRepository orderBookRepository, IOrderAlgorithm orderAlgorithm)
    {
        if (orderBookRepository == null)
        {
            throw new ArgumentNullException(nameof(orderBookRepository));
        }

        if (orderAlgorithm == null)
        {
            throw new ArgumentNullException(nameof(orderAlgorithm));
        }
        _orderBookRepository = orderBookRepository;
        _exchangeAlgorithm = orderAlgorithm;
    }

    public async Task<List<Order>> ProvideBestPriceOrdersAsync(Order customerOrder)
    {
        var availableOrderRecords = await _orderBookRepository.LoadOrderBookDataAsync();
        return _exchangeAlgorithm.GetOrdersWithBestPrice(customerOrder, availableOrderRecords);
    }
}
