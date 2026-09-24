namespace AtsCv.Application.Jobs;

public sealed record JobApplicationMatchResult(
    Guid CandidateProfileId,
    Guid JobPostingId,
    IReadOnlyCollection<string> MatchingSkills,
    IReadOnlyCollection<string> RequestedAdditionalSkills);
