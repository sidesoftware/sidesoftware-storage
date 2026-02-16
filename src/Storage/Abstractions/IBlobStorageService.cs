using SideSoftware.Storage.Config;
using SideSoftware.Storage.Models;
using SideSoftware.Storage.Results;

namespace SideSoftware.Storage.Abstractions;

/// <summary>
/// Service for managing blob storage operations.
/// </summary>
public interface IBlobStorageService
{
    /// <summary>
    /// Uploads a file from a stream.
    /// </summary>
    Task<UploadBlobResult> UploadAsync(
        string blobName,
        Stream content,
        UploadBlobOptions? options = null,
        CancellationToken ct = default);

    /// <summary>
    /// Uploads a file from a byte array.
    /// </summary>
    Task<UploadBlobResult> UploadAsync(
        string blobName,
        byte[] content,
        UploadBlobOptions? options = null,
        CancellationToken ct = default);

    /// <summary>
    /// Downloads a blob's content.
    /// </summary>
    Task<DownloadBlobResult> DownloadAsync(
        string blobName,
        string? containerName = null,
        CancellationToken ct = default);

    /// <summary>
    /// Downloads a blob's content to a byte array.
    /// </summary>
    Task<byte[]> DownloadBytesAsync(
        string blobName,
        string? containerName = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets information about a blob without downloading its content.
    /// </summary>
    Task<StoredBlobInfo?> GetInfoAsync(
        string blobName,
        string? containerName = null,
        CancellationToken ct = default);

    /// <summary>
    /// Checks if a blob exists.
    /// </summary>
    Task<bool> ExistsAsync(
        string blobName,
        string? containerName = null,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes a blob.
    /// </summary>
    /// <returns>True if deleted, false if blob didn't exist.</returns>
    Task<bool> DeleteAsync(
        string blobName,
        string? containerName = null,
        CancellationToken ct = default);

    /// <summary>
    /// Generates a time-limited SAS URL for direct blob access.
    /// </summary>
    Task<BlobSasUrl> GetSasUrlAsync(
        string blobName,
        BlobSasOptions? options = null,
        CancellationToken ct = default);

    /// <summary>
    /// Lists all blobs in a container, optionally filtered by prefix.
    /// </summary>
    IAsyncEnumerable<StoredBlobInfo> ListAsync(
        string? prefix = null,
        string? containerName = null,
        CancellationToken ct = default);

    /// <summary>
    /// Copies a blob to a new location.
    /// </summary>
    Task<UploadBlobResult> CopyAsync(
        string sourceBlobName,
        string destinationBlobName,
        string? sourceContainerName = null,
        string? destinationContainerName = null,
        CancellationToken ct = default);

    /// <summary>
    /// Ensures a container exists, creating it if necessary.
    /// </summary>
    Task EnsureContainerExistsAsync(
        string? containerName = null,
        CancellationToken ct = default);
}