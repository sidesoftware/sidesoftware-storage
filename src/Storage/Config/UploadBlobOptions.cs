namespace SideSoftware.Storage.Config;

/// <summary>
/// Options for uploading a blob.
/// </summary>
public record UploadBlobOptions
{
    /// <summary>
    /// Container name. Uses default if not specified.
    /// </summary>
    public string? ContainerName { get; init; }

    /// <summary>
    /// Content type. Will be inferred from file extension if not specified.
    /// </summary>
    public string? ContentType { get; init; }

    /// <summary>
    /// Optional metadata to attach to the blob.
    /// </summary>
    public IDictionary<string, string>? Metadata { get; init; }

    /// <summary>
    /// If true, overwrites existing blob. If false, throws if blob exists.
    /// </summary>
    public bool Overwrite { get; init; } = true;
}