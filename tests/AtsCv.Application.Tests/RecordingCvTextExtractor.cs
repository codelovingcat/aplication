using AtsCv.Application.Documents;

namespace AtsCv.Application.Tests;

/// <summary>
/// Text extractor double that records whether downstream processing was started, so the
/// tests can prove that rejected uploads never reach the extraction step.
/// </summary>
internal sealed class RecordingCvTextExtractor : ICvTextExtractor
{
    private readonly DocumentType _handledDocumentType;
    private readonly string _extractedText;

    public RecordingCvTextExtractor(
        DocumentType handledDocumentType = DocumentType.Pdf,
        string extractedText = "Extracted CV text.")
    {
        _handledDocumentType = handledDocumentType;
        _extractedText = extractedText;
    }

    public int CallCount { get; private set; }

    /// <summary>Position of the content stream when extraction was requested.</summary>
    public long? StreamPositionAtCall { get; private set; }

    public bool CanHandle(DocumentType documentType) => documentType == _handledDocumentType;

    public Task<string> ExtractTextAsync(Stream content, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        cancellationToken.ThrowIfCancellationRequested();

        CallCount++;
        StreamPositionAtCall = content.CanSeek ? content.Position : null;

        return Task.FromResult(_extractedText);
    }
}
