using Microsoft.Extensions.Logging;
using Moq;
using OrderBookAlgorithm.DomainClasses;

namespace OrderBookAlgorithm.Tests;

public class OrderAlgorithmTests
{
    private readonly Mock<ILogger<OrderAlgorithm>> _loggerMock;
    private readonly OrderAlgorithm _orderAlgorithm;

    public OrderAlgorithmTests()
    {
        _loggerMock = new Mock<ILogger<OrderAlgorithm>>();
        _orderAlgorithm = new OrderAlgorithm(_loggerMock.Object);
    }

    [Fact]
    public void GetOrdersWithBestPrice_WithNullCustomerOrder_ThrowsArgumentNullException()
    {
        // Arrange
        var availableOrders = new List<OrderBookRecord>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _orderAlgorithm.GetOrdersWithBestPrice(default!, availableOrders));
    }

    [Fact]
    public void GetOrdersWithBestPrice_WithNullAvailableOrders_ThrowsArgumentNullException()
    {
        // Arrange
        var customerOrder = CreateBuyOrder(1.0m);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _orderAlgorithm.GetOrdersWithBestPrice(customerOrder, default!));
    }

    [Theory]
    [InlineData((OrderType)999)]
    [InlineData((OrderType)(-1))]
    public void GetOrdersWithBestPrice_WithUnsupportedOrderType_ThrowsInvalidOperationException(OrderType orderType)
    {
        // Arrange
        var customerOrder = new Order { Type = orderType, Amount = 1.0m };
        var availableOrders = new List<OrderBookRecord>();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            _orderAlgorithm.GetOrdersWithBestPrice(customerOrder, availableOrders));
    }

    [Fact]
    public void GetOrdersWithBestPrice_BuyOrder_WithEmptyOrderBook_ReturnsEmptyList()
    {
        // Arrange
        var customerOrder = CreateBuyOrder(1.0m);
        var availableOrders = new List<OrderBookRecord>();

        // Act
        var result = _orderAlgorithm.GetOrdersWithBestPrice(customerOrder, availableOrders);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetOrdersWithBestPrice_SellOrder_WithEmptyOrderBook_ReturnsEmptyList()
    {
        // Arrange
        var customerOrder = CreateSellOrder(1.0m);
        var availableOrders = new List<OrderBookRecord>();

        // Act
        var result = _orderAlgorithm.GetOrdersWithBestPrice(customerOrder, availableOrders);

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData(1.0, 1.0, 49000, 1.0)] // Exact match
    [InlineData(2.0, 1.0, 49000, 1.0)] // Customer wants more, limited by available
    [InlineData(0.5, 1.0, 49000, 0.5)] // Customer wants less
    public void GetOrdersWithBestPrice_BuyOrder_SingleExchange_ReturnsCorrectAmount(
        decimal customerAmount, decimal askAmount, decimal askPrice, decimal expectedAmount)
    {
        // Arrange
        var customerOrder = CreateBuyOrder(customerAmount);
        var availableOrders = new List<OrderBookRecord>
            {
                CreateOrderBookRecord("Exchange1",
                    euroFunds: 100000m,
                    cryptoFunds: 2.0m,
                    asks: [(askAmount, askPrice)])
            };

        // Act
        var result = _orderAlgorithm.GetOrdersWithBestPrice(customerOrder, availableOrders);

        // Assert
        Assert.Single(result);
        Assert.Equal(expectedAmount, result[0].Amount);
        Assert.Equal(askPrice, result[0].Price);
    }

    [Theory]
    [InlineData(1.0, 1.0, 51000, 1.0)] // Exact match
    [InlineData(2.0, 1.0, 51000, 1.0)] // Customer wants more, limited by available
    [InlineData(0.5, 1.0, 51000, 0.5)] // Customer wants less
    public void GetOrdersWithBestPrice_SellOrder_SingleExchange_ReturnsCorrectAmount(
        decimal customerAmount, decimal bidAmount, decimal bidPrice, decimal expectedAmount)
    {
        // Arrange
        var customerOrder = CreateSellOrder(customerAmount);
        var availableOrders = new List<OrderBookRecord>
            {
                CreateOrderBookRecord("Exchange1",
                    euroFunds: 100000m,
                    cryptoFunds: 2.0m,
                    bids: [(bidAmount, bidPrice)])
            };

        // Act
        var result = _orderAlgorithm.GetOrdersWithBestPrice(customerOrder, availableOrders);

        // Assert
        Assert.Single(result);
        Assert.Equal(expectedAmount, result[0].Amount);
        Assert.Equal(bidPrice, result[0].Price);
    }

    [Fact]
    public void GetOrdersWithBestPrice_BuyOrder_MultipleExchanges_SelectsCheapestFirst()
    {
        // Arrange
        var customerOrder = CreateBuyOrder(2.0m);
        var availableOrders = new List<OrderBookRecord>
            {
                CreateOrderBookRecord("Exchange1",
                    euroFunds: 100000m,
                    cryptoFunds: 2.0m,
                    asks: [(1.0m, 52000m)]),
                CreateOrderBookRecord("Exchange2",
                    euroFunds: 100000m,
                    cryptoFunds: 2.0m,
                    asks: [(1.0m, 50000m)]) // Cheaper
            };

        // Act
        var result = _orderAlgorithm.GetOrdersWithBestPrice(customerOrder, availableOrders);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(50000m, result[0].Price); // Cheapest first
        Assert.Equal(52000m, result[1].Price);
    }

    [Fact]
    public void GetOrdersWithBestPrice_SellOrder_MultipleExchanges_SelectsHighestFirst()
    {
        // Arrange
        var customerOrder = CreateSellOrder(2.0m);
        var availableOrders = new List<OrderBookRecord>
            {
                CreateOrderBookRecord("Exchange1",
                    euroFunds: 100000m,
                    cryptoFunds: 2.0m,
                    bids: [(1.0m, 51000m)]),
                CreateOrderBookRecord("Exchange2",
                    euroFunds: 100000m,
                    cryptoFunds: 2.0m,
                    bids: [(1.0m, 53000m)]) // Higher
            };

        // Act
        var result = _orderAlgorithm.GetOrdersWithBestPrice(customerOrder, availableOrders);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(53000m, result[0].Price); // Highest first
        Assert.Equal(51000m, result[1].Price);
    }

    [Fact]
    public void GetOrdersWithBestPrice_BuyOrder_LimitedByEuroFunds_ReturnsPartialFill()
    {
        // Arrange
        var customerOrder = CreateBuyOrder(2.0m);
        var availableOrders = new List<OrderBookRecord>
            {
                CreateOrderBookRecord("Exchange1",
                    euroFunds: 50000m, // Only enough for 1 BTC at 50000 EUR
                    cryptoFunds: 2.0m,
                    asks: [(2.0m, 50000m)])
            };

        // Act
        var result = _orderAlgorithm.GetOrdersWithBestPrice(customerOrder, availableOrders);

        // Assert
        Assert.Single(result);
        Assert.Equal(1.0m, result[0].Amount); // Limited by funds
    }

    [Fact]
    public void GetOrdersWithBestPrice_SellOrder_LimitedByCryptoFunds_ReturnsPartialFill()
    {
        // Arrange
        var customerOrder = CreateSellOrder(2.0m);
        var availableOrders = new List<OrderBookRecord>
            {
                CreateOrderBookRecord("Exchange1",
                    euroFunds: 200000m,
                    cryptoFunds: 1.0m, // Only 1 BTC available
                    bids: [(2.0m, 50000m)])
            };

        // Act
        var result = _orderAlgorithm.GetOrdersWithBestPrice(customerOrder, availableOrders);

        // Assert
        Assert.Single(result);
        Assert.Equal(1.0m, result[0].Amount); // Limited by crypto funds
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    public void GetOrdersWithBestPrice_WithZeroOrNegativeCustomerAmount_ReturnsEmptyList(decimal amount)
    {
        // Arrange
        var customerOrder = CreateBuyOrder(amount);
        var availableOrders = new List<OrderBookRecord>
            {
                CreateOrderBookRecord("Exchange1",
                    euroFunds: 100000m,
                    cryptoFunds: 2.0m,
                    asks: [(1.0m, 49000m)])
            };

        // Act
        var result = _orderAlgorithm.GetOrdersWithBestPrice(customerOrder, availableOrders);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetOrdersWithBestPrice_ResultOrdersHaveCorrectProperties()
    {
        // Arrange
        var customerOrder = CreateBuyOrder(1.0m);
        var originalOrder = new Order
        {
            Id = Guid.NewGuid(),
            Time = DateTime.Now,
            Kind = OrderKind.Limit,
            Type = OrderType.Sell,
            Price = 49000m,
            Amount = 1.0m
        };

        var availableOrders = new List<OrderBookRecord>
            {
                new()
                {
                    Id = "Exchange1",
                    AvailableFunds = new Balance { Euro = 100000m, Crypto = 2.0m },
                    OrderBook = new OrderBook
                    {
                        Asks =
                        [
                            new() { Order = originalOrder }
                        ],
                        Bids = []
                    }
                }
            };

        // Act
        var result = _orderAlgorithm.GetOrdersWithBestPrice(customerOrder, availableOrders);

        // Assert
        Assert.Single(result);
        var resultOrder = result[0];
        Assert.Equal(originalOrder.Id, resultOrder.Id);
        Assert.Equal(originalOrder.Time, resultOrder.Time);
        Assert.Equal(originalOrder.Kind, resultOrder.Kind);
        Assert.Equal(originalOrder.Type, resultOrder.Type);
        Assert.Equal(originalOrder.Price, resultOrder.Price);
        Assert.Equal(1.0m, resultOrder.Amount); // Amount from customer order
    }

    // Helper methods
    private static Order CreateBuyOrder(decimal amount) =>
        new() { Type = OrderType.Buy, Amount = amount };

    private static Order CreateSellOrder(decimal amount) =>
        new() { Type = OrderType.Sell, Amount = amount };

    private static OrderBookRecord CreateOrderBookRecord(
        string exchangeId,
        decimal euroFunds,
        decimal cryptoFunds,
        (decimal Amount, decimal Price)[]? asks = null,
        (decimal Amount, decimal Price)[]? bids = null)
    {
        var askEntries = asks?.Select(ask => new OrderEntry
        {
            Order = new Order
            {
                Id = Guid.NewGuid(),
                Type = OrderType.Sell,
                Amount = ask.Amount,
                Price = ask.Price,
                Time = DateTime.Now,
                Kind = OrderKind.Limit
            }
        }).ToList() ?? [];

        var bidEntries = bids?.Select(bid => new OrderEntry
        {
            Order = new Order
            {
                Id = Guid.NewGuid(),
                Type = OrderType.Buy,
                Amount = bid.Amount,
                Price = bid.Price,
                Time = DateTime.Now,
                Kind = OrderKind.Limit
            }
        }).ToList() ?? [];

        return new OrderBookRecord
        {
            Id = exchangeId,
            AvailableFunds = new Balance { Euro = euroFunds, Crypto = cryptoFunds },
            OrderBook = new OrderBook
            {
                Asks = askEntries,
                Bids = bidEntries
            }
        };
    }
}
