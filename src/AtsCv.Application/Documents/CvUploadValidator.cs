using AtsCv.Application.Resumes;

namespace AtsCv.Application.Documents;

public sealed class CvUploadValidator
{
    public const long DefaultMaxFileSizeBytes = 10 * 1024 * 1024;
    public const int DefaultMaxAdditionalInformationLength = 2000;

    private static readonly IReadOnlyDictionary<DocumentType, string[]> AllowedExtensions = new Dictionary<DocumentType, string[]>
    {
        [DocumentType.Pdf] = [".pdf"],
        [DocumentType.Docx] = [".docx"]
    };

    private static readonly IReadOnlyDictionary<DocumentType, string[]> AllowedContentTypes = new Dictionary<DocumentType, string[]>
    {
        [DocumentType.Pdf] = ["application/pdf"],
        [DocumentType.Docx] = ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"]
    };

    private readonly long _maxFileSizeBytes;
    private readonly int _maxAdditionalInformationLength;

    public CvUploadValidator(
        long maxFileSizeBytes = DefaultMaxFileSizeBytes,
        int maxAdditionalInformationLength = DefaultMaxAdditionalInformationLength)
    {
        if (maxFileSizeBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxFileSizeBytes));

        if (maxAdditionalInformationLength <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxAdditionalInformationLength));

        _maxFileSizeBytes = maxFileSizeBytes;
        _maxAdditionalInformationLength = maxAdditionalInformationLength;
    }

    public CvUploadValidationResult Validate(ResumeUploadRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.FileName))
        {
            errors.Add("A file name is required.");
        }
        else
        {
            var fileName = request.FileName.Trim();

            if (fileName.IndexOfAny(['/', '\\']) >= 0 || fileName.IndexOf('\0') >= 0 ||
                fileName != Path.GetFileName(fileName))
            {
                errors.Add("The file name must not contain path segments.");
            }

            if (fileName is "." or "..")
                errors.Add("The file name is not valid.");

            if (!AllowedExtensions.TryGetValue(request.DocumentType, out var extensions) ||
                !extensions.Contains(Path.GetExtension(fileName), StringComparer.OrdinalIgnoreCase))
            {
                errors.Add("The file extension does not match the selected document type.");
            }
        }

        if (!AllowedContentTypes.TryGetValue(request.DocumentType, out var contentTypes) ||
            !contentTypes.Contains(request.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            errors.Add("The content type does not match the selected document type.");
        }

        if (request.AdditionalInformation is { Length: > DefaultMaxAdditionalInformationLength })
        {
            errors.Add(
                $"Additional information must not exceed {_maxAdditionalInformationLength} characters.");
        }

        if (request.Content is null || !request.Content.CanRead)
        {
            errors.Add("The CV content must be a readable stream.");
        }
        else if (request.Content.CanSeek)
        {
            if (request.Content.Length <= 0)
                errors.Add("The CV file must not be empty.");
            else if (request.Content.Length > _maxFileSizeBytes)
                errors.Add($"The CV file must not exceed {_maxFileSizeBytes / (1024 * 1024)} MB.");
        }

        return new CvUploadValidationResult(errors);
    }
}

public sealed record CvUploadValidationResult(IReadOnlyList<string> Errors)
{
    public bool IsValid => Errors.Count == 0;
}
