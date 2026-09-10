using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Shared.Application.Common.Interfaces;

namespace Shared.Infrastructure.Storage;

/// <summary>
/// Azure Blob Storage implementation. Works against Azurite (local emulator) and real Azure —
/// same SDK, swap the connection string.
/// Storage paths match LocalFileStorageService: yyyy-MM/{guid}{ext}
/// </summary>
public sealed class AzureBlobFileStorageService : IFileStorageService
{
    private readonly BlobContainerClient _container;

    public AzureBlobFileStorageService(BlobContainerClient container)
    {
        _container = container;
    }

    public async Task EnsureContainerAsync(CancellationToken cancellationToken = default)
    {
        await _container.CreateIfNotExistsAsync(
            PublicAccessType.None,
            cancellationToken: cancellationToken);
    }

    public async Task<string> SaveFileAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";
        var dateFolder = DateTime.UtcNow.ToString("yyyy-MM");
        var blobName = $"{dateFolder}/{uniqueFileName}";

        var blob = _container.GetBlobClient(blobName);
        var headers = new BlobHttpHeaders
        {
            ContentType = string.IsNullOrWhiteSpace(contentType)
                ? "application/octet-stream"
                : contentType
        };

        await blob.UploadAsync(
            fileStream,
            new BlobUploadOptions { HttpHeaders = headers },
            cancellationToken);

        return blobName;
    }

    public async Task<(Stream FileStream, string ContentType, string FileName)> GetFileAsync(
        string storagePath,
        CancellationToken cancellationToken = default)
    {
        var blobName = NormalizeBlobName(storagePath);
        var blob = _container.GetBlobClient(blobName);

        if (!await blob.ExistsAsync(cancellationToken))
            throw new FileNotFoundException($"Blob not found at storage path: {storagePath}");

        var download = await blob.DownloadStreamingAsync(cancellationToken: cancellationToken);
        var contentType = download.Value.Details.ContentType ?? "application/octet-stream";
        var fileName = Path.GetFileName(blobName);

        // Caller owns disposing the returned stream (same contract as LocalFileStorageService).
        return (download.Value.Content, contentType, fileName);
    }

    public async Task DeleteFileAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var blobName = NormalizeBlobName(storagePath);
        await _container.DeleteBlobIfExistsAsync(blobName, cancellationToken: cancellationToken);
    }

    private static string NormalizeBlobName(string storagePath) =>
        storagePath.Replace("\\", "/").TrimStart('/');
}
