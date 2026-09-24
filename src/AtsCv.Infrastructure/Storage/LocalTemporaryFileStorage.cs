using AtsCv.Application.Storage;

namespace AtsCv.Infrastructure.Storage;

public sealed class LocalTemporaryFileStorage : ITemporaryFileStorage
{
    private readonly string _rootPath;

    public LocalTemporaryFileStorage(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
            throw new ArgumentException("A storage root path is required.", nameof(rootPath));

        _rootPath = Path.GetFullPath(rootPath);
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> SaveAsync(
        Stream content,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        var storageId = $"{Guid.NewGuid():N}.tmp";
        var targetPath = Path.Combine(_rootPath, storageId);

        await using var file = new FileStream(
            targetPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 64 * 1024,
            useAsync: true);

        await content.CopyToAsync(file, cancellationToken);
        return storageId;
    }

    public Task DeleteAsync(
        string storageId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storageId))
            return Task.CompletedTask;

        cancellationToken.ThrowIfCancellationRequested();

        var safeId = Path.GetFileName(storageId);
        if (!string.Equals(storageId, safeId, StringComparison.Ordinal))
            throw new ArgumentException("Invalid storage identifier.", nameof(storageId));

        var targetPath = Path.Combine(_rootPath, safeId);

        if (File.Exists(targetPath))
            File.Delete(targetPath);

        return Task.CompletedTask;
    }
}
