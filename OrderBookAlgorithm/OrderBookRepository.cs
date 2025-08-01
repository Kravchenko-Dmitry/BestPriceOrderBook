using Microsoft.Extensions.Logging;
using OrderBookAlgorithm.DomainClasses;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OrderBookAlgorithm;

public class OrderBookRepository : IOrderBookRepository
{
    private readonly ILogger<OrderBookRepository> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _sourceFolderPath;
    private const string FOLDER_NAME_WITH_SOURCE_FILES = "OrderBookSources";

    public OrderBookRepository(ILogger<OrderBookRepository> logger)
    {
        _logger = logger;
        _sourceFolderPath = Path.Combine(AppContext.BaseDirectory, FOLDER_NAME_WITH_SOURCE_FILES);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    public async Task<List<OrderBookRecord>> LoadOrderBookDataAsync()
    {
        var orderBookRecords = new List<OrderBookRecord>();

        await foreach (var json in ReadSourceDataAsync())
        {
            var record = DeserializeOrderBook(json);
            if (record != null)
            {
                orderBookRecords.Add(record);
            }
        }
        return orderBookRecords;
    }

    private async IAsyncEnumerable<string> ReadSourceDataAsync()
    {
        _logger.LogInformation("Reading order book files from folder: {Folder}", _sourceFolderPath);

        if (!Directory.Exists(_sourceFolderPath))
        {
            _logger.LogWarning("Source folder does not exist: {Folder}", _sourceFolderPath);
            yield break;
        }

        var files = Directory.GetFiles(_sourceFolderPath);
        _logger.LogInformation("Found {Count} order book files.", files.Length);

        foreach (var file in files)
        {
            string? content = null;

            try
            {
                content = await File.ReadAllTextAsync(file);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Error reading file: {File}", file);
            }

            if (content != null)
            {
                yield return content;
            }
        }
    }

    private OrderBookRecord? DeserializeOrderBook(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<OrderBookRecord>(json, _jsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON format: {JsonSnippet}", json.Length > 100 ? json[..100] : json);
            return null;
        }
    }
}
