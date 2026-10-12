using AtsCv.Application.Documents;
using Xunit;

namespace AtsCv.Application.Tests;

public sealed class CvUploadValidatorTests
{
    // Small configurable limits keep the boundary tests cheap and independent of the
    // production defaults. The production defaults are pinned separately below.
    private const int ConfiguredFileSizeLimit = 512;
    private const int ConfiguredAdditionalInformationLimit = 64;

    private static CvUploadValidator CreateValidator(
        long maxFileSizeBytes = CvUploadValidator.DefaultMaxFileSizeBytes,
        int maxAdditionalInformationLength = CvUploadValidator.DefaultMaxAdditionalInformationLength) =>
        new(maxFileSizeBytes, maxAdditionalInformationLength);

    private static CvUploadValidator CreateValidatorWithSmallLimits() =>
        CreateValidator(ConfiguredFileSizeLimit, ConfiguredAdditionalInformationLimit);

    [Fact]
    public void DefaultLimits_MatchTheDocumentedUploadContract()
    {
        Assert.Equal(10L * 1024 * 1024, CvUploadValidator.DefaultMaxFileSizeBytes);
        Assert.Equal(2000, CvUploadValidator.DefaultMaxAdditionalInformationLength);
    }

    [Fact]
    public void Validate_WithNullRequest_Throws() =>
        Assert.Throws<ArgumentNullException>(() => CreateValidator().Validate(null!));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithNonPositiveFileSizeLimit_Throws(int maxFileSizeBytes) =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CvUploadValidator(maxFileSizeBytes: maxFileSizeBytes));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithNonPositiveAdditionalInformationLimit_Throws(int maxAdditionalInformationLength) =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CvUploadValidator(maxAdditionalInformationLength: maxAdditionalInformationLength));

    [Fact]
    public void Validate_WithValidPdfRequest_ReturnsValidResult()
    {
        var result = CreateValidator().Validate(CvTestData.ValidPdfRequest());

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.False(result.HasUnsupportedFileType);
        Assert.False(result.HasFileTooLarge);
    }

    [Fact]
    public void Validate_WithValidDocxRequest_ReturnsValidResult()
    {
        var result = CreateValidator().Validate(CvTestData.ValidDocxRequest());

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.False(result.HasUnsupportedFileType);
        Assert.False(result.HasFileTooLarge);
    }

    [Theory]
    [InlineData("CV.PDF")]
    [InlineData("Cv.PdF")]
    [InlineData("cv.Pdf")]
    public void Validate_WithUpperCasePdfExtension_ReturnsValidResult(string fileName)
    {
        var request = CvTestData.ValidPdfRequest() with { FileName = fileName };

        Assert.True(CreateValidator().Validate(request).IsValid);
    }

    [Theory]
    [InlineData("CV.DOCX")]
    [InlineData("cv.Docx")]
    [InlineData("Cv.docX")]
    public void Validate_WithUpperCaseDocxExtension_ReturnsValidResult(string fileName)
    {
        var request = CvTestData.ValidDocxRequest() with { FileName = fileName };

        Assert.True(CreateValidator().Validate(request).IsValid);
    }

    [Theory]
    [InlineData("APPLICATION/PDF")]
    [InlineData("Application/Pdf")]
    public void Validate_WithUpperCaseContentType_ReturnsValidResult(string contentType)
    {
        var request = CvTestData.ValidPdfRequest() with { ContentType = contentType };

        Assert.True(CreateValidator().Validate(request).IsValid);
    }

    [Theory]
    [InlineData(" cv.pdf")]
    [InlineData("cv.pdf ")]
    [InlineData("  cv.pdf  ")]
    [InlineData("\tcv.pdf")]
    public void Validate_WithSurroundingWhitespaceInFileName_ReturnsValidResult(string fileName)
    {
        var request = CvTestData.ValidPdfRequest() with { FileName = fileName };

        Assert.True(CreateValidator().Validate(request).IsValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Recently completed an AWS certification.")]
    public void Validate_WithOptionalAdditionalInformation_ReturnsValidResult(string? additionalInformation)
    {
        var request = CvTestData.ValidPdfRequest() with { AdditionalInformation = additionalInformation };

        var result = CreateValidator().Validate(request);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithSingleByteFile_ReturnsValidResult()
    {
        var request = CvTestData.ValidPdfRequest() with { Content = new MemoryStream(new byte[] { 0x25 }) };

        Assert.True(CreateValidator().Validate(request).IsValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\r\n")]
    public void Validate_WithoutFileName_ReportsMissingFileName(string? fileName)
    {
        var request = CvTestData.ValidPdfRequest() with { FileName = fileName! };

        var result = CreateValidator().Validate(request);

        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Contains(
            result.Errors,
            error => error.Contains("file name", StringComparison.OrdinalIgnoreCase));
        Assert.False(result.HasUnsupportedFileType);
        Assert.False(result.HasFileTooLarge);
    }

    [Theory]
    [InlineData("cv/other.pdf")]
    [InlineData("uploads/cv.pdf")]
    [InlineData("folder\\cv.pdf")]
    [InlineData("..\\..\\cv.pdf")]
    [InlineData("../../etc/passwd.pdf")]
    [InlineData("C:\\Users\\candidate\\cv.pdf")]
    [InlineData("cv\u0000.pdf")]
    public void Validate_WithPathSegmentsInFileName_ReportsInvalidFileName(string fileName)
    {
        var request = CvTestData.ValidPdfRequest() with { FileName = fileName };

        var result = CreateValidator().Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("path", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(".")]
    [InlineData("..")]
    public void Validate_WithDotOnlyFileName_ReportsInvalidFileName(string fileName)
    {
        var request = CvTestData.ValidPdfRequest() with { FileName = fileName };

        var result = CreateValidator().Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("not valid", StringComparison.OrdinalIgnoreCase));
        Assert.True(result.HasUnsupportedFileType);
    }

    [Theory]
    [InlineData("cv.txt")]
    [InlineData("cv.doc")]
    [InlineData("cv.rtf")]
    [InlineData("cv.pdf.exe")]
    [InlineData("cv")]
    [InlineData("cv.")]
    public void Validate_WithUnsupportedExtension_ReportsUnsupportedFileType(string fileName)
    {
        var request = CvTestData.ValidPdfRequest() with { FileName = fileName };

        var result = CreateValidator().Validate(request);

        Assert.False(result.IsValid);
        Assert.True(result.HasUnsupportedFileType);
        Assert.Contains(result.Errors, error => error.Contains("extension", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_WithPdfExtensionForDocxRequest_ReportsUnsupportedFileType()
    {
        var request = CvTestData.ValidDocxRequest() with { FileName = "cv.pdf" };

        var result = CreateValidator().Validate(request);

        Assert.False(result.IsValid);
        Assert.True(result.HasUnsupportedFileType);
        Assert.Single(result.Errors);
    }

    [Fact]
    public void Validate_WithDocxExtensionForPdfRequest_ReportsUnsupportedFileType()
    {
        var request = CvTestData.ValidPdfRequest() with { FileName = "cv.docx" };

        var result = CreateValidator().Validate(request);

        Assert.False(result.IsValid);
        Assert.True(result.HasUnsupportedFileType);
        Assert.Single(result.Errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("text/plain")]
    [InlineData("application/octet-stream")]
    [InlineData("application/msword")]
    [InlineData("image/png")]
    public void Validate_WithUnsupportedContentType_ReportsUnsupportedFileType(string? contentType)
    {
        var request = CvTestData.ValidPdfRequest() with { ContentType = contentType! };

        var result = CreateValidator().Validate(request);

        Assert.False(result.IsValid);
        Assert.True(result.HasUnsupportedFileType);
        Assert.Single(result.Errors);
        Assert.Contains(result.Errors, error => error.Contains("content type", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_WithContentTypeOfTheOtherDocumentType_ReportsUnsupportedFileType()
    {
        var request = CvTestData.ValidPdfRequest() with { ContentType = CvTestData.DocxContentType };

        var result = CreateValidator().Validate(request);

        Assert.False(result.IsValid);
        Assert.True(result.HasUnsupportedFileType);
        Assert.Single(result.Errors);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(99)]
    public void Validate_WithUnsupportedDocumentType_ReportsUnsupportedFileType(int documentType)
    {
        var request = CvTestData.ValidPdfRequest() with { DocumentType = (DocumentType)documentType };

        var result = CreateValidator().Validate(request);

        Assert.False(result.IsValid);
        Assert.True(result.HasUnsupportedFileType);
        Assert.True(
            result.Errors.Count >= 2,
            $"Expected the extension and content type checks to report a problem, but got: {string.Join(" | ", result.Errors)}");
    }

    [Fact]
    public void Validate_WithSeveralInconsistencies_ReportsEveryProblemAndBothFlags()
    {
        var request = CvTestData.ValidPdfRequest() with
        {
            FileName = "cv.txt",
            ContentType = "text/plain",
            AdditionalInformation = new string('a', ConfiguredAdditionalInformationLimit + 1),
            Content = new MemoryStream(new byte[ConfiguredFileSizeLimit + 1])
        };

        var result = CreateValidatorWithSmallLimits().Validate(request);

        Assert.False(result.IsValid);
        Assert.True(result.HasUnsupportedFileType);
        Assert.True(result.HasFileTooLarge);
        Assert.True(
            result.Errors.Count >= 4,
            $"Expected every problem to be reported, but got: {string.Join(" | ", result.Errors)}");
    }

    [Fact]
    public void Validate_WithNullContent_ReportsUnreadableContent()
    {
        var request = CvTestData.ValidPdfRequest() with { Content = null! };

        var result = CreateValidator().Validate(request);

        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Contains(result.Errors, error => error.Contains("readable", StringComparison.OrdinalIgnoreCase));
        Assert.False(result.HasUnsupportedFileType);
        Assert.False(result.HasFileTooLarge);
    }

    [Fact]
    public void Validate_WithUnreadableContent_ReportsUnreadableContent()
    {
        var request = CvTestData.ValidPdfRequest() with { Content = new StubStream(1024, canRead: false) };

        var result = CreateValidator().Validate(request);

        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Contains(result.Errors, error => error.Contains("readable", StringComparison.OrdinalIgnoreCase));
        Assert.False(result.HasFileTooLarge);
    }

    [Fact]
    public void Validate_WithEmptyContent_ReportsEmptyFile()
    {
        var request = CvTestData.ValidPdfRequest() with { Content = new MemoryStream(Array.Empty<byte>()) };

        var result = CreateValidator().Validate(request);

        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Contains(result.Errors, error => error.Contains("empty", StringComparison.OrdinalIgnoreCase));
        Assert.False(result.HasUnsupportedFileType);
        Assert.False(result.HasFileTooLarge);
    }

    [Fact]
    public void Validate_WithNonSeekableContent_ReportsNoSizeProblem()
    {
        // The file size is only checked when the stream reports a length. The upload
        // endpoint always hands over a seekable stream, and content that cannot be read
        // at all is still rejected by the checks above and by the signature validation.
        var request = CvTestData.ValidPdfRequest() with { Content = new StubStream(1024, canSeek: false) };

        var result = CreateValidator().Validate(request);

        Assert.False(result.HasFileTooLarge);
        Assert.DoesNotContain(result.Errors, error => error.Contains("exceed", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_WithFileExactlyAtConfiguredLimit_ReturnsValidResult()
    {
        var request = CvTestData.ValidPdfRequest() with
        {
            Content = new MemoryStream(new byte[ConfiguredFileSizeLimit])
        };

        var result = CreateValidatorWithSmallLimits().Validate(request);

        Assert.True(result.IsValid);
        Assert.False(result.HasFileTooLarge);
    }

    [Fact]
    public void Validate_WithFileOneByteOverConfiguredLimit_ReportsFileTooLarge()
    {
        var request = CvTestData.ValidPdfRequest() with
        {
            Content = new MemoryStream(new byte[ConfiguredFileSizeLimit + 1])
        };

        var result = CreateValidatorWithSmallLimits().Validate(request);

        Assert.False(result.IsValid);
        Assert.True(result.HasFileTooLarge);
        Assert.Single(result.Errors);
        Assert.Contains(result.Errors, error => error.Contains("must not exceed", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_WithFileExactlyAtDefaultLimit_ReturnsValidResult()
    {
        var request = CvTestData.ValidPdfRequest() with
        {
            Content = new StubStream(CvUploadValidator.DefaultMaxFileSizeBytes)
        };

        var result = CreateValidator().Validate(request);

        Assert.True(result.IsValid);
        Assert.False(result.HasFileTooLarge);
    }

    [Fact]
    public void Validate_WithFileOneByteOverDefaultLimit_ReportsFileTooLarge()
    {
        var request = CvTestData.ValidPdfRequest() with
        {
            Content = new StubStream(CvUploadValidator.DefaultMaxFileSizeBytes + 1)
        };

        var result = CreateValidator().Validate(request);

        Assert.False(result.IsValid);
        Assert.True(result.HasFileTooLarge);
        Assert.False(result.HasUnsupportedFileType);
        Assert.Single(result.Errors);
    }

    [Fact]
    public void Validate_WithAdditionalInformationExactlyAtConfiguredLimit_ReturnsValidResult()
    {
        var request = CvTestData.ValidPdfRequest() with
        {
            AdditionalInformation = new string('a', ConfiguredAdditionalInformationLimit)
        };

        Assert.True(CreateValidatorWithSmallLimits().Validate(request).IsValid);
    }

    [Fact]
    public void Validate_WithAdditionalInformationOneCharacterOverConfiguredLimit_ReportsLengthProblem()
    {
        var request = CvTestData.ValidPdfRequest() with
        {
            AdditionalInformation = new string('a', ConfiguredAdditionalInformationLimit + 1)
        };

        var result = CreateValidatorWithSmallLimits().Validate(request);

        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Contains(
            result.Errors,
            error => error.Contains("Additional information", StringComparison.OrdinalIgnoreCase));
        Assert.False(result.HasUnsupportedFileType);
        Assert.False(result.HasFileTooLarge);
    }

    [Fact]
    public void Validate_WithAdditionalInformationExactlyAtDefaultLimit_ReturnsValidResult()
    {
        var request = CvTestData.ValidPdfRequest() with
        {
            AdditionalInformation = new string('a', CvUploadValidator.DefaultMaxAdditionalInformationLength)
        };

        Assert.True(CreateValidator().Validate(request).IsValid);
    }

    [Fact]
    public void Validate_WithAdditionalInformationOneCharacterOverDefaultLimit_ReportsLengthProblem()
    {
        var request = CvTestData.ValidPdfRequest() with
        {
            AdditionalInformation = new string('a', CvUploadValidator.DefaultMaxAdditionalInformationLength + 1)
        };

        var result = CreateValidator().Validate(request);

        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Contains(
            result.Errors,
            error => error.Contains("Additional information", StringComparison.OrdinalIgnoreCase));
    }
}
