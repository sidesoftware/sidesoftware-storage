namespace SideSoftware.Storage.Exceptions;

/// <summary>
/// Base exception for blob storage operations.
/// </summary>
public class BlobStorageException : Exception
{
    public string? BlobName { get; }
    public string? ContainerName { get; }

    public BlobStorageException(string message, string? blobName = null, string? containerName = null)
        : base(message)
    {
        BlobName = blobName;
        ContainerName = containerName;
    }

    public BlobStorageException(string message, Exception innerException, string? blobName = null, string? containerName = null)
        : base(message, innerException)
    {
        BlobName = blobName;
        ContainerName = containerName;
    }
}

/// <summary>
/// Thrown when a blob is not found.
/// </summary>
public class BlobNotFoundException(string blobName, string? containerName = null) : BlobStorageException(
    $"Blob '{blobName}' not found in container '{containerName ?? "default"}'.", blobName, containerName);

/// <summary>
/// Thrown when a blob already exists and overwrite is false.
/// </summary>
public class BlobAlreadyExistsException(string blobName, string? containerName = null) : BlobStorageException(
    $"Blob '{blobName}' already exists in container '{containerName ?? "default"}'.", blobName, containerName);

/// <summary>
/// Thrown when the uploaded file exceeds the maximum allowed size.
/// </summary>
public class BlobTooLargeException(long actualSize, long maxSize, string? blobName = null)
    : BlobStorageException($"File size ({actualSize} bytes) exceeds maximum allowed size ({maxSize} bytes).", blobName)
{
    public long ActualSize { get; } = actualSize;
    public long MaxSize { get; } = maxSize;
}

/// <summary>
/// Thrown when the content type is not allowed.
/// </summary>
public class BlobContentTypeNotAllowedException(
    string contentType,
    IEnumerable<string> allowedTypes,
    string? blobName = null)
    : BlobStorageException($"Content type '{contentType}' is not allowed.", blobName)
{
    public string ContentType { get; } = contentType;
    public IReadOnlyList<string> AllowedTypes { get; } = allowedTypes.ToList();
}