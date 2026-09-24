namespace AtsCv.Application.Resumes;

public sealed record ResumeAnalysisResult(
    int Score,
    IReadOnlyCollection<string> Strengths,
    IReadOnlyCollection<string> Issues,
    IReadOnlyCollection<string> Recommendations);
