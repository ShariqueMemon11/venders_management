using System.Net.Sockets;

namespace Vendors.Tests.Unit;

/// <summary>
/// One short TCP probe per test collection. Azure blob integration tests skip when Azurite is down.
/// </summary>
public sealed class AzuriteFixture
{
    public const string Host = "127.0.0.1";
    public const int Port = 10000;
    public const string SkipReason =
        "Azurite is not reachable at 127.0.0.1:10000. Start it to run Azure blob storage tests (see docs/file-storage.md).";

    public bool IsAvailable { get; }

    public AzuriteFixture()
    {
        IsAvailable = Probe();
    }

    private static bool Probe()
    {
        try
        {
            using var client = new TcpClient();
            var connect = client.ConnectAsync(Host, Port);
            return connect.Wait(TimeSpan.FromMilliseconds(400)) && client.Connected;
        }
        catch
        {
            return false;
        }
    }
}

[CollectionDefinition(Name)]
public sealed class AzuriteCollection : ICollectionFixture<AzuriteFixture>
{
    public const string Name = "Azurite";
}
