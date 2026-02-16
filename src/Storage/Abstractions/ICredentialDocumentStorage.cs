using SideSoftware.Storage.Models;

namespace SideSoftware.Storage.Abstractions;

/// <summary>
/// Specialized storage service for credential documents (pilot licenses, medical certs, etc.)
/// </summary>
public interface ICredentialDocumentStorage
{
    /// <summary>
    /// Uploads a credential document for a person.
    /// </summary>
    Task<CredentialDocumentInfo> UploadAsync(
        Guid personId,
        string credentialType,
        Stream content,
        string originalFileName,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a secure, time-limited URL to view/download a credential document.
    /// </summary>
    Task<string> GetViewUrlAsync(
        string blobPath,
        int expirationMinutes = 15,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a secure URL with download disposition (prompts browser to download).
    /// </summary>
    Task<string> GetDownloadUrlAsync(
        string blobPath,
        string downloadFileName,
        int expirationMinutes = 15,
        CancellationToken ct = default);

    /// <summary>
    /// Downloads the raw bytes of a credential document.
    /// </summary>
    Task<byte[]> DownloadAsync(
        string blobPath,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes a credential document.
    /// </summary>
    Task<bool> DeleteAsync(
        string blobPath,
        CancellationToken ct = default);

    /// <summary>
    /// Lists all credential documents for a person.
    /// </summary>
    IAsyncEnumerable<CredentialDocumentInfo> ListForPersonAsync(
        Guid personId,
        CancellationToken ct = default);
}