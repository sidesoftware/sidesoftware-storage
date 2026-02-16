namespace SideSoftware.Storage.Results;

/// <summary>
/// Result of a blob download operation.
/// </summary>
public record DownloadBlobResult(
    Stream Content,
    string ContentType,
    long SizeBytes,
    string ETag,
    DateTimeOffset LastModified,
    IDictionary<string, string> Metadata);