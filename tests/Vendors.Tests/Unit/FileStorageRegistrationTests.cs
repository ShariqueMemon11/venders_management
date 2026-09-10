using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Common.Interfaces;
using Shared.Infrastructure.Storage;

namespace Vendors.Tests.Unit;

public class FileStorageRegistrationTests
{
    [Fact]
    public void AddFileStorage_RegistersLocal_WhenConfigured()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FileStorage:Provider"] = "Local",
                ["FileStorage:LocalRootPath"] = Path.Combine(Path.GetTempPath(), "fs-reg-local")
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFileStorage(config);
        using var sp = services.BuildServiceProvider();

        sp.GetRequiredService<IFileStorageService>().Should().BeOfType<LocalFileStorageService>();
    }

    [Fact]
    public void AddFileStorage_RegistersAzureBlob_WhenConfigured()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FileStorage:Provider"] = "AzureBlob",
                ["FileStorage:AzureBlob:ConnectionString"] =
                    FileStorageServiceCollectionExtensions.AzuriteConnectionString,
                ["FileStorage:AzureBlob:ContainerName"] = "vendor-documents"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFileStorage(config);
        using var sp = services.BuildServiceProvider();

        sp.GetRequiredService<IFileStorageService>().Should().BeOfType<AzureBlobFileStorageService>();
    }

    [Fact]
    public void AddFileStorage_Throws_WhenAzureBlobMissingConnectionString()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FileStorage:Provider"] = "AzureBlob",
                ["FileStorage:AzureBlob:ContainerName"] = "vendor-documents"
            })
            .Build();

        var services = new ServiceCollection();
        var act = () => services.AddFileStorage(config);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ConnectionString*");
    }

    [Fact]
    public void AddFileStorage_Throws_WhenProviderMissing()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FileStorage:LocalRootPath"] = "LocalStorage"
            })
            .Build();

        var services = new ServiceCollection();
        var act = () => services.AddFileStorage(config);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Provider is required*");
    }

    [Fact]
    public void AddFileStorage_Throws_WhenProviderInvalid()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FileStorage:Provider"] = "S3Typo"
            })
            .Build();

        var services = new ServiceCollection();
        var act = () => services.AddFileStorage(config);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*S3Typo*not supported*");
    }
}
