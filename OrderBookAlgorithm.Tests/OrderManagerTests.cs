using Moq;
using OrderBookAlgorithm.DomainClasses;

namespace OrderBookAlgorithm.Tests;

public class OrderManagerTests
{
    private readonly Mock<IOrderBookRepository> _orderBookRepositoryMock;
    private readonly Mock<IOrderAlgorithm> _orderAlgorithmMock;
    private readonly OrderManager _orderManager;

    public OrderManagerTests()
    {
        _orderBookRepositoryMock = new Mock<IOrderBookRepository>();
        _orderAlgorithmMock = new Mock<IOrderAlgorithm>();
        _orderManager = new OrderManager(_orderBookRepositoryMock.Object, _orderAlgorithmMock.Object);
    }

    [Fact]
    public void Constructor_WithNullOrderBookRepository_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new OrderManager(null!, _orderAlgorithmMock.Object));
    }

    [Fact]
    public void Constructor_WithNullOrderAlgorithm_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new OrderManager(_orderBookRepositoryMock.Object, null!));
    }

    [Fact]
    public void Constructor_WithBothParametersNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new OrderManager(null!, null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Act
        var orderManager = new OrderManager(_orderBookRepositoryMock.Object, _orderAlgorithmMock.Object);

        // Assert
        Assert.NotNull(orderManager);
    }

    [Fact]
    public async Task ProvideBestPriceOrdersAsync_CallsRepositoryLoadOrderBookDataAsync()
    {
        // Arrange
        var customerOrder = CreateOrder(OrderType.Buy, 1.0m, 50000m);
        var orderBookRecords = new List<OrderBookRecord>();

        _orderBookRepositoryMock
            .Setup(x => x.LoadOrderBookDataAsync())
            .ReturnsAsync(orderBookRecords);

        _orderAlgorithmMock
            .Setup(x => x.GetOrdersWithBestPrice(customerOrder, orderBookRecords))
            .Returns(new List<Order>());

        // Act
        await _orderManager.ProvideBestPriceOrdersAsync(customerOrder);

        // Assert
        _orderBookRepositoryMock.Verify(x => x.LoadOrderBookDataAsync(), Times.Once);
    }

    [Fact]
    public async Task ProvideBestPriceOrdersAsync_CallsAlgorithmGetOrdersWithBestPrice()
    {
        // Arrange
        var customerOrder = CreateOrder(OrderType.Buy, 1.0m, 50000m);
        var orderBookRecords = new List<OrderBookRecord>
        {
            CreateOrderBookRecord("Exchange1", 100000m, 2.0m)
        };

        _orderBookRepositoryMock
            .Setup(x => x.LoadOrderBookDataAsync())
            .ReturnsAsync(orderBookRecords);

        _orderAlgorithmMock
            .Setup(x => x.GetOrdersWithBestPrice(customerOrder, orderBookRecords))
            .Returns(new List<Order>());

        // Act
        await _orderManager.ProvideBestPriceOrdersAsync(customerOrder);

        // Assert
        _orderAlgorithmMock.Verify(
            x => x.GetOrdersWithBestPrice(customerOrder, orderBookRecords),
            Times.Once);
    }

    [Fact]
    public async Task ProvideBestPriceOrdersAsync_ReturnsResultFromAlgorithm()
    {
        // Arrange
        var customerOrder = CreateOrder(OrderType.Buy, 1.0m, 50000m);
        var orderBookRecords = new List<OrderBookRecord>();
        var expectedResult = new List<Order>
        {
            CreateOrder(OrderType.Sell, 0.5m, 49000m),
            CreateOrder(OrderType.Sell, 0.5m, 49500m)
        };

        _orderBookRepositoryMock
            .Setup(x => x.LoadOrderBookDataAsync())
            .ReturnsAsync(orderBookRecords);

        _orderAlgorithmMock
            .Setup(x => x.GetOrdersWithBestPrice(customerOrder, orderBookRecords))
            .Returns(expectedResult);

        // Act
        var result = await _orderManager.ProvideBestPriceOrdersAsync(customerOrder);

        // Assert
        Assert.Equal(expectedResult, result);
        Assert.Equal(2, result.Count);
        Assert.Equal(0.5m, result[0].Amount);
        Assert.Equal(49000m, result[0].Price);
    }

    [Theory]
    [InlineData(OrderType.Buy)]
    [InlineData(OrderType.Sell)]
    public async Task ProvideBestPriceOrdersAsync_WithDifferentOrderTypes_CallsServicesCorrectly(OrderType orderType)
    {
        // Arrange
        var customerOrder = CreateOrder(orderType, 1.0m, 50000m);
        var orderBookRecords = new List<OrderBookRecord>();

        _orderBookRepositoryMock
            .Setup(x => x.LoadOrderBookDataAsync())
            .ReturnsAsync(orderBookRecords);

        _orderAlgorithmMock
            .Setup(x => x.GetOrdersWithBestPrice(customerOrder, orderBookRecords))
            .Returns(new List<Order>());

        // Act
        await _orderManager.ProvideBestPriceOrdersAsync(customerOrder);

        // Assert
        _orderBookRepositoryMock.Verify(x => x.LoadOrderBookDataAsync(), Times.Once);
        _orderAlgorithmMock.Verify(
            x => x.GetOrdersWithBestPrice(customerOrder, orderBookRecords),
            Times.Once);
    }

    [Fact]
    public async Task ProvideBestPriceOrdersAsync_WhenRepositoryThrows_PropagatesException()
    {
        // Arrange
        var customerOrder = CreateOrder(OrderType.Buy, 1.0m, 50000m);
        var expectedException = new InvalidOperationException("Repository error");

        _orderBookRepositoryMock
            .Setup(x => x.LoadOrderBookDataAsync())
            .ThrowsAsync(expectedException);

        // Act & Assert
        var actualException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _orderManager.ProvideBestPriceOrdersAsync(customerOrder));

        Assert.Equal(expectedException.Message, actualException.Message);
    }

    [Fact]
    public async Task ProvideBestPriceOrdersAsync_WhenAlgorithmThrows_PropagatesException()
    {
        // Arrange
        var customerOrder = CreateOrder(OrderType.Buy, 1.0m, 50000m);
        var orderBookRecords = new List<OrderBookRecord>();
        var expectedException = new ArgumentException("Algorithm error");

        _orderBookRepositoryMock
            .Setup(x => x.LoadOrderBookDataAsync())
            .ReturnsAsync(orderBookRecords);

        _orderAlgorithmMock
            .Setup(x => x.GetOrdersWithBestPrice(customerOrder, orderBookRecords))
            .Throws(expectedException);

        // Act & Assert
        var actualException = await Assert.ThrowsAsync<ArgumentException>(
            () => _orderManager.ProvideBestPriceOrdersAsync(customerOrder));

        Assert.Equal(expectedException.Message, actualException.Message);
    }

    [Fact]
    public async Task ProvideBestPriceOrdersAsync_WithEmptyOrderBookRecords_ReturnsEmptyResult()
    {
        // Arrange
        var customerOrder = CreateOrder(OrderType.Buy, 1.0m, 50000m);
        var emptyOrderBookRecords = new List<OrderBookRecord>();
        var emptyResult = new List<Order>();

        _orderBookRepositoryMock
            .Setup(x => x.LoadOrderBookDataAsync())
            .ReturnsAsync(emptyOrderBookRecords);

        _orderAlgorithmMock
            .Setup(x => x.GetOrdersWithBestPrice(customerOrder, emptyOrderBookRecords))
            .Returns(emptyResult);

        // Act
        var result = await _orderManager.ProvideBestPriceOrdersAsync(customerOrder);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ProvideBestPriceOrdersAsync_CallsServicesInCorrectOrder()
    {
        // Arrange
        var customerOrder = CreateOrder(OrderType.Buy, 1.0m, 50000m);
        var orderBookRecords = new List<OrderBookRecord>();
        var callSequence = new List<string>();

        _orderBookRepositoryMock
            .Setup(x => x.LoadOrderBookDataAsync())
            .ReturnsAsync(orderBookRecords)
            .Callback(() => callSequence.Add("Repository"));

        _orderAlgorithmMock
            .Setup(x => x.GetOrdersWithBestPrice(customerOrder, orderBookRecords))
            .Returns(new List<Order>())
            .Callback(() => callSequence.Add("Algorithm"));

        // Act
        await _orderManager.ProvideBestPriceOrdersAsync(customerOrder);

        // Assert
        Assert.Equal(2, callSequence.Count);
        Assert.Equal("Repository", callSequence[0]);
        Assert.Equal("Algorithm", callSequence[1]);
    }

    // Helper methods
    private static Order CreateOrder(OrderType type, decimal amount, decimal price) =>
        new()
        {
            Id = Guid.NewGuid(),
            Type = type,
            Amount = amount,
            Price = price,
            Time = DateTime.Now,
            Kind = OrderKind.Limit
        };

    private static OrderBookRecord CreateOrderBookRecord(string exchangeId, decimal euroFunds, decimal cryptoFunds) =>
        new()
        {
            Id = exchangeId,
            AvailableFunds = new Balance { Euro = euroFunds, Crypto = cryptoFunds },
            OrderBook = new OrderBook
            {
                Asks = new List<OrderEntry>(),
                Bids = new List<OrderEntry>()
            }
        };
}
