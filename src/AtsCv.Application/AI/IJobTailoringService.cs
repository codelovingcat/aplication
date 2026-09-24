namespace AtsCv.Application.AI;

public interface IJobTailoringService
{
    Task<JobTailoringResult> TailorAsync(
        string candidateProfile,
        string jobDescription,
        IReadOnlyCollection<string> extraSkills,
        CancellationToken cancellationToken = default);
}

public sealed record JobTailoringResult(
    IReadOnlyCollection<string> RelevantSkills,
    IReadOnlyCollection<string> SuggestedChanges,
    string TailoredSummary);
