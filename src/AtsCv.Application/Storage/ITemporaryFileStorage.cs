namespace AtsCv.Application.Storage;

public interface ITemporaryFileStorage
{
    Task<string> SaveAsync(
        Stream content,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string storageId,
        CancellationToken cancellationToken = default);
}
