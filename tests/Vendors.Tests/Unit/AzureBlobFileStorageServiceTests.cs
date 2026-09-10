using Azure.Storage.Blobs;
using FluentAssertions;
using Shared.Infrastructure.Storage;

namespace Vendors.Tests.Unit;

/// <summary>
/// Round-trips against Azurite when it is already running on 127.0.0.1:10000.
/// Start: see docs/file-storage.md. When Azurite is down, tests are skipped (not failed).
/// </summary>
[Collection(AzuriteCollection.Name)]
public class AzureBlobFileStorageServiceTests
{
    private readonly AzuriteFixture _azurite;

    public AzureBlobFileStorageServiceTests(AzuriteFixture azurite)
    {
        _azurite = azurite;
    }

    private static AzureBlobFileStorageService CreateSut(string containerName)
    {
        var container = new BlobServiceClient(FileStorageServiceCollectionExtensions.AzuriteConnectionString)
            .GetBlobContainerClient(containerName);
        return new AzureBlobFileStorageService(container);
    }

    private void RequireAzurite() =>
        Skip.If(!_azurite.IsAvailable, AzuriteFixture.SkipReason);

    [SkippableFact]
    public async Task SaveGetDelete_RoundTripsBytes_AgainstAzurite()
    {
        RequireAzurite();

        var containerName = $"vendor-docs-test-{Guid.NewGuid():N}";
        var sut = CreateSut(containerName);
        await sut.EnsureContainerAsync();

        try
        {
            var payload = "hello-azure-blob"u8.ToArray();
            await using var upload = new MemoryStream(payload);

            var path = await sut.SaveFileAsync(upload, "invoice.pdf", "application/pdf");

            path.Should().MatchRegex(@"^\d{4}-\d{2}/.+\.pdf$");
            path.Should().NotContain("\\");

            var (stream, contentType, fileName) = await sut.GetFileAsync(path);
            await using (stream)
            {
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                ms.ToArray().Should().Equal(payload);
                contentType.Should().Be("application/pdf");
                fileName.Should().EndWith(".pdf");
            }

            await sut.DeleteFileAsync(path);

            var act = async () => await sut.GetFileAsync(path);
            await act.Should().ThrowAsync<FileNotFoundException>();
        }
        finally
        {
            var container = new BlobServiceClient(FileStorageServiceCollectionExtensions.AzuriteConnectionString)
                .GetBlobContainerClient(containerName);
            await container.DeleteIfExistsAsync();
        }
    }

    [SkippableFact]
    public async Task GetFileAsync_Throws_WhenBlobMissing()
    {
        RequireAzurite();

        var containerName = $"vendor-docs-miss-{Guid.NewGuid():N}";
        var sut = CreateSut(containerName);
        await sut.EnsureContainerAsync();

        try
        {
            var act = async () => await sut.GetFileAsync("2099-01/does-not-exist.bin");
            await act.Should().ThrowAsync<FileNotFoundException>();
        }
        finally
        {
            var container = new BlobServiceClient(FileStorageServiceCollectionExtensions.AzuriteConnectionString)
                .GetBlobContainerClient(containerName);
            await container.DeleteIfExistsAsync();
        }
    }

    [SkippableFact]
    public async Task EnsureContainerAsync_IsIdempotent_AgainstAzurite()
    {
        RequireAzurite();

        var containerName = $"vendor-docs-ensure-{Guid.NewGuid():N}";
        var sut = CreateSut(containerName);

        try
        {
            await sut.EnsureContainerAsync();
            await sut.EnsureContainerAsync();

            var container = new BlobServiceClient(FileStorageServiceCollectionExtensions.AzuriteConnectionString)
                .GetBlobContainerClient(containerName);
            (await container.ExistsAsync()).Value.Should().BeTrue();
        }
        finally
        {
            var container = new BlobServiceClient(FileStorageServiceCollectionExtensions.AzuriteConnectionString)
                .GetBlobContainerClient(containerName);
            await container.DeleteIfExistsAsync();
        }
    }

    [SkippableFact]
    public async Task SaveFileAsync_PreservesExtension_AndContentType()
    {
        RequireAzurite();

        var containerName = $"vendor-docs-meta-{Guid.NewGuid():N}";
        var sut = CreateSut(containerName);
        await sut.EnsureContainerAsync();

        try
        {
            var payload = "png-bytes"u8.ToArray();
            await using var upload = new MemoryStream(payload);

            var path = await sut.SaveFileAsync(upload, "logo.PNG", "image/png");

            path.Should().EndWith(".PNG");
            var (stream, contentType, fileName) = await sut.GetFileAsync(path);
            await using (stream)
            {
                contentType.Should().Be("image/png");
                fileName.Should().EndWith(".PNG");
            }
        }
        finally
        {
            var container = new BlobServiceClient(FileStorageServiceCollectionExtensions.AzuriteConnectionString)
                .GetBlobContainerClient(containerName);
            await container.DeleteIfExistsAsync();
        }
    }

    [SkippableFact]
    public async Task DeleteFileAsync_IsIdempotent_WhenBlobMissing()
    {
        RequireAzurite();

        var containerName = $"vendor-docs-del-{Guid.NewGuid():N}";
        var sut = CreateSut(containerName);
        await sut.EnsureContainerAsync();

        try
        {
            var act = async () => await sut.DeleteFileAsync("2099-01/never-uploaded.bin");
            await act.Should().NotThrowAsync();
        }
        finally
        {
            var container = new BlobServiceClient(FileStorageServiceCollectionExtensions.AzuriteConnectionString)
                .GetBlobContainerClient(containerName);
            await container.DeleteIfExistsAsync();
        }
    }

    [SkippableFact]
    public async Task SaveGet_RoundTripsEmptyPayload()
    {
        RequireAzurite();

        var containerName = $"vendor-docs-empty-{Guid.NewGuid():N}";
        var sut = CreateSut(containerName);
        await sut.EnsureContainerAsync();

        try
        {
            await using var upload = new MemoryStream(Array.Empty<byte>());
            var path = await sut.SaveFileAsync(upload, "empty.bin", "application/octet-stream");

            var (stream, _, _) = await sut.GetFileAsync(path);
            await using (stream)
            {
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                ms.ToArray().Should().BeEmpty();
            }
        }
        finally
        {
            var container = new BlobServiceClient(FileStorageServiceCollectionExtensions.AzuriteConnectionString)
                .GetBlobContainerClient(containerName);
            await container.DeleteIfExistsAsync();
        }
    }

    [SkippableFact]
    public async Task SaveFileAsync_TwoFiles_GetDistinctPaths()
    {
        RequireAzurite();

        var containerName = $"vendor-docs-uniq-{Guid.NewGuid():N}";
        var sut = CreateSut(containerName);
        await sut.EnsureContainerAsync();

        try
        {
            await using var a = new MemoryStream("a"u8.ToArray());
            await using var b = new MemoryStream("b"u8.ToArray());
            var pathA = await sut.SaveFileAsync(a, "a.txt", "text/plain");
            var pathB = await sut.SaveFileAsync(b, "b.txt", "text/plain");

            pathA.Should().NotBe(pathB);
        }
        finally
        {
            var container = new BlobServiceClient(FileStorageServiceCollectionExtensions.AzuriteConnectionString)
                .GetBlobContainerClient(containerName);
            await container.DeleteIfExistsAsync();
        }
    }

    [SkippableFact]
    public async Task GetFileAsync_Throws_AfterDelete()
    {
        RequireAzurite();

        var containerName = $"vendor-docs-gone-{Guid.NewGuid():N}";
        var sut = CreateSut(containerName);
        await sut.EnsureContainerAsync();

        try
        {
            await using var upload = new MemoryStream("bye"u8.ToArray());
            var path = await sut.SaveFileAsync(upload, "bye.txt", "text/plain");
            await sut.DeleteFileAsync(path);

            var act = async () => await sut.GetFileAsync(path);
            await act.Should().ThrowAsync<FileNotFoundException>();
        }
        finally
        {
            var container = new BlobServiceClient(FileStorageServiceCollectionExtensions.AzuriteConnectionString)
                .GetBlobContainerClient(containerName);
            await container.DeleteIfExistsAsync();
        }
    }
}
