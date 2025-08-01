namespace OrderBookAlgorithm.DomainClasses;

public class OrderBookRecord
{
    public string Id { get; set; } = string.Empty;
    public Balance AvailableFunds { get; set; } = new Balance();

    public OrderBook OrderBook { get; set; } = new OrderBook();
}
