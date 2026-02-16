namespace SideSoftware.Storage.Models;

/// <summary>
/// A time-limited URL for secure blob access.
/// </summary>
public record BlobSasUrl(
    string Url,
    DateTimeOffset ExpiresOn,
    string BlobName,
    string ContainerName);