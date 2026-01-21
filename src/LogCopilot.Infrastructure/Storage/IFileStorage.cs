namespace LogCopilot.Infrastructure.Storage;

public interface IFileStorage
{
    Task<string> SaveFileAsync(Stream stream, string fileName);
    Task<Stream> GetFileStreamAsync(string filePath);
    Task DeleteFileAsync(string filePath);
}
