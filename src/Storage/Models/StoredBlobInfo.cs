namespace SideSoftware.Storage.Models;

/// <summary>
/// Information about a blob without its content.
/// </summary>
public record StoredBlobInfo(
    string BlobName,
    string ContainerName,
    string FullPath,
    string ContentType,
    long SizeBytes,
    DateTimeOffset? CreatedOn,
    DateTimeOffset? LastModified,
    IDictionary<string, string> Metadata);