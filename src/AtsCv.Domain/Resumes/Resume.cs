namespace AtsCv.Domain.Resumes;

public sealed class Resume
{
    private Resume(
        Guid id,
        Guid candidateProfileId,
        string originalFileName,
        string contentType)
    {
        Id = id;
        CandidateProfileId = candidateProfileId;
        OriginalFileName = originalFileName;
        ContentType = contentType;
        Status = ResumeStatus.Uploaded;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid CandidateProfileId { get; private set; }
    public string OriginalFileName { get; private set; }
    public string ContentType { get; private set; }
    public string? ExtractedText { get; private set; }
    public ResumeStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static Resume Create(
        Guid candidateProfileId,
        string originalFileName,
        string contentType)
    {
        if (candidateProfileId == Guid.Empty)
            throw new ArgumentException("Candidate profile is required.", nameof(candidateProfileId));

        if (string.IsNullOrWhiteSpace(originalFileName))
            throw new ArgumentException("Original file name is required.", nameof(originalFileName));

        return new Resume(
            Guid.NewGuid(),
            candidateProfileId,
            originalFileName.Trim(),
            contentType);
    }

    public void SetExtractedText(string text)
    {
        ExtractedText = text;
        Status = ResumeStatus.TextExtracted;
    }

    public void MarkAnalyzed() => Status = ResumeStatus.Analyzed;

    public void MarkOptimized() => Status = ResumeStatus.Optimized;

    public void MarkFailed() => Status = ResumeStatus.Failed;
}
