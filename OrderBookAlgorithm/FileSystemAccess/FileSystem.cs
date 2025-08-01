namespace OrderBookAlgorithm.FileSystemAccess;

public class FileSystem : IFileSystem
{
    public bool DirectoryExists(string path) => Directory.Exists(path);

    public string[] GetFiles(string path) => Directory.GetFiles(path);

    public Task<string> ReadAllTextAsync(string filePath) => File.ReadAllTextAsync(filePath);
}
