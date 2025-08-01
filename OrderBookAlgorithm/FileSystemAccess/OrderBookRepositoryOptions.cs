namespace OrderBookAlgorithm.FileSystemAccess;

public class OrderBookRepositoryOptions
{
    public string SourceFolderName { get; set; } = "OrderBookSources";
    public string? BasePath { get; set; }
}
