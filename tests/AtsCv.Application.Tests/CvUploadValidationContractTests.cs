using AtsCv.Application.Documents;
using AtsCv.Application.Resumes;
using Xunit;

namespace AtsCv.Application.Tests;

public sealed class CvUploadValidationContractTests
{
    [Fact]
    public void ValidationResult_WithoutErrors_IsValidAndReportsNoSpecificProblem()
    {
        var result = new CvUploadValidationResult(Array.Empty<string>());

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.False(result.HasUnsupportedFileType);
        Assert.False(result.HasFileTooLarge);
    }

    [Fact]
    public void ValidationResult_WithErrors_IsInvalid()
    {
        var result = new CvUploadValidationResult(new[] { "The file name is not valid." });

        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
    }

    [Fact]
    public void ValidationException_ExposesTheValidationResult()
    {
        var validation = new CvUploadValidationResult(new[] { "The file name is not valid." })
        {
            HasUnsupportedFileType = true
        };

        var exception = new CvUploadValidationException(validation);

        Assert.Same(validation, exception.ValidationResult);
        Assert.True(exception.ValidationResult.HasUnsupportedFileType);
    }

    [Fact]
    public void ValidationException_WithNullValidationResult_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new CvUploadValidationException(null!));
}
