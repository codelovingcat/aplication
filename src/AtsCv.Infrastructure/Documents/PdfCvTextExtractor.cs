using AtsCv.Application.Documents;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace AtsCv.Infrastructure.Documents;

public sealed class PdfCvTextExtractor : ICvTextExtractor
{
    public bool CanHandle(DocumentType documentType) =>
        documentType == DocumentType.Pdf;

    public Task<string> ExtractTextAsync(
        Stream content,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            using var document = PdfDocument.Open(content);
            var text = new List<string>();

            foreach (var page in document.GetPages())
            {
                cancellationToken.ThrowIfCancellationRequested();
                text.Add(ContentOrderTextExtractor.GetText(page));
            }

            var result = string.Join(Environment.NewLine, text).Trim();

            if (string.IsNullOrWhiteSpace(result))
            {
                throw new InvalidOperationException(
                    "The PDF does not contain extractable text.");
            }

            return Task.FromResult(result);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "The PDF could not be read or text could not be extracted.",
                ex);
        }
    }
}
