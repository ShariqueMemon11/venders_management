using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Application.Common.Interfaces;

namespace Shared.Infrastructure.Storage;

public static class FileStorageServiceCollectionExtensions
{
    /// <summary>
    /// Well-known Azurite / Azure Storage Emulator account — published by Microsoft, safe for local only.
    /// </summary>
    public const string AzuriteConnectionString =
        "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;";

    public static IServiceCollection AddFileStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration.GetSection(FileStorageOptions.SectionName).Get<FileStorageOptions>()
            ?? new FileStorageOptions();

        services.AddSingleton(options);

        // Require an explicit Provider — do not silently fall back to Local on typo/omission.
        var configuredProvider = configuration[$"{FileStorageOptions.SectionName}:Provider"];
        if (string.IsNullOrWhiteSpace(configuredProvider) && string.IsNullOrWhiteSpace(options.Provider))
        {
            throw new InvalidOperationException(
                "FileStorage:Provider is required. Set it to \"Local\" or \"AzureBlob\" in appsettings.");
        }

        var provider = (configuredProvider ?? options.Provider).Trim();

        if (string.Equals(provider, "AzureBlob", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(options.AzureBlob.ConnectionString))
            {
                throw new InvalidOperationException(
                    "FileStorage:Provider is AzureBlob but FileStorage:AzureBlob:ConnectionString is missing. " +
                    "For local Azurite use the well-known devstoreaccount1 connection string.");
            }

            if (string.IsNullOrWhiteSpace(options.AzureBlob.ContainerName))
            {
                throw new InvalidOperationException(
                    "FileStorage:AzureBlob:ContainerName is required when Provider is AzureBlob.");
            }

            services.AddSingleton(_ =>
            {
                var serviceClient = new BlobServiceClient(options.AzureBlob.ConnectionString);
                return serviceClient.GetBlobContainerClient(options.AzureBlob.ContainerName);
            });

            services.AddSingleton<AzureBlobFileStorageService>();
            services.AddSingleton<IFileStorageService>(sp =>
                sp.GetRequiredService<AzureBlobFileStorageService>());
            services.AddHostedService<AzureBlobContainerInitializer>();
        }
        else if (string.Equals(provider, "Local", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IFileStorageService>(_ =>
                new LocalFileStorageService(options.LocalRootPath));
        }
        else
        {
            throw new InvalidOperationException(
                $"FileStorage:Provider '{provider}' is not supported. Use \"Local\" or \"AzureBlob\".");
        }

        return services;
    }
}

/// <summary>Ensures the blob container exists on startup (Azurite or Azure).</summary>
internal sealed class AzureBlobContainerInitializer : IHostedService
{
    private readonly AzureBlobFileStorageService _storage;
    private readonly ILogger<AzureBlobContainerInitializer> _logger;

    public AzureBlobContainerInitializer(
        AzureBlobFileStorageService storage,
        ILogger<AzureBlobContainerInitializer> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _storage.EnsureContainerAsync(cancellationToken);
            _logger.LogInformation("Azure Blob container ready for document storage.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to initialize Azure Blob container. Is Azurite running on port 10000 " +
                "(npx azurite --silent) or is the connection string wrong?");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
