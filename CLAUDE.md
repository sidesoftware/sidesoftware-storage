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

NuGet metadata is in `src/Storage/Storage.csproj`. The `README.md` at repo root is packed into the `.nupkg` via `PackageReadmeFile`.

## Publishing to NuGet

**One-time setup:** Create an API key at https://www.nuget.org/account/apikeys (scope it to push for `SideSoftware.Storage`). Store it somewhere safe — you'll need it for the push command.

**Steps to publish a new version:**

1. **Bump the version** in `src/Storage/Storage.csproj` — update `<Version>` (e.g., `1.0.0` → `1.1.0`)
2. **Ensure tests pass:**
   ```bash
   dotnet test tests/Storage.Tests/Storage.Tests.csproj
   ```
3. **Pack in Release mode:**
   ```bash
   dotnet pack src/Storage/Storage.csproj -c Release -o ./nupkg
   ```
4. **Inspect the package** (optional but recommended):
   ```bash
   unzip -l ./nupkg/SideSoftware.Storage.<VERSION>.nupkg
   ```
5. **Push to NuGet:**
   ```bash
   dotnet nuget push ./nupkg/SideSoftware.Storage.<VERSION>.nupkg --api-key <YOUR_API_KEY> --source https://api.nuget.org/v3/index.json
   ```
6. **Tag the release** in git:
   ```bash
   git tag v<VERSION>
   git push origin v<VERSION>
   ```

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
