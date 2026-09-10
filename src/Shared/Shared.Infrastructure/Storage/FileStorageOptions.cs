namespace Shared.Infrastructure.Storage;

public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    /// <summary>"Local" or "AzureBlob" — must be set explicitly in configuration.</summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>Root folder for LocalFileStorageService (relative to content root or absolute).</summary>
    public string LocalRootPath { get; set; } = "LocalStorage";

    public AzureBlobStorageOptions AzureBlob { get; set; } = new();
}

public sealed class AzureBlobStorageOptions
{
    /// <summary>
    /// Connection string. Local/Azurite well-known string is safe to commit;
    /// production should use Key Vault / env vars.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    public string ContainerName { get; set; } = "vendor-documents";
}
