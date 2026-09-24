using System.IO.Compression;
using System.Xml.Linq;
using AtsCv.Application.Documents;

namespace AtsCv.Infrastructure.Documents;

public sealed class DocxCvTextExtractor : ICvTextExtractor
{
    public bool CanHandle(DocumentType documentType) =>
        documentType == DocumentType.Docx;

    public async Task<string> ExtractTextAsync(
        Stream content,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        using var archive = new ZipArchive(content, ZipArchiveMode.Read, leaveOpen: true);
        var documentEntry = archive.GetEntry("word/document.xml")
            ?? throw new InvalidDataException("The DOCX document content is missing.");

        await using var documentStream = documentEntry.Open();
        using var reader = new StreamReader(documentStream);
        var xml = await reader.ReadToEndAsync(cancellationToken);

        var document = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
        var text = string.Join(
            " ",
            document
                .Descendants(XName.Get("t", "http://schemas.openxmlformats.org/wordprocessingml/2006/main"))
                .Select(node => node.Value));

        return text.Trim();
    }
}
