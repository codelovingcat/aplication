namespace AtsCv.Api.Contracts;

public sealed record ResumeAnalysisResponse(
    Guid ResumeId,
    string Status,
    int Score,
    IReadOnlyCollection<string> Strengths,
    IReadOnlyCollection<string> Issues,
    IReadOnlyCollection<string> Recommendations,
    IReadOnlyCollection<string> SectionFindings,
    IReadOnlyCollection<string> KeywordFindings);
