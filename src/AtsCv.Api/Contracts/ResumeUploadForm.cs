namespace AtsCv.Api.Contracts;

public sealed class ResumeUploadForm
{
    public IFormFile? File { get; set; }

    public string? AdditionalInformation { get; set; }
}
