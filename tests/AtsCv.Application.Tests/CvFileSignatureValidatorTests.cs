using AtsCv.Application.Documents;
using Xunit;

namespace AtsCv.Application.Tests;

public sealed class CvFileSignatureValidatorTests
{
    private readonly CvFileSignatureValidator _validator = new();

    [Fact]
    public void IsValid_WithPdfSignature_ReturnsTrue()
    {
        using var content = CvTestData.PdfContent();

        Assert.True(_validator.IsValid(content, DocumentType.Pdf));
    }

    [Fact]
    public void IsValid_WithDocxSignature_ReturnsTrue()
    {
        using var content = CvTestData.DocxContent();

        Assert.True(_validator.IsValid(content, DocumentType.Docx));
    }

    [Fact]
    public void IsValid_WithContentOfTheOtherDocumentType_ReturnsFalse()
    {
        using var content = CvTestData.DocxContent();

        Assert.False(_validator.IsValid(content, DocumentType.Pdf));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(99)]
    public void IsValid_WithUnsupportedDocumentType_ReturnsFalse(int documentType)
    {
        using var content = CvTestData.PdfContent();

        Assert.False(_validator.IsValid(content, (DocumentType)documentType));
    }

    [Fact]
    public void IsValid_WithEmptyContent_ReturnsFalse()
    {
        using var content = new MemoryStream(Array.Empty<byte>());

        Assert.False(_validator.IsValid(content, DocumentType.Pdf));
    }

    [Fact]
    public void IsValid_WithTruncatedSignature_ReturnsFalse()
    {
        using var content = new MemoryStream(new byte[] { 0x25, 0x50 });

        Assert.False(_validator.IsValid(content, DocumentType.Pdf));
    }

    [Fact]
    public void IsValid_WithContentPrefixedByOtherBytes_ReturnsFalse()
    {
        // A PDF header that does not start at the beginning of the file is not accepted.
        using var content = new MemoryStream(new byte[] { 0x4A, 0x55, 0x4E, 0x4B, 0x25, 0x50, 0x44, 0x46, 0x2D });

        Assert.False(_validator.IsValid(content, DocumentType.Pdf));
    }

    [Fact]
    public void IsValid_WithNonSeekableContent_ReturnsFalse()
    {
        using var content = new StubStream(1024, canSeek: false);

        Assert.False(_validator.IsValid(content, DocumentType.Pdf));
    }

    [Fact]
    public void IsValid_WithUnreadableContent_ReturnsFalse()
    {
        using var content = new StubStream(1024, canRead: false);

        Assert.False(_validator.IsValid(content, DocumentType.Pdf));
    }

    [Fact]
    public void IsValid_WithNullContent_Throws() =>
        Assert.Throws<ArgumentNullException>(() => _validator.IsValid(null!, DocumentType.Pdf));

    [Fact]
    public void IsValid_WithCancelledToken_Throws()
    {
        using var content = CvTestData.PdfContent();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(
            () => _validator.IsValid(content, DocumentType.Pdf, cancellation.Token));
    }

    [Fact]
    public void IsValid_WhenSignatureIsReadInPartialChunks_ReturnsTrue()
    {
        using var content = new PartialReadStream(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31 }, maxBytesPerRead: 1);

        Assert.True(_validator.IsValid(content, DocumentType.Pdf));
    }

    [Fact]
    public void IsValid_WithMatchingContent_PreservesContentPosition()
    {
        using var content = CvTestData.PdfContent();

        Assert.True(_validator.IsValid(content, DocumentType.Pdf));
        Assert.Equal(0, content.Position);
    }

    [Fact]
    public void IsValid_WithNonMatchingContent_PreservesContentPosition()
    {
        using var content = CvTestData.PdfContent();
        content.Position = 1;

        Assert.False(_validator.IsValid(content, DocumentType.Docx));
        Assert.Equal(1, content.Position);
    }
}
