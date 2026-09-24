using AtsCv.Application.Resumes;

namespace AtsCv.Application.AI;

public interface IAtsResumeAnalyzer
{
    Task<ResumeAnalysisResult> AnalyzeAsync(
        string resumeText,
        CancellationToken cancellationToken = default);
}
