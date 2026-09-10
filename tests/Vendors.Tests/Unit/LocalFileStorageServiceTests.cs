using FluentAssertions;
using Shared.Infrastructure.Storage;

namespace Vendors.Tests.Unit;

public class LocalFileStorageServiceTests : IDisposable
{
    private readonly string _root;
    private readonly LocalFileStorageService _sut;

    public LocalFileStorageServiceTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "vendors-local-storage-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        _sut = new LocalFileStorageService(_root);
    }

    [Fact]
    public async Task SaveGetDelete_RoundTripsBytes_AndRemovesFile()
    {
        var payload = "hello-local-storage"u8.ToArray();
        await using var upload = new MemoryStream(payload);

        var path = await _sut.SaveFileAsync(upload, "note.txt", "text/plain");

        path.Should().MatchRegex(@"^\d{4}-\d{2}/.+\.txt$");
        path.Should().NotContain("\\");

        var fullPath = Path.Combine(_root, path.Replace('/', Path.DirectorySeparatorChar));
        File.Exists(fullPath).Should().BeTrue();

        var (stream, contentType, fileName) = await _sut.GetFileAsync(path);
        await using (stream)
        {
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            ms.ToArray().Should().Equal(payload);
            contentType.Should().Be("application/octet-stream");
            fileName.Should().EndWith(".txt");
        }

        await _sut.DeleteFileAsync(path);
        File.Exists(fullPath).Should().BeFalse();
    }

    [Fact]
    public async Task GetFileAsync_Throws_WhenMissing()
    {
        var act = async () => await _sut.GetFileAsync("2099-01/missing.bin");

        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }
        catch
        {
            // best-effort cleanup
        }
    }
}
