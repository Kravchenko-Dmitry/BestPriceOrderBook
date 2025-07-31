using OrderBookAlgorithm.DataClasses;

namespace OrderBookAlgorithm;

public interface IOrderBookRepository
{
    Task<List<OrderBookRecord>> LoadOrderBookDataAsync();
}
