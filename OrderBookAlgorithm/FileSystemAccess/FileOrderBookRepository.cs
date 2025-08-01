using Microsoft.Extensions.Logging;
using OrderBookAlgorithm.DomainClasses;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OrderBookAlgorithm.FileSystemAccess;

public class FileOrderBookRepository : IOrderBookRepository
{
    private readonly ILogger<FileOrderBookRepository> _logger;
    private readonly IFileSystem _fileSystem;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _sourceFolderPath;

    public FileOrderBookRepository(ILogger<FileOrderBookRepository> logger, IFileSystem fileSystem, OrderBookRepositoryOptions? options = null)
    {
        _logger = logger;
        _fileSystem = fileSystem;

        options ??= new OrderBookRepositoryOptions();
        var basePath = options.BasePath ?? AppContext.BaseDirectory;
        _sourceFolderPath = Path.Combine(basePath, options.SourceFolderName);

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

        if (!_fileSystem.DirectoryExists(_sourceFolderPath))
        {
            _logger.LogWarning("Source folder does not exist: {Folder}", _sourceFolderPath);
            yield break;
        }

        var files = _fileSystem.GetFiles(_sourceFolderPath);
        _logger.LogInformation("Found {Count} order book files.", files.Length);

        foreach (var file in files)
        {
            string? content = null;

            try
            {
                content = await _fileSystem.ReadAllTextAsync(file);
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
