using OrderBookAlgorithm.DomainClasses;

namespace OrderBookAlgorithm;

public interface IOrderBookRepository
{
    Task<List<OrderBookRecord>> LoadOrderBookDataAsync();
}
