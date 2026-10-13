using AtsCv.Application.Documents;
using AtsCv.Application.Persistence;
using AtsCv.Domain.Candidates;
using AtsCv.Domain.Resumes;

namespace AtsCv.Application.Resumes;

/// <summary>
/// Coordinates the first CV intake slice: validation, signature inspection,
/// text extraction, and creation of a candidate/resume draft.
/// </summary>
public sealed class CvIntakeService
{
    private readonly CvUploadValidator _uploadValidator;
    private readonly CvFileSignatureValidator _signatureValidator;
    private readonly IReadOnlyCollection<ICvTextExtractor> _textExtractors;
    private readonly ICandidateProfileRepository _candidateProfileRepository;
    private readonly IResumeRepository _resumeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CvIntakeService(
        CvUploadValidator uploadValidator,
        CvFileSignatureValidator signatureValidator,
        IEnumerable<ICvTextExtractor> textExtractors,
        ICandidateProfileRepository candidateProfileRepository,
        IResumeRepository resumeRepository,
        IUnitOfWork unitOfWork)
    {
        ArgumentNullException.ThrowIfNull(uploadValidator);
        ArgumentNullException.ThrowIfNull(signatureValidator);
        ArgumentNullException.ThrowIfNull(textExtractors);
        ArgumentNullException.ThrowIfNull(candidateProfileRepository);
        ArgumentNullException.ThrowIfNull(resumeRepository);
        ArgumentNullException.ThrowIfNull(unitOfWork);

        _uploadValidator = uploadValidator;
        _signatureValidator = signatureValidator;
        _textExtractors = textExtractors.ToArray();
        _candidateProfileRepository = candidateProfileRepository;
        _resumeRepository = resumeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CvIntakeResult> IntakeAsync(
        ResumeUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var validation = _uploadValidator.Validate(request);
        if (!validation.IsValid)
            throw new CvUploadValidationException(validation);

        if (request.Content.CanSeek)
            request.Content.Position = 0;

        if (!_signatureValidator.IsValid(request.Content, request.DocumentType, cancellationToken))
            throw new UnsupportedCvFileException();

        var extractor = _textExtractors.FirstOrDefault(candidate => candidate.CanHandle(request.DocumentType));
        if (extractor is null)
            throw new UnsupportedCvFileException();

        // A new upload starts as a draft: no candidate identity is inferred from
        // a file name or guessed from unstructured resume text.
        var candidateProfile = CandidateProfile.CreateDraft(request.AdditionalInformation);
        var resume = Resume.Create(candidateProfile.Id, request.FileName, request.ContentType);

        string extractedText;
        try
        {
            if (request.Content.CanSeek)
                request.Content.Position = 0;

            extractedText = await extractor.ExtractTextAsync(request.Content, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new CvTextExtractionException(exception);
        }

        if (string.IsNullOrWhiteSpace(extractedText))
            throw new CvTextExtractionException();

        resume.SetExtractedText(extractedText);

        // The two related drafts are committed through one unit of work so a
        // successful upload never returns identifiers for unsaved entities.
        _candidateProfileRepository.Add(candidateProfile);
        _resumeRepository.Add(resume);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CvIntakeResult(
            resume.Id,
            resume.OriginalFileName,
            extractedText,
            candidateProfile.AdditionalInformation)
        {
            CandidateProfileId = candidateProfile.Id
        };
    }
}

public sealed class CvUploadValidationException : Exception
{
    public CvUploadValidationException(CvUploadValidationResult validationResult)
        : base("The CV upload request is invalid.")
    {
        ArgumentNullException.ThrowIfNull(validationResult);
        ValidationResult = validationResult;
    }

    public CvUploadValidationResult ValidationResult { get; }
}

public sealed class UnsupportedCvFileException : Exception
{
    public UnsupportedCvFileException()
        : base("The uploaded file type is not supported or its content does not match its type.")
    {
    }
}

public sealed class CvTextExtractionException : Exception
{
    public CvTextExtractionException(Exception? innerException = null)
        : base("The uploaded CV could not be read.", innerException)
    {
    }
}
