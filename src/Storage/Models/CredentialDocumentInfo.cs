namespace SideSoftware.Storage.Models;

/// <summary>
/// Information about an uploaded credential document.
/// </summary>
public record CredentialDocumentInfo(
    string BlobPath,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    DateTimeOffset UploadedAt);