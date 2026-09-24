namespace AtsCv.Application.Documents;

public interface ICvTextExtractor
{
    bool CanHandle(DocumentType documentType);

    Task<string> ExtractTextAsync(
        Stream content,
        CancellationToken cancellationToken = default);
}
