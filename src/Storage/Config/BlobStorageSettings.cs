namespace SideSoftware.Storage.Config;

public class BlobStorageSettings
{
    public const string SectionName = "BlobStorage";

    /// <summary>
    /// Azure Storage connection string.
    /// For local development, use "UseDevelopmentStorage=true" for Azurite.
    /// </summary>
    public string ConnectionString { get; set; } = null!;

    /// <summary>
    /// Default container name if not specified per-operation.
    /// </summary>
    public string DefaultContainer { get; set; } = "documents";

    /// <summary>
    /// Default SAS URL expiration in minutes.
    /// </summary>
    public int DefaultSasExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// Maximum allowed file size in bytes. Default is 10MB.
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;

    /// <summary>
    /// Allowed content types. Empty list means all types allowed.
    /// </summary>
    public List<string> AllowedContentTypes { get; set; } = [];
}