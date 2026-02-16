namespace SideSoftware.Storage.Results;

/// <summary>
/// Result of a blob upload operation.
/// </summary>
public record UploadBlobResult(
    string BlobName,
    string ContainerName,
    string FullPath,
    string ContentType,
    long SizeBytes,
    string ETag,
    DateTimeOffset UploadedAt);