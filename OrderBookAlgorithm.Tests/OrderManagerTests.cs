using Moq;
using OrderBookAlgorithm.DomainClasses;

namespace OrderBookAlgorithm.Tests;

public class OrderManagerTests
{
    [Fact]
    public async Task ProvideBestPriceOrdersAsync_ShouldUseDataFromRepository()
    {
        // Arrange
        var mockRepo = new Mock<IOrderBookRepository>();
        mockRepo.Setup(r => r.LoadOrderBookDataAsync())
                .ReturnsAsync(new List<OrderBookRecord>
                {
                    new OrderBookRecord() { OrderBook = new OrderBook() {Asks = new List<OrderEntry>() { new OrderEntry() { Order = new Order() { Id = Guid.NewGuid(), Time = DateTime.Now, Type = OrderType.Buy, Kind = OrderKind.Limit, Price = 100, Amount = 1 } } } } }
                });

        var mockAlgo = new Mock<IOrderAlgorithm>();
        mockAlgo.Setup(a => a.GetOrdersWithBestPrice(It.IsAny<Order>(), It.IsAny<List<OrderBookRecord>>()))
                .Returns(new List<Order>() { new Order() { Id = Guid.NewGuid(), Time = DateTime.Now, Type = OrderType.Buy, Kind = OrderKind.Limit, Price = 100, Amount = 1 } });

        var manager = new OrderManager(mockRepo.Object, mockAlgo.Object);

        // Act
        var result = await manager.ProvideBestPriceOrdersAsync(new Order() { Id = Guid.NewGuid(), Time = DateTime.Now, Type = OrderType.Buy, Kind = OrderKind.Limit, Price = 100, Amount = 1 });

        // Assert
        Assert.Equal(1, result.Count);
        mockRepo.Verify(r => r.LoadOrderBookDataAsync(), Times.Once);
        mockAlgo.Verify(a => a.GetOrdersWithBestPrice(It.IsAny<Order>(), It.IsAny<List<OrderBookRecord>>()), Times.Once);
    }

    [Fact]
    public async Task GetBestBuyPrice_ShouldReturnNull_WhenNoOrders()
    {
    }

    [Fact]
    public async Task GetBestBuyPrice_ShouldPreferFirst_WhenPricesEqual()
    {
    }
}
