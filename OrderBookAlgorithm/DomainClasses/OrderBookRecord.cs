namespace OrderBookAlgorithm.DomainClasses;

public class OrderBookRecord
{
    public string Id { get; set; } = string.Empty;
    public Balance? AvailableFunds { get; set; }

    public OrderBook? OrderBook { get; set; }
}
