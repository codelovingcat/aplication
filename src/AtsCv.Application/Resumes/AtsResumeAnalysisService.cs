using AtsCv.Application.AI;
using AtsCv.Domain.Resumes;

namespace AtsCv.Application.Resumes;

/// <summary>
/// Coordinates analysis for a resume entity. Its status changes to Analyzed
/// only after the analyzer has successfully returned a non-null result.
/// </summary>
public sealed class AtsResumeAnalysisService
{
    private readonly IAtsResumeAnalyzer _analyzer;

    public AtsResumeAnalysisService(IAtsResumeAnalyzer analyzer)
    {
        ArgumentNullException.ThrowIfNull(analyzer);
        _analyzer = analyzer;
    }

    public async Task<ResumeAnalysisResult> AnalyzeAsync(
        Resume resume,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resume);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _analyzer.AnalyzeAsync(
            resume.ExtractedText ?? string.Empty,
            cancellationToken);

        ArgumentNullException.ThrowIfNull(result);
        resume.MarkAnalyzed();
        return result;
    }
}
