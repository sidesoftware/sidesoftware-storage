using Microsoft.Extensions.Logging;
using SideSoftware.Storage.Abstractions;
using SideSoftware.Storage.Config;
using SideSoftware.Storage.Models;

namespace SideSoftware.Storage;

public class CredentialDocumentStorage(
    IBlobStorageService blobStorage,
    ILogger<CredentialDocumentStorage> logger)
    : ICredentialDocumentStorage
{
    private const string ContainerName = "credentials";

    public async Task<CredentialDocumentInfo> UploadAsync(
        Guid personId,
        string credentialType,
        Stream content,
        string originalFileName,
        CancellationToken ct = default)
    {
        // Structured path: credentials/{personId}/{credentialType}/{guid}{extension}
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        var blobName = $"{personId}/{credentialType}/{Guid.NewGuid()}{extension}";

        var result = await blobStorage.UploadAsync(
            blobName,
            content,
            new UploadBlobOptions
            {
                ContainerName = ContainerName,
                Metadata = new Dictionary<string, string>
                {
                    ["OriginalFileName"] = originalFileName,
                    ["PersonId"] = personId.ToString(),
                    ["CredentialType"] = credentialType,
                    ["UploadedAt"] = DateTimeOffset.UtcNow.ToString("O")
                }
            },
            ct);

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation(
                "Uploaded credential document for person {PersonId}, type {CredentialType}: {BlobPath}",
                personId, credentialType, result.FullPath);

        return new CredentialDocumentInfo(
            BlobPath: result.FullPath,
            OriginalFileName: originalFileName,
            ContentType: result.ContentType,
            SizeBytes: result.SizeBytes,
            UploadedAt: result.UploadedAt);
    }

    public async Task<string> GetViewUrlAsync(
        string blobPath,
        int expirationMinutes = 15,
        CancellationToken ct = default)
    {
        var blobName = RemoveContainerPrefix(blobPath);

        var sasUrl = await blobStorage.GetSasUrlAsync(
            blobName,
            new BlobSasOptions
            {
                ContainerName = ContainerName,
                ExpirationMinutes = expirationMinutes,
                AllowRead = true
            },
            ct);

        return sasUrl.Url;
    }

    public async Task<string> GetDownloadUrlAsync(
        string blobPath,
        string downloadFileName,
        int expirationMinutes = 15,
        CancellationToken ct = default)
    {
        var blobName = RemoveContainerPrefix(blobPath);

        var sasUrl = await blobStorage.GetSasUrlAsync(
            blobName,
            new BlobSasOptions
            {
                ContainerName = ContainerName,
                ExpirationMinutes = expirationMinutes,
                AllowRead = true,
                ContentDisposition = $"attachment; filename=\"{downloadFileName}\""
            },
            ct);

        return sasUrl.Url;
    }

    public async Task<byte[]> DownloadAsync(
        string blobPath,
        CancellationToken ct = default)
    {
        var blobName = RemoveContainerPrefix(blobPath);
        return await blobStorage.DownloadBytesAsync(blobName, ContainerName, ct);
    }

    public async Task<bool> DeleteAsync(
        string blobPath,
        CancellationToken ct = default)
    {
        var blobName = RemoveContainerPrefix(blobPath);

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Deleting credential document: {BlobPath}", blobPath);

        return await blobStorage.DeleteAsync(blobName, ContainerName, ct);
    }

    public async IAsyncEnumerable<CredentialDocumentInfo> ListForPersonAsync(
        Guid personId,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var prefix = $"{personId}/";

        await foreach (var blob in blobStorage.ListAsync(prefix, ContainerName, ct))
        {
            var originalFileName = blob.Metadata.TryGetValue("OriginalFileName", out var name)
                ? name
                : Path.GetFileName(blob.BlobName);

            yield return new CredentialDocumentInfo(
                BlobPath: blob.FullPath,
                OriginalFileName: originalFileName,
                ContentType: blob.ContentType,
                SizeBytes: blob.SizeBytes,
                UploadedAt: blob.CreatedOn ?? blob.LastModified ?? DateTimeOffset.MinValue);
        }
    }

    private static string RemoveContainerPrefix(string blobPath)
    {
        // Handle both "credentials/..." and just the blob name
        return blobPath.StartsWith($"{ContainerName}/", StringComparison.OrdinalIgnoreCase)
            ? blobPath[(ContainerName.Length + 1)..]
            : blobPath;
    }
}