using System.Text.Json.Serialization;

namespace OrderBookApi.Dto;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OrderType
{
    Buy = 1,
    Sell = 2
}
