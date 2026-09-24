namespace AtsCv.Application.Jobs;

public sealed record JobApplicationMatchRequest(
    Guid CandidateProfileId,
    Guid JobPostingId,
    IReadOnlyCollection<string>? AdditionalSkills = null);
