namespace OrderBookAlgorithm.DataClasses;

public class OrderBookRecord
{
    public string Id { get; set; }
    public BalanceSheet AvailableFunds { get; set; }

    public OrderBook OrderBook { get; set; }
}
