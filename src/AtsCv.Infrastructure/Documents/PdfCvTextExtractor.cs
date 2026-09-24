using AtsCv.Application.Documents;

namespace AtsCv.Infrastructure.Documents;

public sealed class PdfCvTextExtractor : ICvTextExtractor
{
    public bool CanHandle(DocumentType documentType) =>
        documentType == DocumentType.Pdf;

    public Task<string> ExtractTextAsync(
        Stream content,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException(
            "Register a PDF extraction provider in Infrastructure before processing PDF files.");
    }
}
