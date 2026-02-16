namespace SideSoftware.Storage.Models;

/// <summary>
/// Options for generating a SAS URL.
/// </summary>
public record BlobSasOptions
{
    /// <summary>
    /// Container name. Uses default if not specified.
    /// </summary>
    public string? ContainerName { get; init; }

    /// <summary>
    /// Expiration time in minutes. Uses default if not specified.
    /// </summary>
    public int? ExpirationMinutes { get; init; }

    /// <summary>
    /// Allow read access. Default true.
    /// </summary>
    public bool AllowRead { get; init; } = true;

    /// <summary>
    /// Allow write access. Default false.
    /// </summary>
    public bool AllowWrite { get; init; } = false;

    /// <summary>
    /// Allow delete access. Default false.
    /// </summary>
    public bool AllowDelete { get; init; } = false;

    /// <summary>
    /// Content-Disposition header for downloads (e.g., "attachment; filename=doc.pdf").
    /// </summary>
    public string? ContentDisposition { get; init; }
}