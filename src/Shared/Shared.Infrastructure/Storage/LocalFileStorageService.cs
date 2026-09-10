using Shared.Application.Common.Interfaces;

namespace Shared.Infrastructure.Storage;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _storageRootPath;

    public LocalFileStorageService(string? storageRootPath = null)
    {
        _storageRootPath = string.IsNullOrWhiteSpace(storageRootPath)
            ? Path.Combine(Directory.GetCurrentDirectory(), "LocalStorage")
            : Path.IsPathRooted(storageRootPath)
                ? storageRootPath
                : Path.Combine(Directory.GetCurrentDirectory(), storageRootPath);

        Directory.CreateDirectory(_storageRootPath);
    }

    public async Task<string> SaveFileAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";
        var dateFolder = DateTime.UtcNow.ToString("yyyy-MM");
        var directoryPath = Path.Combine(_storageRootPath, dateFolder);

        Directory.CreateDirectory(directoryPath);

        var filePath = Path.Combine(directoryPath, uniqueFileName);

        await using var fileStreamToWrite = new FileStream(
            filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
        await fileStream.CopyToAsync(fileStreamToWrite, cancellationToken);

        // Relative logical path — same shape as AzureBlobFileStorageService blob names.
        return Path.Combine(dateFolder, uniqueFileName).Replace("\\", "/");
    }

    public Task<(Stream FileStream, string ContentType, string FileName)> GetFileAsync(
        string storagePath,
        CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_storageRootPath, storagePath.Replace("/", Path.DirectorySeparatorChar.ToString()));

        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"File not found at storage path: {storagePath}");

        var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        return Task.FromResult(((Stream)stream, "application/octet-stream", Path.GetFileName(fullPath)));
    }

    public Task DeleteFileAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_storageRootPath, storagePath.Replace("/", Path.DirectorySeparatorChar.ToString()));

        if (File.Exists(fullPath))
            File.Delete(fullPath);

        return Task.CompletedTask;
    }
}
