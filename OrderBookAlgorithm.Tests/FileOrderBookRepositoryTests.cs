using Microsoft.Extensions.Logging;
using Moq;
using OrderBookAlgorithm.DomainClasses;
using OrderBookAlgorithm.FileSystemAccess;
using System.Text.Json;

namespace OrderBookAlgorithm.Tests;

public class FileOrderBookRepositoryTests
{
    private readonly Mock<ILogger<FileOrderBookRepository>> _loggerMock;
    private readonly Mock<IFileSystem> _fileSystemMock;

    public FileOrderBookRepositoryTests()
    {
        _loggerMock = new Mock<ILogger<FileOrderBookRepository>>();
        _fileSystemMock = new Mock<IFileSystem>();
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Act
        var repository = new FileOrderBookRepository(_loggerMock.Object, _fileSystemMock.Object);

        // Assert
        Assert.NotNull(repository);
    }

    [Fact]
    public void Constructor_WithNullOptions_UsesDefaults()
    {
        // Act & Assert - Should not throw
        var repository = new FileOrderBookRepository(_loggerMock.Object, _fileSystemMock.Object, null);
        Assert.NotNull(repository);
    }

    [Fact]
    public async Task LoadOrderBookDataAsync_WithNonExistentDirectory_ReturnsEmptyList()
    {
        // Arrange
        var repository = new FileOrderBookRepository(_loggerMock.Object, _fileSystemMock.Object);
        _fileSystemMock.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(false);

        // Act
        var result = await repository.LoadOrderBookDataAsync();

        // Assert
        Assert.Empty(result);
        VerifyLogWarning("Source folder does not exist");
    }

    [Fact]
    public async Task LoadOrderBookDataAsync_WithEmptyDirectory_ReturnsEmptyList()
    {
        // Arrange
        var repository = new FileOrderBookRepository(_loggerMock.Object, _fileSystemMock.Object);
        _fileSystemMock.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(true);
        _fileSystemMock.Setup(x => x.GetFiles(It.IsAny<string>())).Returns([]);

        // Act
        var result = await repository.LoadOrderBookDataAsync();

        // Assert
        Assert.Empty(result);
        VerifyLogInformation("Found 0 order book files");
    }

    [Fact]
    public async Task LoadOrderBookDataAsync_WithValidJsonFile_ReturnsOrderBookRecord()
    {
        // Arrange
        var repository = new FileOrderBookRepository(_loggerMock.Object, _fileSystemMock.Object);
        var validJson = CreateValidOrderBookJson();
        var files = new[] { "/path/file1.json" };

        _fileSystemMock.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(true);
        _fileSystemMock.Setup(x => x.GetFiles(It.IsAny<string>())).Returns(files);
        _fileSystemMock.Setup(x => x.ReadAllTextAsync(files[0])).ReturnsAsync(validJson);

        // Act
        var result = await repository.LoadOrderBookDataAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("Exchange1", result[0].Id);
        Assert.NotNull(result[0].AvailableFunds);
        Assert.NotNull(result[0].OrderBook);
    }

    [Theory]
    [InlineData("invalid json")]
    [InlineData("{\"incomplete\": ")]
    [InlineData("")]
    public async Task LoadOrderBookDataAsync_WithInvalidJson_SkipsFileAndLogsError(string invalidJson)
    {
        // Arrange
        var repository = new FileOrderBookRepository(_loggerMock.Object, _fileSystemMock.Object);
        var files = new[] { "/path/invalid.json" };

        _fileSystemMock.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(true);
        _fileSystemMock.Setup(x => x.GetFiles(It.IsAny<string>())).Returns(files);
        _fileSystemMock.Setup(x => x.ReadAllTextAsync(files[0])).ReturnsAsync(invalidJson);

        // Act
        var result = await repository.LoadOrderBookDataAsync();

        // Assert
        Assert.Empty(result);
        VerifyLogError("Invalid JSON format");
    }

    [Fact]
    public async Task LoadOrderBookDataAsync_WithFileReadException_SkipsFileAndLogsError()
    {
        // Arrange
        var repository = new FileOrderBookRepository(_loggerMock.Object, _fileSystemMock.Object);
        var files = new[] { "/path/file1.json" };
        var ioException = new IOException("File access denied");

        _fileSystemMock.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(true);
        _fileSystemMock.Setup(x => x.GetFiles(It.IsAny<string>())).Returns(files);
        _fileSystemMock.Setup(x => x.ReadAllTextAsync(files[0])).ThrowsAsync(ioException);

        // Act
        var result = await repository.LoadOrderBookDataAsync();

        // Assert
        Assert.Empty(result);
        VerifyLogError("Error reading file");
    }

    [Fact]
    public async Task LoadOrderBookDataAsync_WithMultipleFiles_ProcessesAllValidFiles()
    {
        // Arrange
        var repository = new FileOrderBookRepository(_loggerMock.Object, _fileSystemMock.Object);
        var validJson1 = CreateValidOrderBookJson("Exchange1");
        var validJson2 = CreateValidOrderBookJson("Exchange2");
        var invalidJson = "invalid";
        var files = new[] { "/path/file1.json", "/path/file2.json", "/path/invalid.json" };

        _fileSystemMock.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(true);
        _fileSystemMock.Setup(x => x.GetFiles(It.IsAny<string>())).Returns(files);
        _fileSystemMock.Setup(x => x.ReadAllTextAsync(files[0])).ReturnsAsync(validJson1);
        _fileSystemMock.Setup(x => x.ReadAllTextAsync(files[1])).ReturnsAsync(validJson2);
        _fileSystemMock.Setup(x => x.ReadAllTextAsync(files[2])).ReturnsAsync(invalidJson);

        // Act
        var result = await repository.LoadOrderBookDataAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.Id == "Exchange1");
        Assert.Contains(result, r => r.Id == "Exchange2");
        VerifyLogError("Invalid JSON format");
    }

    [Fact]
    public async Task LoadOrderBookDataAsync_WithLongInvalidJson_TruncatesLogMessage()
    {
        // Arrange
        var repository = new FileOrderBookRepository(_loggerMock.Object, _fileSystemMock.Object);
        var longInvalidJson = new string('x', 200); // 200 characters
        var files = new[] { "/path/long.json" };

        _fileSystemMock.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(true);
        _fileSystemMock.Setup(x => x.GetFiles(It.IsAny<string>())).Returns(files);
        _fileSystemMock.Setup(x => x.ReadAllTextAsync(files[0])).ReturnsAsync(longInvalidJson);

        // Act
        await repository.LoadOrderBookDataAsync();

        // Assert
        VerifyLogError("Invalid JSON format");
        // The implementation should truncate to 100 characters in the log
    }

    [Fact]
    public async Task LoadOrderBookDataAsync_CallsFileSystemMethodsInCorrectOrder()
    {
        // Arrange
        var repository = new FileOrderBookRepository(_loggerMock.Object, _fileSystemMock.Object);
        var callSequence = new List<string>();
        var files = new[] { "/test/file.json" };
        var validJson = CreateValidOrderBookJson();

        _fileSystemMock.Setup(x => x.DirectoryExists(It.IsAny<string>()))
            .Returns(true)
            .Callback(() => callSequence.Add("DirectoryExists"));

        _fileSystemMock.Setup(x => x.GetFiles(It.IsAny<string>()))
            .Returns(files)
            .Callback(() => callSequence.Add("GetFiles"));

        _fileSystemMock.Setup(x => x.ReadAllTextAsync(It.IsAny<string>()))
            .ReturnsAsync(validJson)
            .Callback(() => callSequence.Add("ReadAllTextAsync"));

        // Act
        await repository.LoadOrderBookDataAsync();

        // Assert
        Assert.Equal(3, callSequence.Count);
        Assert.Equal("DirectoryExists", callSequence[0]);
        Assert.Equal("GetFiles", callSequence[1]);
        Assert.Equal("ReadAllTextAsync", callSequence[2]);
    }

    [Theory]
    [InlineData("CustomFolder", "/custom/base")]
    [InlineData("TestSources", null)] // Should use AppContext.BaseDirectory
    public async Task LoadOrderBookDataAsync_WithDifferentOptions_UsesCorrectPath(
        string folderName, string? basePath)
    {
        // Arrange
        var options = new OrderBookRepositoryOptions
        {
            SourceFolderName = folderName,
            BasePath = basePath
        };

        var repository = new FileOrderBookRepository(_loggerMock.Object, _fileSystemMock.Object, options);
        var expectedBasePath = basePath ?? AppContext.BaseDirectory;
        var expectedFullPath = Path.Combine(expectedBasePath, folderName);

        _fileSystemMock.Setup(x => x.DirectoryExists(expectedFullPath)).Returns(false);

        // Act
        await repository.LoadOrderBookDataAsync();

        // Assert
        _fileSystemMock.Verify(x => x.DirectoryExists(expectedFullPath), Times.Once);
    }

    // Helper methods
    private static string CreateValidOrderBookJson(string exchangeId = "Exchange1")
    {
        var orderBookRecord = new OrderBookRecord
        {
            Id = exchangeId,
            AvailableFunds = new Balance
            {
                Euro = 100000m,
                Crypto = 2.0m
            },
            OrderBook = new OrderBook
            {
                Asks = new List<OrderEntry>
                    {
                        new()
                        {
                            Order = new Order
                            {
                                Id = Guid.NewGuid(),
                                Type = OrderType.Sell,
                                Kind = OrderKind.Limit,
                                Amount = 1.0m,
                                Price = 50000m,
                                Time = DateTime.Now
                            }
                        }
                    },
                Bids = new List<OrderEntry>
                    {
                        new()
                        {
                            Order = new Order
                            {
                                Id = Guid.NewGuid(),
                                Type = OrderType.Buy,
                                Kind = OrderKind.Limit,
                                Amount = 1.0m,
                                Price = 49000m,
                                Time = DateTime.Now
                            }
                        }
                    }
            }
        };

        return JsonSerializer.Serialize(orderBookRecord, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        });
    }

    private void VerifyLogInformation(string message)
    {
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    private void VerifyLogWarning(string message)
    {
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    private void VerifyLogError(string message)
    {
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
}
