namespace AtsCv.Application.Resumes;

public sealed record CvIntakeResult(
    Guid ResumeId,
    string FileName,
    string ExtractedText,
    string? AdditionalInformation);
