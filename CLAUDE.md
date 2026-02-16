# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A .NET 10 library (`SideSoftware.Storage`) providing abstractions and an Azure Blob Storage implementation for blob operations. Published as a NuGet package.

## Build Commands

```bash
dotnet build src/Storage/Storage.csproj                # Build
dotnet test tests/Storage.Tests/Storage.Tests.csproj   # Run tests
dotnet pack src/Storage/Storage.csproj -o ./nupkg      # Create NuGet package
```

Solution file uses `.slnx` format: `SideSoftware.Storage.slnx`

NuGet metadata is in `src/Storage/Storage.csproj`. The `README.md` at repo root is packed into the `.nupkg` via `PackageReadmeFile`. Bump `<Version>` in the csproj before publishing.

## Architecture

**Two-layer abstraction pattern:**

1. **`IBlobStorageService`** — General-purpose blob storage contract (upload, download, delete, list, SAS URLs, copy, exists, metadata). Implemented by `AzureBlobStorageService`.
2. **`ICredentialDocumentStorage`** — Domain-specific wrapper for credential documents (pilot licenses, medical certs). Implemented by `CredentialDocumentStorage`, which delegates to `IBlobStorageService` using container `"credentials"` and path scheme `{personId}/{credentialType}/{guid}{extension}`.

**Namespace layout:**

| Namespace | Contents |
|---|---|
| `SideSoftware.Storage.Abstractions` | `IBlobStorageService`, `ICredentialDocumentStorage` |
| `SideSoftware.Storage.Config` | `BlobStorageSettings` (Options pattern), `UploadBlobOptions` |
| `SideSoftware.Storage.Models` | `StoredBlobInfo`, `BlobSasUrl`, `BlobSasOptions`, `CredentialDocumentInfo` |
| `SideSoftware.Storage.Results` | `UploadBlobResult`, `DownloadBlobResult` |
| `SideSoftware.Storage.Exceptions` | `BlobStorageException` base + `BlobNotFoundException`, `BlobAlreadyExistsException`, `BlobTooLargeException`, `BlobContentTypeNotAllowedException` |
| `SideSoftware.Storage.Services` | `AzureBlobStorageService` |
| `SideSoftware.Storage` | `CredentialDocumentStorage` |

**Key patterns:**
- `IOptions<BlobStorageSettings>` for configuration injection
- `IAsyncEnumerable<T>` for streaming blob listings
- Azure SDK `RequestFailedException` with status code matching converted to domain exceptions
- No DI registration extensions yet — consumers register services manually

## Code Conventions

- **C# 13 / .NET 10**: primary constructors, collection expressions (`[]`), file-scoped namespaces
- **Records** for all DTOs/models/results (positional for simple types, property syntax when defaults needed)
- **Nullable reference types** enabled
- **CancellationToken parameter** named `ct`
- **Structured logging** with `{PropertyName}` templates and `logger.IsEnabled()` guard checks
- **`StringComparer.OrdinalIgnoreCase`** for content-type comparisons
