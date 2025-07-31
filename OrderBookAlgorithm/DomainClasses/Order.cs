using System.Text.Json.Serialization;

namespace OrderBookAlgorithm.DomainClasses;

public class Order
{
    public Guid Id { get; set; }
    public DateTime Time { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public OrderType Type { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public OrderKind Kind { get; set; }

    public decimal Amount { get; set; }

    public decimal Price { get; set; }
}
