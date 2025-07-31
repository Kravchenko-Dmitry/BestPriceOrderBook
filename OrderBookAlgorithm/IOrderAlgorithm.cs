using OrderBookAlgorithm.DataClasses;

namespace OrderBookAlgorithm;

public interface IOrderAlgorithm
{
    List<Order> GetOrdersWithBestPrice(Order customerOrder, List<OrderBookRecord> availableOrders);
}
