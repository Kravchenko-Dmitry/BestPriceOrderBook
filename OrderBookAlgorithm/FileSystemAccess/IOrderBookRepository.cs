using OrderBookAlgorithm.DomainClasses;

namespace OrderBookAlgorithm.FileSystemAccess;

public interface IOrderBookRepository
{
    Task<List<OrderBookRecord>> LoadOrderBookDataAsync();
}
