using AtsCv.Application.Documents;

namespace AtsCv.Application.Resumes;

public sealed record ResumeUploadRequest(
    string FileName,
    string ContentType,
    DocumentType DocumentType,
    Stream Content,
    string? AdditionalInformation);
