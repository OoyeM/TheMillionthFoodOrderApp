using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TheMillionthFoodOrderApp.Application.BrandSettings;

namespace TheMillionthFoodOrderApp.Infrastructure.FileStorage;

/// <summary>
/// Options for <see cref="LocalFileStorageService"/>.
/// </summary>
public sealed class LocalFileStorageOptions
{
    /// <summary>
    /// The absolute path to the directory where uploaded files will be stored.
    /// This directory must be served as static files (e.g. wwwroot/uploads).
    /// </summary>
    public string UploadsPath { get; set; } = string.Empty;

    /// <summary>
    /// The URL prefix under which uploaded files are publicly reachable (e.g. "/uploads").
    /// </summary>
    public string UrlPrefix { get; set; } = "/uploads";
}

/// <summary>
/// Local filesystem implementation of <see cref="IFileStorageService"/>.
/// Files are stored in the configured <see cref="LocalFileStorageOptions.UploadsPath"/> and
/// served as static files from the matching URL prefix.
/// This implementation is intended for development only. Use Azure Blob Storage in production.
/// </summary>
public sealed class LocalFileStorageService(
    IOptions<LocalFileStorageOptions> options,
    ILogger<LocalFileStorageService> logger) : IFileStorageService
{
    private readonly LocalFileStorageOptions _options = options.Value;

    public async Task<string> SaveAsync(
        string fileName,
        string contentType,
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_options.UploadsPath);

        // Sanitise the caller-supplied name to prevent path-traversal attacks
        var safeName = Path.GetFileName(fileName)
            .Replace("\0", string.Empty)
            .Trim();
        if (string.IsNullOrEmpty(safeName))
            safeName = "upload";

        // Prefix with a timestamp-based segment to avoid name collisions
        var uniqueName = $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{safeName}";
        var filePath = Path.Combine(_options.UploadsPath, uniqueName);

        await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await stream.CopyToAsync(fileStream, cancellationToken);

        var url = $"{_options.UrlPrefix.TrimEnd('/')}/{uniqueName}";
        logger.LogInformation("Saved uploaded file to '{FilePath}' (URL: {Url}).", filePath, url);

        return url;
    }

    public Task DeleteAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        // Extract file name from the URL and resolve to physical path
        var fileName = Path.GetFileName(fileUrl);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            logger.LogWarning("Cannot delete file: could not parse file name from URL '{Url}'.", fileUrl);
            return Task.CompletedTask;
        }

        var filePath = Path.Combine(_options.UploadsPath, fileName);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            logger.LogInformation("Deleted uploaded file '{FilePath}'.", filePath);
        }
        else
        {
            logger.LogDebug("Delete requested for '{FilePath}' but file does not exist — ignoring.", filePath);
        }

        return Task.CompletedTask;
    }
}
