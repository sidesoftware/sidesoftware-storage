using Microsoft.Extensions.Logging;
using NSubstitute;
using SideSoftware.Storage.Abstractions;
using SideSoftware.Storage.Config;
using SideSoftware.Storage.Models;
using SideSoftware.Storage.Results;
using Xunit;

namespace SideSoftware.Storage.Tests;

public class CredentialDocumentStorageTests
{
    private readonly IBlobStorageService _blobStorage = Substitute.For<IBlobStorageService>();
    private readonly ILogger<CredentialDocumentStorage> _logger = Substitute.For<ILogger<CredentialDocumentStorage>>();
    private readonly CredentialDocumentStorage _sut;

    public CredentialDocumentStorageTests()
    {
        _sut = new CredentialDocumentStorage(_blobStorage, _logger);
    }

    // ── UploadAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task UploadAsync_BuildsCorrectBlobPathAndDelegatesToBlobStorage()
    {
        var personId = Guid.NewGuid();
        const string credentialType = "medical";
        const string originalFileName = "cert.pdf";
        using var content = new MemoryStream([0x01, 0x02]);
        var now = DateTimeOffset.UtcNow;

        _blobStorage.UploadAsync(
                Arg.Any<string>(),
                Arg.Any<Stream>(),
                Arg.Any<UploadBlobOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var blobName = callInfo.ArgAt<string>(0);
                return new UploadBlobResult(
                    BlobName: blobName,
                    ContainerName: "credentials",
                    FullPath: $"credentials/{blobName}",
                    ContentType: "application/pdf",
                    SizeBytes: 2,
                    ETag: "\"etag\"",
                    UploadedAt: now);
            });

        var result = await _sut.UploadAsync(personId, credentialType, content, originalFileName);

        // Verify blob path pattern: {personId}/{credentialType}/{guid}.pdf
        await _blobStorage.Received(1).UploadAsync(
            Arg.Is<string>(name =>
                name.StartsWith($"{personId}/medical/") && name.EndsWith(".pdf")),
            content,
            Arg.Is<UploadBlobOptions>(o =>
                o.ContainerName == "credentials"
                && o.Metadata != null
                && o.Metadata["OriginalFileName"] == originalFileName
                && o.Metadata["PersonId"] == personId.ToString()
                && o.Metadata["CredentialType"] == credentialType
                && o.Metadata.ContainsKey("UploadedAt")),
            Arg.Any<CancellationToken>());

        Assert.Equal(originalFileName, result.OriginalFileName);
        Assert.Equal("application/pdf", result.ContentType);
        Assert.Equal(2, result.SizeBytes);
        Assert.Equal(now, result.UploadedAt);
        Assert.StartsWith("credentials/", result.BlobPath);
    }

    [Fact]
    public async Task UploadAsync_PreservesExtensionAsLowercase()
    {
        var personId = Guid.NewGuid();
        using var content = new MemoryStream([0x01]);

        _blobStorage.UploadAsync(
                Arg.Any<string>(), Arg.Any<Stream>(),
                Arg.Any<UploadBlobOptions>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => new UploadBlobResult(
                callInfo.ArgAt<string>(0), "credentials",
                $"credentials/{callInfo.ArgAt<string>(0)}",
                "image/jpeg", 1, "\"e\"", DateTimeOffset.UtcNow));

        await _sut.UploadAsync(personId, "license", content, "Photo.JPG");

        await _blobStorage.Received(1).UploadAsync(
            Arg.Is<string>(n => n.EndsWith(".jpg")),
            Arg.Any<Stream>(), Arg.Any<UploadBlobOptions>(), Arg.Any<CancellationToken>());
    }

    // ── GetViewUrlAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetViewUrlAsync_PassesCorrectOptionsAndReturnsUrl()
    {
        const string blobPath = "credentials/person/medical/abc.pdf";
        const string expectedUrl = "https://storage.blob.core.windows.net/credentials/person/medical/abc.pdf?sas";

        _blobStorage.GetSasUrlAsync(
                Arg.Any<string>(), Arg.Any<BlobSasOptions>(), Arg.Any<CancellationToken>())
            .Returns(new BlobSasUrl(expectedUrl, DateTimeOffset.UtcNow.AddMinutes(30), "person/medical/abc.pdf", "credentials"));

        var url = await _sut.GetViewUrlAsync(blobPath, expirationMinutes: 30);

        Assert.Equal(expectedUrl, url);
        await _blobStorage.Received(1).GetSasUrlAsync(
            "person/medical/abc.pdf",
            Arg.Is<BlobSasOptions>(o =>
                o.ContainerName == "credentials"
                && o.ExpirationMinutes == 30
                && o.AllowRead
                && o.ContentDisposition == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetViewUrlAsync_WithoutContainerPrefix_PassesBlobPathDirectly()
    {
        const string blobPath = "person/medical/abc.pdf";

        _blobStorage.GetSasUrlAsync(
                Arg.Any<string>(), Arg.Any<BlobSasOptions>(), Arg.Any<CancellationToken>())
            .Returns(new BlobSasUrl("https://url", DateTimeOffset.UtcNow.AddMinutes(15), blobPath, "credentials"));

        await _sut.GetViewUrlAsync(blobPath);

        await _blobStorage.Received(1).GetSasUrlAsync(
            blobPath,
            Arg.Any<BlobSasOptions>(),
            Arg.Any<CancellationToken>());
    }

    // ── GetDownloadUrlAsync ──────────────────────────────────────────

    [Fact]
    public async Task GetDownloadUrlAsync_SetsContentDispositionWithFilename()
    {
        const string blobPath = "credentials/person/license/doc.pdf";
        const string downloadName = "my-license.pdf";
        const string expectedUrl = "https://storage.blob.core.windows.net/sas-url";

        _blobStorage.GetSasUrlAsync(
                Arg.Any<string>(), Arg.Any<BlobSasOptions>(), Arg.Any<CancellationToken>())
            .Returns(new BlobSasUrl(expectedUrl, DateTimeOffset.UtcNow.AddMinutes(15), "person/license/doc.pdf", "credentials"));

        var url = await _sut.GetDownloadUrlAsync(blobPath, downloadName);

        Assert.Equal(expectedUrl, url);
        await _blobStorage.Received(1).GetSasUrlAsync(
            "person/license/doc.pdf",
            Arg.Is<BlobSasOptions>(o =>
                o.ContentDisposition == $"attachment; filename=\"{downloadName}\""
                && o.AllowRead
                && o.ContainerName == "credentials"),
            Arg.Any<CancellationToken>());
    }

    // ── DownloadAsync ────────────────────────────────────────────────

    [Fact]
    public async Task DownloadAsync_DelegatesToDownloadBytesWithCorrectArgs()
    {
        const string blobPath = "credentials/person/medical/file.pdf";
        byte[] expected = [0xDE, 0xAD];

        _blobStorage.DownloadBytesAsync("person/medical/file.pdf", "credentials", Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.DownloadAsync(blobPath);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task DownloadAsync_WithoutContainerPrefix_PassesPathDirectly()
    {
        const string blobPath = "person/medical/file.pdf";
        byte[] expected = [0xCA, 0xFE];

        _blobStorage.DownloadBytesAsync(blobPath, "credentials", Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.DownloadAsync(blobPath);

        Assert.Equal(expected, result);
    }

    // ── DeleteAsync ──────────────────────────────────────────────────

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DeleteAsync_ReturnsResultFromBlobStorage(bool deleted)
    {
        const string blobPath = "credentials/person/license/abc.pdf";

        _blobStorage.DeleteAsync("person/license/abc.pdf", "credentials", Arg.Any<CancellationToken>())
            .Returns(deleted);

        var result = await _sut.DeleteAsync(blobPath);

        Assert.Equal(deleted, result);
    }

    // ── ListForPersonAsync ───────────────────────────────────────────

    [Fact]
    public async Task ListForPersonAsync_MapsMetadataOriginalFileName()
    {
        var personId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var blobs = new[]
        {
            new StoredBlobInfo(
                BlobName: $"{personId}/medical/abc.pdf",
                ContainerName: "credentials",
                FullPath: $"credentials/{personId}/medical/abc.pdf",
                ContentType: "application/pdf",
                SizeBytes: 1024,
                CreatedOn: now,
                LastModified: now,
                Metadata: new Dictionary<string, string> { ["OriginalFileName"] = "my-cert.pdf" })
        };

        _blobStorage.ListAsync($"{personId}/", "credentials", Arg.Any<CancellationToken>())
            .Returns(blobs.ToAsyncEnumerable());

        var results = new List<CredentialDocumentInfo>();
        await foreach (var item in _sut.ListForPersonAsync(personId))
            results.Add(item);

        Assert.Single(results);
        Assert.Equal("my-cert.pdf", results[0].OriginalFileName);
        Assert.Equal($"credentials/{personId}/medical/abc.pdf", results[0].BlobPath);
        Assert.Equal("application/pdf", results[0].ContentType);
        Assert.Equal(1024, results[0].SizeBytes);
        Assert.Equal(now, results[0].UploadedAt);
    }

    [Fact]
    public async Task ListForPersonAsync_FallsBackToFileNameWhenMetadataMissing()
    {
        var personId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var blobs = new[]
        {
            new StoredBlobInfo(
                BlobName: $"{personId}/license/deadbeef.jpg",
                ContainerName: "credentials",
                FullPath: $"credentials/{personId}/license/deadbeef.jpg",
                ContentType: "image/jpeg",
                SizeBytes: 512,
                CreatedOn: null,
                LastModified: now,
                Metadata: new Dictionary<string, string>())
        };

        _blobStorage.ListAsync($"{personId}/", "credentials", Arg.Any<CancellationToken>())
            .Returns(blobs.ToAsyncEnumerable());

        var results = new List<CredentialDocumentInfo>();
        await foreach (var item in _sut.ListForPersonAsync(personId))
            results.Add(item);

        Assert.Single(results);
        Assert.Equal("deadbeef.jpg", results[0].OriginalFileName);
        // Falls back to LastModified when CreatedOn is null
        Assert.Equal(now, results[0].UploadedAt);
    }

    [Fact]
    public async Task ListForPersonAsync_UsesMinValueWhenNoDatesAvailable()
    {
        var personId = Guid.NewGuid();

        var blobs = new[]
        {
            new StoredBlobInfo(
                BlobName: $"{personId}/medical/nodates.pdf",
                ContainerName: "credentials",
                FullPath: $"credentials/{personId}/medical/nodates.pdf",
                ContentType: "application/pdf",
                SizeBytes: 100,
                CreatedOn: null,
                LastModified: null,
                Metadata: new Dictionary<string, string>())
        };

        _blobStorage.ListAsync($"{personId}/", "credentials", Arg.Any<CancellationToken>())
            .Returns(blobs.ToAsyncEnumerable());

        var results = new List<CredentialDocumentInfo>();
        await foreach (var item in _sut.ListForPersonAsync(personId))
            results.Add(item);

        Assert.Equal(DateTimeOffset.MinValue, results[0].UploadedAt);
    }
}
