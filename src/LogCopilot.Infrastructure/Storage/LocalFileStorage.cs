using Microsoft.Extensions.Configuration;

namespace LogCopilot.Infrastructure.Storage;

public class LocalFileStorage : IFileStorage
{
    private readonly string _storagePath;

    public LocalFileStorage(IConfiguration configuration)
    {
        _storagePath = configuration["Storage:LocalPath"] ?? "uploads";
        Directory.CreateDirectory(_storagePath);
    }

    public async Task<string> SaveFileAsync(Stream stream, string fileName)
    {
        var safeFileName = Path.GetFileName(fileName);
        var uniqueFileName = $"{Guid.NewGuid()}_{safeFileName}";
        var filePath = Path.Combine(_storagePath, uniqueFileName);

        using var fileStream = File.Create(filePath);
        await stream.CopyToAsync(fileStream);

        return filePath;
    }

    public Task<Stream> GetFileStreamAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("File not found", filePath);

        return Task.FromResult<Stream>(File.OpenRead(filePath));
    }

    public Task DeleteFileAsync(string filePath)
    {
        if (File.Exists(filePath))
            File.Delete(filePath);

        return Task.CompletedTask;
    }
}
