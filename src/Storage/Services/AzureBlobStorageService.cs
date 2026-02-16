using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SideSoftware.Storage.Abstractions;
using SideSoftware.Storage.Config;
using SideSoftware.Storage.Exceptions;
using SideSoftware.Storage.Models;
using SideSoftware.Storage.Results;
using DownloadBlobResult = SideSoftware.Storage.Results.DownloadBlobResult;
using StoredBlobInfo = SideSoftware.Storage.Models.StoredBlobInfo;
using UploadBlobOptions = SideSoftware.Storage.Config.UploadBlobOptions;

namespace SideSoftware.Storage.Services;

/// <summary>
/// Azure Blob Storage implementation of IBlobStorageService.
/// </summary>
public class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _serviceClient;
    private readonly BlobStorageSettings _settings;
    private readonly ILogger<AzureBlobStorageService> _logger;

    public AzureBlobStorageService(
        IOptions<BlobStorageSettings> settings,
        ILogger<AzureBlobStorageService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        _serviceClient = new BlobServiceClient(_settings.ConnectionString);
    }

    public async Task<UploadBlobResult> UploadAsync(
        string blobName,
        Stream content,
        UploadBlobOptions? options = null,
        CancellationToken ct = default)
    {
        options ??= new UploadBlobOptions();
        var containerName = options.ContainerName ?? _settings.DefaultContainer;
        var contentType = options.ContentType ?? InferContentType(blobName);

        // Validate file size if stream supports seeking
        if (content.CanSeek)
        {
            if (content.Length > _settings.MaxFileSizeBytes)
            {
                throw new BlobTooLargeException(content.Length, _settings.MaxFileSizeBytes, blobName);
            }
        }

        // Validate content type
        if (_settings.AllowedContentTypes.Count > 0 &&
            !_settings.AllowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
        {
            throw new BlobContentTypeNotAllowedException(contentType, _settings.AllowedContentTypes, blobName);
        }

        var containerClient = _serviceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: ct);

        var blobClient = containerClient.GetBlobClient(blobName);

        // Check if exists when overwrite is false
        if (!options.Overwrite && await blobClient.ExistsAsync(ct))
        {
            throw new BlobAlreadyExistsException(blobName, containerName);
        }

        // Create Azure SDK upload options (different from our UploadBlobOptions)
        var azureUploadOptions = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType },
            Metadata = options.Metadata
        };

        _logger.LogInformation(
            "Uploading blob {BlobName} to container {Container}",
            blobName, containerName);

        var response = await blobClient.UploadAsync(content, azureUploadOptions, ct);

        // Get the blob properties for the result
        var properties = await blobClient.GetPropertiesAsync(cancellationToken: ct);

        _logger.LogInformation(
            "Successfully uploaded blob {BlobName} ({Size} bytes)",
            blobName, properties.Value.ContentLength);

        return new UploadBlobResult(
            BlobName: blobName,
            ContainerName: containerName,
            FullPath: $"{containerName}/{blobName}",
            ContentType: contentType,
            SizeBytes: properties.Value.ContentLength,
            ETag: response.Value.ETag.ToString(),
            UploadedAt: DateTimeOffset.UtcNow);
    }

    public async Task<UploadBlobResult> UploadAsync(
        string blobName,
        byte[] content,
        UploadBlobOptions? options = null,
        CancellationToken ct = default)
    {
        using var stream = new MemoryStream(content);
        return await UploadAsync(blobName, stream, options, ct);
    }

    public async Task<DownloadBlobResult> DownloadAsync(
        string blobName,
        string? containerName = null,
        CancellationToken ct = default)
    {
        containerName ??= _settings.DefaultContainer;
        var blobClient = GetBlobClient(blobName, containerName);

        try
        {
            var response = await blobClient.DownloadStreamingAsync(cancellationToken: ct);
            var properties = response.Value.Details;

            return new DownloadBlobResult(
                Content: response.Value.Content,
                ContentType: properties.ContentType,
                SizeBytes: properties.ContentLength,
                ETag: properties.ETag.ToString(),
                LastModified: properties.LastModified,
                Metadata: properties.Metadata);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            throw new BlobNotFoundException(blobName, containerName);
        }
    }

    public async Task<byte[]> DownloadBytesAsync(
        string blobName,
        string? containerName = null,
        CancellationToken ct = default)
    {
        containerName ??= _settings.DefaultContainer;
        var blobClient = GetBlobClient(blobName, containerName);

        try
        {
            var response = await blobClient.DownloadContentAsync(ct);
            return response.Value.Content.ToArray();
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            throw new BlobNotFoundException(blobName, containerName);
        }
    }

    public async Task<StoredBlobInfo?> GetInfoAsync(
        string blobName,
        string? containerName = null,
        CancellationToken ct = default)
    {
        containerName ??= _settings.DefaultContainer;
        var blobClient = GetBlobClient(blobName, containerName);

        try
        {
            var properties = await blobClient.GetPropertiesAsync(cancellationToken: ct);

            return new StoredBlobInfo(
                BlobName: blobName,
                ContainerName: containerName,
                FullPath: $"{containerName}/{blobName}",
                ContentType: properties.Value.ContentType,
                SizeBytes: properties.Value.ContentLength,
                CreatedOn: properties.Value.CreatedOn,
                LastModified: properties.Value.LastModified,
                Metadata: properties.Value.Metadata);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<bool> ExistsAsync(
        string blobName,
        string? containerName = null,
        CancellationToken ct = default)
    {
        containerName ??= _settings.DefaultContainer;
        var blobClient = GetBlobClient(blobName, containerName);
        return await blobClient.ExistsAsync(ct);
    }

    public async Task<bool> DeleteAsync(
        string blobName,
        string? containerName = null,
        CancellationToken ct = default)
    {
        containerName ??= _settings.DefaultContainer;
        var blobClient = GetBlobClient(blobName, containerName);

        _logger.LogInformation(
            "Deleting blob {BlobName} from container {Container}",
            blobName, containerName);

        var response = await blobClient.DeleteIfExistsAsync(cancellationToken: ct);
        return response.Value;
    }

    public Task<BlobSasUrl> GetSasUrlAsync(
        string blobName,
        BlobSasOptions? options = null,
        CancellationToken ct = default)
    {
        options ??= new BlobSasOptions();
        var containerName = options.ContainerName ?? _settings.DefaultContainer;
        var expirationMinutes = options.ExpirationMinutes ?? _settings.DefaultSasExpirationMinutes;

        var blobClient = GetBlobClient(blobName, containerName);

        if (!blobClient.CanGenerateSasUri)
        {
            throw new BlobStorageException(
                "Cannot generate SAS URL. Ensure the connection string includes account key or use a BlobServiceClient with credentials.",
                blobName,
                containerName);
        }

        var expiresOn = DateTimeOffset.UtcNow.AddMinutes(expirationMinutes);

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = containerName,
            BlobName = blobName,
            Resource = "b",
            ExpiresOn = expiresOn
        };

        // Set permissions
        // Build permissions flags (default is 0, no permissions)
        var permissions = default(BlobSasPermissions);
        if (options.AllowRead) permissions |= BlobSasPermissions.Read;
        if (options.AllowWrite) permissions |= BlobSasPermissions.Write;
        if (options.AllowDelete) permissions |= BlobSasPermissions.Delete;
        sasBuilder.SetPermissions(permissions);

        // Set content disposition if specified
        if (!string.IsNullOrEmpty(options.ContentDisposition))
        {
            sasBuilder.ContentDisposition = options.ContentDisposition;
        }

        var sasUrl = blobClient.GenerateSasUri(sasBuilder);

        _logger.LogDebug(
            "Generated SAS URL for blob {BlobName}, expires {ExpiresOn}",
            blobName, expiresOn);

        return Task.FromResult(new BlobSasUrl(
            Url: sasUrl.ToString(),
            ExpiresOn: expiresOn,
            BlobName: blobName,
            ContainerName: containerName));
    }

    public async IAsyncEnumerable<StoredBlobInfo> ListAsync(
        string? prefix = null,
        string? containerName = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        containerName ??= _settings.DefaultContainer;
        var containerClient = _serviceClient.GetBlobContainerClient(containerName);
        var options = new GetBlobsOptions
        {
            Traits = BlobTraits.Metadata,
            Prefix = prefix
        };

        await foreach (var blobItem in containerClient.GetBlobsAsync(options, ct))
        {
            yield return new StoredBlobInfo(
                BlobName: blobItem.Name,
                ContainerName: containerName,
                FullPath: $"{containerName}/{blobItem.Name}",
                ContentType: blobItem.Properties.ContentType ?? "application/octet-stream",
                SizeBytes: blobItem.Properties.ContentLength ?? 0,
                CreatedOn: blobItem.Properties.CreatedOn,
                LastModified: blobItem.Properties.LastModified,
                Metadata: blobItem.Metadata);
        }
    }

    public async Task<UploadBlobResult> CopyAsync(
        string sourceBlobName,
        string destinationBlobName,
        string? sourceContainerName = null,
        string? destinationContainerName = null,
        CancellationToken ct = default)
    {
        sourceContainerName ??= _settings.DefaultContainer;
        destinationContainerName ??= _settings.DefaultContainer;

        var sourceClient = GetBlobClient(sourceBlobName, sourceContainerName);
        var destClient = GetBlobClient(destinationBlobName, destinationContainerName);

        // Ensure source exists
        if (!await sourceClient.ExistsAsync(ct))
        {
            throw new BlobNotFoundException(sourceBlobName, sourceContainerName);
        }

        // Ensure destination container exists
        var destContainer = _serviceClient.GetBlobContainerClient(destinationContainerName);
        await destContainer.CreateIfNotExistsAsync(cancellationToken: ct);

        _logger.LogInformation(
            "Copying blob from {Source} to {Destination}",
            $"{sourceContainerName}/{sourceBlobName}",
            $"{destinationContainerName}/{destinationBlobName}");

        // Start the copy operation
        var copyOperation = await destClient.StartCopyFromUriAsync(sourceClient.Uri, cancellationToken: ct);
        await copyOperation.WaitForCompletionAsync(ct);

        // Get properties of the new blob
        var properties = await destClient.GetPropertiesAsync(cancellationToken: ct);

        return new UploadBlobResult(
            BlobName: destinationBlobName,
            ContainerName: destinationContainerName,
            FullPath: $"{destinationContainerName}/{destinationBlobName}",
            ContentType: properties.Value.ContentType,
            SizeBytes: properties.Value.ContentLength,
            ETag: properties.Value.ETag.ToString(),
            UploadedAt: DateTimeOffset.UtcNow);
    }

    public async Task EnsureContainerExistsAsync(
        string? containerName = null,
        CancellationToken ct = default)
    {
        containerName ??= _settings.DefaultContainer;
        var containerClient = _serviceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: ct);
    }

    private BlobClient GetBlobClient(string blobName, string containerName)
    {
        var containerClient = _serviceClient.GetBlobContainerClient(containerName);
        return containerClient.GetBlobClient(blobName);
    }

    private static string InferContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".txt" => "text/plain",
            ".json" => "application/json",
            ".xml" => "application/xml",
            ".html" => "text/html",
            ".css" => "text/css",
            ".js" => "application/javascript",
            ".zip" => "application/zip",
            _ => "application/octet-stream"
        };
    }
}