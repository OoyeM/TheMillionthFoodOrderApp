namespace TheMillionthFoodOrderApp.Application.BrandSettings;

/// <summary>
/// Abstraction for storing and retrieving uploaded files (e.g. brand logos).
/// Implementations live in the Infrastructure layer:
/// - Development: <c>LocalFileStorageService</c> (writes to wwwroot/uploads/)
/// - Production: Azure Blob Storage implementation (not yet implemented)
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Saves the uploaded file and returns the publicly accessible URL.
    /// </summary>
    /// <param name="fileName">
    /// The target file name (including extension). The implementation is responsible for
    /// sanitising this value (stripping path separators, null bytes, etc.) and may add a
    /// uniqueness prefix to avoid collisions.
    /// </param>
    /// <param name="contentType">MIME type of the uploaded file (e.g. "image/png").</param>
    /// <param name="stream">The file content stream. The caller is responsible for disposing it.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The absolute or root-relative URL under which the file is publicly reachable.</returns>
    Task<string> SaveAsync(
        string fileName,
        string contentType,
        Stream stream,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a previously uploaded file identified by its public URL.
    /// Silently succeeds if the file does not exist.
    /// </summary>
    /// <param name="fileUrl">The URL returned by a prior call to <see cref="SaveAsync"/>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteAsync(string fileUrl, CancellationToken cancellationToken = default);
}
