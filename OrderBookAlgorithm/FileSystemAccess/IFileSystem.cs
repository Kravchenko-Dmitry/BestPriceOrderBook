namespace OrderBookAlgorithm.FileSystemAccess;

public interface IFileSystem
{
    bool DirectoryExists(string path);

    string[] GetFiles(string path);

    Task<string> ReadAllTextAsync(string filePath);
}
