using AtsCv.Application.Documents;

namespace AtsCv.Infrastructure.Documents;

public sealed class DocxCvTextExtractor : ICvTextExtractor
{
    public bool CanHandle(DocumentType documentType) =>
        documentType == DocumentType.Docx;

    public Task<string> ExtractTextAsync(
        Stream content,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException(
            "Register a DOCX extraction provider in Infrastructure before processing DOCX files.");
    }
}
