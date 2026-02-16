using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SideSoftware.Storage.Abstractions;
using SideSoftware.Storage.Config;
using SideSoftware.Storage.Services;

namespace SideSoftware.Storage.Extensions;

public static class BlobStorageServiceCollectionExtensions
{
    /// <summary>
    /// Adds Azure Blob Storage services to the service collection.
    /// Reads configuration from the "BlobStorage" section.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Configuration containing BlobStorage section.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBlobStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var section = configuration.GetSection(BlobStorageSettings.SectionName);

        services.Configure<BlobStorageSettings>(options =>
        {
            options.ConnectionString = section[nameof(BlobStorageSettings.ConnectionString)] ?? "";
            options.DefaultContainer = section[nameof(BlobStorageSettings.DefaultContainer)] ?? "documents";

            if (int.TryParse(section[nameof(BlobStorageSettings.DefaultSasExpirationMinutes)], out var sasExpiration))
                options.DefaultSasExpirationMinutes = sasExpiration;

            if (long.TryParse(section[nameof(BlobStorageSettings.MaxFileSizeBytes)], out var maxSize))
                options.MaxFileSizeBytes = maxSize;

            // Read AllowedContentTypes as array from config
            var allowedTypesSection = section.GetSection(nameof(BlobStorageSettings.AllowedContentTypes));
            var allowedTypes = allowedTypesSection.GetChildren()
                .Select(c => c.Value)
                .Where(v => !string.IsNullOrEmpty(v))
                .Cast<string>()
                .ToList();

            if (allowedTypes.Count > 0)
            {
                options.AllowedContentTypes = allowedTypes;
            }
        });

        services.AddSingleton<IBlobStorageService, AzureBlobStorageService>();

        return services;
    }

    /// <summary>
    /// Adds Azure Blob Storage services with custom configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure settings.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBlobStorage(
        this IServiceCollection services,
        Action<BlobStorageSettings> configure)
    {
        services.Configure(configure);
        services.AddSingleton<IBlobStorageService, AzureBlobStorageService>();

        return services;
    }
}