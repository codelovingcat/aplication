using AtsCv.Application.Documents;
using AtsCv.Application.Persistence;
using AtsCv.Application.Resumes;
using Xunit;

namespace AtsCv.Application.Tests;

public sealed class CvIntakeServiceTests
{
    private static CvIntakeService CreateService(
        ICvTextExtractor extractor,
        long maxFileSizeBytes = CvUploadValidator.DefaultMaxFileSizeBytes) =>
        new(
            new CvUploadValidator(maxFileSizeBytes),
            new CvFileSignatureValidator(),
            new[] { extractor },
            new InMemoryCandidateProfileRepository(),
            new InMemoryResumeRepository(),
            new InMemoryUnitOfWork());

    [Fact]
    public async Task IntakeAsync_WithNullRequest_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => CreateService(new RecordingCvTextExtractor()).IntakeAsync(null!));
    }

    [Fact]
    public async Task IntakeAsync_WithNoFileName_RejectsTheRequestBeforeProcessing()
    {
        var extractor = new RecordingCvTextExtractor();
        var request = CvTestData.ValidPdfRequest() with { FileName = "   " };

        var exception = await Assert.ThrowsAsync<CvUploadValidationException>(
            () => CreateService(extractor).IntakeAsync(request));

        Assert.False(exception.ValidationResult.IsValid);
        Assert.Contains(
            exception.ValidationResult.Errors,
            error => error.Contains("file name", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(0, extractor.CallCount);
    }

    [Fact]
    public async Task IntakeAsync_WithUnsupportedExtension_RejectsTheRequestBeforeProcessing()
    {
        var extractor = new RecordingCvTextExtractor();
        var request = CvTestData.ValidPdfRequest() with { FileName = "cv.txt" };

        var exception = await Assert.ThrowsAsync<CvUploadValidationException>(
            () => CreateService(extractor).IntakeAsync(request));

        Assert.False(exception.ValidationResult.IsValid);
        Assert.True(exception.ValidationResult.HasUnsupportedFileType);
        Assert.Equal(0, extractor.CallCount);
    }

    [Fact]
    public async Task IntakeAsync_WithUnsupportedContentType_RejectsTheRequestBeforeProcessing()
    {
        var extractor = new RecordingCvTextExtractor();
        var request = CvTestData.ValidPdfRequest() with { ContentType = "application/octet-stream" };

        var exception = await Assert.ThrowsAsync<CvUploadValidationException>(
            () => CreateService(extractor).IntakeAsync(request));

        Assert.False(exception.ValidationResult.IsValid);
        Assert.True(exception.ValidationResult.HasUnsupportedFileType);
        Assert.Equal(0, extractor.CallCount);
    }

    [Fact]
    public async Task IntakeAsync_WithFileOverTheSizeLimit_ReportsFileTooLargeBeforeProcessing()
    {
        var extractor = new RecordingCvTextExtractor();
        var request = CvTestData.ValidPdfRequest() with
        {
            Content = new StubStream(CvUploadValidator.DefaultMaxFileSizeBytes + 1)
        };

        var exception = await Assert.ThrowsAsync<CvUploadValidationException>(
            () => CreateService(extractor).IntakeAsync(request));

        Assert.False(exception.ValidationResult.IsValid);
        Assert.True(exception.ValidationResult.HasFileTooLarge);
        Assert.Equal(0, extractor.CallCount);
    }

    [Fact]
    public async Task IntakeAsync_WithEmptyFile_RejectsTheRequestBeforeProcessing()
    {
        var extractor = new RecordingCvTextExtractor();
        var request = CvTestData.ValidPdfRequest() with { Content = new MemoryStream(Array.Empty<byte>()) };

        var exception = await Assert.ThrowsAsync<CvUploadValidationException>(
            () => CreateService(extractor).IntakeAsync(request));

        Assert.False(exception.ValidationResult.IsValid);
        Assert.Equal(0, extractor.CallCount);
    }

    [Fact]
    public async Task IntakeAsync_WithContentThatDoesNotMatchItsType_RejectsTheRequestBeforeExtraction()
    {
        var extractor = new RecordingCvTextExtractor();
        var request = CvTestData.ValidPdfRequest() with { Content = CvTestData.UnsupportedContent() };

        await Assert.ThrowsAsync<UnsupportedCvFileException>(
            () => CreateService(extractor).IntakeAsync(request));

        Assert.Equal(0, extractor.CallCount);
    }

    [Fact]
    public async Task IntakeAsync_WithValidUpload_ReturnsTheDraftCreatedFromExtractedText()
    {
        var extractor = new RecordingCvTextExtractor(extractedText: "Extracted CV body.");
        var request = CvTestData.ValidPdfRequest() with
        {
            AdditionalInformation = "  Recently certified in Kubernetes.  "
        };

        var result = await CreateService(extractor).IntakeAsync(request);

        Assert.Equal(CvTestData.ValidPdfFileName, result.FileName);
        Assert.Equal("Extracted CV body.", result.ExtractedText);
        Assert.Equal("Recently certified in Kubernetes.", result.AdditionalInformation);
        Assert.NotEqual(Guid.Empty, result.ResumeId);
        Assert.NotEqual(Guid.Empty, result.CandidateProfileId);
        Assert.Equal(1, extractor.CallCount);
        Assert.Equal(0, extractor.StreamPositionAtCall);
    }
    [Fact]
    public async Task IntakeAsync_WithValidUpload_PersistsRelatedDraftsWithOneSave()
    {
        var extractor = new RecordingCvTextExtractor(extractedText: "Extracted CV body.");
        var candidates = new InMemoryCandidateProfileRepository();
        var resumes = new InMemoryResumeRepository();
        var unitOfWork = new InMemoryUnitOfWork();
        var service = new CvIntakeService(
            new CvUploadValidator(),
            new CvFileSignatureValidator(),
            new[] { extractor },
            candidates,
            resumes,
            unitOfWork);

        var result = await service.IntakeAsync(CvTestData.ValidPdfRequest());

        var candidate = Assert.Single(candidates.All);
        var resume = Assert.Single(resumes.All);
        Assert.Equal(result.CandidateProfileId, candidate.Id);
        Assert.Equal(result.ResumeId, resume.Id);
        Assert.Equal(candidate.Id, resume.CandidateProfileId);
        Assert.Equal("Extracted CV body.", resume.ExtractedText);
        Assert.Equal(ResumeStatus.TextExtracted, resume.Status);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

}
