namespace AtsCv.Application.Resumes;

public sealed record ResumeUploadRequest(
    string FileName,
    string ContentType,
    Stream Content);
