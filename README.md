# SideSoftware.Storage

Azure Blob Storage abstractions and implementation for .NET 10, with configurable file size limits, content type validation, SAS URL generation, and async streaming.

## Installation

```bash
dotnet add package SideSoftware.Storage
```

## Configuration

Add blob storage settings to your `appsettings.json`:

```json
{
  "BlobStorage": {
    "ConnectionString": "UseDevelopmentStorage=true",
    "DefaultContainer": "documents",
    "DefaultSasExpirationMinutes": 60,
    "MaxFileSizeBytes": 10485760,
    "AllowedContentTypes": [
      "image/jpeg",
      "image/png"
    ]
  }
}
```

Register services in your DI container:

```csharp
builder.Services.Configure<BlobStorageSettings>(
    builder.Configuration.GetSection(BlobStorageSettings.SectionName));

builder.Services.AddSingleton<IBlobStorageService, AzureBlobStorageService>();
```

## Usage

### Upload a file

```csharp
var result = await blobStorage.UploadAsync("reports/monthly.pdf", fileStream);
// result.FullPath => "documents/reports/monthly.pdf"
```

### Upload with options

```csharp
var result = await blobStorage.UploadAsync("photo.jpg", imageBytes, new UploadBlobOptions
{
    ContainerName = "images",
    ContentType = "image/jpeg",
    Overwrite = false,
    Metadata = new Dictionary<string, string> { ["Author"] = "Jane" }
});
```

### Download a file

```csharp
// Streaming download
var result = await blobStorage.DownloadAsync("reports/monthly.pdf");
await result.Content.CopyToAsync(outputStream);

// Byte array download
byte[] bytes = await blobStorage.DownloadBytesAsync("reports/monthly.pdf");
```

### Generate a SAS URL

```csharp
var sas = await blobStorage.GetSasUrlAsync("reports/monthly.pdf", new BlobSasOptions
{
    ExpirationMinutes = 30,
    AllowRead = true,
    ContentDisposition = "attachment; filename=\"report.pdf\""
});
// sas.Url => time-limited URL for direct access
```

### List blobs

```csharp
await foreach (var blob in blobStorage.ListAsync(prefix: "reports/"))
{
    Console.WriteLine($"{blob.BlobName} ({blob.SizeBytes} bytes)");
}
```

### Other operations

```csharp
bool exists = await blobStorage.ExistsAsync("photo.jpg");
StoredBlobInfo? info = await blobStorage.GetInfoAsync("photo.jpg");
bool deleted = await blobStorage.DeleteAsync("photo.jpg");
await blobStorage.CopyAsync("photo.jpg", "photo-backup.jpg");
await blobStorage.EnsureContainerExistsAsync("my-container");
```

## Credential Document Storage

A higher-level `ICredentialDocumentStorage` interface is included for managing credential documents (e.g., pilot licenses, medical certificates) with structured blob paths and metadata:

```csharp
builder.Services.AddSingleton<ICredentialDocumentStorage, CredentialDocumentStorage>();
```

```csharp
// Upload
var doc = await credentialStorage.UploadAsync(
    personId, "pilot-license", fileStream, "license.pdf");

// Get a time-limited view URL
string viewUrl = await credentialStorage.GetViewUrlAsync(doc.BlobPath);

// Get a download URL (prompts browser to save)
string downloadUrl = await credentialStorage.GetDownloadUrlAsync(
    doc.BlobPath, "pilot-license.pdf");

// List all documents for a person
await foreach (var item in credentialStorage.ListForPersonAsync(personId))
{
    Console.WriteLine($"{item.OriginalFileName} - {item.ContentType}");
}
```

## License

[MIT](LICENSE)
